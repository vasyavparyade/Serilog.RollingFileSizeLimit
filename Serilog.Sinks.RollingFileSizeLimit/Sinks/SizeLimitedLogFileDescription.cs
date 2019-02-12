namespace Serilog.Sinks.RollingFileSizeLimit.Sinks
{
    internal class SizeLimitedLogFileDescription
    {
        public string FileName => LogFileInfo.FileName;

        public readonly SizeLimitedLogFileInfo LogFileInfo;
        public readonly long SizeLimitBytes;
        public readonly string LogFilePrefix;

        public SizeLimitedLogFileDescription(
            SizeLimitedLogFileInfo logFileInfo
            , long sizeLimitBytes
            , string logFilePrefix
        )
        {
            LogFileInfo    = logFileInfo;
            SizeLimitBytes = sizeLimitBytes;
            LogFilePrefix  = logFilePrefix;
        }

        internal SizeLimitedLogFileDescription Next() =>
            new SizeLimitedLogFileDescription(LogFileInfo.Next(LogFilePrefix), SizeLimitBytes, LogFilePrefix);
    }
}