using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace POESKillTree
{
    public partial class SkillTree
    {
        #region Members
        public DrawingVisual PicSkillIconLayer;
        public DrawingVisual PicSkillSurround;
        public DrawingVisual PicLinks;
        public DrawingVisual PicActiveLinks;
        public DrawingVisual PicPathOverlay;
        public DrawingVisual PicBackground;
        public DrawingVisual PicFaces;
        public DrawingVisual PicHighlights;
        public DrawingVisual PicSkillBaseSurround;
        public DrawingVisual SkillTreeVisual;

        public Dictionary<bool, KeyValuePair<Rect, ImageBrush>> StartBackgrounds = new Dictionary<bool, KeyValuePair<Rect, ImageBrush>>();
        public List<KeyValuePair<Size, ImageBrush>> NodeSurroundBrush = new List<KeyValuePair<Size, ImageBrush>>();
        public List<KeyValuePair<Rect, ImageBrush>> FacesBrush = new List<KeyValuePair<Rect, ImageBrush>>();

        public void CreateCombineVisual()
        {
            SkillTreeVisual = new DrawingVisual();
            SkillTreeVisual.Children.Add(PicBackground);
            SkillTreeVisual.Children.Add(PicLinks);
            SkillTreeVisual.Children.Add(PicActiveLinks);
            SkillTreeVisual.Children.Add(PicPathOverlay);
            SkillTreeVisual.Children.Add(PicSkillIconLayer);
            SkillTreeVisual.Children.Add(PicSkillBaseSurround);
            SkillTreeVisual.Children.Add(PicSkillSurround);
            SkillTreeVisual.Children.Add(PicFaces);
            SkillTreeVisual.Children.Add(PicHighlights);
        }
        #endregion
        private void InitOtherDynamicLayers()
        {
            PicActiveLinks = new DrawingVisual();
            PicPathOverlay = new DrawingVisual();
            PicHighlights = new DrawingVisual();
        }
        private void DrawBackgroundLayer()
        {
            PicBackground = new DrawingVisual();
            using (DrawingContext dc = PicBackground.RenderOpen())
            {
                BitmapImage[] iscr =
                {
                    _assets["PSGroupBackground1"].PImage, _assets["PSGroupBackground2"].PImage,
                    _assets["PSGroupBackground3"].PImage
                };
                var orbitBrush = new Brush[3];
                orbitBrush[0] = new ImageBrush(_assets["PSGroupBackground1"].PImage);
                orbitBrush[1] = new ImageBrush(_assets["PSGroupBackground2"].PImage);
                orbitBrush[2] = new ImageBrush(_assets["PSGroupBackground3"].PImage);
                (orbitBrush[2] as ImageBrush).TileMode = TileMode.FlipXY;
                (orbitBrush[2] as ImageBrush).Viewport = new Rect(0, 0, 1, .5f);

                var backgroundBrush = new ImageBrush(_assets["Background1"].PImage) {TileMode = TileMode.FlipXY};
                dc.DrawRectangle(backgroundBrush, null, Rect);
                foreach (var ngp in NodeGroups)
                {
                    if (ngp.OcpOrb == null)
                        ngp.OcpOrb = new Dictionary<int, bool>();
                    var cgrp = ngp.OcpOrb.Keys.Where(ng => ng <= 3);
                    if (!cgrp.Any()) continue;
                    int maxr = cgrp.Max( ng => ng );
                    if (maxr == 0) continue;
                    maxr = maxr > 3 ? 2 : maxr - 1;
                    int maxfac = maxr == 2 ? 2 : 1;
                    dc.DrawRectangle(orbitBrush[maxr], null,
                                     new Rect(
                                         ngp.Position - new Vector2D(iscr[maxr].PixelWidth*1.5, iscr[maxr].PixelHeight*1.5 * maxfac),
                                         new Size(iscr[maxr].PixelWidth * 3, iscr[maxr].PixelHeight * 3 * maxfac)));
                  
                }
            }
        }
        private void InitFaceBrushesAndLayer()
        {
            foreach (string faceName in FaceNames)
            {
                var bi = new BitmapImage(new Uri("Data\\Assets\\" + faceName + ".png", UriKind.Relative));
                FacesBrush.Add(new KeyValuePair<Rect, ImageBrush>(new Rect(0, 0, bi.PixelWidth, bi.PixelHeight),
                                                                  new ImageBrush(bi)));
            }

            var bi2 = new BitmapImage(new Uri("Data\\Assets\\PSStartNodeBackgroundInactive.png", UriKind.Relative));
            StartBackgrounds.Add(false,
                                 (new KeyValuePair<Rect, ImageBrush>(new Rect(0, 0, bi2.PixelWidth, bi2.PixelHeight),
                                                                     new ImageBrush(bi2))));
            PicFaces = new DrawingVisual();

        }
        private void DrawLinkBackgroundLayer(IEnumerable<ushort[]> links)
        {
            PicLinks = new DrawingVisual();
            var pen2 = new Pen(Brushes.DarkSlateGray, 32);
            using (DrawingContext dc = PicLinks.RenderOpen())
            {
                foreach (var nid in links)
                {
                    var n1 = Skillnodes[nid[0]];
                    var n2 = Skillnodes[nid[1]];
                    DrawConnection(dc, pen2, n1, n2);
                    //if (n1.NodeGroup == n2.NodeGroup && n1.orbit == n2.orbit)
                    //{
                    //    if (n1.Arc - n2.Arc > 0 && n1.Arc - n2.Arc < Math.PI || n1.Arc - n2.Arc < -Math.PI)
                    //    {
                    //        dc.DrawArc(null, pen2, n1.Position, n2.Position,
                    //                   new Size(SkillTree.SkillNode.orbitRadii[n1.orbit],
                    //                            SkillTree.SkillNode.orbitRadii[n1.orbit]));
                    //    }
                    //    else
                    //    {
                    //        dc.DrawArc(null, pen2, n2.Position, n1.Position,
                    //                   new Size(SkillTree.SkillNode.orbitRadii[n1.orbit],
                    //                            SkillTree.SkillNode.orbitRadii[n1.orbit]));
                    //    }
                    //}
                    //else
                    //{
                    //    dc.DrawLine(pen2, n1.Position, n2.Position);
                    //}
                }
            }
        }
        private void DrawSkillIconLayer()
        {
            var pen = new Pen(Brushes.Black, 5);
            PicSkillIconLayer = new DrawingVisual();

            Geometry g = new RectangleGeometry(Rect);
            using (DrawingContext dc = PicSkillIconLayer.RenderOpen())
            {
                dc.DrawGeometry(null, pen, g);
                foreach (var skillNode in Skillnodes)
                {
                    var br = new ImageBrush();
                    Rect r = IconActiveSkills.SkillPositions[skillNode.Value.Icon].Key;
                    BitmapImage bi = IconActiveSkills.Images[IconActiveSkills.SkillPositions[skillNode.Value.Icon].Value];
                    br.Stretch = Stretch.Uniform;
                    br.ImageSource = bi;

                    br.ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
                    br.Viewbox = new Rect(r.X / bi.PixelWidth, r.Y / bi.PixelHeight, r.Width / bi.PixelWidth, r.Height / bi.PixelHeight);
                    Vector2D pos = (skillNode.Value.Position);
                    dc.DrawEllipse(br, null, pos, r.Width, r.Height);
                }
            }

        }
        private void InitNodeSurround()
        {
            PicSkillSurround = new DrawingVisual();
            PicSkillBaseSurround = new DrawingVisual();
            var brNot = new ImageBrush {Stretch = Stretch.Uniform};
            BitmapImage pImageNot = _assets[NodeBackgrounds["notable"]].PImage;
            brNot.ImageSource = pImageNot;
            var sizeNot = new Size(pImageNot.PixelWidth, pImageNot.PixelHeight);


            var brKs = new ImageBrush {Stretch = Stretch.Uniform};
            BitmapImage pImageKr = _assets[NodeBackgrounds["keystone"]].PImage;
            brKs.ImageSource = pImageKr;
            var sizeKs = new Size(pImageKr.PixelWidth, pImageKr.PixelHeight);

            var brNotH = new ImageBrush {Stretch = Stretch.Uniform};
            BitmapImage pImageNotH = _assets[NodeBackgroundsActive["notable"]].PImage;
            brNotH.ImageSource = pImageNotH;
            var sizeNotH = new Size(pImageNotH.PixelWidth, pImageNotH.PixelHeight);


            var brKsh = new ImageBrush {Stretch = Stretch.Uniform};
            BitmapImage pImageKrH = _assets[NodeBackgroundsActive["keystone"]].PImage;
            brKsh.ImageSource = pImageKrH;
            var sizeKsH = new Size(pImageKrH.PixelWidth, pImageKrH.PixelHeight);

            var brNorm = new ImageBrush {Stretch = Stretch.Uniform};
            BitmapImage pImageNorm = _assets[NodeBackgrounds["normal"]].PImage;
            brNorm.ImageSource = pImageNorm;
            var isizeNorm = new Size(pImageNorm.PixelWidth, pImageNorm.PixelHeight);

            var brNormA = new ImageBrush {Stretch = Stretch.Uniform};
            BitmapImage pImageNormA = _assets[NodeBackgroundsActive["normal"]].PImage;
            brNormA.ImageSource = pImageNormA;
            var isizeNormA = new Size(pImageNormA.PixelWidth, pImageNormA.PixelHeight);

            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(isizeNorm, brNorm));
            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(isizeNormA, brNormA));
            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(sizeKs, brKs));
            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(sizeNot, brNot));
            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(sizeKsH, brKsh));
            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(sizeNotH, brNotH));
        }
        private void DrawNodeBaseSurround()
        {
            using (DrawingContext dc = PicSkillBaseSurround.RenderOpen())
            {

                foreach (var skillNode in Skillnodes.Keys)
                {
                    Vector2D pos = (Skillnodes[skillNode].Position);

                    if (Skillnodes[skillNode].Not)
                    {
                        dc.DrawRectangle(NodeSurroundBrush[3].Value, null,
                                         new Rect((int)pos.X - NodeSurroundBrush[3].Key.Width,
                                                  (int)pos.Y - NodeSurroundBrush[3].Key.Height,
                                                  NodeSurroundBrush[3].Key.Width * 2,
                                                  NodeSurroundBrush[3].Key.Height * 2));
                    }
                    else if (Skillnodes[skillNode].Ks)
                    {
                        dc.DrawRectangle(NodeSurroundBrush[2].Value, null,
                                         new Rect((int)pos.X - NodeSurroundBrush[2].Key.Width,
                                                  (int)pos.Y - NodeSurroundBrush[2].Key.Height,
                                                  NodeSurroundBrush[2].Key.Width * 2,
                                                  NodeSurroundBrush[2].Key.Height * 2));
                    }
                    else
                        dc.DrawRectangle(NodeSurroundBrush[0].Value, null,
                                        new Rect((int)pos.X - NodeSurroundBrush[0].Key.Width,
                                                 (int)pos.Y - NodeSurroundBrush[0].Key.Height,
                                                 NodeSurroundBrush[0].Key.Width * 2,
                                                 NodeSurroundBrush[0].Key.Height * 2));
                }
            }
        }
        private void DrawNodeSurround()
        {
            using (DrawingContext dc = PicSkillSurround.RenderOpen())
            {

                foreach (var skillNode in SkilledNodes)
                {
                    Vector2D pos = (Skillnodes[skillNode].Position);

                    if (Skillnodes[skillNode].Not)
                    {
                        dc.DrawRectangle(NodeSurroundBrush[5].Value, null,
                                         new Rect((int)pos.X - NodeSurroundBrush[5].Key.Width,
                                                  (int)pos.Y - NodeSurroundBrush[5].Key.Height,
                                                  NodeSurroundBrush[5].Key.Width * 2,
                                                  NodeSurroundBrush[5].Key.Height * 2));
                    }
                    else if (Skillnodes[skillNode].Ks)
                    {
                        dc.DrawRectangle(NodeSurroundBrush[4].Value, null,
                                         new Rect((int)pos.X - NodeSurroundBrush[4].Key.Width,
                                                  (int)pos.Y - NodeSurroundBrush[4].Key.Height,
                                                  NodeSurroundBrush[4].Key.Width * 2,
                                                  NodeSurroundBrush[4].Key.Height * 2));
                    }
                    else
                        dc.DrawRectangle(NodeSurroundBrush[1].Value, null,
                                        new Rect((int)pos.X - NodeSurroundBrush[1].Key.Width,
                                                 (int)pos.Y - NodeSurroundBrush[1].Key.Height,
                                                 NodeSurroundBrush[1].Key.Width * 2,
                                                 NodeSurroundBrush[1].Key.Height * 2));

                }
            }
        }
        public void DrawHighlights(List<SkillNode> nodes)
        {
            var hpen = new Pen(Brushes.Aqua, 20);
            using (var dc = PicHighlights.RenderOpen())
            {
                foreach (var node in nodes)
                {
                    dc.DrawEllipse(null, hpen, node.Position, 80, 80);
                }
            }
        }
        private void DrawConnection(DrawingContext dc, Pen pen2, SkillNode n1, SkillNode n2)
        {
            if (n1.NodeGroup == n2.NodeGroup && n1.Orbit == n2.Orbit)
            {
                if (n1.Arc - n2.Arc > 0 && n1.Arc - n2.Arc <= Math.PI ||
                    n1.Arc - n2.Arc < -Math.PI)
                {
                    dc.DrawArc(null, pen2, n1.Position, n2.Position,
                               new Size(SkillNode.OrbitRadii[n1.Orbit],
                                        SkillNode.OrbitRadii[n1.Orbit]));
                }
                else
                {
                    dc.DrawArc(null, pen2, n2.Position, n1.Position,
                               new Size(SkillNode.OrbitRadii[n1.Orbit],
                                        SkillNode.OrbitRadii[n1.Orbit]));
                }
            }
            else
            {
                dc.DrawLine(pen2, n1.Position, n2.Position);
            }
        }
        public void DrawFaces()
        {
            using (DrawingContext dc = PicFaces.RenderOpen())
            {
                for (int i = 0; i < CharName.Count; i++)
                {
                    var s = CharName[i];
                    var pos = Skillnodes.First(nd => nd.Value.Name.ToUpper() == s.ToUpper()).Value.Position;
                    dc.DrawRectangle(StartBackgrounds[false].Value, null, new Rect(pos - new Vector2D(StartBackgrounds[false].Key.Width, StartBackgrounds[false].Key.Height), pos + new Vector2D(StartBackgrounds[false].Key.Width, StartBackgrounds[false].Key.Height)));
                    if (_chartype==i)
                    {
                        dc.DrawRectangle(FacesBrush[i].Value, null, new Rect(pos - new Vector2D(FacesBrush[i].Key.Width, FacesBrush[i].Key.Height), pos + new Vector2D(FacesBrush[i].Key.Width, FacesBrush[i].Key.Height)));
                        
                    }
                }
            }
        }
        public void DrawPath(List<ushort> path)
        {
            var pen2 = new Pen(Brushes.LawnGreen, 15f) {DashStyle = new DashStyle(new DoubleCollection {2}, 2)};

            using (DrawingContext dc = PicPathOverlay.RenderOpen())
            {
                for (int i = -1; i < path.Count - 1; i++)
                {
                    SkillNode n1 = i == -1 ? Skillnodes[path[i + 1]].Neighbor.First(sn => SkilledNodes.Contains(sn.Id)) : Skillnodes[path[i]];
                    SkillNode n2 = Skillnodes[path[i + 1]];

                    DrawConnection(dc, pen2, n1, n2);
                }
            }
        }
        public void DrawRefundPreview(HashSet<ushort> nodes)
        {
            var pen2 = new Pen(Brushes.Red, 15f) {DashStyle = new DashStyle(new DoubleCollection {2}, 2)};

            using (DrawingContext dc = PicPathOverlay.RenderOpen())
            {
                foreach (ushort node in nodes)
                {
                    foreach (SkillNode n2 in Skillnodes[node].Neighbor)
                    {
                        if (SkilledNodes.Contains(n2.Id) && (node < n2.Id || !(nodes.Contains(n2.Id))))
                            DrawConnection(dc, pen2, Skillnodes[node], n2);
                    }
                }
            }

        }
        public void ClearPath()
        {
            PicPathOverlay.RenderOpen().Close();
        }
    }
}
