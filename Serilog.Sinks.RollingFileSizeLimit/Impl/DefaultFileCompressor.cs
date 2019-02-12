using System;
using System.IO;
using System.IO.Compression;

using Serilog.Sinks.RollingFileSizeLimit.Api;

namespace Serilog.Sinks.RollingFileSizeLimit.Impl
{
    public class DefaultFileCompressor : IFileCompressor
    {
        public void Compress(string filePath, string archivePath) => FileCompress(filePath, archivePath);

        private void FileCompress(string filePath, string archivePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException(nameof(filePath));

            if (string.IsNullOrWhiteSpace(archivePath))
                throw new ArgumentException(nameof(archivePath));

            using (var zipToOpen = new FileStream(archivePath, FileMode.Create))
            {
                using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(filePath, Path.GetFileName(filePath));
                }
            }
        }
    }
}