using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ManagedServices;
using UDKTools;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

#region Legacy runtime support

/// <summary>
/// Enables support for legacy runtime and mixed mode assemblies
/// For more info, please look at the following:
/// http://support.microsoft.com/kb/2572158
/// http://reedcopsey.com/2011/09/15/setting-uselegacyv2runtimeactivationpolicy-at-runtime/
/// https://community.dynamics.com/product/ax/axtechnical/b/dynamicsax_wpfandnetinnovations/archive/2012/11/04/issues-relating-to-mixed-mode-assemblies.aspx
/// Also keep in mind that for 'Platform Target' in 'Build' settings, for both the lib and the app you should set the value
/// 'Any CPU'
/// </summary>
public static class RuntimePolicyHelper
{
    public static bool LegacyV2RuntimeEnabledSuccessfully { get; private set; }

    static RuntimePolicyHelper()
    {
        ICLRRuntimeInfo clrRuntimeInfo =
            (ICLRRuntimeInfo)RuntimeEnvironment.GetRuntimeInterfaceAsObject(
                Guid.Empty,
                typeof(ICLRRuntimeInfo).GUID);
        try
        {
            clrRuntimeInfo.BindAsLegacyV2Runtime();
            LegacyV2RuntimeEnabledSuccessfully = true;
        }
        catch (COMException)
        {
            // This occurs with an HRESULT meaning 
            // "A different runtime was already bound to the legacy CLR version 2 activation policy."
            LegacyV2RuntimeEnabledSuccessfully = false;
        }
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("BD39D1D2-BA2F-486A-89B0-B4B0CB466891")]
    private interface ICLRRuntimeInfo
    {
        void xGetVersionString();
        void xGetRuntimeDirectory();
        void xIsLoaded();
        void xIsLoadable();
        void xLoadErrorString();
        void xLoadLibrary();
        void xGetProcAddress();
        void xGetInterface();
        void xSetDefaultStartupFlags();
        void xGetDefaultStartupFlags();

        [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void BindAsLegacyV2Runtime();
    }
}

#endregion

namespace AssetBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, System.Windows.Forms.IWin32Window
    {
        private const string M_ASSET_ICON = "asset-icon.png";
        private const string M_DROP_ASSET_SCRIPT = "DropAssetScript.ds";
        private const string M_ASSET_FILE_PATH_TEMPLATE_KEY = "{ASSETFILEPATH}";

        private const string M_ASSET_CTX_COMMIT = "Commit";

        private string m_appPath = string.Empty;
        private AssetDatabase m_assetsDatabase = null;
        private ListViewItem[] m_assetItems = { };
        private string[] m_typeFilters = { };
        private string[] m_classFilters = { };
        private string[] m_categoryFilters = { };
        private string[] m_styleFilters = { };
        private string[] m_sceneFilters = { };
        private string m_dropAssetScriptTemplate = string.Empty;
        private string m_tempPath = string.Empty;

        private int m_iconSize = -1;
        private string m_searchPhrase = string.Empty;
        private BitmapImage m_defaultThumbnail;

        private SVNClient m_svnClient;
        
        public AssetDatabase AssetsDataBase
        {
            get
            {
                return m_assetsDatabase;
            }
            set
            {
                if (m_assetsDatabase != null)
                {
                    // TODO: Unhook previous event handlers
                }

                m_assetsDatabase = value;
                if (m_assetsDatabase != null)
                {
                    m_assetsDatabase.AssetChanged += new AssetDatabase.AssetChangedDelegate(OnAssetChanged);
                    m_assetsDatabase.AssetAdded += new AssetDatabase.AssetChangedDelegate(OnAssetAdded);
                    m_assetsDatabase.AssetRemoved += new AssetDatabase.AssetChangedDelegate(OnAssetRemoved);
                }
            }
        }

        void OnAssetRemoved(AssetDatabase database, Asset asset)
        {
            // TODO: Not implemented
        }

        void OnAssetAdded(AssetDatabase database, Asset asset)
        {
            // TODO: Not implemented
        }

        void OnAssetChanged(AssetDatabase database, Asset asset)
        {
            // TODO: Not implemented
        }

        public MainWindow(AssetDatabase db)
        {
            AssetsDataBase = db;
            InitializeComponent();

            // runtime support for mixed mode assemblies
            // this line must stay here, exactly after the InitializeComponent()
            if (RuntimePolicyHelper.LegacyV2RuntimeEnabledSuccessfully)
            {
            }

            m_svnClient = new SVNClient(new WPF32Window(this));
        }

        public IntPtr Handle
        {
            get
            {
                var interopHelper = new System.Windows.Interop.WindowInteropHelper(this);
                return interopHelper.Handle;
            }
        }

        private void PrintException(string msg)
        {
            try
            {
                MaxscriptSDK.ExecuteMaxscriptCommand(string.Format("print(\"!!! .NET Exception :  {0}\")", msg));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void AddTypeFilter(string typeName)
        {
            AddFilter(typeListView, typeName, m_typeFilters, "type");
        }

        public void AddClassFilter(string className)
        {
            AddFilter(classListView, className, m_classFilters, "class");
        }

        public void AddCategoryFilter(string categoryName)
        {
            AddFilter(categoryListView, categoryName, m_categoryFilters, "category");
        }

        public void AddStyleFilter(string styleName)
        {
            AddFilter(styleListView, styleName, m_styleFilters, "style");
        }

        public void AddSceneFilter(string sceneName)
        {
            AddFilter(sceneListView, sceneName, m_sceneFilters, "scene");
        }

        private void Init()
        {
            m_appPath = Environment.CurrentDirectory;
            if (!m_appPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
                m_appPath += System.IO.Path.DirectorySeparatorChar;

            m_tempPath = CreateTempPath();
            
            using (MemoryStream memory = new MemoryStream())
            {
                AssetBrowser.Properties.Resources.asset_icon.Save( memory, System.Drawing.Imaging.ImageFormat.Png );                    
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                m_defaultThumbnail = bitmapImage;
            }

            SetFilters();

            iconSizeComboBox.SelectedIndex = 1;
            FillIconSizeFromComboBox();
        }

        private void FillIconSizeFromComboBox()
        {
            ComboBoxItem item = (ComboBoxItem)iconSizeComboBox.SelectedItem;
            int.TryParse((item.Tag).ToString(), out m_iconSize);
        }

        private static string GenerateRandName()
        {
            Random rnd = new Random();
            string key = string.Empty;
            int min = -1, max = -1;

            for (int i = 0; i < 33; i++)
            {
                switch (rnd.Next(2))
                {
                    case 0:
                        min = 48;
                        max = 58;
                        break;
                    case 1:
                        min = 97;
                        max = 123;
                        break;
                    default:
                        break;
                }
                key = string.Concat(key, Convert.ToChar(rnd.Next(min, max)));
            }

            return key;
        }

        public static string GetTempPath()
        {
            string path = System.IO.Path.GetTempPath();
            path += path.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()) ? string.Empty : System.IO.Path.DirectorySeparatorChar.ToString();
            return path;
        }

        public static string CreateTempPath()
        {
            string path = GetTempPath();

            while (true)
            {
                path += GenerateRandName() + "\\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    break;
                }
            }

            return path;
        }

        private void AddAllFilter(ListView listView, string name)
        {
            CheckBox checkBox = new CheckBox();
            checkBox.Name = name;
            checkBox.Content = "All";
            checkBox.Checked += new RoutedEventHandler(AllFilterCheckBox_Checked);
            checkBox.Unchecked += new RoutedEventHandler(AllFilterCheckBox_Unchecked);

            ListViewItem item = new ListViewItem();
            item.Content = checkBox;

            listView.Items.Add(item);
        }

        private void SetFilters()
        {
            AddAllFilter(typeListView, "typeAllFilterCheckBox");
            AddAllFilter(classListView, "classAllFilterCheckBox");
            AddAllFilter(categoryListView, "categoryAllFilterCheckBox");
            AddAllFilter(styleListView, "styleAllFilterCheckBox");
            AddAllFilter(sceneListView, "sceneAllFilterCheckBox");

            List<AssetClass> classes = m_assetsDatabase.GetRegisteredClasses();
            List<AssetType> types = m_assetsDatabase.GetRegisteredTypes();

            foreach (AssetClass c in classes)
            {
                AddClassFilter(c.Name);
            }
            foreach (AssetType t in types)
            {
                AddTypeFilter(t.Name);
            }
        }

        private void ReadAssets()
        {
            try
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    Array.Resize(ref m_assetItems, 0);
                }), DispatcherPriority.Normal);

                foreach (Asset match in m_assetsDatabase.GetAllAssets())
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        StackPanel panel = new StackPanel();
                        panel.Orientation = Orientation.Vertical;

                        Image image = new Image();

                        image.Source = m_defaultThumbnail;

                        if (match.HasProperty("ThumbnailFile"))
                        {
                            String thumbnailPath = String.Concat( AssetsDataBase.AssetCacheDirectory, @"\", match.GetProperty("ThumbnailFile").Value );                            
                            if( File.Exists(thumbnailPath) )
                            {
                                BitmapImage bitmap = new BitmapImage();
                                bitmap.BeginInit();
                            
                                bitmap.UriSource = new Uri(thumbnailPath);
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();
                                
                                image.Source = bitmap;
                            }                            
                        }

                        image.MaxHeight = m_iconSize;
                        image.MaxWidth = m_iconSize;
                        image.MouseLeftButtonDown += new MouseButtonEventHandler(this.AssetItemMouseLeftButtonDown);

                        TextBlock text = new TextBlock();
                        text.TextWrapping = TextWrapping.WrapWithOverflow;
                        text.TextAlignment = TextAlignment.Center;
                        text.Text = match.LongName;

                        panel.Children.Add(image);
                        panel.Children.Add(text);

                        ListViewItem item = new ListViewItem();
                        item.Content = panel;
                        item.MaxWidth = m_iconSize;
                        item.Margin = new Thickness(8, 8, 8, 8);

                        Dictionary<string, Asset> tag = new Dictionary<string, Asset>();
                        tag.Add("ASSET", match);
                        item.Tag = tag;

                        Array.Resize(ref m_assetItems, m_assetItems.Length + 1);
                        m_assetItems[m_assetItems.Length - 1] = item;

                        ContextMenu ctx = new ContextMenu();
                        MenuItem commitMenuItem = new MenuItem();
                        commitMenuItem.Header = M_ASSET_CTX_COMMIT;
                        commitMenuItem.Click += OnAssetContextMenuItemClicked;
                        ctx.Items.Add(commitMenuItem);
                        item.ContextMenu = ctx;

                        assetsListView.Items.Add(item);

                        string filterName = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(match.CategoryName.Trim().ToLower());
                        if (filterName != string.Empty)
                        {
                            bool isDuplicate = false;
                            foreach (var i in categoryListView.Items)
                            {
                                ListViewItem listViewItem = (ListViewItem)i;
                                CheckBox checkBox = (CheckBox)listViewItem.Content;
                                if (filterName == checkBox.Content.ToString().Trim())
                                {
                                    isDuplicate = true;
                                    break;
                                }
                            }
                            if (!isDuplicate)
                                AddCategoryFilter(filterName);
                        }

                        filterName = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(match.StyleName.Trim().ToLower());
                        if (filterName != string.Empty)
                        {
                            bool isDuplicate = false;
                            foreach (var i in styleListView.Items)
                            {
                                ListViewItem listViewItem = (ListViewItem)i;
                                CheckBox checkBox = (CheckBox)listViewItem.Content;
                                if (filterName == checkBox.Content.ToString().Trim())
                                {
                                    isDuplicate = true;
                                    break;
                                }
                            }
                            if (!isDuplicate)
                                AddStyleFilter(filterName);
                        }

                        filterName = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(match.SceneName.Trim().ToLower());
                        if (filterName != string.Empty)
                        {
                            bool isDuplicate = false;
                            foreach (var i in sceneListView.Items)
                            {
                                ListViewItem listViewItem = (ListViewItem)i;
                                CheckBox checkBox = (CheckBox)listViewItem.Content;
                                if (filterName == checkBox.Content.ToString().Trim())
                                {
                                    isDuplicate = true;
                                    break;
                                }
                            }
                            if (!isDuplicate)
                                AddSceneFilter(filterName);
                        }
                    }), DispatcherPriority.Normal);
                }
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

                Init();

                Thread thread = new Thread(new ThreadStart(ReadAssets));
                thread.IsBackground = true;
                thread.Start();
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }
        }

        private void AddFilter(ListView listView, string name, string[] filters, string tag)
        {
            Array.Resize(ref filters, filters.Length + 1);
            filters[filters.Length - 1] = name;

            CheckBox checkBox = new CheckBox();
            checkBox.Content = name;
            checkBox.Tag = tag;
            checkBox.Checked += new RoutedEventHandler(FilterCheckBox_Checked);
            checkBox.Unchecked += new RoutedEventHandler(FilterCheckBox_Unchecked);

            ListViewItem item = new ListViewItem();
            item.Content = checkBox;

            listView.Items.Add(item);
        }

        private void AssetsFilterHandler()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                assetsListView.Items.Clear();

                for (int i = 0; i < m_assetItems.Length; ++i)
                {
                    Dictionary<string, Asset> dic = (Dictionary<string, Asset>)m_assetItems[i].Tag;
                    if (dic.ContainsKey("ASSET"))
                    {
                        Asset asset = dic["ASSET"];

                        bool found = false;

                        if (m_searchPhrase != string.Empty)
                        {
                            if (asset.LongName.Contains(m_searchPhrase))
                                found = true;
                        }
                        else
                        {
                            found = true;
                        }

                        if (!found)
                            continue;


                        bool noneChecked = true;
                        found = false;
                        foreach (var item in typeListView.Items)
                        {
                            ListViewItem listViewItem = (ListViewItem)item;
                            CheckBox checkBox = (CheckBox)listViewItem.Content;
                            if (checkBox.Tag != null)
                            {
                                if (checkBox.Tag.ToString() != "All")
                                {
                                    if (checkBox.IsChecked.Value)
                                    {
                                        noneChecked = false;
                                        if (asset.Type.Name == checkBox.Content.ToString())
                                            found = true;
                                    }
                                }
                            }
                        }

                        if (!noneChecked && !found)
                            continue;

                        
                        noneChecked = true;
                        found = false;
                        foreach (var item in classListView.Items)
                        {
                            ListViewItem listViewItem = (ListViewItem)item;
                            CheckBox checkBox = (CheckBox)listViewItem.Content;
                            if (checkBox.Tag != null)
                            {
                                if (checkBox.Tag.ToString() != "All")
                                {
                                    if (checkBox.IsChecked.Value)
                                    {
                                        noneChecked = false;
                                        if (asset.Class.Name == checkBox.Content.ToString())
                                            found = true;
                                    }
                                }
                            }
                        }

                        if (!noneChecked && !found)
                            continue;


                        noneChecked = true;
                        found = false;
                        foreach (var item in categoryListView.Items)
                        {
                            ListViewItem listViewItem = (ListViewItem)item;
                            CheckBox checkBox = (CheckBox)listViewItem.Content;
                            if (checkBox.Tag != null)
                            {
                                if (checkBox.Tag.ToString() != "All")
                                {
                                    if (checkBox.IsChecked.Value)
                                    {
                                        noneChecked = false;
                                        if (System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(asset.CategoryName.ToLower()) == checkBox.Content.ToString())
                                            found = true;
                                    }
                                }
                            }
                        }

                        if (!noneChecked && !found)
                            continue;


                        noneChecked = true;
                        found = false;
                        foreach (var item in styleListView.Items)
                        {
                            ListViewItem listViewItem = (ListViewItem)item;
                            CheckBox checkBox = (CheckBox)listViewItem.Content;
                            if (checkBox.Tag != null)
                            {
                                if (checkBox.Tag.ToString() != "All")
                                {
                                    if (checkBox.IsChecked.Value)
                                    {
                                        noneChecked = false;
                                        if (System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(asset.StyleName.ToLower()) == checkBox.Content.ToString())
                                            found = true;
                                    }
                                }

                            }
                        }

                        if (!noneChecked && !found)
                            continue;


                        noneChecked = true;
                        found = false;
                        foreach (var item in sceneListView.Items)
                        {
                            ListViewItem listViewItem = (ListViewItem)item;
                            CheckBox checkBox = (CheckBox)listViewItem.Content;
                            if (checkBox.Tag != null)
                            {
                                if (checkBox.Tag.ToString() != "All")
                                {
                                    if (checkBox.IsChecked.Value)
                                    {
                                        noneChecked = false;
                                        if (System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(asset.SceneName.ToLower()) == checkBox.Content.ToString())
                                            found = true;
                                    }
                                }
                            }
                        }

                        if (!noneChecked && !found)
                            continue;


                        assetsListView.Items.Add(m_assetItems[i]);
                    }
                }
            }), DispatcherPriority.Normal);
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            m_searchPhrase = textBox.Text;

            Thread thread = new Thread(new ThreadStart(AssetsFilterHandler));
            thread.IsBackground = true;
            thread.Start();
        }

        private void IconSizeChangedHandler()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                for (int i = 0; i < m_assetItems.Length; ++i)
                {
                    m_assetItems[i].MaxWidth = m_iconSize;
                    Image image = (Image)((StackPanel)m_assetItems[i].Content).Children[0];
                    image.MaxHeight = m_iconSize;
                    image.MaxWidth = m_iconSize;
                }
            }), DispatcherPriority.Normal);
        }

        private void iconSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FillIconSizeFromComboBox();

            Thread thread = new Thread(new ThreadStart(IconSizeChangedHandler));
            thread.IsBackground = true;
            thread.Start();
        }

        private void UnchekAllFilter(ListView listView)
        {
            foreach (var item in listView.Items)
            {
                ListViewItem listViewItem = (ListViewItem)item;
                CheckBox checkBox = (CheckBox)listViewItem.Content;
                if (checkBox.Content.ToString() == "All")
                {
                    checkBox.IsChecked = false;
                    break;
                }
            }
        }

        private void UnchekAllFilters(ListView listView)
        {
            foreach (var item in listView.Items)
            {
                ListViewItem listViewItem = (ListViewItem)item;
                CheckBox checkBox = (CheckBox)listViewItem.Content;
                if (checkBox.Content.ToString() != "All")
                {
                    checkBox.IsChecked = false;
                }
            }
        }

        private void AllFilterCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox chk = (CheckBox)sender;

            switch (chk.Name)
            {
                case "typeAllFilterCheckBox":
                    UnchekAllFilters(typeListView);
                    break;
                case "classAllFilterCheckBox":
                    UnchekAllFilters(classListView);
                    break;
                case "categoryAllFilterCheckBox":
                    UnchekAllFilters(categoryListView);
                    break;
                case "styleAllFilterCheckBox":
                    UnchekAllFilters(styleListView);
                    break;
                case "sceneAllFilterCheckBox":
                    UnchekAllFilters(sceneListView);
                    break;
                default:
                    break;
            }
        }

        private void AllFilterCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
        }

        private void FilterCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox chk = (CheckBox)sender;

            switch (chk.Tag.ToString())
            {
                case "type":
                    UnchekAllFilter(typeListView);
                    break;
                case "class":
                    UnchekAllFilter(classListView);
                    break;
                case "category":
                    UnchekAllFilter(categoryListView);
                    break;
                case "style":
                    UnchekAllFilter(styleListView);
                    break;
                case "scene":
                    UnchekAllFilter(sceneListView);
                    break;
                default:
                    break;
            }

            Thread thread = new Thread(new ThreadStart(AssetsFilterHandler));
            thread.IsBackground = true;
            thread.Start();
        }

        private void FilterCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox chk = (CheckBox)sender;

            Thread thread = new Thread(new ThreadStart(AssetsFilterHandler));
            thread.IsBackground = true;
            thread.Start();
        }

        public static ListViewItem GetItemAt(ListView listView, Point clientRelativePosition)
        {
            var hitTestResult = VisualTreeHelper.HitTest(listView, clientRelativePosition);
            var selectedItem = hitTestResult.VisualHit;

            while (selectedItem != null)
            {
                if (selectedItem is ListViewItem)
                    break;

                selectedItem = VisualTreeHelper.GetParent(selectedItem);
            }

            return selectedItem != null ? ((ListViewItem)selectedItem) : null;
        }

        private void AssetItemMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ListViewItem item = GetItemAt(assetsListView, e.GetPosition(assetsListView));

                if (item != null)
                {
                    Dictionary<string, Asset> dic = (Dictionary<string, Asset>)item.Tag;

                    Asset asset = null;
                    if (dic.ContainsKey("ASSET"))
                        asset = dic["ASSET"];

                    string[] files = new String[1];
                    string scriptTemplate = AssetBrowser.Properties.Resources.DropAssetScript;
                    
                    string scriptFile = m_tempPath + "DropAsset.ds";
                    using (StreamWriter sw = new StreamWriter(scriptFile, false, Encoding.ASCII))
                    {
                        if (asset != null)
                            sw.Write(scriptTemplate.Replace(M_ASSET_FILE_PATH_TEMPLATE_KEY, asset.LongName));
                    }

                    files[0] = scriptFile;
                    DataObject dataObj = new DataObject(DataFormats.FileDrop, files);
                    dataObj.SetData(DataFormats.StringFormat, files[0]);
                    DragDrop.DoDragDrop(item, dataObj, DragDropEffects.All);
                }
            }
            catch (Exception ex)
            {
                PrintException(ex.Message);
            }
        }

        private void OnAssetContextMenuItemClicked(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            if (menuItem != null)
            {
                ContextMenu parentContextMenu = (ContextMenu)menuItem.Parent;
                if (parentContextMenu != null)
                {
                    ListViewItem listViewItem = parentContextMenu.PlacementTarget as ListViewItem;
                    if (listViewItem != null)
                    {
                        switch (menuItem.Header.ToString())
                        {
                            case M_ASSET_CTX_COMMIT:
                                CommitWindow commitWindow = new CommitWindow();
                                commitWindow.ShowDialog();
                                if (commitWindow.IsAccepted)
                                {
                                    List<string> paths = new List<string>();

                                    Asset asset = ((Dictionary<string, Asset>)listViewItem.Tag)["ASSET"];
                                    if (asset != null)
                                    {
                                        AssetMetadataProperty thumbnailFile = asset.GetProperty("ThumbnailFile");
                                        AssetMetadataProperty sourcePath = asset.GetProperty("SourcePath");
                                        AssetMetadataProperty exportPath = asset.GetProperty("ExportPath");

                                        paths.Add(System.IO.Path.Combine(AssetsDataBase.AssetCacheDirectory, asset.LongName + ".xml"));

                                        if (thumbnailFile != null && !string.IsNullOrWhiteSpace(thumbnailFile.Value.Trim()))
                                            paths.Add(System.IO.Path.Combine(AssetsDataBase.AssetCacheDirectory, thumbnailFile.Value));

                                        if (sourcePath != null && !string.IsNullOrWhiteSpace(sourcePath.Value.Trim()))
                                            paths.Add(System.IO.Path.Combine(AssetsDataBase.SourceRootDirectory, sourcePath.Value));

                                        if (exportPath != null && !string.IsNullOrWhiteSpace(exportPath.Value.Trim()))
                                            paths.Add(System.IO.Path.Combine(AssetsDataBase.ContentRootDirectory, exportPath.Value));

                                        m_svnClient.Commit(paths, commitWindow.Message);
                                    }
                                }
                                break;
                            default:
                                break;

                        }
                    }
                }
            }
        }
    }
}

