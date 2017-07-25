using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace EndModLoader
{
    public partial class MainWindow : Window
    {
        private const string EndIsNighPath = "C:/Program Files (x86)/Steam/steamapps/common/theendisnigh/";
        private const string ExeName = "TheEndIsNigh.exe";
        private readonly string ModPath = Path.Combine(EndIsNighPath, "mods");

        public MainWindow()
        {
            InitializeComponent();

            if (!FileSystem.EnsureDir(EndIsNighPath, ModPath))
            {
                MessageBox.Show("");
            }

            var mods = FileSystem.ReadModFolder(ModPath);
            ModList.ItemsSource = mods;
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            var modToPlay = ModList.SelectedItem as Mod;
            FileSystem.LoadMod(modToPlay, EndIsNighPath);

            Process.Start(Path.Combine(EndIsNighPath, ExeName));
            await HookGameExit("TheEndIsNigh", (s, ev) =>
            {
                MessageBox.Show("fuck");
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
    }
}
