namespace EventStore.Projections.Facts.Docker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ClientAPI;
    using Newtonsoft.Json;

    static class EventStoreRunningInDockerExtensions
    {
        static readonly JsonSerializerSettings SerializationSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None
        };

        internal static async Task GenerateRandomEvents(this EventStoreRunningInDocker eventStoreRunningInDocker,
            string streamId,
            int numberOfEvents)
        {
            var events = Enumerable.Range(0, numberOfEvents).Select(s => (object) new RandomEvent()).ToList();
            await AppendEventsToStream(eventStoreRunningInDocker.Connection, streamId, events);
        }

        static Task AppendEventsToStream(IEventStoreConnection connection, string streamId, List<object> events)
        {
            var eventsToSave = events.Select(e => ToEventData(Guid.NewGuid(), e)).ToList();
            return connection.AppendToStreamAsync(streamId, ExpectedVersion.NoStream, eventsToSave);
        }

        static EventData ToEventData(Guid eventId, object @event)
        {
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, SerializationSettings));

            var eventHeaders = new Dictionary<string, object>
            {
                {"EventClrTypeName", @event.GetType().AssemblyQualifiedName}
            };
            var metadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventHeaders, SerializationSettings));
            var typeName = @event.GetType().Name;

            return new EventData(eventId, typeName, true, data, metadata);
        }
    }

    class RandomEvent
    {
    }
}