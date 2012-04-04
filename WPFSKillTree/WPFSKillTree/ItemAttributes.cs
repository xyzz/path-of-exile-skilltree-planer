using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using System.Xml;
using System.Text.RegularExpressions;
using Raven.Json.Linq;

namespace POESKillTree
{
    class ItemAttributes
    {
        public class Item
        {
            public enum ItemClass
            {
                Armor,
                MainHand,
                OffHand,
                Ring,
                Amulet,
                Helm,
                Gloves,
                Boots,
                Gem
            }

            public class Mod
            {
                enum ValueType
                {
                    Flat, Percentage, FlatMinMax
                }
                public static List<Mod> CreateMods(string attribute, ItemClass ic)
                {
                    List<Mod> mods = new List<Mod>();
                    List<float> values = new List<float>();
                    foreach (Match match in numberfilter.Matches(attribute))
                    {
                        values.Add(float.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture));
                    }
                    string at = numberfilter.Replace(attribute, "#");
                    if (at == "+# to all Attributes")
                    {
                        mods.Add(new Mod()
                        {
                            itemclass = ic,
                            Value = values,
                            Attribute = "+# to Strength"
                        });
                        mods.Add(new Mod()
                        {
                            itemclass = ic,
                            Value = values,
                            Attribute = "+# to Dexterity"
                        });
                        mods.Add(new Mod()
                        {
                            itemclass = ic,
                            Value = values,
                            Attribute = "+# to Intelligence"
                        });
                    }
                    else if (at == "#% increased Elemental Damage")
                    {
                        mods.Add(new Mod()
                        {
                            itemclass = ic,
                            Value = values,
                            Attribute = "#% increased Fire Damage"
                        });
                        mods.Add(new Mod()
                        {
                            itemclass = ic,
                            Value = values,
                            Attribute = "#% increased Cold Damage"
                        });
                        mods.Add(new Mod()
                        {
                            itemclass = ic,
                            Value = values,
                            Attribute = "#% increased Cold Damage"
                        });
                       }
                    else if (at == "#% increased Elemental Damage with Weapons")
                    {
                        mods.Add(new Mod()
                        {
                            itemclass = ic,
                            Value = values,
                            Attribute = "#% increased Fire Damage with Weapons"
                        });
                        mods.Add(new Mod()
                        {
                            itemclass = ic,
                            Value = values,
                            Attribute = "#% increased Cold Damage with Weapons"
                        });
                        mods.Add(new Mod()
                        {
                            itemclass = ic,
                            Value = values,
                            Attribute = "#% increased Lightning Damage with Weapons"
                        });
                    }
                    else if (at == "+#% to all Elemental Resistances")
                    {
                        mods.Add(new Mod()
                        {
                            itemclass = ic,
                            Value = values,
                            Attribute = "+#% to Fire Resistance"
                        });
                        mods.Add(new Mod()
                        {
                            itemclass = ic,
                            Value = values,
                            Attribute = "+#% to Cold Resistance"
                        });
                        mods.Add(new Mod()
                        {
                            itemclass = ic,
                            Value = values,
                            Attribute = "+#% to Lightning Resistance"
                        });
                    }
                    else
                    {
                        mods.Add(new Mod()
                        {
                            itemclass = ic,
                            Value = values,
                            Attribute = at
                        });
                    }
                    return mods;
                }

                private ItemClass itemclass;
                public string Attribute;
                public List<float> Value;
                public bool isLocal
                {
                    get
                    {
                        return (itemclass != Item.ItemClass.Amulet && itemclass != Item.ItemClass.Ring) &&
                              (Attribute.Contains("increased Physical Damage") ||
                                Attribute.Contains("Armour") ||
                                Attribute.Contains("Evasion") ||
                                Attribute.Contains("Energy Shield"));
                    }
                }
            }


            public ItemClass Class;
            public string Type;
            public string Name;
            public Dictionary<string, List<float>> Attributes;
            public List<Mod> Mods;
            public List<Item> Gems;

            public Item(ItemClass iClass)
            {
                Type = "";
                Attributes = new Dictionary<string, List<float>>();
                Mods = new List<Mod>();
                Class = iClass;
                if (iClass != ItemClass.Gem)
                {
                    Gems = new List<Item>();
                }
            }
            static Regex numberfilter = new Regex("[0-9]*\\.?[0-9]+");
            public Item XmlRead(XmlReader xml)
            {

                while (xml.Read())
                {
                    if (xml.HasAttributes)
                    {
                        for (int i = 0; i < xml.AttributeCount; i++)
                        {
                            string s = xml.GetAttribute(i);
                            if (s == "socketPopups")
                                return this;
                            if (s.Contains("itemName"))
                            {
                                var xs = xml.ReadSubtree();
                                xs.ReadToDescendant("span");
                                for (int j = 0; xs.Read(); )
                                {
                                    if (xs.NodeType == XmlNodeType.Text)
                                    {
                                        if (j == 0) Name = xs.Value.Replace("Additional ", "");
                                        if (j == 1) Type = xs.Value;
                                        j++;
                                    }
                                }
                            }
                            if (s.Contains("displayProperty"))
                            {
                                List<float> attrval = new List<float>();
                                string[] span = new string[2] { "", "" };
                                var xs = xml.ReadSubtree();
                                xs.ReadToDescendant("span");
                                for (int j = 0; xs.Read(); )
                                {
                                    if (xs.NodeType == XmlNodeType.Text)
                                    {
                                        span[j] = xs.Value.Replace("Additional ", ""); ;
                                        j++;
                                    }
                                }
                                var matches = numberfilter.Matches(span[1]);
                                if (matches != null && matches.Count != 0)
                                {
                                    foreach (Match match in matches)
                                    {
                                        attrval.Add(float.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture));
                                    }
                                    Attributes.Add(span[0] + "#", attrval);
                                }
                            }
                            if (s == "implicitMod" || s == "explicitMod")
                            {
                                string span = "";
                                var xs = xml.ReadSubtree();
                                xs.ReadToDescendant("span");
                                while (xs.Read())
                                {
                                    if (xs.NodeType == XmlNodeType.Text)
                                    {
                                        var mods = Mod.CreateMods(xs.Value.Replace("Additional ", ""), this.Class);
                                        Mods.AddRange(mods );
                                    }
                                }

                            }

                        }
                    }
                }
                return this;

            }
        }

        List<Item> Equip = new List<Item>();

        private Dictionary<string, List<float>> AgregatedAttributes;
        public ListCollectionView Attributes;
        private List<Attribute> aList = new List<Attribute>();
        public List<Attribute> NonLocalMods = new List<Attribute>();
        public class Attribute : INotifyPropertyChanged
        {
            public Attribute(string s, List<float> val, string grp)
            {
                attribute = s;
                value = new List<float>(val);
                group = grp;
            }

            Regex backreplace = new Regex("#");
            private string InsertNumbersInAttributes(string s, List<float> attrib)
            {
                foreach (var f in attrib)
                {
                    s = backreplace.Replace(s, f + "", 1);
                }
                return s;
            }
            private string attribute;
            private List<float> value;
            private string group;
            public List<float> Value { get { return value; } }
            public string TextAttribute
            {
                get { return attribute; }
            }

            public string ValuedAttribute
            {
                get { return InsertNumbersInAttributes(attribute, value); }
            }
            public string Group { get { return group; } }
            public bool Add(string s, List<float> val)
            {
                if (attribute != s) return false;
                if (value.Count != val.Count) return false;
                for (int i = 0; i < val.Count; i++)
                {
                    value[i] += val[i];
                }
                OnPropertyChanged("ValuedAttribute");
                return true;
            }

            private void OnPropertyChanged(string info)
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(info));
                }
            }

            /*    public override string ToString()
                {
                    return ValuedAttribute;
                }*/
            public event PropertyChangedEventHandler PropertyChanged;
        }
        private void AddItem(string val, Item.ItemClass iclass)
        {
            XmlReader xml = XmlReader.Create(new StringReader(val));
            Item item = null;
            while (xml.Read())
            {
                if (xml.HasAttributes)
                {
                    for (int i = 0; i < xml.AttributeCount; i++)
                    {
                        string s = xml.GetAttribute(i);
                        if (s == "itemContainer pFix itemNotInline itemContainerNotVerified")
                        {
                            item = new Item(iclass).XmlRead(xml.ReadSubtree());
                        }
                        if (s == "itemPopupContainer pFix hidden itemGemPopup")
                        {
                            
                            item.Gems.Add(new Item(Item.ItemClass.Gem).XmlRead(xml.ReadSubtree()));
                        }
                    }
                }

            }
            Equip.Add(item);
        }
        public ItemAttributes(string path)
        {
            #region Readin
            RavenJObject jObject = RavenJObject.Parse(File.ReadAllText(path));
            foreach (RavenJObject jobj in (RavenJArray)jObject["items"])
            {
                string html = jobj["html"].Value<string>();
                html =
                    html.Replace("\\\"", "\"").Replace("\\/", "/").Replace("\\n", " ").Replace("\\t", " ").Replace(
                        "\\r", "").Replace("e\"", "e\" ").Replace("\"style", "\" style");
                string id = jobj["inventory_id"].Value<string>();
                if (id == "BodyArmour")
                {
                    AddItem(html, Item.ItemClass.Armor);
                }
                if (id == "Ring" || id == "Ring2")
                {
                  AddItem(html, Item.ItemClass.Ring);
                }
                if (id == "Gloves")
                {
                   AddItem(html, Item.ItemClass.Gloves);
                }
                if (id == "Weapon")
                {
                   AddItem(html, Item.ItemClass.MainHand);
                }
                if (id == "Offhand")
                {
                   AddItem(html, Item.ItemClass.OffHand);
                }
                if (id == "Helm")
                {
                   AddItem(html, Item.ItemClass.Helm);
                }
                if (id == "Boots")
                {
                   AddItem(html, Item.ItemClass.Boots);
                }
                if (id == "Amulet")
                {
                    AddItem(html, Item.ItemClass.Amulet);
                }


            }
            #endregion
            aList.Clear();
            NonLocalMods.Clear();
            Attributes = new ListCollectionView(aList);
            foreach (Item item in Equip)
            {
                foreach (KeyValuePair<string, List<float>> attr in item.Attributes)
                {
                    if (attr.Key == "Quality: #") continue;
                    aList.Add(new Attribute(attr.Key, attr.Value, item.Class.ToString()));
                }

                foreach (Item.Mod mod in item.Mods)
                {
                    Attribute attTo = null;
                    attTo = aList.Find(ad => ad.TextAttribute == mod.Attribute && ad.Group == (mod.isLocal ? item.Class.ToString() : "Independent"));
                    if (attTo == null)
                    {
                        aList.Add(new Attribute(mod.Attribute, mod.Value, (mod.isLocal ? item.Class.ToString() : "Independent")));
                    }
                    else
                    {
                        attTo.Add(mod.Attribute, mod.Value);
                    }

                }


                foreach (KeyValuePair<string, List<float>> attr in item.Attributes)
                {
                    if (attr.Key == "Quality: #") continue;
                    if (attr.Key == "Attacks per Second: #") continue;
                    if (attr.Key == "Critical Strike Chance: #") continue;
                    if (attr.Key.ToLower().Contains("damage")) continue;
                    Attribute attTo = null;
                    attTo = NonLocalMods.Find(ad => ad.TextAttribute == attr.Key);
                    if (attTo == null)
                    {
                        NonLocalMods.Add(new Attribute(attr.Key, attr.Value, ""));
                    }
                    else
                    {
                        attTo.Add(attr.Key, attr.Value);
                    }
                }

                foreach (Item.Mod mod in item.Mods)
                {
                    if (mod.isLocal) continue;
                    Attribute attTo = null;
                    attTo = NonLocalMods.Find(ad => ad.TextAttribute == mod.Attribute);
                    if (attTo == null)
                    {
                        NonLocalMods.Add(new Attribute(mod.Attribute, mod.Value, ""));
                    }
                    else
                    {
                        attTo.Add(mod.Attribute, mod.Value);
                    }

                }

            }



            PropertyGroupDescription pgd = new PropertyGroupDescription("");
            pgd.PropertyName = "Group";
            Attributes.GroupDescriptions.Add(pgd);
            Attributes.CustomSort = new NumberLessStringComparer();


            Binding itemsBinding = new Binding();

            Attributes.Refresh();

        }
        public class NumberLessStringComparer : System.Collections.IComparer
        {
            static Regex numberfilter = new Regex("[0-9]*\\.?[0-9]+");

            public int Compare(string x, string y)
            {
                return numberfilter.Replace(x, "").CompareTo(numberfilter.Replace(y, ""));
            }

            public int Compare(object x, object y)
            {
                if (x is Attribute && y is Attribute)
                {
                    if (((Attribute)x).Group == "Independent" && !(((Attribute)y).Group == "Independent")) return +1;
                    if (((Attribute)y).Group == "Independent" && !(((Attribute)x).Group == "Independent")) return -1;
                    return numberfilter.Replace(((Attribute)y).Group, "").CompareTo(numberfilter.Replace(((Attribute)y).Group, ""));
                }
                return 0;
            }
        }
    }
}
