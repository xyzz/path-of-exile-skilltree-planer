using System.Collections.Generic;
using Newtonsoft.Json;

namespace POESKillTree
{
   
    internal class Character
    {
        
        public int BaseStr { get; set; }
        
        public int BaseDex { get; set; }
        
        public int BaseInt { get; set; }
    }

   
    internal class NodeGroup
    {
        
        public double X { get; set; }
        
        public double Y { get; set; }
        public Dictionary<int, bool> Oo { get; set; }
        
        public List<int> N { get; set; }
    }

   
    internal class Main
    {
        
        public int G { get; set; }
        
        public int O { get; set; }
        
        public int Oidx { get; set; }
        
        public int Sa { get; set; }
        
        public int Da { get; set; }
        
        public int Ia { get; set; }

        [JsonProperty("out")]
        public int[] Ot { get; set; }
    }
       
    internal class Node
    {
        
        public ushort Id { get; set; }
        
        public string Icon { get; set; }
        
        public bool Ks { get; set; }
        
        public bool Not { get; set; }
        
        public string Dn { get; set; }
        
        public bool M { get; set; }
        
        public int[] Spc { get; set; }
        
        public string[] Sd { get; set; }
        
        public int G { get; set; }
        
        public int O { get; set; }
        
        public int Oidx { get; set; }
        
        public int Sa { get; set; }
        
        public int Da { get; set; }
        
        public int Ia { get; set; }


        [JsonProperty("out")]
        public List<int> Ot { get; set; }
    }

    internal class Constants
    {
        
        public Dictionary<string, int> Classes { get; set; }
        
        public Dictionary<string, int> CharacterAttributes { get; set; }
        
        public int PssCentreInnerRadius { get; set; }
    }

    internal class Art2D
    {
        
        public int X { get; set; }
        
        public int Y { get; set; }
        
        public int W { get; set; }
        
        public int H { get; set; }
    }

    internal class SkillSprite
    {
        
        public string Filename { get; set; }
        
        public Dictionary<string, Art2D> Coords { get; set; }
    }


    internal class PoESkillTree
    {

        
        public Dictionary<int, Character> CharacterData { get; set; }
        
        public Dictionary<int,NodeGroup> Groups { get; set; }
        
        public Main Main { get; set; }
        
        public Node[] Nodes { get; set; }

        // ReSharper disable once InconsistentNaming
        public int min_x { get; set; }

        // ReSharper disable once InconsistentNaming
        public int min_y { get; set; }

        // ReSharper disable once InconsistentNaming
        public int max_x { get; set; }

        // ReSharper disable once InconsistentNaming        
        public int max_y { get; set; }
        
        public Dictionary<string, Dictionary<float, string>> Assets { get; set; }
        
        public Constants Constants { get; set; }
        
        public string ImageRoot { get; set; }
        
        public Dictionary<string, SkillSprite[]> SkillSprites { get; set; }
        
        public double[] ImageZoomLevels { get; set; }
    }

}
