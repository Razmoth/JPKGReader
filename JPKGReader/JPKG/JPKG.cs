using System.Text;

namespace JPKGReader;
public abstract class JPKG : IDisposable
{
    protected const int MaxBlockSize = 0x40000;

    private readonly BinaryReader _reader;

    public int FilesCount { get; set; }
    public int BlocksCount { get; set; }
    public int FilesSize { get; set; }
    public int BlocksSize { get; set; }
    public int DataOffset { get; set; }
    public long Version { get; set; }
    public long Size { get; set; }

    public BinaryReader Reader => _reader;

    public JPKG(Stream stream) => _reader = new BinaryReader(stream, Encoding.UTF8, true);

    public abstract void Parse();

    public void Dispose()
    {
        _reader.Close();
        GC.SuppressFinalize(this);
    }
}

