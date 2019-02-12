using System;
using System.IO;
using System.Text;

using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.RollingFileSizeLimit.Sinks
{
    internal class SizeLimitedFileSink : ILogEventSink, IDisposable
    {
        private static readonly string _thisObjectName = typeof(SizeLimitedFileSink).Name;

        internal bool SizeLimitReached { get; private set; }

        internal SizeLimitedLogFileDescription LogFileDescription { get; }

        internal string FilePath { get; private set; }

        private readonly ITextFormatter _formatter;
        private readonly StreamWriter _output;
        private readonly object _syncRoot = new object();
        private bool _disposed;
        private bool _exceptionAlreadyThrown;

        public SizeLimitedFileSink(
            ITextFormatter formatter
            , string logDirectory
            , SizeLimitedLogFileDescription sizeLimitedLogFileDescription
            , Encoding encoding = null
        )
        {
            _formatter         = formatter;
            LogFileDescription = sizeLimitedLogFileDescription;
            _output            = OpenFileForWriting(logDirectory, sizeLimitedLogFileDescription, encoding ?? Encoding.UTF8);
        }

        // For tests
        internal SizeLimitedFileSink(
            ITextFormatter formatter
            , SizeLimitedLogFileDescription sizeLimitedLogFileDescription
            , StreamWriter writer
        )
        {
            _formatter         = formatter;
            LogFileDescription = sizeLimitedLogFileDescription;
            _output            = writer;
        }

        private StreamWriter OpenFileForWriting(
            string folderPath
            , SizeLimitedLogFileDescription logFileDescription
            , Encoding encoding
        )
        {
            EnsureDirectoryCreated(folderPath);

            try
            {
                FilePath = Path.Combine(folderPath, logFileDescription.FileName);
                FileStream stream = File.Open(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

                return new StreamWriter(stream, encoding ?? Encoding.UTF8);
            }
            catch (IOException ex)
            {
                if (!ex.Message.StartsWith("The process cannot access the file", StringComparison.Ordinal))
                    throw;
            }
            catch (UnauthorizedAccessException)
            {
                if (_exceptionAlreadyThrown)
                    throw;

                _exceptionAlreadyThrown = true;
            }

            return OpenFileForWriting(folderPath, logFileDescription.Next(), encoding);
        }

        private static void EnsureDirectoryCreated(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
                throw new ArgumentNullException(nameof(logEvent));

            lock (_syncRoot)
            {
                if (_disposed)
                    throw new ObjectDisposedException(_thisObjectName, "Cannot write to disposed file");

                if (_output == null)
                    return;

                _formatter.Format(logEvent, _output);
                _output.Flush();

                if (_output.BaseStream.Length > LogFileDescription.SizeLimitBytes)
                    SizeLimitReached = true;
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed || _output == null)
                    return;

                _output.Flush();
                _output.Close();
                _output.Dispose();

                _disposed = true;
            }
        }
    }
}