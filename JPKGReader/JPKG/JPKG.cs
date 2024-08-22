﻿using System.Text;

namespace JPKGReader;
public abstract class JPKG : IDisposable
{
    private readonly BinaryReader _reader;
    private readonly Dictionary<string, string> _extensions = new()
    {
        ["OggS"] = "ogg",
        ["jTOC"] = "jtoc",
        ["jARC"] = "jarc",
        ["jLUA"] = "jlua",
        ["jlev"] = "jlev",
        ["jpfb"] = "jpfb",
        ["jMSG"] = "jmsg",
        ["coli"] = "coli",
        ["soli"] = "soli",
        ["jtex"] = "jtex",
        ["jmo2"] = "jmo2",
        ["OTTO"] = "otto",
        ["jSHD"] = "jshd",
        ["jprj"] = "jprj",
        ["BKHD"] = "bnk",
        ["jIDT"] = "jidt",
        ["jTXS"] = "jtxs",
        ["jSDF"] = "jsdf",
        ["jfxc"] = "jfxc",
        ["jvfx"] = "jvfx",
        ["mesh"] = "mesh",
        ["skel"] = "skel",
        ["jSWD"] = "jswd",
        ["jSCR"] = "jscr"
    };

    public BinaryReader Reader => _reader;
    public Dictionary<string, string> Extensions => _extensions;

    public JPKG(Stream stream) => _reader = new BinaryReader(stream, Encoding.UTF8, true);

    public abstract void Parse();

    public void Dispose()
    {
        _reader.Close();
        GC.SuppressFinalize(this);
    }
}

