namespace JPKGReader;

public class Program
{
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

        try
        {
            pkg = new JPKGV1(fs);
            pkg.Parse();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while parsing jPKG v1, {ex}");
        }

        fs.Position = 0;

        try
        {
            pkg = new JPKG3(fs);
            pkg.Parse();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while parsing jPKG v3, {ex}");
        }
    }
}