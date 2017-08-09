using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Xml.Linq;

namespace EndModLoader
{
    public class Mod : IComparable<Mod>
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string Author { get; private set; }
        public string Version { get; private set; }
        public string ModPath { get; private set; }

        public static readonly string MetaFile = "meta.xml";
        public static readonly string MetaFileNotFound = $"no {MetaFile} found";

        public override string ToString() => $"{Title} {Author} {Version}";

        public int CompareTo(Mod other) => Title.CompareTo(other.Title);

        public static Mod FromZip(string path)
        {
            // Various wacky Changed, Renamed, Removed events eventually lead to this.
            // C# Optionals when.
            if (!File.Exists(path)) return null;

            var mod = new Mod { ModPath = path };
            try
            {
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
                            else if (element.Name == "description" || element.Name == "desc") mod.Description = element.Value;
                            else if (element.Name == "author") mod.Author = element.Value;
                            else if (element.Name == "version") mod.Version = element.Value;
                        }
                    }
                    catch (FileNotFoundException) { }
                }
            }
            // On the weird off chance that you are currently still copying the folder
            // to the mods directory, it will be "opened by another process" and crash
            // the program horribly, to the point of not even responding to Task Manager.
            catch (IOException)
            {
                for (int i = 0; i < 10; ++i)
                {
                    Thread.Sleep(1000);
                    try
                    {
                        var again = FromZip(path);
                        return again;
                    }
                    catch (IOException) { } 
                }
                // Fuck it, give up on this file.
                return null;
            }
            catch (UnauthorizedAccessException e)
            {
                throw e;
            }

            return mod;
        }
    }
}
