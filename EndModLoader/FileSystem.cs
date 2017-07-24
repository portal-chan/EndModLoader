using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EndModLoader
{
    public static class FileSystem
    {
        public static IEnumerable<Mod> ReadModFolder(string path)
        {
            // TODO: Make this use .zip files      or whatever.
            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                var file = Path.Combine(dir, "meta.xml");
                yield return Mod.ReadMetadata(file);
            }
        }

        public static bool EnsureDir(string path, string folder)
        {
            var modPath = Path.Combine(path, folder);
            try
            {
                if (!Directory.Exists(modPath) && Directory.Exists(path))
                {
                    Directory.CreateDirectory(modPath);
                }
                return true;
            }
            catch (UnauthorizedAccessException e)
            {
                return false;
            }
        }
    }
}
