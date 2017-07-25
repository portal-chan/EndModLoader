using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EndModLoader
{
    public class Mod
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string ModPath { get; set; }

        public static readonly string MetaFile = "meta.xml";
        public static readonly string MetaFileNotFound = $"no {MetaFile} found";

        public override string ToString() => $"{Title} {Author} {Version}";

        public static Mod FromPath(string path)
        {
            var mod = new Mod { ModPath = path };

            var meta = Path.Combine(path, MetaFile);
            if (!File.Exists(meta))
            {
                mod.Title = new DirectoryInfo(path).Name;
                return mod;
            }

            try
            {
                var doc = XDocument.Load(meta);

                foreach (var element in doc.Root.Elements())
                {
                    if (element.Name == "title") mod.Title = element.Value;
                    else if (element.Name == "author") mod.Author = element.Value;
                    else if (element.Name == "version") mod.Version = element.Value;
                }
            }
            catch (FileNotFoundException e) { }

            return mod;
        }
    }
}
