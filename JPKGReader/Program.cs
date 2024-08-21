using K4os.Compression.LZ4;
using System.Buffers;
using System.Text;

namespace JPKGReader;

public record Node(ulong ID, long Offset, long Size);
public record Entry(long Offset, int Size, int Type);

public class Program
{
    private const int Version = 1;
    private const int MaxBlockSize = 0x40000;

    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("JPKGReader <file>");
            return;
        }

        if (!File.Exists(args[0]))
        {
            Console.WriteLine("File does not exist !!");
            return;
        }

        Parse(args[0]);
    }

    public static void Parse(string path)
    {
        using var fs = File.OpenRead(path);
        using var reader = new BinaryReader(fs);
        var signature = Encoding.UTF8.GetString(reader.ReadBytes(4));
        if (signature != "jPKG")
            throw new Exception("Invalid signautre !!");

        var filesCount = reader.ReadInt32();
        var blocksCount = reader.ReadInt32();
        var filesSize = reader.ReadInt32();
        var blocksSize = reader.ReadInt32();
        var dataOffset = reader.ReadInt32();
        var version = reader.ReadInt64();
        var size = reader.ReadInt64();

        if (version != Version)
        {
            throw new Exception($"Expected version {Version}, got {version} instead, not supported !!");
        }

        var files = new List<Node>();
        var blocks = new List<Entry>();

        var pos = reader.BaseStream.Position;

        while (reader.BaseStream.Position - pos < filesSize)
        {
            files.Add(new(reader.ReadUInt64(), reader.ReadInt64(), reader.ReadInt64()));
        }

        if (files.Count != filesCount)
        {
            throw new IOException($"Expected {filesCount} nodes, got {files.Count} instead !!");
        }

        pos = reader.BaseStream.Position;

        while (reader.BaseStream.Position - pos < blocksSize)
        {
            blocks.Add(new(reader.ReadInt64(), reader.ReadInt32(), reader.ReadInt32()));
        }

        if (blocks.Count != blocksCount)
        {
            throw new IOException($"Expected {filesCount} nodes, got {files.Count} instead !!");
        }

        using MemoryStream blocksStream = new();
        var compressedBuffer = ArrayPool<byte>.Shared.Rent(MaxBlockSize);
        var decompressedBuffer = ArrayPool<byte>.Shared.Rent(MaxBlockSize);
        try
        {
            foreach (var block in blocks)
            {
                reader.BaseStream.Position = block.Offset;
                reader.Read(compressedBuffer, 0, block.Size);

                if (block.Size == MaxBlockSize)
                {
                    blocksStream.Write(compressedBuffer, 0, block.Size);
                }
                else
                {
                    var numWrite = LZ4Codec.Decode(compressedBuffer.AsSpan(0, block.Size), decompressedBuffer.AsSpan(0, MaxBlockSize));
                    if (numWrite == -1)
                    {
                        throw new IOException($"Lz4 decompression error, write {numWrite} bytes but expected {MaxBlockSize} bytes");
                    }

                    blocksStream.Write(decompressedBuffer, 0, numWrite);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(compressedBuffer);
            ArrayPool<byte>.Shared.Return(decompressedBuffer);
        }

        using BinaryReader blocksReader = new(blocksStream);
        blocksReader.BaseStream.Position = 0;

        var folderName = Path.GetFileNameWithoutExtension(path);
        Directory.CreateDirectory($"output/{folderName}");

        foreach (var file in files)
        {
            blocksReader.BaseStream.Position = file.Offset;
            byte[] data = blocksReader.ReadBytes((int)file.Size);

            var fileName = $"{file.ID:X8}." + Encoding.UTF8.GetString(data[..4]) switch
            {
                "OggS" => "ogg",
                "jTOC" => "jtoc",
                "jARC" => "jarc",
                "jLUA" => "jlua",
                "jlev" => "jlev",
                "jpfb" => "jpfb",
                "jMSG" => "jmsg",
                "coli" => "coli",
                "soli" => "soli",
                "jtex" => "jtex",
                "jmo2" => "jmo2",
                "OTTO" => "otto",
                _ => "dat"
            };

            Console.WriteLine($"Writing {fileName}");
            File.WriteAllBytes($"output/{folderName}/{fileName}", data);
        }
    }
}