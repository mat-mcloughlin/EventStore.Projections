namespace EventStore.Projections
{
    using System;
    using ClientAPI;

    public class LoggingAdaptor
    {
        readonly Action<LogLevel, string, Exception, object[]> _eventLogger;

        internal static LoggingAdaptor Empty => new LoggingAdaptor((level, message, exception, eventLogger) => { });

        public LoggingAdaptor(Action<LogLevel, string, Exception, object[]> eventLogger)
        {
            _eventLogger = eventLogger;
        }

        internal void LogMessage(LogLevel level, string message, params object[] formatParameters)
        {
            _eventLogger(level, message, null, formatParameters);
        }
        
        internal void Warning(string message, params object[] formatParameters)
        {
            _eventLogger(LogLevel.Warn, message, null, formatParameters);
        }
        
        internal void Information(string message, params object[] formatParameters)
        {
            _eventLogger(LogLevel.Info, message, null, formatParameters);
        }

        internal void Error(string message,
            Exception exception,
            params object[] formatParameters)
        {
            _eventLogger(LogLevel.Error, message, exception, formatParameters);
        }
    }
}