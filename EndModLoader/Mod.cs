using System;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;

namespace EndModLoader
{
    public class Mod : IComparable<Mod>
    {
        public string Title { get; private set; }
        public string Author { get; private set; }
        public string Version { get; private set; }
        public string ModPath { get; private set; }

        public static readonly string MetaFile = "meta.xml";
        public static readonly string MetaFileNotFound = $"no {MetaFile} found";

        public override string ToString() => $"{Title} {Author} {Version}";

        public int CompareTo(Mod other) => Title.CompareTo(other.Title);

        public static Mod FromZip(string path)
        {
            var mod = new Mod { ModPath = path };

            using (var zip = ZipFile.Open(path, ZipArchiveMode.Read))
            {
                var meta = zip.GetEntry(MetaFile);

                if (meta == null)
                {
                    mod.Title = Path.GetFileNameWithoutExtension(path);
                    return mod;
                }

                try
                {
                    var stream = meta.Open();
                    var doc = XDocument.Load(stream);
                    foreach (var element in doc.Root.Elements())
                    {
                        if (element.Name == "title") mod.Title = element.Value;
                        else if (element.Name == "author") mod.Author = element.Value;
                        else if (element.Name == "version") mod.Version = element.Value;
                    }
                }
                catch (FileNotFoundException) { }
            }

            return mod;
        }
    }
}
