using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EndModLoader
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // TODO: switch to HKEY_CURRENT_USER/Software/Valve/Steam/SteamPath
        private static readonly string DefaultEndIsNighPath = Environment.Is64BitOperatingSystem ? 
            "C:/Program Files (x86)/Steam/steamapps/common/theendisnigh/" :
            "C:/Program Files/Steam/steamapps/common/theendisnigh/";

        public string EndIsNighPath { get; set; } = DefaultEndIsNighPath;
        private const string ExeName = "TheEndIsNigh.exe";
        private string ModPath { get => Path.Combine(EndIsNighPath, "mods"); }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string property) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        public ObservableCollection<Mod> Mods { get; private set; }

        private AppState _appState;
        public AppState AppState
        {
            get => _appState;
            private set
            {
                _appState = value;
                NotifyPropertyChanged(nameof(AppState));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            AppState = AppState.NoModSelected;

            try
            {
                FileSystem.EnsureDir(EndIsNighPath, ModPath);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Could not create/open mods directory. Try running the program as Administrator.");
                Environment.Exit(1);
            }

            Mods = new ObservableCollection<Mod>(FileSystem.ReadModFolder(ModPath).OrderBy(m => m));
            if (Mods.Count == 0)
            {
                AppState = AppState.NoModsFound;
            }

            FileSystem.EnableWatching(ModPath, OnAdd, OnRemove, OnRename);
        }

        private void OnAdd(object sender, FileSystemEventArgs e)
        {
            var added = Mod.FromZip(e.FullPath);
            Dispatcher.Invoke(() =>
            {
                // Due to ObservableCollection not firing a notify event when it's sorted,
                // it's simpler to insert the new mod at it's sorted index.
                int i = 0;
                while (i < Mods.Count && Mods[i].CompareTo(added) < 0) ++i;
                Mods.Insert(i, added);
            });
        }

        private void OnRemove(object sender, FileSystemEventArgs e)
        {
            var find = Mods.Where(m => m.ModPath == e.FullPath).FirstOrDefault();
            if (find != null)
            {
                Dispatcher.Invoke(() => Mods.Remove(find));
            }
        }

        private void OnRename(object sender, RenamedEventArgs e)
        {
            var find = Mods.Where(m => m.ModPath == e.OldFullPath).FirstOrDefault();
            var renamed = Mod.FromZip(e.FullPath);

            if (find != null)
            {
                Dispatcher.Invoke(() =>
                {
                    Mods.Remove(find);

                    // Same.
                    int i = 0;
                    while (i < Mods.Count && Mods[i].CompareTo(renamed) < 0) ++i;
                    Mods.Insert(i, renamed);
                });
            }
        }

        private async Task PlayMod(Mod mod)
        {
            AppState = AppState.InGame;
            FileSystem.UnloadAll(EndIsNighPath);

            FileSystem.LoadMod(mod, EndIsNighPath);
            Process.Start(Path.Combine(EndIsNighPath, ExeName));

            await HookGameExit("TheEndIsNigh", (s, ev) =>
            {
                AppState = AppState.ReadyToPlay;
                FileSystem.UnloadAll(EndIsNighPath);
            });
        }

        private async Task HookGameExit(string process, EventHandler hook)
        {
            // Since Steam's "launching..." exits and starts the games process,
            // we can't simply hook the Process.Start return value and instead
            // have to wait 5 seconds hoping the real process launches and hook
            // that instead.
            await Task.Delay(5000);
            var end = Process.GetProcessesByName(process).First();
            end.EnableRaisingEvents = true;
            end.Exited += hook;
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e) =>
            await PlayMod(ModList.SelectedItem as Mod);

        private async void ModList_MouseDoubleClick(object sender, MouseButtonEventArgs e) =>
            await PlayMod(ModList.SelectedItem as Mod);

        private void ModList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
            AppState = AppState.ReadyToPlay;

        private void ModList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                foreach (var file in files.Select(f => new FileInfo(f)).Where(f => f.Extension == ".zip"))
                {
                    File.Move(file.FullName, Path.Combine(ModPath, file.Name));
                }
            }
        }
    }
}
