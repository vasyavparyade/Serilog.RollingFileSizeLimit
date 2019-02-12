using System;
using System.IO;
using System.Linq;

using AutoFixture;

using Moq;

using NUnit.Framework;

using Serilog.Formatting.Compact;
using Serilog.Sinks.RollingFileSizeLimit.Api;
using Serilog.Sinks.RollingFileSizeLimit.Sinks;

namespace Serilog.Sinks.RollingFileSizeLimit.UnitTests.Sinks
{
    [TestFixture]
    public class SizeLimitedRollingFileSinkTests
    {
        private MockRepository _repository;
        private Mock<IFileCompressor> _fileCompressor;

        private Fixture _fixture;

        [SetUp]
        public void SetUp()
        {
            _repository     = new MockRepository(MockBehavior.Strict);
            _fileCompressor = _repository.Create<IFileCompressor>();
            _fixture        = new Fixture();
        }

        [Test(Author = "vilejaninov")]
        public void SequenceIsOneWhenNoPreviousFile()
        {
            using (var dir = new TestDirectory())
            {
                var latest = SizeLimitedLogFileInfo.GetLatestOrNew(
                    new DateTime(2015, 01, 15), dir.LogDirectory, dir.ArchiveDirectory, string.Empty);
                Assert.AreEqual(latest.Sequence, 1);
            }
        }

        [Test(Author = "vilejaninov")]
        public void ItCreatesNewFileWhenSizeLimitReached()
        {
            const long fileLimitSize = 50L * 1024 * 1024;
            const int messageSize = 2014;

            _fileCompressor.Setup(x => x.Compress(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

            using (var dir = new TestDirectory())
            using (var sizeRollingSink = new SizeLimitedRollingFileSink(
                dir.LogDirectory
                , dir.ArchiveDirectory
                , new CompactJsonFormatter()
                , fileLimitSize
                , archiveSizeLimitBytes: 10L * 1024 * 1024 * 1024
                , fileCompressor: _fileCompressor.Object
            ))
            {
                long offset = 0;

                var message = _fixture.CreateMany<char>(messageSize).ToArray();

                while (offset <= fileLimitSize)
                {
                    var logEvent = Some.InformationEvent(null, new string(message));
                    sizeRollingSink.Emit(logEvent);

                    offset += messageSize;
                }

                var files = Directory.GetFiles(dir.LogDirectory, "*.*", SearchOption.TopDirectoryOnly);

                Assert.AreEqual(1, files.Length);
                _fileCompressor.Verify(x => x.Compress(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            }
        }

        private class TestDirectory : IDisposable
        {
            private readonly object _lock = new object();

            private static readonly string _systemTemp =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Serilog-SizeRollingFileTests");

            private bool _disposed;

            public TestDirectory()
            {
                var subfolderPath = Path.Combine(_systemTemp, Guid.NewGuid().ToString("N"));
                var di = Directory.Exists(subfolderPath)
                    ? new DirectoryInfo(subfolderPath)
                    : Directory.CreateDirectory(subfolderPath);
                LogDirectory = di.FullName;
                ArchiveDirectory = di.FullName + @"\Archive";

                if (!Directory.Exists(ArchiveDirectory))
                    Directory.CreateDirectory(ArchiveDirectory);
            }

            public string LogDirectory { get; }
            public string ArchiveDirectory { get; set; }

            public void Dispose()
            {
                lock (_lock)
                {
                    if (_disposed)
                        return;

                    try
                    {
                        Directory.GetFiles(LogDirectory).ToList().ForEach(File.Delete);
                        Directory.Delete(LogDirectory, true);
                    }
                    finally
                    {
                        _disposed = true;
                    }
                }
            }
        }
    }
}
