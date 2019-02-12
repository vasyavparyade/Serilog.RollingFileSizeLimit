using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.RollingFileSizeLimit.Api;

namespace Serilog.Sinks.RollingFileSizeLimit.Sinks
{
    internal class SizeLimitedRollingFileSink : ILogEventSink, IDisposable
    {
        private const string DefaultLogPrefix = "log";

        internal SizeLimitedLogFileDescription CurrentLogFile => _currentSink.LogFileDescription;

        private readonly ManualResetEvent _archiveEvent = new ManualResetEvent(true);

        private static readonly string _thisObjectName = typeof (SizeLimitedFileSink).Name;
        private readonly ITextFormatter _formatter;
        private readonly string _logDirectory;
        private readonly string _archiveLogDirectory;
        private readonly long _archiveSizeLimitBytes;
        private readonly long _fileSizeLimitBytes;
        private readonly Encoding _encoding;
        private SizeLimitedFileSink _currentSink;
        private readonly object _syncRoot = new object();
        private bool _disposed;
        private readonly string _logFilePrefix;
        private readonly IFileCompressor _fileCompressor;

        public SizeLimitedRollingFileSink(
            string logDirectory
            , string archiveLogDirectory
            , ITextFormatter formatter
            , long fileSizeLimitBytes
            , long archiveSizeLimitBytes
            , IFileCompressor fileCompressor = null
            , Encoding encoding = null
            , string logFilePrefix = ""
        )
        {
            _logDirectory = string.IsNullOrEmpty(logDirectory) ?
                throw new ArgumentNullException(nameof(logDirectory)) :
                logDirectory;

            _archiveLogDirectory = string.IsNullOrEmpty(archiveLogDirectory) ?
                throw new ArgumentNullException(nameof(archiveLogDirectory)) :
                archiveLogDirectory;

            _formatter             = formatter;
            _fileSizeLimitBytes    = fileSizeLimitBytes;
            _archiveSizeLimitBytes = archiveSizeLimitBytes;
            _fileCompressor        = fileCompressor;
            _encoding              = encoding;
            _logFilePrefix         = string.IsNullOrEmpty(logFilePrefix) ? DefaultLogPrefix : logFilePrefix;
            _currentSink           = GetOrCreate();
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
                throw new ArgumentNullException(nameof(logEvent));

            lock (_syncRoot)
            {
                if (_disposed)
                    throw new ObjectDisposedException(_thisObjectName, "The rolling file sink has been disposed");

                bool newDay = _currentSink.LogFileDescription.LogFileInfo.Date.Date != DateTime.Now.Date;

                if (_currentSink.SizeLimitReached || newDay)
                    _currentSink = NextSizeLimitedFileSink();

                _currentSink?.Emit(logEvent);
            }
        }

        private SizeLimitedFileSink GetOrCreate()
        {
            EnsureDirectoryCreated(_logDirectory);
            EnsureDirectoryCreated(_archiveLogDirectory);

            SizeLimitedLogFileInfo logFileInfo = SizeLimitedLogFileInfo.GetLatestOrNew(DateTime.Now, _logDirectory, _archiveLogDirectory, _logFilePrefix);

            return new SizeLimitedFileSink(
                _formatter,
                _logDirectory,
                new SizeLimitedLogFileDescription(logFileInfo, _fileSizeLimitBytes, _logFilePrefix),
                _encoding);
        }

        private SizeLimitedFileSink NextSizeLimitedFileSink()
        {
            string filePath = Path.Combine(_logDirectory, _currentSink.LogFileDescription.FileName);
            string newFilePath = Path.Combine(_archiveLogDirectory, _currentSink.LogFileDescription.LogFileInfo.FileArchiveName);
            string newFileName = $"{newFilePath}.log";
            string archivePath = $"{newFilePath}.zip";

            SizeLimitedLogFileDescription next = _currentSink.LogFileDescription.Next();

            _currentSink.Dispose();

            FileMove(filePath, newFileName);

            Task.Run(() => CompressOldLogFile(newFileName, archivePath));

            return new SizeLimitedFileSink(_formatter, _logDirectory, next, _encoding);
        }

        private static void FileMove(string filePath, string newFileName)
        {
            try
            {
                File.Move(filePath, newFileName);
            }
            catch (Exception exception)
            {
                SelfLog.WriteLine("Error {0}", exception);
            }
        }

        private void CompressOldLogFile(string newFileName, string archivePath)
        {
            try
            {
                _archiveEvent.Reset();

                ApplyRetentionPolicy();

                _fileCompressor?.Compress(newFileName, archivePath);

                File.Delete(newFileName);

                _archiveEvent.Set();
            }
            catch (Exception exception)
            {
                SelfLog.WriteLine("Error {0}", exception);
            }
        }

        private static void EnsureDirectoryCreated(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private void ApplyRetentionPolicy()
        {
            var newestFirst = Directory.GetFiles(_archiveLogDirectory)
                .Select(m => new FileInfo(m))
                .OrderByDescending(m => m.CreationTime)
                .ToArray();

            long directorySize = newestFirst.Sum(x => x.Length);

            if (directorySize < _archiveSizeLimitBytes)
                return;

            int skip = newestFirst.Count() / 2;

            List<string> toRemove = newestFirst
                .Select(x => x.FullName)
                .Where(n => StringComparer.OrdinalIgnoreCase.Compare(
                    Path.Combine(_archiveLogDirectory, Path.GetFileName(_currentSink.LogFileDescription.FileName)), n) != 0)
                .Skip(skip)
                .ToList();

            foreach (string obsolete in toRemove)
            {
                string fullPath = Path.Combine(_archiveLogDirectory, obsolete);
                try
                {
                    File.Delete(fullPath);
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("Error {0} while removing obsolete file {1}", ex, fullPath);
                }
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed || _currentSink == null)
                    return;

                _currentSink.Dispose();

                _archiveEvent.WaitOne();

                _currentSink = null;
                _disposed = true;
            }
        }
    }
}
