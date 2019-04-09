namespace EventStore.Projections.Facts.Helpers
{
    using Xunit.Abstractions;

    class XUnitLogging
    {
        readonly ITestOutputHelper _output;

        public XUnitLogging(ITestOutputHelper output)
        {
            _output = output;
        }

        public LoggingAdaptor Adaptor => new LoggingAdaptor((level, message, exception, formatParameters) =>
        {
            var formattedMessage = string.Format(message, formatParameters);
            _output.WriteLine($"{level.ToString().ToUpper()}: {formattedMessage}");

            if (exception != null)
            {
                _output.WriteLine($"{level.ToString().ToUpper()}: {exception.Message}");
            }
        });
    }
}