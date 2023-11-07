using System;
using Microsoft.Extensions.Logging;
using Rebus.Logging;

namespace Rebus.ServiceProvider.Internals;

class MicrosoftExtensionsLoggingLoggerFactory : AbstractRebusLoggerFactory
{
    readonly ILoggerFactory _loggerFactory;
    readonly ILogger _logger;

    public MicrosoftExtensionsLoggingLoggerFactory(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public MicrosoftExtensionsLoggingLoggerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    protected override ILog GetLogger(Type type)
    {
        return _loggerFactory != null
            ? new MicrosoftExtensionsLoggingLogger(_loggerFactory.CreateLogger(type), RenderString)
            : new MicrosoftExtensionsLoggingLogger(_logger, RenderString);
    }

    class MicrosoftExtensionsLoggingLogger : ILog
    {
        readonly Func<string, object[], string> _renderString;
        readonly ILogger _logger;

        public MicrosoftExtensionsLoggingLogger(ILogger logger, Func<string, object[], string> renderString)
        {
            _logger = logger;
            _renderString = renderString;
        }

        public void Debug(string message, params object[] objs)
        {
            _logger.LogDebug(_renderString(message, objs));
        }

        public void Info(string message, params object[] objs)
        {
            _logger.LogInformation(_renderString(message, objs));
        }

        public void Warn(string message, params object[] objs)
        {
            _logger.LogWarning(_renderString(message, objs));
        }

        public void Warn(Exception exception, string message, params object[] objs)
        {
            _logger.LogWarning(exception, _renderString(message, objs));
        }

        public void Error(string message, params object[] objs)
        {
            _logger.LogError(_renderString(message, objs));
        }

        public void Error(Exception exception, string message, params object[] objs)
        {
            _logger.LogError(exception, _renderString(message, objs));
        }
    }
}