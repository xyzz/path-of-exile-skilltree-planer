using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace POESKillTree
{
    public class ZoomBorder : Border
    {
        private UIElement _child;
        private Point _origin;
        private Point _start;
        public Point Origin
        {
            get
            {
                var tt = GetTranslateTransform(_child);

                return new Point(tt.X, tt.Y);
            }
        }

        public TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is TranslateTransform);
        }

        public ScaleTransform GetScaleTransform( UIElement element )
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get
            {
                return base.Child;
            }
            set
            {
                if (value != null && value != Child)
                {
                    Initialize(value);
                }

                base.Child = value;
            }
        }

        public void Initialize(UIElement element)
        {

            _child = element;
            if (_child != null)
            {
                var group = new TransformGroup();

                var st = new ScaleTransform();
                group.Children.Add(st);

                var tt = new TranslateTransform();

                group.Children.Add(tt);

                _child.RenderTransform = group;
                _child.RenderTransformOrigin = new Point(0.0, 0.0);

                _child.MouseWheel += ChildMouseWheel;
                _child.MouseLeftButtonDown += ChildMouseLeftButtonDown;
                _child.MouseLeftButtonUp += ChildMouseLeftButtonUp;
                _child.MouseMove += ChildMouseMove;
                _child.PreviewMouseRightButtonDown += ChildPreviewMouseRightButtonDown;
            }
        }

        public void Reset()
        {
            if (_child != null)
            {
                // reset zoom
                var st = GetScaleTransform(_child);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                var tt = GetTranslateTransform(_child);
                tt.X = 0.0;
                tt.Y = 0.0;
            }
        }

        #region Child Events

        private void ChildMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_child != null)
            {
                var st = GetScaleTransform(_child);
                var tt = GetTranslateTransform(_child);

                double zoom = e.Delta > 0 ? .3 : -.3;
                if (!(e.Delta > 0) && (st.ScaleX < 0.4 || st.ScaleY < 0.4))
                    return;

                Point relative = e.GetPosition(_child);

                double absoluteX = relative.X * st.ScaleX + tt.X;
                double absoluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX += zoom * st.ScaleX;
                st.ScaleY += zoom * st.ScaleY;

                tt.X = absoluteX - relative.X * st.ScaleX;
                tt.Y = absoluteY - relative.Y * st.ScaleY;
            }
        }

        private void ChildMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_child != null)
            {
                var tt = GetTranslateTransform(_child);
                _start = e.GetPosition(this);
                _origin = new Point(tt.X, tt.Y);
                Cursor = Cursors.Hand;
                _child.CaptureMouse();
            }
        }

        private void ChildMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_child != null)
            {
                _child.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
            }
        }

        void ChildPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Reset();
        }

        private void ChildMouseMove(object sender, MouseEventArgs e)
        {
            if (_child != null)
            {
                if (_child.IsMouseCaptured)
                {
                    var tt = GetTranslateTransform(_child);
                    Vector v = _start - e.GetPosition(this);
                    tt.X = _origin.X - v.X;
                    tt.Y = _origin.Y - v.Y;
                }
            }
        }


        public static readonly RoutedEvent ClickEvent;



        static ZoomBorder()
        {

            ClickEvent = ButtonBase.ClickEvent.AddOwner(typeof(ZoomBorder));

        }



        public event RoutedEventHandler Click
        {

            add { AddHandler(ClickEvent, value); }

            remove { RemoveHandler(ClickEvent, value); }

        }


        protected override void OnMouseUp(MouseButtonEventArgs e)
        {

            base.OnMouseUp(e);

           // if (IsMouseCaptured)
           // {

                //ReleaseMouseCapture();

                if (IsMouseOver)

                    RaiseEvent(new RoutedEventArgs(ClickEvent, e));

        //    }

        }
        #endregion
    }
}