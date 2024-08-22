namespace JPKGReader;

public class Program
{
    private static List<Func<Stream, JPKG>> _versions;
    static Program()
    {
        _versions = [
            stream => new JPKGV1(stream),
            stream => new JPKGV3(stream),
            stream => new JPKGV4(stream)
        ];
    }
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

        JPKG pkg;
        using var fs = File.OpenRead(args[0]);

        foreach (var version in _versions)
        {
            try
            {
                pkg = version(fs);
                pkg.Parse();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while parsing {nameof(pkg)}, {ex}");
            }

            fs.Position = 0;
        }
    }
}