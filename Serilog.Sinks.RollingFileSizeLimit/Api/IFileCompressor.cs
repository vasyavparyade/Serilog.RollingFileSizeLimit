namespace Serilog.Sinks.RollingFileSizeLimit.Api
{
    public interface IFileCompressor
    {
        void Compress(string filePath, string archivePath);
    }
}
