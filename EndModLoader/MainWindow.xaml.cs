using System;
using System.Collections.Generic;
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

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException(nameof(PlayButton));
        }
    }
}
