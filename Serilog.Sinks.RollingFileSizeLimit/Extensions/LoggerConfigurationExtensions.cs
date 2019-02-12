using System;

using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Sinks.RollingFileSizeLimit.Api;
using Serilog.Sinks.RollingFileSizeLimit.Sinks;

namespace Serilog.Sinks.RollingFileSizeLimit.Extensions
{
    public static class LoggerConfigurationExtensions
    {
        private const string DefaultOutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}";

        private const long DefaultFileLimit = 1024 * 1024 * 100L;
        private const long DefaultArchiveLimit = 1024 * 1024 * 1024 * 1L;

        public static LoggerConfiguration RollingFileSizeLimited(
            this LoggerSinkConfiguration configuration,
            string logDirectory,
            string archiveDirectory,
            LogEventLevel minimumLevel = LevelAlias.Minimum,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            long? fileSizeLimitBytes = null,
            long? archiveSizeLimitBytes = null,
            IFileCompressor fileCompressor = null
        )
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var templateFormatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
            var sink = new SizeLimitedRollingFileSink(
                logDirectory,
                archiveDirectory,
                templateFormatter,
                fileSizeLimitBytes ?? DefaultFileLimit,
                archiveSizeLimitBytes ?? DefaultArchiveLimit,
                fileCompressor
            );

            return configuration.Sink(sink, minimumLevel);
        }

        public static LoggerConfiguration RollingFileSizeLimited(
            this LoggerSinkConfiguration configuration,
            string logDirectory,
            string archiveDirectory,
            string logFilePrefix,
            LogEventLevel minimumLevel = LevelAlias.Minimum,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            long? fileSizeLimitBytes = null,
            long? archiveSizeLimitBytes = null,
            IFileCompressor fileCompressor = null
        )
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var templateFormatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
            var sink = new SizeLimitedRollingFileSink(
                logDirectory,
                archiveDirectory,
                templateFormatter,
                fileSizeLimitBytes ?? DefaultFileLimit,
                archiveSizeLimitBytes ?? DefaultArchiveLimit,
                fileCompressor,
                logFilePrefix: logFilePrefix
            );

            return configuration.Sink(sink, minimumLevel);
        }

        public static LoggerConfiguration RollingFileSizeLimited(
            this LoggerSinkConfiguration configuration,
            ITextFormatter formatter,
            string logDirectory,
            string archiveDirectory,
            LogEventLevel minimumLevel = LevelAlias.Minimum,
            long? fileSizeLimitBytes = null,
            long? archiveSizeLimitBytes = null,
            IFileCompressor fileCompressor = null
        )
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var sink = new SizeLimitedRollingFileSink(
                logDirectory,
                archiveDirectory,
                formatter,
                fileSizeLimitBytes ?? DefaultFileLimit,
                archiveSizeLimitBytes ?? DefaultArchiveLimit,
                fileCompressor
            );

            return configuration.Sink(sink, minimumLevel);
        }
    }
}
