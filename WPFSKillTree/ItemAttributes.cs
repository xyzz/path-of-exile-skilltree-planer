﻿using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
                Gem,
                Belt
            }

            public class Mod
            {
                public static List<Mod> CreateMods(string attribute, ItemClass ic)
                {
                    var mods = new List<Mod>();
                    var values = (from Match match in numberfilter.Matches(attribute) select float.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
                    string at = numberfilter.Replace(attribute, "#");
                    if (at == "+# to all Attributes")
                    {
                        mods.Add(new Mod
                                 {
                            _itemclass = ic,
                            Value = values,
                            Attribute = "+# to Strength"
                        });
                        mods.Add(new Mod
                                 {
                            _itemclass = ic,
                            Value = values,
                            Attribute = "+# to Dexterity"
                        });
                        mods.Add(new Mod
                                 {
                            _itemclass = ic,
                            Value = values,
                            Attribute = "+# to Intelligence"
                        });
                    }        
                    else
                    {
                        mods.Add(new Mod
                                 {
                            _itemclass = ic,
                            Value = values,
                            Attribute = at
                        });
                    }
                    return mods;
                }

                private ItemClass _itemclass;
                public string Attribute;
                public List<float> Value;
                public bool IsLocal
                {
                    get
                    {
                        return ( _itemclass != ItemClass.Amulet && _itemclass != ItemClass.Ring && _itemclass != ItemClass.Belt ) &&
                              (Attribute.Contains("increased Physical Damage") ||
                                Attribute.Contains("Armour") ||
                                Attribute.Contains("Evasion") ||
                                Attribute.Contains("Energy Shield") ||
                                Attribute.Contains("Weapon Class") ||
                                Attribute.Contains("Critical Strike Chance with this Weapon") ||
                                Attribute.Contains("Critical Strike Damage Multiplier with this Weapon")) ||
                               ((_itemclass == ItemClass.MainHand ||_itemclass == ItemClass.OffHand)&&  Attribute.Contains("increased Attack Speed"));
                    }
                }
            }


            public ItemClass Class;
            public string Type;
            public string Name;
            public Dictionary<string, List<float>> Attributes;
            public List<Mod> Mods;
            public List<Item> Gems;

            public Item(ItemClass iClass, RavenJObject val)
            {
                Type = "";
                Attributes = new Dictionary<string, List<float>>();
                Mods = new List<Mod>();
                Class = iClass;
                if (iClass != ItemClass.Gem)
                {
                    Gems = new List<Item>();
                }
                Name = val["typeLine"].Value<string>();
                if (val.ContainsKey("properties"))
                    foreach (RavenJObject obj in (RavenJArray)val["properties"])
                    {
                        var values = new List<float>();
                        string s = "";
                      
                            foreach (RavenJArray jva in (RavenJArray)obj["values"])
                            {
                                s += " "+jva[0].Value<string>() ;
                            }

                            if (s == "") continue;
                        
                        foreach (Match m in numberfilter.Matches(s))
                        {
                            values.Add(m.Value == ""
                                ? float.NaN
                                : float.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture));
                        }
                        string cs = obj["name"].Value<string>() + ": " + (numberfilter.Replace(s, "#"));


                        Attributes.Add(cs, values);
                    }
                if (val.ContainsKey("explicitMods"))
                    foreach (string s in val["explicitMods"].Values<string>())
                    {
                        var mods = Mod.CreateMods(s.Replace("Additional ", ""), Class);
                        Mods.AddRange(mods);
                    }
                if (val.ContainsKey("implicitMods"))
                    foreach (string s in val["implicitMods"].Values<string>())
                    {
                        var mods = Mod.CreateMods(s.Replace("Additional ", ""), Class);
                        Mods.AddRange(mods);
                    }
            }

            static readonly Regex numberfilter = new Regex("[0-9]*\\.?[0-9]+");
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
                                string[] span = { "", "" };
                                var xs = xml.ReadSubtree();
                                xs.ReadToDescendant("span");
                                for (int j = 0; xs.Read(); )
                                {
                                    if (xs.NodeType == XmlNodeType.Text)
                                    {
                                        span[j] = xs.Value.Replace("Additional ", "");
                                        j++;
                                    }
                                }
                                var matches = numberfilter.Matches(span[1]);
                                if (matches != null && matches.Count != 0)
                                {
                                    attrval.AddRange(from Match match in matches select float.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture));
                                    Attributes.Add(span[0] + "#", attrval);
                                }
                            }
                            if (s == "implicitMod" || s == "explicitMod")
                            {
                                var xs = xml.ReadSubtree();
                                xs.ReadToDescendant("span");
                                while (xs.Read())
                                {
                                    if (xs.NodeType == XmlNodeType.Text)
                                    {
                                        var mods = Mod.CreateMods(xs.Value.Replace("Additional ", ""), Class);
                                        Mods.AddRange(mods);
                                    }
                                }

                            }

                        }
                    }
                }
                return this;

            }
        }

        readonly List<Item> _equip = new List<Item>();

        public ListCollectionView Attributes;
        private readonly List<Attribute> aList = new List<Attribute>();
        public List<Attribute> NonLocalMods = new List<Attribute>();
        public class Attribute : INotifyPropertyChanged
        {
            public Attribute(string s, IEnumerable<float> val, string grp)
            {
                attribute = s;
                value = new List<float>(val);
                group = grp;
            }

            readonly Regex backreplace = new Regex("#");
            private string InsertNumbersInAttributes(string s, IEnumerable<float> attrib)
            {
                return attrib.Aggregate(s, (current, f) => backreplace.Replace(current, f + "", 1));
            }

            private readonly string attribute;
            private readonly List<float> value;
            private readonly string group;
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
        private void AddItem(RavenJObject val, Item.ItemClass iclass)
        {

            Item item = null;

            item = new Item(iclass, val);




            _equip.Add(item);
        }
        public ItemAttributes(string path)
        {
            #region Readin
            RavenJObject jObject = RavenJObject.Parse(File.ReadAllText(path));
            foreach (RavenJObject jobj in (RavenJArray)jObject["items"])
            {
                string html = jobj["x"].Value<string>();
                //html =
                //   html.Replace("\\\"", "\"").Replace("\\/", "/").Replace("\\n", " ").Replace("\\t", " ").Replace(
                //     "\\r", "").Replace("e\"", "e\" ").Replace("\"style", "\" style");
                string id = jobj["inventoryId"].Value<string>();
                if (id == "BodyArmour")
                {
                    AddItem(jobj, Item.ItemClass.Armor);
                }
                if (id == "Ring" || id == "Ring2")
                {
                    AddItem(jobj, Item.ItemClass.Ring);
                }
                if (id == "Gloves")
                {
                    AddItem(jobj, Item.ItemClass.Gloves);
                }
                if (id == "Weapon")
                {
                    AddItem(jobj, Item.ItemClass.MainHand);
                }
                if (id == "Offhand")
                {
                    AddItem(jobj, Item.ItemClass.OffHand);
                }
                if (id == "Helm")
                {
                    AddItem(jobj, Item.ItemClass.Helm);
                }
                if (id == "Boots")
                {
                    AddItem(jobj, Item.ItemClass.Boots);
                }
                if (id == "Amulet")
                {
                    AddItem(jobj, Item.ItemClass.Amulet);
                }
                if ( id == "Belt" )
                {
                    AddItem( jobj , Item.ItemClass.Belt );
                }
                


            }
            #endregion
            aList.Clear();
            NonLocalMods.Clear();
            Attributes = new ListCollectionView(aList);
            foreach (Item item in _equip)
            {
                foreach (KeyValuePair<string, List<float>> attr in item.Attributes)
                {
                    if (attr.Key == "Quality: #") continue;
                    aList.Add(new Attribute(attr.Key, attr.Value, item.Class.ToString()));
                }

                foreach (Item.Mod mod in item.Mods)
                {
                    Attribute attTo = null;
                    attTo = aList.Find(ad => ad.TextAttribute == mod.Attribute && ad.Group == (mod.IsLocal ? item.Class.ToString() : "Independent"));
                    if (attTo == null)
                    {
                        aList.Add(new Attribute(mod.Attribute, mod.Value, (mod.IsLocal ? item.Class.ToString() : "Independent")));
                    }
                    else
                    {
                        attTo.Add(mod.Attribute, mod.Value);
                    }

                }


                foreach (KeyValuePair<string, List<float>> attr in item.Attributes)
                {
                    if (attr.Key == "Quality: +#%") continue;
                    if (attr.Key == "Attacks per Second: #") continue;
                    if (attr.Key == "Critical Strike Chance: #%") continue;
                    if (attr.Key.ToLower().Contains("damage")) continue;
                    if (attr.Key.Contains("Weapon Class")) continue;
                    if (attr.Key.Contains("Elemental Damage")) continue;
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
                    if (mod.IsLocal) continue;
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
