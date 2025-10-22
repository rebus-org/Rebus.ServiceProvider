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

    class MicrosoftExtensionsLoggingLogger(ILogger logger, Func<string, object[], string> renderString)
        : ILog
    {
        public void Debug(string message, params object[] objs)
        {
            logger.LogDebug(renderString(message, objs));
        }

        public void Info(string message, params object[] objs)
        {
            logger.LogInformation(renderString(message, objs));
        }

        public void Warn(string message, params object[] objs)
        {
            logger.LogWarning(renderString(message, objs));
        }

        public void Warn(Exception exception, string message, params object[] objs)
        {
            logger.LogWarning(exception, renderString(message, objs));
        }

        public void Error(string message, params object[] objs)
        {
            logger.LogError(renderString(message, objs));
        }

        public void Error(Exception exception, string message, params object[] objs)
        {
            logger.LogError(exception, renderString(message, objs));
        }
    }
}