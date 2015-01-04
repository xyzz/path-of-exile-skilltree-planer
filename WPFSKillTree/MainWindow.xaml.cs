using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace POESKillTree
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        readonly List<PoEBuild> _savedBuilds = new List<PoEBuild>();

        private ItemAttributes _itemAttributes;
        SkillTree _tree;
        readonly ToolTip _sToolTip = new ToolTip();
        private string _lasttooltip;
        private Vector2D _multransform;
        private Vector2D _addtransform;
        public MainWindow()
        {

            Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            //AppDomain.CurrentDomain.AssemblyResolve += ( sender, args ) =>
            //{

            //    String resourceName = "POESKillTree." +

            //       new AssemblyName( args.Name ).Name + ".dll";

            //    using ( var stream = Assembly.GetExecutingAssembly( ).GetManifestResourceStream( resourceName ) )
            //    {

            //        Byte[] assemblyData = new Byte[ stream.Length ];

            //        stream.Read( assemblyData, 0, assemblyData.Length );

            //        return Assembly.Load( assemblyData );

            //    }

            //};

            InitializeComponent();

        }
        static readonly Action EmptyDelegate = delegate
        {
        };

        private LoadingWindow _loadingWindow;
        private void StartLoadingWindow()
        {
            _loadingWindow = new LoadingWindow();
            _loadingWindow.Show();
        }
        private void UpdatetLoadingWindow(double c, double max)
        {
            _loadingWindow.progressBar1.Maximum = max;
            _loadingWindow.progressBar1.Value = c;
            _loadingWindow.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
        private void CloseLoadingWindow()
        {

            _loadingWindow.Close();
        }

        private void Border1MouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(border1.Child);
            var v = new Vector2D(p.X, p.Y);
            v = v * _multransform + _addtransform;
            textBox1.Text = "" + v.X;
            textBox2.Text = "" + v.Y;
            SkillTree.SkillNode node = null;

            var nodes = _tree.Skillnodes.Where(n => ((n.Value.Position - v).Length < 50));
            if (nodes != null && nodes.Any())
                node = nodes.First().Value;
                
            

            if (node != null && node.Attributes.Count != 0)
            {
                System.Console.WriteLine("In node");
                string tooltip = node.Name + "\n" + node.AttributesString.Aggregate((s1, s2) => s1 + "\n" + s2);
                if (!(_sToolTip.IsOpen && _lasttooltip == tooltip))
                {
                    _sToolTip.Content = tooltip;
                    _sToolTip.IsOpen = true;
                    _lasttooltip = tooltip;
                }
                if (_tree.SkilledNodes.Contains(node.Id))
                {
                    _toRemove = _tree.ForceRefundNodePreview(node.Id);
                    if (_toRemove != null)
                        _tree.DrawRefundPreview(_toRemove);
                }
                else
                {
                    _prePath = _tree.GetShortestPathTo(node.Id);
                    _tree.DrawPath(_prePath);
                }

            }
            else
            {
                
                System.Console.WriteLine("No node");
                _sToolTip.Tag = false;
                _sToolTip.IsOpen = false;
                _prePath = null;
                _toRemove = null;
                if (_tree != null)
                {
                    _tree.ClearPath();
                }

            }

        }
        private List<ushort> _prePath;
        private HashSet<ushort> _toRemove;
        private bool _justLoaded;
        private void ComboBox1SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_justLoaded)
            {
                _justLoaded = false;
                return;
            }

            if (_tree == null)
                return;
            SkillTree.SkillNode startnode = _tree.Skillnodes.First(nd => nd.Value.Name.ToUpper() == (_tree.CharName[cbCharType.SelectedIndex]).ToUpper()).Value;
            _tree.SkilledNodes.Clear();
            _tree.SkilledNodes.Add(startnode.Id);
            _tree.Chartype = _tree.CharName.IndexOf((_tree.CharName[cbCharType.SelectedIndex]).ToUpper());
            _tree.UpdateAvailNodes();
            UpdateAllAttributeList();
        }
        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {

        }
        private void Border1Click(object sender, RoutedEventArgs e)
        {
            
            Point p = ((MouseEventArgs)e.OriginalSource).GetPosition(border1.Child);
            var v = new Vector2D(p.X, p.Y);

            v = v * _multransform + _addtransform;

            var nodes = _tree.Skillnodes.Where(n => ((n.Value.Position - v).Length < 50));
            if (nodes != null && nodes.Any())
            {
                SkillTree.SkillNode node = nodes.First().Value;

                if (node.Spc == null)
                {
                    if (_tree.SkilledNodes.Contains(node.Id))
                    {
                        _tree.ForceRefundNode(node.Id);
                        UpdateAllAttributeList();

                        _prePath = _tree.GetShortestPathTo(node.Id);
                        _tree.DrawPath(_prePath);
                    }
                    else if (_prePath != null)
                    {
                        foreach (ushort i in _prePath)
                        {
                            _tree.SkilledNodes.Add(i);
                        }
                        UpdateAllAttributeList();
                        _tree.UpdateAvailNodes();

                        _toRemove = _tree.ForceRefundNodePreview(node.Id);
                        if (_toRemove != null)
                            _tree.DrawRefundPreview(_toRemove);
                    }
                }
            }
            tbSkillURL.Text = _tree.SaveToUrl();
        }
        private readonly List<string> _attriblist = new List<string>();
        private ListCollectionView _attibuteCollection;
        readonly Regex _backreplace = new Regex("#");
        private string InsertNumbersInAttributes(KeyValuePair<string, List<float>> attrib)
        {
            return attrib.Value.Aggregate(attrib.Key, (current, f) => _backreplace.Replace(current, f + "", 1));
        }

        public void UpdateAttributeList()
        {

            _attriblist.Clear();
            foreach (var item in (_tree.SelectedAttributes.Select(InsertNumbersInAttributes)))
            {
                _attriblist.Add(item);

            }
            _attibuteCollection.Refresh();
            tbUsedPoints.Text = "" + (_tree.SkilledNodes.Count - 1);
        }
        private readonly List<string> _allAttributesList = new List<string>();
        private ListCollectionView _allAttributeCollection;
        public void UpdateAllAttributeList()
        {
            if (_itemAttributes != null)
            {


                var attritemp = _tree.SelectedAttributesWithoutImplicit;
                foreach (ItemAttributes.Attribute mod in _itemAttributes.NonLocalMods)
                {
                    if (attritemp.ContainsKey(mod.TextAttribute))
                    {
                        for (int i = 0; i < mod.Value.Count; i++)
                        {
                            attritemp[mod.TextAttribute][i] += mod.Value[i];
                        }
                    }
                    else
                    {
                        attritemp[mod.TextAttribute] = mod.Value;
                    }
                }

                foreach (var a in _tree.ImplicitAttributes(attritemp))
                {
                    if (!attritemp.ContainsKey(a.Key))
                        attritemp[a.Key] = new List<float>();
                    for (int i = 0; i < a.Value.Count; i++)
                    {

                        if (attritemp.ContainsKey(a.Key) && attritemp[a.Key].Count > i)
                            attritemp[a.Key][i] += a.Value[i];
                        else
                        {
                            attritemp[a.Key].Add(a.Value[i]);
                        }
                    }
                }

                _allAttributesList.Clear();
                foreach (var item in (attritemp.Select(InsertNumbersInAttributes)))
                {
                    _allAttributesList.Add(item);

                }
                _allAttributeCollection.Refresh();
            }

            UpdateAttributeList();
        }

        private void Button2Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tbSkillURL.Text.Contains("poezone.ru"))
                {
                    SkillTreeImporter.LoadBuildFromPoezone(_tree, tbSkillURL.Text);
                    tbSkillURL.Text = _tree.SaveToUrl();
                }
                else
                    _tree.LoadFromUrl(tbSkillURL.Text);

                _justLoaded = true;
                cbCharType.SelectedIndex = _tree.Chartype;
                UpdateAllAttributeList();
            }
            catch (Exception)
            {
                MessageBox.Show("The Build you tried to load, is invalid");
            }
        }
        [ValueConversion(typeof(string), typeof(string))]
        public class GroupStringConverter : IValueConverter
        {
            static GroupStringConverter()
            {
                if (!File.Exists("groups.txt"))
                    return;
                Groups.Clear();
                foreach (string s in File.ReadAllLines("groups.txt"))
                {
                    string[] sa = s.Split(',');
                    Groups.Add(sa);
                }
            }
            public static List<string[]> Groups = new List<string[]>
                                                  { 
                                                                     new []{"charg","Charge"},
                                                                     new []{"weapon","Weapon"},
                                                                       new []{"melee phys","Weapon"},
                                                                       new []{"physical dam","Weapon"},
                                                                       new []{"area","Spell"},
                                                                       new []{"crit","Crit"},
                                                                       new []{"pierc","Weapon"},
                                                                       new []{"proj","Weapon"},
                                                                       new []{"minio","Minion"},
                                                                       new []{"move","Defense"},
                                                                       new []{"mana","Spell"},
                                                                       new []{"life","Defense"},
                                                                       new []{"armour","Defense"},
                                                                       new []{"evasi","Defense"},
                                                                       new []{"defence","Defense"},
                                                                       new []{"buff","Spell"},
                                                                       new []{"spell","Spell"},
                                                                       new []{"cast","Spell"},
                                                                       new []{"trap","Traps"},
                                                                       new []{"attack","Weapon"},
                                                                       new []{"accur","Weapon"},
                                                                       new []{"intel","BaseStats"},
                                                                       new []{"dex","BaseStats"},
                                                                       new []{"stre","BaseStats"},
                                                                       new []{"shield","Defense"},
                                                                       new []{"dual wiel","Weapon"},
                                                                       new []{"bow","Weapon"},
                                                                       new []{"axe","Weapon"},
                                                                       new []{"mace","Weapon"},
                                                                       new []{"stav","Weapon"},
                                                                       new []{"staff","Weapon"},
                                                                       new []{"dagg","Weapon"},
                                                                       new []{"claw","Weapon"},
                                                                       new []{"wand","Weapon"},
                                                                       new []{"zombie","Minion"},
                                                                       new []{"spectre","Minion"},
                                                                       new []{"all attrib","BaseStats"},
                                                                       new []{"resist","Defense"},

                                                                 };
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var s = (string)value;
                foreach (var gp in Groups)
                {
                    if (s.ToLower().Contains(gp[0].ToLower()))
                    {
                        return gp[1];
                    }
                }
                return "Everything else";
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        public class NumberLessStringComparer : IComparer<string>
        {
            static readonly Regex Numberfilter = new Regex(@"[0-9\\.]+");

            public int Compare(string x, string y)
            {
                return String.Compare(Numberfilter.Replace(x, ""), Numberfilter.Replace(y, ""), StringComparison.Ordinal);
            }
        }
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            File.WriteAllText("skilltreeAddress.txt", tbSkillURL.Text + "\n" + tbLevel.Text);

            if (lvSavedBuilds.Items.Count > 0)
            {
                var rawBuilds = new StringBuilder();
                foreach (ListViewItem lvi in lvSavedBuilds.Items)
                {
                    var build = (PoEBuild)lvi.Content;
                    rawBuilds.Append(build.Name + '|' + build.Description + ';' + build.Url + '\n');
                }
                File.WriteAllText("savedBuilds", rawBuilds.ToString().Trim());
            }
            else
            {
                if (File.Exists("savedBuilds"))
                {
                    File.Delete("savedBuilds");
                }
            }
        }
        private void TbSkillUrlMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            tbSkillURL.SelectAll();
        }
        private void Button1Click1(object sender, RoutedEventArgs e)
        {
            string filetoload;
            if (File.Exists("Data\\get-items"))
            {
                filetoload = "Data\\get-items";
            }
            else if (File.Exists("Data\\get-items.txt"))
            {
                filetoload = "Data\\get-items.txt";
            }
            else
            {
                popup1.IsOpen = true;
                return;
            }


            _itemAttributes = new ItemAttributes(filetoload);
            lbItemAttr.ItemsSource = _itemAttributes.Attributes;
            UpdateAllAttributeList();




        }
        private void btnPopup_OnClick(object sender, RoutedEventArgs e)
        {
            popup1.IsOpen = false;
        }
        private void BtnDownloadItemDataClick(object sender, RoutedEventArgs e)
        {
            popup1.IsOpen = false;
            System.Diagnostics.Process.Start("http://www.pathofexile.com/character-window/get-items?character=" + tbCharName.Text);
        }
        private void TbCharNameTextChanged(object sender, TextChangedEventArgs e)
        {
            tbCharLink.Text = "http://www.pathofexile.com/character-window/get-items?character=" + tbCharName.Text;
        }
        private void TbSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _tree.HighlightNodes(tbSearch.Text, checkBox1.IsChecked.Value);
        }
        private void TextBox3TextChanged(object sender, TextChangedEventArgs e)
        {
            int lvl;
            if (int.TryParse(tbLevel.Text, out lvl))
            {
                _tree.Level = lvl;
                UpdateAllAttributeList();
            }
        }
        private void Button3Click(object sender, RoutedEventArgs e)
        {
            _tree.SkillAllHighligtedNodes();
            UpdateAllAttributeList();
        }
        private void Button4Click(object sender, RoutedEventArgs e)
        {
            if (_tree == null)
                return;
            _tree.Reset();

            UpdateAllAttributeList();
        }
        private RenderTargetBitmap _clipboardBmp;
        private void BtnScreenShotClick(object sender, RoutedEventArgs e)
        {
            const int maxsize = 3000;
            Rect2D contentBounds = _tree.PicActiveLinks.ContentBounds;
            contentBounds *= 1.2;


            var aspect = contentBounds.Width / contentBounds.Height;
            var xmax = contentBounds.Width;
            var ymax = contentBounds.Height;
            if (aspect > 1 && xmax > maxsize)
            {
                xmax = maxsize;
                ymax = xmax / aspect;
            }
            if (aspect < 1 & ymax > maxsize)
            {
                ymax = maxsize;
                xmax = ymax * aspect;
            }

            _clipboardBmp = new RenderTargetBitmap((int)xmax, (int)ymax, 96, 96, PixelFormats.Pbgra32);
            var db = new VisualBrush(_tree.SkillTreeVisual)
                     {
                         ViewboxUnits = BrushMappingMode.Absolute,
                         Viewbox = contentBounds
                     };
            var dw = new DrawingVisual();

            using (var dc = dw.RenderOpen())
            {
                dc.DrawRectangle(db, null, new Rect(0, 0, xmax, ymax));
            }
            _clipboardBmp.Render(dw);
            _clipboardBmp.Freeze();

            Clipboard.SetImage(_clipboardBmp);

            image1.Fill = new VisualBrush(_tree.SkillTreeVisual);

        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            _attibuteCollection = new ListCollectionView(_attriblist);

            listBox1.ItemsSource = _attibuteCollection;
            // AttibuteCollection.CustomSort = 
            var pgd = new PropertyGroupDescription("") {Converter = new GroupStringConverter()};
            _attibuteCollection.GroupDescriptions.Add(pgd);

            _allAttributeCollection = new ListCollectionView(_allAttributesList);
            _allAttributeCollection.GroupDescriptions.Add(pgd);
            lbAllAttr.ItemsSource = _allAttributeCollection;

            _tree = SkillTree.CreateSkillTree(StartLoadingWindow, UpdatetLoadingWindow, CloseLoadingWindow);
            image1.Fill = new VisualBrush(_tree.SkillTreeVisual);


            _tree.Chartype = _tree.CharName.IndexOf(((string)((ComboBoxItem)cbCharType.SelectedItem).Content).ToUpper());
            _tree.UpdateAvailNodes();
            UpdateAllAttributeList();

            _multransform = _tree.Rect.Size / image1.RenderSize.Height;
            _addtransform = _tree.Rect.TopLeft;

            // loading last build
            if (File.Exists("skilltreeAddress.txt"))
            {
                string s = File.ReadAllText("skilltreeAddress.txt");
                tbSkillURL.Text = s.Split('\n')[0];
                tbLevel.Text = s.Split('\n')[1];
                Button2Click(this, new RoutedEventArgs());
                _justLoaded = false;
            }

            // loading saved build
            try
            {
                if (File.Exists("savedBuilds"))
                {
                    string[] builds = File.ReadAllText("savedBuilds").Split('\n');
                    foreach (string b in builds)
                    {
                        _savedBuilds.Add(new PoEBuild(b.Split(';')[0].Split('|')[0], b.Split(';')[0].Split('|')[1], b.Split(';')[1]));
                    }

                    lvSavedBuilds.Items.Clear();
                    foreach (PoEBuild build in _savedBuilds)
                    {
                        ListViewItem lvi = new ListViewItem
                        {
                            Content = build
                        };
                        lvi.MouseDoubleClick += LviMouseDoubleClick;
                        lvSavedBuilds.Items.Add(lvi);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to load the saved builds.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void LviMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var lvi = (ListViewItem)sender;
            tbSkillURL.Text = ((PoEBuild)lvi.Content).Url;
            Button2Click(this, null); // loading the build
        }

        private void BtnSaveNewBuildClick(object sender, RoutedEventArgs e)
        {
            FormBuildName formBuildName = new FormBuildName();
            if ((bool)formBuildName.ShowDialog())
            {
                var lvi = new ListViewItem
                {
                    Content = new PoEBuild(formBuildName.getBuildName(), cbCharType.Text + ", " + tbUsedPoints.Text + " points used", tbSkillURL.Text)
                };
                lvi.MouseDoubleClick += LviMouseDoubleClick;
                lvSavedBuilds.Items.Add(lvi);
            }
        }

        private void BtnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (lvSavedBuilds.SelectedItems.Count > 0)
            {
                lvSavedBuilds.Items.Remove(lvSavedBuilds.SelectedItem);
            }
        }

        private void BtnOverwriteBuildClick(object sender, RoutedEventArgs e)
        {
            if (lvSavedBuilds.SelectedItems.Count > 0)
            {
                ((ListViewItem)lvSavedBuilds.SelectedItem).Content = new PoEBuild(((ListViewItem)lvSavedBuilds.SelectedItem).Content.ToString().Split('\n')[0], cbCharType.Text + ", " + tbUsedPoints.Text + " points used", tbSkillURL.Text);
            }
            else
            {
                MessageBox.Show("Please select an existing build first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDownloadItemDataCopyClick(object sender, RoutedEventArgs e)
        {
            popup1.IsOpen = false;
            var fileDialog = new OpenFileDialog {Multiselect = false};
            bool? ftoload = fileDialog.ShowDialog(this);
            if (ftoload.Value)
            {
                _itemAttributes = new ItemAttributes(fileDialog.FileName);
                lbItemAttr.ItemsSource = _itemAttributes.Attributes;
                UpdateAllAttributeList();
            }


        }

        private void BtnCopyStatsClick(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();
            foreach (var at in _attriblist)
            {
                sb.AppendLine(at);
            }
            Clipboard.SetText(sb.ToString(), TextDataFormat.Text);

        }
    }

    class PoEBuild
    {
        public string Name, Description, Url;
        public PoEBuild(string n, string d, string u)
        {
            Name = n;
            Description = d;
            Url = u;
        }
        public override string ToString()
        {
            return Name + '\n' + Description;
        }
    }
}

