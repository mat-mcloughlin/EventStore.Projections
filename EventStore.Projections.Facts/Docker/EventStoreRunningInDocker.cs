namespace EventStore.Projections.Facts.Docker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using ClientAPI;
    using ClientAPI.SystemData;
    using global::Docker.DotNet;
    using global::Docker.DotNet.Models;
    using Xunit;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class EventStoreRunningInDocker : IAsyncLifetime
    {
        const string EventStoreContainer = "PartPayTestEventStore";

        const string ImageName = "eventstore/eventstore";

        const string Tag = "latest";

        DockerClient _client;

        static string ImageWithTag => $"{ImageName}:{Tag}";

        internal IEventStoreConnection Connection { get; private set; }

        public async Task InitializeAsync()
        {
            var address = Environment.OSVersion.Platform == PlatformID.Unix
                ? new Uri("unix:///var/run/docker.sock")
                : new Uri("npipe://./pipe/docker_engine");
            var config = new DockerClientConfiguration(address);
            _client = config.CreateClient();

            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters {All = true});

            if (containers.Any(x => x.Names.Any(n => n.Contains(EventStoreContainer))))
            {
                await CleanUpContainerAsync();
            }

            var images = await _client.Images.ListImagesAsync(new ImagesListParameters {MatchName = ImageName});
            if (images.Count == 0)
            {
                await _client.Images.CreateImageAsync(new ImagesCreateParameters
                    {
                        FromImage = ImageName,
                        Tag = Tag
                    },
                    null,
                    IgnoreProgress.Forever);
            }

            await _client.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = ImageWithTag,
                Name = EventStoreContainer,
                Tty = true,
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {"2113/tcp", new List<PortBinding> {new PortBinding {HostPort = "2114"}}},
                        {"1113/tcp", new List<PortBinding> {new PortBinding {HostPort = "1114"}}}
                    }
                }
            });

            await _client.Containers.StartContainerAsync(EventStoreContainer, new ContainerStartParameters());

            using (var s = await _client.Containers.GetContainerLogsAsync(EventStoreContainer,
                new ContainerLogsParameters
                {
                    ShowStdout = true,
                    Follow = true
                }))
            {
                var sr = new StreamReader(s);
                var count = 0;
                while (true)
                {
                    var line = await sr.ReadLineAsync();
                    count++;
                    if (line.Contains("Created stats stream") && line.Contains("code = Success") || count > 200)
                    {
                        break;
                    }
                }
            }

            var endpoint = new Uri("tcp://127.0.0.1:1114");
            var settings = ConnectionSettings
                .Create()
                .SetDefaultUserCredentials(new UserCredentials("admin", "changeit"));
            var connectionName =
                $"M={Environment.MachineName},P={Process.GetCurrentProcess().Id},T={DateTimeOffset.UtcNow.Ticks}";
            Connection = EventStoreConnection.Create(settings, endpoint, connectionName);
            await Connection.ConnectAsync();
        }

        public async Task DisposeAsync()
        {
            Connection?.Dispose();
            await CleanUpContainerAsync();
            _client?.Dispose();
        }

        async Task CleanUpContainerAsync()
        {
            if (_client != null)
            {
                await _client.Containers.StopContainerAsync(EventStoreContainer, new ContainerStopParameters());
                await _client.Containers.RemoveContainerAsync(EventStoreContainer,
                    new ContainerRemoveParameters {Force = true});
            }
        }

        class IgnoreProgress : IProgress<JSONMessage>
        {
            public static readonly IProgress<JSONMessage> Forever = new IgnoreProgress();

            public void Report(JSONMessage value)
            {
            }
        }
    }
}