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

        public override string ToString() => $"{Title} {Author} {Version}";

        public static Mod ReadMetadata(string path)
        {
            try
            {
                var mod = new Mod();
                var doc = XDocument.Load(path);

                foreach (var element in doc.Root.Elements())
                {
                    if (element.Name == "title") mod.Title = element.Value;
                    else if (element.Name == "author") mod.Author = element.Value;
                    else if (element.Name == "version") mod.Version = element.Value;
                }

                return mod;
            }
            catch (FileNotFoundException e)
            {
                return new Mod { };
            }
        }
    }
}
