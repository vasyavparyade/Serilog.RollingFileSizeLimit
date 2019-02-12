using System;
using System.IO;
using System.Text;

using NUnit.Framework;

using Serilog.Formatting.Compact;
using Serilog.Sinks.RollingFileSizeLimit.Sinks;

namespace Serilog.Sinks.RollingFileSizeLimit.UnitTests.Sinks
{
    [TestFixture]
    public class SizeLimitedFileSinkTests
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test(Author = "vilejaninov")]
        public void ReachedWhenAmountOfCharactersWritten()
        {
            var formatter = new CompactJsonFormatter();
            var components = new SizeLimitedLogFileInfo(new DateTime(2015, 01, 15), 0, string.Empty);
            var logFile = new SizeLimitedLogFileDescription(components, 1, string.Empty);
            using (var str = new MemoryStream())
            using (var wr = new StreamWriter(str, Encoding.UTF8))
            using (var sink = new SizeLimitedFileSink(formatter, logFile, wr))
            {
                var ev = Some.InformationEvent();
                sink.Emit(ev);
                Assert.True(sink.SizeLimitReached);
            }
        }
    }
}
