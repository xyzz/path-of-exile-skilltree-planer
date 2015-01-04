﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using Newtonsoft.Json;

namespace POESKillTree
{
    public partial class SkillTree
    {
        public delegate void StartLoadingWindow();
        public delegate void CloseLoadingWindow();
        public delegate void UpdateLoadingWindow(double current, double max);


        private const string TreeAddress = "http://www.pathofexile.com/passive-skill-tree/";
        public List<NodeGroup> NodeGroups = new List<NodeGroup>();
        public Dictionary<UInt16, SkillNode> Skillnodes = new Dictionary<UInt16, SkillNode>();
        public List<string> AttributeTypes = new List<string>();
        // public Bitmap iconActiveSkills;
        public SkillIcons IconInActiveSkills = new SkillIcons();
        public SkillIcons IconActiveSkills = new SkillIcons();
        public Dictionary<string, string> NodeBackgrounds = new Dictionary<string, string> { { "normal", "PSSkillFrame" }, { "notable", "NotableFrameUnallocated" }, { "keystone", "KeystoneFrameUnallocated" } };
        public Dictionary<string, string> NodeBackgroundsActive = new Dictionary<string, string> { { "normal", "PSSkillFrameActive" }, { "notable", "NotableFrameAllocated" }, { "keystone", "KeystoneFrameAllocated" } };
        public List<string> FaceNames = new List<string> {"centerscion", "centermarauder", "centerranger", "centerwitch", "centerduelist", "centertemplar", "centershadow"  };
        public List<string> CharName = new List<string> { "SEVEN","MARAUDER", "RANGER", "WITCH", "DUELIST", "TEMPLAR", "SIX" };
        public Dictionary<string, float>[] CharBaseAttributes = new Dictionary<string, float>[7];
        public Dictionary<string, float> BaseAttributes = new Dictionary<string, float>
                                                          {
                                                                  {"+# to maximum Mana",36},
                                                                  {"+# to maximum Life",44},
                                                                  {"Evasion Rating: #",50},
                                                                  {"+# Maximum Endurance Charge",3},
                                                                  {"+# Maximum Frenzy Charge",3},
                                                                  {"+# Maximum Power Charge",3},
                                                                  {"#% Additional Elemental Resistance per Endurance Charge",4},
                                                                  {"#% Physical Damage Reduction per Endurance Charge",4},
                                                                  {"#% Attack Speed Increase per Frenzy Charge",5},
                                                                  {"#% Cast Speed Increase per Frenzy Charge",5},
                                                                  {"#% Critical Strike Chance Increase per Power Charge",50},
                                                              };
        public static float LifePerLevel = 12;
        public static float EvasPerLevel = 3;
        public static float ManaPerLevel = 4;
        public static float IntPerMana = 2;
        public static float IntPerEs = 5; //%
        public static float StrPerLife = 2;
        public static float StrPerEd = 5; //%
        public static float DexPerAcc = 0.5f;
        public static float DexPerEvas = 5; //%
        private List<SkillNode> _highlightnodes;
        private int _level = 1;
        private int _chartype;
        public HashSet<ushort> SkilledNodes = new HashSet<ushort>();
        public HashSet<ushort> AvailNodes = new HashSet<ushort>();
        readonly Dictionary<string, Asset> _assets = new Dictionary<string, Asset>();
        public Rect2D Rect = new Rect2D();
        public float ScaleFactor = 1;
        public HashSet<int[]> Links = new HashSet<int[]>();
        public void Reset()
        {
            SkilledNodes.Clear();
            var node = Skillnodes.First(nd => nd.Value.Name.ToUpper() == CharName[_chartype]);
            SkilledNodes.Add(node.Value.Id);
            UpdateAvailNodes();
        }

        public static SkillTree CreateSkillTree(StartLoadingWindow start = null, UpdateLoadingWindow update = null, CloseLoadingWindow finish = null)
        {

            string skilltreeobj = "";
            if (Directory.Exists("Data"))
            {
                if (File.Exists("Data\\Skilltree.txt"))
                {
                    skilltreeobj = File.ReadAllText("Data\\Skilltree.txt");
                }
            }
            else
            {
                Directory.CreateDirectory("Data");
                Directory.CreateDirectory("Data\\Assets");
            }

            if (skilltreeobj == "")
            {
                bool displayProgress = (start != null && update != null && finish != null);
                if (displayProgress)
                    start();
                //loadingWindow.Dispatcher.Invoke(DispatcherPriority.Background,new Action(delegate { }));


                var req = (HttpWebRequest)WebRequest.Create(TreeAddress);
                var resp = (HttpWebResponse)req.GetResponse();
                string code = new StreamReader(resp.GetResponseStream()).ReadToEnd();
                var regex = new Regex("var passiveSkillTreeData.*");
                skilltreeobj = regex.Match(code).Value.Replace("root", "main").Replace("\\/", "/");
                skilltreeobj = skilltreeobj.Substring(27, skilltreeobj.Length - 27 - 2) + "";
                File.WriteAllText("Data\\Skilltree.txt", skilltreeobj);
                if (displayProgress)
                    finish();
            }

            return new SkillTree(skilltreeobj, start, update, finish);
        }
        public SkillTree(String treestring, StartLoadingWindow start = null, UpdateLoadingWindow update = null, CloseLoadingWindow finish = null)
        {
            bool displayProgress = ( start != null && update != null && finish != null );
           // RavenJObject jObject = RavenJObject.Parse( treestring.Replace( "Additional " , "" ) );
          var jss = new JsonSerializerSettings
            {
            Error = delegate(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
            {
            Debug.WriteLine(args.ErrorContext.Error.Message);
            args.ErrorContext.Handled = true;
            }
            };

          var inTree = JsonConvert.DeserializeObject<PoESkillTree>(treestring.Replace("Additional ", ""), jss);


            foreach (var obj in inTree.SkillSprites)
            {
                if (obj.Key.Contains("inactive"))
                    continue;
                IconActiveSkills.Images[obj.Value[3].Filename] = null;
                foreach (var o in obj.Value[3].Coords)
                {
                    IconActiveSkills.SkillPositions[o.Key] = new KeyValuePair<Rect, string>(new Rect(o.Value.X, o.Value.Y, o.Value.W, o.Value.H), obj.Value[3].Filename);
                }
            }
            foreach (var obj in inTree.SkillSprites)
            {
                if (obj.Key.Contains("active"))
                    continue;
                IconActiveSkills.Images[obj.Value[3].Filename] = null;
                foreach (var o in obj.Value[3].Coords)
                {
                    IconActiveSkills.SkillPositions[o.Key] = new KeyValuePair<Rect, string>(new Rect(o.Value.X, o.Value.Y, o.Value.W, o.Value.H), obj.Value[3].Filename);
                }
            }

            foreach(var ass in inTree.Assets)
            {
               
                _assets[ass.Key] = new Asset(ass.Key,ass.Value.ContainsKey(0.3835f)?ass.Value[0.3835f]:ass.Value.Values.First());
                                     
            }
           
            if ( displayProgress )
                start( );
            IconActiveSkills.OpenOrDownloadImages(update );
            IconInActiveSkills.OpenOrDownloadImages(update );
            if ( displayProgress )
                finish( );
            foreach( var c in inTree.CharacterData)
            {
                CharBaseAttributes[c.Key] = new Dictionary<string, float> { { "+# to Strength", c.Value.BaseStr }, { "+# to Dexterity", c.Value.BaseDex }, { "+# to Intelligence", c.Value.BaseInt } };
            }
           foreach (var nd in inTree.Nodes)
           {
               Skillnodes.Add(nd.Id, new SkillNode
                                     {
                   Id = nd.Id,                
                   Name = nd.Dn,
                   AttributesString = nd.Sd,
                   Orbit = nd.O,
                   OrbitIndex =nd.Oidx,
                   Icon = nd.Icon,
                   LinkId =nd.Ot,
                   G = nd.G,
                   Da = nd.Da,
                   Ia = nd.Ia,
                   Ks = nd.Ks,
                   Not = nd.Not,
                   Sa = nd.Sa,
                   Mastery = nd.M,
                   Spc=nd.Spc.Count()>0?(int?)nd.Spc[0]:null
               });
           }         
            var links = new List<ushort[]>( );
            foreach ( var skillNode in Skillnodes )
            {
                foreach ( var i in skillNode.Value.LinkId )
                {
                    KeyValuePair<ushort, SkillNode> node = skillNode;
                    int i1 = i;
                    if (
                        links.Count(
                            nd => ( nd[ 0 ] == i1 && nd[ 1 ] == node.Key ) || nd[ 0 ] == node.Key && nd[ 1 ] == i1 ) ==
                        1 )
                    {
                        continue;
                    }
                    links.Add( new[] { skillNode.Key , (ushort)i } );
                }
            }
            foreach ( ushort[] ints in links )
            {
                if ( !Skillnodes[ ints[ 0 ] ].Neighbor.Contains( Skillnodes[ ints[ 1 ] ] ) )
                    Skillnodes[ ints[ 0 ] ].Neighbor.Add( Skillnodes[ ints[ 1 ] ] );
                if ( !Skillnodes[ ints[ 1 ] ].Neighbor.Contains( Skillnodes[ ints[ 0 ] ] ) )
                    Skillnodes[ ints[ 1 ] ].Neighbor.Add( Skillnodes[ ints[ 0 ] ] );
            }
           
            foreach(var gp in inTree.Groups )
            {
                var ng = new NodeGroup
                         {
                             OcpOrb = gp.Value.Oo,
                             Position = new Vector2D(gp.Value.X, gp.Value.Y),
                             Nodes = gp.Value.N
                         };

                NodeGroups.Add(ng);
            }
          
            foreach ( var group in NodeGroups )
            {
                foreach ( int node in group.Nodes )
                {
                    Skillnodes[ (ushort)node ].NodeGroup = group;
                }
            }

            Rect = new Rect2D( new Vector2D( inTree.MinX * 1.1 , inTree.MinY * 1.1 ) ,
                               new Vector2D(inTree.MaxX * 1.1, inTree.MaxY * 1.1));




            InitNodeSurround( );
            DrawNodeSurround( );
            DrawNodeBaseSurround( );
            DrawSkillIconLayer( );
            DrawBackgroundLayer( );
            InitFaceBrushesAndLayer( );
            DrawLinkBackgroundLayer( links );
            InitOtherDynamicLayers( );
            CreateCombineVisual( );


            var regexAttrib = new Regex( "[0-9]*\\.?[0-9]+" );
            foreach ( var skillNode in Skillnodes )
            {
                skillNode.Value.Attributes = new Dictionary<string , List<float>>( );
                foreach ( string s in skillNode.Value.AttributesString )
                {
                    var values = new List<float>( );

                    foreach ( Match m in regexAttrib.Matches( s ) )
                    {
                        if ( !AttributeTypes.Contains( regexAttrib.Replace( s , "#" ) ) )
                            AttributeTypes.Add( regexAttrib.Replace( s , "#" ) );
                        values.Add(m.Value == ""
                            ? float.NaN
                            : float.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture));
                    }
                    string cs = ( regexAttrib.Replace( s , "#" ) );

                    skillNode.Value.Attributes[ cs ] = values;



                }
                
            }


        }
        public Dictionary<string, List<float>> ImplicitAttributes(Dictionary<string, List<float>> attribs)
        {
            var retval = new Dictionary<string, List<float>>();
            // +# to Strength", co["base_str"].Value<int>() }, { "+# to Dexterity", co["base_dex"].Value<int>() }, { "+# to Intelligence", co["base_int"].Value<int>() } };
            retval["+# to maximum Mana"] = new List<float> { attribs["+# to Intelligence"][0] / IntPerMana + _level * ManaPerLevel };
            retval["+#% Energy Shield"] = new List<float> { attribs["+# to Intelligence"][0] / IntPerEs };

            retval["+# to maximum Life"] = new List<float> { attribs["+# to Strength"][0] / IntPerMana + _level * LifePerLevel };
            retval["+#% increased Melee Physical Damage"] = new List<float> { attribs["+# to Strength"][0] / StrPerEd };

            retval["+# Accuracy Rating"] = new List<float> { attribs["+# to Dexterity"][0] / DexPerAcc };
            retval["Evasion Rating: #"] = new List<float> { _level * EvasPerLevel };
            retval["#% increased Evasion Rating"] = new List<float> { attribs["+# to Dexterity"][0] / DexPerEvas };
            return retval;
        }
        public int Level
        {
            get
            {
                return _level;
            }
            set
            {
                _level = value;
            }
        }
        public int Chartype
        {
            get
            {
                return _chartype;
            }
            set
            {

                _chartype = value;
                SkilledNodes.Clear();
                var node = Skillnodes.First(nd => nd.Value.Name.ToUpper() == CharName[_chartype]);
                SkilledNodes.Add(node.Value.Id);
                UpdateAvailNodes();
                DrawFaces();
            }
        }
        public List<ushort> GetShortestPathTo(ushort targetNode)
        {
            if (SkilledNodes.Contains(targetNode))
                return new List<ushort>();
            if (AvailNodes.Contains(targetNode))
                return new List<ushort> { targetNode };
            var visited = new HashSet<ushort>(SkilledNodes);
            var parent = new Dictionary<ushort, ushort>();
            var newOnes = new Queue<ushort>();
            Dictionary<int, int> distance = SkilledNodes.ToDictionary<ushort, int, int>(node => node, node => 0);
            foreach (var node in AvailNodes)
            {
                newOnes.Enqueue(node);
                distance.Add(node, 1);
            }
            while (newOnes.Count > 0)
            {
                ushort newNode = newOnes.Dequeue();
                int dis = distance[newNode];
                visited.Add(newNode);
                foreach (var connection in Skillnodes[newNode].Neighbor.Select(nd => nd.Id))
                {
                    if (visited.Contains(connection))
                        continue;
                    if (distance.ContainsKey(connection))
                        continue;
                    if (Skillnodes[newNode].Spc.HasValue)
                        continue;
                    if (Skillnodes[newNode].Mastery)
                        continue;
                    distance.Add(connection, dis + 1);
                    newOnes.Enqueue(connection);

                    parent.Add(connection, newNode);

                    if (connection == targetNode)
                        break;
                }
            }

            if (!distance.ContainsKey(targetNode))
                return new List<ushort>();

            var path = new Stack<ushort>();
            ushort curr = targetNode;
            path.Push(curr);
            while (parent.ContainsKey(curr))
            {
                path.Push(parent[curr]);
                curr = parent[curr];
            }

            var result = new List<ushort>();
            while (path.Count > 0)
                result.Add(path.Pop());

            return result;
        }
        public HashSet<ushort> ForceRefundNodePreview(ushort nodeId)
        {
            if (!SkilledNodes.Remove(nodeId))
                return new HashSet<ushort>();

            SkilledNodes.Remove(nodeId);

            var front = new HashSet<ushort> {SkilledNodes.First()};
            foreach (var i in Skillnodes[SkilledNodes.First()].Neighbor)
                if (SkilledNodes.Contains(i.Id))
                    front.Add(i.Id);

            var skilledReachable = new HashSet<ushort>(front);
            while (front.Count > 0)
            {
                var newFront = new HashSet<ushort>();
                foreach (var i in front)
                    foreach (var j in Skillnodes[i].Neighbor.Select(nd => nd.Id))
                        if (!skilledReachable.Contains(j) && SkilledNodes.Contains(j))
                        {
                            newFront.Add(j);
                            skilledReachable.Add(j);
                        }

                front = newFront;
            }

            var unreachable = new HashSet<ushort>(SkilledNodes);
            foreach (var i in skilledReachable)
                unreachable.Remove(i);
            unreachable.Add(nodeId);

            SkilledNodes.Add(nodeId);

            return unreachable;
        }
        public void ForceRefundNode(ushort nodeId)
        {
            if (!SkilledNodes.Remove(nodeId))
                throw new InvalidOperationException();

            //SkilledNodes.Remove(nodeId);

            var front = new HashSet<ushort> {SkilledNodes.First()};
            foreach (var i in Skillnodes[SkilledNodes.First()].Neighbor)
                if (SkilledNodes.Contains(i.Id))
                    front.Add(i.Id);
            var skilledReachable = new HashSet<ushort>(front);
            while (front.Count > 0)
            {
                var newFront = new HashSet<ushort>();
                foreach (var i in front)
                    foreach (var j in Skillnodes[i].Neighbor.Select(nd => nd.Id))
                        if (!skilledReachable.Contains(j) && SkilledNodes.Contains(j))
                        {
                            newFront.Add(j);
                            skilledReachable.Add(j);
                        }

                front = newFront;
            }

            SkilledNodes = skilledReachable;
            AvailNodes = new HashSet<ushort>();
            UpdateAvailNodes();
        }
        public void LoadFromUrl(string url)
        {
            string s = url.Substring(TreeAddress.Length + (url.StartsWith("https") ? 1 : 0)).Replace("-", "+").Replace("_", "/");
            byte[] decbuff = Convert.FromBase64String(s);
            var b = decbuff[4];
     
            var nodes = new List<UInt16>();
            for (int k = 6; k < decbuff.Length; k += 2)
            {
                byte[] dbff = { decbuff[k + 1], decbuff[k + 0] };
                if (Skillnodes.Keys.Contains(BitConverter.ToUInt16(dbff, 0)))
                    nodes.Add((BitConverter.ToUInt16(dbff, 0)));

            }
            Chartype = b;
            SkilledNodes.Clear();
            var startnode = Skillnodes.First(nd => nd.Value.Name.ToUpper() == CharName[Chartype].ToUpper()).Value;
            SkilledNodes.Add(startnode.Id);
            foreach (ushort node in nodes)
            {
                SkilledNodes.Add(node);
            }
            UpdateAvailNodes();
        }
        public string SaveToUrl()
        {
            var b = new byte[(SkilledNodes.Count - 1) * 2 + 6];
            var b2 = BitConverter.GetBytes(2);
            b[0] = b2[3];
            b[1] = b2[2];
            b[2] = b2[1];
            b[3] = b2[0];
            b[4] = (byte)(Chartype);
            b[5] = 0;
            int pos = 6;
            foreach (var inn in SkilledNodes)
            {
                if (CharName.Contains(Skillnodes[inn].Name.ToUpper()))
                    continue;
                byte[] dbff = BitConverter.GetBytes((Int16)inn);
                b[pos++] = dbff[1];
                b[pos++] = dbff[0];
            }
            return TreeAddress + Convert.ToBase64String(b).Replace("/", "_").Replace("+", "-");

        }
        public void UpdateAvailNodes()
        {
            AvailNodes.Clear();
            foreach (ushort inode in SkilledNodes)
            {
                SkillNode node = Skillnodes[inode];
                foreach (SkillNode skillNode in node.Neighbor)
                {
                    if (!CharName.Contains(skillNode.Name) && !SkilledNodes.Contains(skillNode.Id))
                        AvailNodes.Add(skillNode.Id);
                }
            }
            //  picActiveLinks = new DrawingVisual();

            var pen2 = new Pen(Brushes.Yellow, 15f);

            using (DrawingContext dc = PicActiveLinks.RenderOpen())
            {
                foreach (var n1 in SkilledNodes)
                {
                    foreach (var n2 in Skillnodes[n1].Neighbor)
                    {
                        if (SkilledNodes.Contains(n2.Id))
                        {
                            DrawConnection(dc, pen2, n2, Skillnodes[n1]);
                        }
                    }
                }
            }
            // picActiveLinks.Clear();
            DrawNodeSurround();
        }
        public Dictionary<string, List<float>> SelectedAttributes
        {
            get
            {
                Dictionary<string, List<float>> temp = SelectedAttributesWithoutImplicit;

                foreach (var a in ImplicitAttributes(temp))
                {
                    if (!temp.ContainsKey(a.Key))
                        temp[a.Key] = new List<float>();
                    for (int i = 0; i < a.Value.Count; i++)
                    {

                        if (temp.ContainsKey(a.Key) && temp[a.Key].Count > i)
                            temp[a.Key][i] += a.Value[i];
                        else
                        {
                            temp[a.Key].Add(a.Value[i]);
                        }
                    }
                }
                return temp;
            }
        }
        public Dictionary<string, List<float>> SelectedAttributesWithoutImplicit
        {
            get
            {
                var temp = new Dictionary<string, List<float>>();
                foreach (var attr in CharBaseAttributes[Chartype])
                {
                    if (!temp.ContainsKey(attr.Key))
                        temp[attr.Key] = new List<float>();

                    if (temp.ContainsKey(attr.Key) && temp[attr.Key].Count > 0)
                        temp[attr.Key][0] += attr.Value;
                    else
                    {
                        temp[attr.Key].Add(attr.Value);
                    }
                }

                foreach (var attr in BaseAttributes)
                {
                    if (!temp.ContainsKey(attr.Key))
                        temp[attr.Key] = new List<float>();

                    if (temp.ContainsKey(attr.Key) && temp[attr.Key].Count > 0)
                        temp[attr.Key][0] += attr.Value;
                    else
                    {
                        temp[attr.Key].Add(attr.Value);
                    }
                }

                foreach (ushort inode in SkilledNodes)
                {
                    SkillNode node = Skillnodes[inode];
                    foreach (var attr in node.Attributes)
                    {
                        if (!temp.ContainsKey(attr.Key))
                            temp[attr.Key] = new List<float>();
                        for (int i = 0; i < attr.Value.Count; i++)
                        {

                            if (temp.ContainsKey(attr.Key) && temp[attr.Key].Count > i)
                                temp[attr.Key][i] += attr.Value[i];
                            else
                            {
                                temp[attr.Key].Add(attr.Value[i]);
                            }
                        }

                    }
                }

                return temp;
            }
        }
        public class SkillIcons
        {

            public enum IconType
            {
                Normal,
                Notable,
                Keystone
            }

            public Dictionary<string, KeyValuePair<Rect, string>> SkillPositions = new Dictionary<string, KeyValuePair<Rect, string>>();
            public Dictionary<String, BitmapImage> Images = new Dictionary<string, BitmapImage>();
            public static string Urlpath = "http://www.pathofexile.com/image/build-gen/passive-skill-sprite/";
            public void OpenOrDownloadImages(UpdateLoadingWindow update = null)
            {
                //Application
                int count = 0;
                foreach (var image in Images.Keys.ToArray())
                {
                    if (!File.Exists("Data\\Assets\\" + image))
                    {
                        var webClient = new WebClient();
                        webClient.DownloadFile(Urlpath + image, "Data\\Assets\\" + image);
                    }
                    Images[image] = new BitmapImage(new Uri("Data\\Assets\\" + image, UriKind.Relative));
                    if (update != null)
                        update(count, Images.Count);
                    ++count;
                }
            }
        }
        public class NodeGroup
        {
            public Vector2D Position;// "x": 1105.14,"y": -5295.31,
            public Dictionary<int, bool> OcpOrb = new Dictionary<int, bool>(); //  "oo": {"1": true},
            public List<int> Nodes = new List<int>();// "n": [-28194677,769796679,-1093139159]

        }
        public class SkillNode
        {
            static public float[] SkillsPerOrbit = { 1, 6, 12, 12, 12 };
            static public float[] OrbitRadii = { 0, 81.5f, 163, 326, 489 };
            public HashSet<int> Connections = new HashSet<int>();
            public bool Skilled = false;
            public UInt16 Id; // "id": -28194677,
            public string Icon;// icon "icon": "Art/2DArt/SkillIcons/passives/tempint.png",
            public bool Ks; //"ks": false,
            public bool Not;   // not": false,
            public string Name;//"dn": "Block Recovery",
            public int A;// "a": 3,
            public string[] AttributesString;// "sd": ["8% increased Block Recovery"],
            public Dictionary<string, List<float>> Attributes;
            // public List<string> AttributeNames;
            //public List<> AttributesValues;
            public int G;// "g": 1,
            public int Orbit;//  "o": 1,
            public int OrbitIndex;// "oidx": 3,
            public int Sa;//s "sa": 0,
            public int Da;// "da": 0,
            public int Ia;//"ia": 0,
            public List<int> LinkId = new List<int>();// "out": []
            public bool Mastery;
            public int? Spc;

            public List<SkillNode> Neighbor = new List<SkillNode>();
            public NodeGroup NodeGroup;
            public Vector2D Position
            {
                get
                {
                if(NodeGroup==null) return new Vector2D();
                    double d = OrbitRadii[Orbit];
                    double b = (2 * Math.PI * OrbitIndex / SkillsPerOrbit[Orbit]);
                    return (NodeGroup.Position - new Vector2D(d * Math.Sin(-b), d * Math.Cos(-b)));
                }
            }
            public double Arc
            {
                get
                {
                    return (2 * Math.PI * OrbitIndex / SkillsPerOrbit[Orbit]);
                }
            }
        }
        public class Asset
        {
            public string Name;
            public BitmapImage PImage;
            public string Url;
            public Asset(string name, string url)
            {
                Name = name;
                Url = url;
                if (!File.Exists("Data\\Assets\\" + Name + ".png"))
                {

                    var webClient = new WebClient();
                    webClient.DownloadFile(Url, "Data\\Assets\\" + Name + ".png");



                }
                PImage = new BitmapImage(new Uri("Data\\Assets\\" + Name + ".png", UriKind.Relative));

            }

        }

        public void HighlightNodes(string search, bool useregex)
        {
            if (search == "")
            {
                DrawHighlights(_highlightnodes = new List<SkillNode>());
                _highlightnodes = null;
                return;
            }

            if (useregex)
            {
                try
                {
                    _highlightnodes = Skillnodes.Values.Where(nd => nd.AttributesString.Where(att => new Regex(search, RegexOptions.IgnoreCase).IsMatch(att)).Any() || new Regex(search, RegexOptions.IgnoreCase).IsMatch(nd.Name) && !nd.Mastery).ToList();
                    DrawHighlights(_highlightnodes);
                }
                catch (Exception)
                {
                }

            }
            else
            {
                _highlightnodes = Skillnodes.Values.Where(nd => nd.AttributesString.Count(att => att.ToLower().Contains(search.ToLower())) != 0 || nd.Name.ToLower().Contains(search.ToLower()) && !nd.Mastery).ToList();

                DrawHighlights(_highlightnodes);
            }
        }
        public void SkillAllHighligtedNodes()
        {
            if (_highlightnodes == null)
                return;
            var nodes = new HashSet<int>();
            foreach (var nd in _highlightnodes)
            {
                nodes.Add(nd.Id);
            }
            SkillStep(nodes);

        }
        private HashSet<int> SkillStep(HashSet<int> hs)
        {
            var pathes = _highlightnodes.Select(nd => GetShortestPathTo(nd.Id)).ToList();
            pathes.Sort((p1, p2) => p1.Count.CompareTo(p2.Count));
            pathes.RemoveAll(p => p.Count == 0);
            foreach (ushort i in pathes[0])
            {
                hs.Remove(i);
                SkilledNodes.Add(i);
            }
            UpdateAvailNodes();

            return hs.Count == 0 ? hs : SkillStep(hs);
        }

    }


}
