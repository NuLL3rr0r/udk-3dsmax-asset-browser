using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UDKTools;

namespace Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetDatabase newDatabase = new AssetDatabase();
                newDatabase.RegisterAssetClass("Environment", "ENV");
                newDatabase.RegisterAssetType("StaticMesh", "SM");
                newDatabase.RegisterAssetType("SkeletalMesh", "SKLM");
                newDatabase.SourceRootDirectory = @"D:\dev\new-game-project\assets\trunk\art";
                newDatabase.GameRootDirectory = @"D:\dev\new-game-project\game\trunk";
                newDatabase.AssetCacheDirectory = @"D:\dev\new-game-project\game\trunk\UDKGame\Content\AssetCache";

                AssetBrowser.MainWindow m = new AssetBrowser.MainWindow(newDatabase);
                m.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
