using K4os.Compression.LZ4;
using System.Buffers;
using System.Text;

namespace JPKGReader;
public class JPKGV4 : JPKG
{
    private const uint Seed = 0x9A44EDF5;

    public int FilesCount { get; set; }
    public int FilesSize { get; set; }
    public long Version { get; set; }
    public long Size { get; set; }
    public List<Node> Files { get; set; } = [];

    public JPKGV4(Stream stream) : base(stream) { }

    public override void Parse()
    {
        ReadHeader();
        ReadFiles();
        ProcessFiles();
    }

    private void ReadHeader()
    {
        byte[] buffer = new byte[0x30];
        Reader.Read(buffer);
        XORShift32.Decrypt(buffer, Seed);

        using MemoryStream ms = new(buffer);
        using BinaryReader reader = new(ms);
        var signature = Encoding.UTF8.GetString(reader.ReadBytes(4));
        if (signature != "jPKG")
            throw new Exception("Invalid signautre !!");

        Version = reader.ReadInt64();
        var num = reader.ReadInt64();
        FilesCount = reader.ReadInt32();
        FilesSize = reader.ReadInt32();
        var num2 = reader.ReadInt64();
        Size = reader.ReadInt64();

        if (Version != 4)
        {
            throw new Exception($"Expected version 4, got {Version} instead, not supported !!");
        }
    }

    private void ReadFiles()
    {
        byte[] buffer = new byte[FilesSize];
        Reader.Read(buffer);
        XORShift32.Decrypt(buffer, Seed);

        using MemoryStream ms = new(buffer);
        using BinaryReader reader = new(ms);
        while (reader.BaseStream.Position < FilesSize)
        {
            Files.Add(new(reader.ReadUInt64(), reader.ReadInt64(), reader.ReadInt64(), reader.ReadInt64()));
        }

        if (Files.Count != FilesCount)
        {
            throw new IOException($"Expected {FilesCount} nodes, got {Files.Count} instead !!");
        }
    }

    private void ProcessFiles()
    {
        Directory.CreateDirectory($"output");

        foreach (var file in Files)
        {
            var compressedBuffer = ArrayPool<byte>.Shared.Rent((int)file.CompressedSize);
            var decompressedBuffer = ArrayPool<byte>.Shared.Rent((int)file.DecompressedSize);
            try
            {
                Reader.BaseStream.Position = file.Offset;
                Reader.Read(compressedBuffer, 0, (int)file.CompressedSize);

                XORShift32.Decrypt(compressedBuffer.AsSpan(0, (int)file.CompressedSize), Seed);

                Span<byte> data;
                if (file.CompressedSize == file.DecompressedSize)
                {
                    data = compressedBuffer.AsSpan(0, (int)file.CompressedSize);
                }
                else
                {
                    var numWrite = LZ4Codec.Decode(compressedBuffer.AsSpan(0, (int)file.CompressedSize), decompressedBuffer.AsSpan(0, (int)file.DecompressedSize));
                    if (numWrite == -1)
                    {
                        throw new IOException($"Lz4 decompression error, write {numWrite} bytes but expected {(int)file.DecompressedSize} bytes");
                    }

                    data = decompressedBuffer.AsSpan(0, (int)file.DecompressedSize);
                }

                var fileName = $"{file.ID:X8}." + (Extensions.TryGetValue(Encoding.UTF8.GetString(data[..4]), out var extension) ? extension : "dat");

                Console.WriteLine($"Writing {fileName}");
                File.WriteAllBytes($"output/{fileName}", data.ToArray());
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(compressedBuffer);
                ArrayPool<byte>.Shared.Return(decompressedBuffer);
            }
        }
    }

    public record Node(ulong ID, long Offset, long DecompressedSize, long CompressedSize);
}
