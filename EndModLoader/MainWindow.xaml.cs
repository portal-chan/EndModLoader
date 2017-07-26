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

            if (!FileSystem.EnsureDir(EndIsNighPath, ModPath))
            {
                MessageBox.Show("");
            }

            Mods = new ObservableCollection<Mod>(FileSystem.ReadModFolder(ModPath).OrderBy(m => m));
            if (Mods.Count == 0)
            {
                AppState = AppState.NoModsFound;
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
    }
}
