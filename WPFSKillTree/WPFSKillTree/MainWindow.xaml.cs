using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using POESKillTree;
using WPFSKillTree;

namespace POESKillTree
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        private ItemAttributes ItemAttributes = null;
        SkillTree Tree;
        ToolTip sToolTip = new ToolTip( );
        private string lasttooltip;
        private Vector2D multransform = new Vector2D( );
        private Vector2D addtransform = new Vector2D( );
        public MainWindow( )
        {

            Thread.CurrentThread.SetApartmentState( ApartmentState.STA );
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
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

            InitializeComponent( );
            AttibuteCollection = new ListCollectionView( attiblist );

            listBox1.ItemsSource = AttibuteCollection;
            // AttibuteCollection.CustomSort = 
            PropertyGroupDescription pgd = new PropertyGroupDescription( "" );
            pgd.Converter = new GroupStringConverter( );
            AttibuteCollection.GroupDescriptions.Add( pgd );

            AllAttributeCollection = new ListCollectionView( allAttributesList );
            AllAttributeCollection.GroupDescriptions.Add( pgd );
            lbAllAttr.ItemsSource = AllAttributeCollection;

            Tree = SkillTree.CreateSkillTree( );
            image1.Fill = new VisualBrush( Tree.SkillTreeVisual );



            Tree.Chartype = Tree.CharName.IndexOf( ( ( string )( ( ComboBoxItem )cbCharType.SelectedItem ).Content ).ToUpper( ) );
            Tree.UpdateAvailNodes( );
            UpdateAllAttributeList( );

            multransform = Tree.TRect.Size / border1.RenderSize.Height;
            addtransform = Tree.TRect.TopLeft;

            if ( File.Exists( "skilltreeAddress.txt" ) )
            {
                string s = File.ReadAllText( "skilltreeAddress.txt" );
                tbSkillURL.Text = s.Split( '\n' )[ 0 ];
                tbLevel.Text = s.Split( '\n' )[ 1 ];
                button2_Click( this, new RoutedEventArgs( ) );
                justLoaded = false;
            }
        }
        private void border1_MouseMove( object sender, MouseEventArgs e )
        {
            Point p = e.GetPosition( border1.Child );
            Vector2D v = new Vector2D( p.X, p.Y );
            v = v * multransform + addtransform;
            textBox1.Text = "" + v.X;
            textBox2.Text = "" + v.Y;
            SkillTree.SkillNode node = null;

            var nodes = Tree.Skillnodes.Where( n => ( ( n.Value.Position - v ).Length < 50 ) );
            if ( nodes != null && nodes.Count( ) != 0 )
                node = nodes.First( ).Value;

            if ( node != null && node.Attributes.Count != 0 )
            {

                string tooltip = node.name + "\n" + node.attributes.Aggregate( ( s1, s2 ) => s1 + "\n" + s2 );
                if ( !( sToolTip.IsOpen == true && lasttooltip == tooltip ) )
                {
                    sToolTip.Content = tooltip;
                    sToolTip.IsOpen = true;
                    lasttooltip = tooltip;
                }
                if ( Tree.SkilledNodes.Contains( node.id ) )
                {
                    toRemove = Tree.ForceRefundNodePreview( node.id );
                    if ( toRemove != null )
                        Tree.DrawRefundPreview( toRemove );
                }
                else
                {
                    prePath = Tree.GetShortestPathTo( node.id );
                    Tree.DrawPath( prePath );
                }

            }
            else
            {
                sToolTip.Tag = false;
                sToolTip.IsOpen = false;
                prePath = null;
                toRemove = null;
                if ( Tree != null )
                {
                    Tree.ClearPath( );
                }

            }

        }
        private List<int> prePath;
        private HashSet<int> toRemove;
        private bool justLoaded = false;
        private void comboBox1_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( justLoaded )
            {
                justLoaded = false;
                return;
            }

            if ( Tree == null ) return;
            SkillTree.SkillNode startnode = Tree.Skillnodes.First( nd => nd.Value.name == ( ( ( string )( ( ComboBoxItem )cbCharType.SelectedItem ).Content ) ).ToUpper( ) ).Value;
            Tree.SkilledNodes.Clear( );
            Tree.SkilledNodes.Add( startnode.id );
            Tree.Chartype = Tree.CharName.IndexOf( ( ( string )( ( ComboBoxItem )cbCharType.SelectedItem ).Content ).ToUpper( ) );
            Tree.UpdateAvailNodes( );
            UpdateAllAttributeList( );
        }
        private void Window_SizeChanged( object sender, SizeChangedEventArgs e )
        {
            if ( Tree != null )
            {
                multransform = Tree.TRect.Size / border1.RenderSize.Height;
                addtransform = Tree.TRect.TopLeft;
            }
        }
        private void border1_Click( object sender, RoutedEventArgs e )
        {

            Point p = ( ( MouseEventArgs )e.OriginalSource ).GetPosition( border1.Child );
            Vector2D v = new Vector2D( p.X, p.Y );
            v = v * multransform + addtransform;
            SkillTree.SkillNode node = null;

            var nodes = Tree.SkilledNodes.Where( n => ( ( Tree.Skillnodes[ n ].Position - v ).Length < 50 ) );
            if ( nodes != null && nodes.Count( ) != 0 )
                node = Tree.Skillnodes[ nodes.First( ) ];

            if ( node != null )
            {
                Tree.ForceRefundNode( node.id );

                UpdateAllAttributeList( );
                //Tree.UpdateAvailNodes();
            }
            if ( prePath != null )
            {
                foreach ( int i in prePath )
                {
                    Tree.SkilledNodes.Add( i );
                }
                UpdateAllAttributeList( );
                Tree.UpdateAvailNodes( );
            }
            tbSkillURL.Text = Tree.SaveToURL( );

        }
        private List<string> attiblist = new List<string>( );
        private ListCollectionView AttibuteCollection;
        Regex backreplace = new Regex( "#" );
        private string InsertNumbersInAttributes( KeyValuePair<string, List<float>> attrib )
        {
            string s = attrib.Key;
            foreach ( var f in attrib.Value )
            {
                s = backreplace.Replace( s, f + "", 1 );
            }
            return s;
        }
        public void UpdateAttributeList( )
        {

            attiblist.Clear( );
            foreach ( var item in ( Tree.SelectedAttributes.Select( InsertNumbersInAttributes ) ) )
            {
                attiblist.Add( item );

            }
            AttibuteCollection.Refresh( );
            tbUsedPoints.Text = "" + ( Tree.SkilledNodes.Count - 1 );
        }
        private List<string> allAttributesList = new List<string>( );
        private ListCollectionView AllAttributeCollection;
        public void UpdateAllAttributeList( )
        {
            if ( ItemAttributes != null )
            {


                var attritemp = Tree.SelectedAttributesWithoutImplicit;
                foreach ( ItemAttributes.Attribute mod in ItemAttributes.NonLocalMods )
                {
                    if ( attritemp.ContainsKey( mod.TextAttribute ) )
                    {
                        for ( int i = 0 ; i < mod.Value.Count ; i++ )
                        {
                            attritemp[ mod.TextAttribute ][ i ] += mod.Value[ i ];
                        }
                    }
                    else
                    {
                        attritemp[ mod.TextAttribute ] = mod.Value;
                    }
                }

                foreach ( var a in Tree.ImplicitAttributes( attritemp ) )
                {
                    if ( !attritemp.ContainsKey( a.Key ) )
                        attritemp[ a.Key ] = new List<float>( );
                    for ( int i = 0 ; i < a.Value.Count ; i++ )
                    {

                        if ( attritemp.ContainsKey( a.Key ) && attritemp[ a.Key ].Count > i )
                            attritemp[ a.Key ][ i ] += a.Value[ i ];
                        else
                        {
                            attritemp[ a.Key ].Add( a.Value[ i ] );
                        }
                    }
                }

                allAttributesList.Clear( );
                foreach ( var item in ( attritemp.Select( InsertNumbersInAttributes ) ) )
                {
                    allAttributesList.Add( item );

                }
                AllAttributeCollection.Refresh( );
            }

            UpdateAttributeList( );
        }
        string TreeAddress = "http://www.pathofexile.com/passive-skill-tree/";
        private void button2_Click( object sender, RoutedEventArgs e )
        {
            if ( tbSkillURL.Text.Contains( "poezone.ru" ) )
            {
                SkillTreeImporter.LoadBuildFromPoezone( Tree, tbSkillURL.Text );
                tbSkillURL.Text = Tree.SaveToURL( );
            }
            else Tree.LoadFromURL( tbSkillURL.Text );

            justLoaded = true;
            cbCharType.SelectedIndex = Tree.Chartype;
            UpdateAllAttributeList( );
        }
        [ValueConversion( typeof( string ), typeof( string ) )]
        public class GroupStringConverter : IValueConverter
        {
            static GroupStringConverter( )
            {
                if ( !File.Exists( "groups.txt" ) ) return;
                Groups.Clear( );
                foreach ( string s in File.ReadAllLines( "groups.txt" ) )
                {
                    string[] sa = s.Split( ',' );
                    Groups.Add( sa );
                }
            }
            public static List<string[]> Groups = new List<string[]>( )
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
            public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
            {
                string s = ( string )value;
                foreach ( var gp in Groups )
                {
                    if ( s.ToLower( ).Contains( gp[ 0 ].ToLower( ) ) )
                    {
                        return gp[ 1 ];
                    }
                }
                return "Everything else";
            }

            public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
            {
                throw new NotImplementedException( );
            }
        }
        public class NumberLessStringComparer : IComparer<string>
        {
            static Regex numberfilter = new Regex( @"[0-9\\.]+" );

            public int Compare( string x, string y )
            {
                return numberfilter.Replace( x, "" ).CompareTo( numberfilter.Replace( y, "" ) );
            }
        }
        private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            File.WriteAllText( "skilltreeAddress.txt", tbSkillURL.Text + "\n" + tbLevel.Text );
        }
        private void tbSkillURL_MouseDoubleClick( object sender, MouseButtonEventArgs e )
        {
            tbSkillURL.SelectAll( );
        }
        private void button1_Click_1( object sender, RoutedEventArgs e )
        {
            if ( !File.Exists( "Data\\get-items" ) )
            {
                popup1.IsOpen = true;
                return;
            }

           // try
           // {
                ItemAttributes = new ItemAttributes( "Data\\get-items" );
                lbItemAttr.ItemsSource = ItemAttributes.Attributes;
                UpdateAllAttributeList( );
            //}
            //catch ( Exception er )
            //{
            //    MessageBoxResult result = MessageBox.Show( "Your ItemData is invalid.\nYou  either tried to download the data for a character not on your account or you were not logged in while downloading it" );
            //    popup1.IsOpen = true;
            //}
            // lbItemAttr.Items.Clear();



        }
        private void btnPopup_OnClick( object sender, RoutedEventArgs e )
        {
            popup1.IsOpen = false;
        }
        private void btnDownloadItemData_Click( object sender, RoutedEventArgs e )
        {
            popup1.IsOpen = false;
            System.Diagnostics.Process.Start( "http://www.pathofexile.com/character-window/get-items?character=" + tbCharName.Text );
        }
        private void tbCharName_TextChanged( object sender, TextChangedEventArgs e )
        {
            tbCharLink.Text = "http://www.pathofexile.com/character-window/get-items?character=" + tbCharName.Text;
        }
        private void tbSearch_TextChanged( object sender, TextChangedEventArgs e )
        {
            Tree.HighlightNodes( tbSearch.Text, checkBox1.IsChecked.Value );
        }
        private void textBox3_TextChanged( object sender, TextChangedEventArgs e )
        {
            int lvl = 0;
            if ( int.TryParse( tbLevel.Text, out lvl ) )
            {
                Tree.Level = lvl;
                UpdateAllAttributeList( );
            }
        }
        private void button3_Click( object sender, RoutedEventArgs e )
        {
            Tree.SkillAllHighligtedNodes( );
            UpdateAllAttributeList( );
        }
        private void button4_Click( object sender, RoutedEventArgs e )
        {
            if ( Tree == null ) return;
            Tree.Reset( );

            UpdateAllAttributeList( );
        }
        private RenderTargetBitmap ClipboardBmp;
        private void btnScreenShot_Click( object sender, RoutedEventArgs e )
        {
            int maxsize = 3000;
            Geometry geometry = Tree.picActiveLinks.Clip;
            Rect2D contentBounds = Tree.picActiveLinks.ContentBounds;
            contentBounds *= 1.2;


            double aspect = contentBounds.Width / contentBounds.Height;
            double xmax = contentBounds.Width;
            double ymax = contentBounds.Height;
            if ( aspect > 1 && xmax > maxsize )
            {
                xmax = maxsize;
                ymax = xmax / aspect;
            }
            if ( aspect < 1 & ymax > maxsize )
            {
                ymax = maxsize;
                xmax = ymax * aspect;
            }

            ClipboardBmp = new RenderTargetBitmap( ( int )xmax, ( int )ymax, 96, 96, PixelFormats.Pbgra32 );
            VisualBrush db = new VisualBrush( Tree.SkillTreeVisual );
            db.ViewboxUnits = BrushMappingMode.Absolute;
            db.Viewbox = contentBounds;
            DrawingVisual dw = new DrawingVisual( );

            using ( DrawingContext dc = dw.RenderOpen( ) )
            {
                dc.DrawRectangle( db, null, new Rect( 0, 0, xmax, ymax ) );
            }
            ClipboardBmp.Render( dw );
            ClipboardBmp.Freeze( );

            Clipboard.SetImage( ClipboardBmp );

            image1.Fill = new VisualBrush( Tree.SkillTreeVisual );

        }
    }
}

