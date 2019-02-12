using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Serilog.Sinks.RollingFileSizeLimit.Sinks
{
    internal class SizeLimitedLogFileInfo
    {
        private const string DateFormat = "yyyyMMdd";

        internal uint Sequence { get; }
        internal string FileName { get; }
        internal string FileArchiveName { get; set; }
        internal DateTime Date { get; }

        public SizeLimitedLogFileInfo(DateTime date, uint sequence, string logFilePrefix)
        {
            Date            = date;
            Sequence        = sequence;
            FileName        = $"{logFilePrefix}.log";
            FileArchiveName = $"{logFilePrefix}-{date.ToString(DateFormat)}-{Sequence:D5}";
        }

        public SizeLimitedLogFileInfo Next(string logFilePrefix)
        {
            var now = DateTime.Now;

            return Date.Date != now.Date
                ? new SizeLimitedLogFileInfo(now, 1, logFilePrefix)
                : new SizeLimitedLogFileInfo(now, Sequence + 1, logFilePrefix);
        }

        internal static SizeLimitedLogFileInfo GetLatestOrNew(
            DateTime date, string logDirectory, string archiveDirectory, string logFilePrefix)
        {
            var logPattern     = logFilePrefix + ".log";
            var archivePattern = logFilePrefix + @"-(\d{8})-(\d{5}).zip";

            var oldDate  = DateTime.MinValue;
            var sequence = uint.MinValue;

            foreach (string filePath in Directory.GetFiles(logDirectory))
            {
                var match = Regex.Match(filePath, logPattern);
                if (match.Success)
                {
                    var fi = new FileInfo(filePath);
                    var dateTime = fi.LastWriteTime.Date;

                    if (dateTime > oldDate)
                        oldDate = dateTime;
                }
            }

            foreach (string filePath in Directory.GetFiles(archiveDirectory))
            {
                var match = Regex.Match(filePath, archivePattern);
                if (match.Success)
                {
                    var seq = uint.Parse(match.Groups[2].Value);

                    if (seq > sequence)
                        sequence = seq;
                }
            }

            return oldDate == DateTime.MinValue
                ? new SizeLimitedLogFileInfo(date, 1, logFilePrefix)
                : new SizeLimitedLogFileInfo(oldDate, sequence + 1, logFilePrefix);
        }
    }
}