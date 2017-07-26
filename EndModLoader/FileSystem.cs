using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace EndModLoader
{
    public static class FileSystem
    {
        private static readonly string[] ModFolders = { "audio", "data", "shaders", "swfs", "textures", "tilemaps" };
        private static FileSystemWatcher Watcher;

        public static IEnumerable<Mod> ReadModFolder(string path)
        {
            foreach (var file in Directory.GetFiles(path, "*.zip", SearchOption.TopDirectoryOnly))
            {
                yield return Mod.FromZip(file);
            }

            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                yield return Mod.FromPath(dir);
            }
        }

        public static void EnableWatching(
            string path, 
            FileSystemEventHandler addHandler, 
            FileSystemEventHandler removeHandler,
            RenamedEventHandler renameHandler
        ) {
            Watcher = new FileSystemWatcher
            {
                Path = path,
                NotifyFilter = NotifyFilters.LastAccess |
                               NotifyFilters.LastWrite |
                               NotifyFilters.CreationTime |
                               NotifyFilters.FileName
            };

            Watcher.Changed += addHandler;
            Watcher.Deleted += removeHandler;
            Watcher.Renamed += renameHandler;
            Watcher.EnableRaisingEvents = true;
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

        public static void LoadMod(Mod mod, string path)
        {
            if (mod.IsZip)
            {
                ZipFile.ExtractToDirectory(mod.ModPath, path);
            }
            else
            {
                CopyDirectory(mod.ModPath, path, ModFolders);
            }
        }

        public static void UnloadAll(string path)
        {
            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                if (ModFolders.Contains(new DirectoryInfo(dir).Name))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
        }

        // Since having that in .NET is too much to ask for...
        private static void CopyDirectory(string from, string to, params string[] filter)
        {
            var dir = new DirectoryInfo(from);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(from);
            }

            if (!Directory.Exists(to))
            {
                Directory.CreateDirectory(to);
            }

            // Ignore the files when there's a directory filter.
            // Mostly a hack to prevent meta.xml being copied around.
            if (filter.Length == 0)
            {
                foreach (var file in dir.GetFiles())
                {
                    file.CopyTo(Path.Combine(to, file.Name), overwrite: true);
                }
            }

            foreach (var sub in dir.GetDirectories())
            {
                CopyDirectory(sub.FullName, Path.Combine(to, sub.Name));
            }
        }
    }
}
