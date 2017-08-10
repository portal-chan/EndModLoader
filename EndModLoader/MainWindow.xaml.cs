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
using Microsoft.WindowsAPICodePack.Dialogs;
using EndModLoader.Properties;

namespace EndModLoader
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public static string ExeName { get => "TheEndIsNigh.exe"; }
        public static string WindowTitle { get => "The End is Nigh MOD Loader v1.1"; }

        private string ModPath { get => Path.Combine(EndIsNighPath, "mods"); }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string property) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        public ObservableCollection<Mod> Mods { get; private set; } = new ObservableCollection<Mod>();

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

        private string _endIsNighPath;
        public string EndIsNighPath
        {
            // Amazing way to display the path properly.
            get => _endIsNighPath?.Replace('\\', '/') ?? "";
            private set
            {
                _endIsNighPath = value;
                Settings.Default["EndIsNighPath"] = value;
                NotifyPropertyChanged(nameof(EndIsNighPath));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            AppState = AppState.NoModSelected;

            EndIsNighPath = Settings.Default[nameof(EndIsNighPath)] as string;
            if (string.IsNullOrWhiteSpace(EndIsNighPath))
            {
                EndIsNighPath = FileSystem.DefaultGameDirectory();
            }
            ReadyEndIsNighPath();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Settings.Default.Save();

            if (AppState == AppState.InGame)
            {
                var result = MessageBox.Show(
                    "It's recommended to quit the game before closing the MOD loader. Are you sure you want to quit anyways?",
                    "Warning",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No
                );

                e.Cancel = result != MessageBoxResult.Yes;
            }
        }

        private void ReadyEndIsNighPath()
        {
            try
            {
                if (FileSystem.IsGamePathCorrect(EndIsNighPath))
                {
                    FileSystem.SetupDir(EndIsNighPath);
                    FileSystem.MakeSaveBackup(EndIsNighPath);
                    LoadModList(FileSystem.ReadModFolder(ModPath).OrderBy(m => m));
                    FileSystem.EnableWatching(ModPath, OnAdd, OnRemove, OnRename);
                }
                else
                {
                    AppState = AppState.IncorrectPath;
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Could not create/open mods directory. Try running the program as Administrator.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Environment.Exit(1);
            }
        }

        private void OnAdd(object sender, FileSystemEventArgs e)
        {
            var added = Mod.FromZip(e.FullPath);
            if (added != null)
            {
                Dispatcher.Invoke(() =>
                {
                    // Due to ObservableCollection not firing a notify event when it's sorted,
                    // it's simpler to insert the new mod at it's sorted index.
                    int i = 0;
                    while (i < Mods.Count && Mods[i].CompareTo(added) < 0) ++i;
                    Mods.Insert(i, added);
                });
            }
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

            if (find != null && renamed != null)
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
            if (AppState == AppState.ReadyToPlay)
            {
                var contains = FileSystem.ContainedFolders(EndIsNighPath, FileSystem.ModFolders).ToList();
                if (contains.Count != 0)
                {
                    // FINALLY an excuse to use tuples!
                    var (isOrAre, a, folderOrFolders, itOrThem) = contains.Count == 1 ?
                        ("is", "a ", "folder", "it") :
                        ("are", "", "folders", "them");

                    var result = MessageBox.Show(
                        $"There {isOrAre} currently {a}modified {String.Join(", ", contains.Select(f => $"\"{f}\""))} {folderOrFolders} present in your game directory. " +
                        $"Delete {itOrThem} to play the MOD?", 
                        "Warning", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Warning,
                        MessageBoxResult.No
                    );

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            FileSystem.UnloadAll(EndIsNighPath);
                        }
                        catch (IOException)
                        {
                            MessageBox.Show(
                                "Could not delete the modified folders because one or more of them are open in an another process.",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                            return;
                        }
                    }
                    else
                    {
                        // Let's not delete any more Cities of Tethys.
                        return;
                    }
                }

                AppState = AppState.InGame;
                FileSystem.LoadMod(mod, EndIsNighPath);

                Process.Start(Path.Combine(EndIsNighPath, ExeName));

                await HookGameExit("TheEndIsNigh", (s, ev) =>
                {
                    AppState = AppState.ReadyToPlay;
                    FileSystem.UnloadAll(EndIsNighPath);
                });
            }
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

        private async void ModList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                await PlayMod(ModList.SelectedItem as Mod);
        }

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

        private void LoadModList(IOrderedEnumerable<Mod> mods)
        {
            Mods.Clear();
            foreach (var mod in mods.OrderBy(m => m))
            {
                Mods.Add(mod);
            }

            if (Mods.Count == 0) AppState = AppState.NoModsFound;
            else if (ModList.SelectedIndex == -1) AppState = AppState.NoModSelected;
            else AppState = AppState.ReadyToPlay;
        }

        private void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select The End Is Nigh Folder"
            };

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                EndIsNighPath = dialog.FileName;
                Mods.Clear();
            }

            ReadyEndIsNighPath();
        }
    }
}
