namespace UdpToolkit.Logging
{
    using System;

    public class SimpleConsoleLogger : ILogger
    {
        private readonly LogLevel _logLevel;

        public SimpleConsoleLogger(LogLevel logLevel)
        {
            _logLevel = logLevel;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _logLevel;
        }

        public void Warning(string message)
        {
            Console.WriteLine(message);
        }

        public void Error(string message)
        {
            Console.WriteLine(message);
        }

        public void Information(string message)
        {
            Console.WriteLine(message);
        }

        public void Debug(string message)
        {
            Console.WriteLine(message);
        }
    }
}