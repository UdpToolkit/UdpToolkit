﻿namespace UdpToolkit.Logging.Serilog
{
    using global::Serilog;

    public sealed class SerilogLogger : IUdpToolkitLogger
    {
        private readonly ILogger _logger;

        public SerilogLogger(
            ILogger logger)
        {
            _logger = logger;
        }

        public void Warning(
            string message)
        {
            _logger.Warning(message);
        }

        public void Error(
            string message)
        {
            _logger.Error(message);
        }

        public void Information(
            string message)
        {
            _logger.Information(message);
        }

        public void Debug(
            string message)
        {
            _logger.Debug(message);
        }
    }
}