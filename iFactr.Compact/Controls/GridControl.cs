using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using iFactr.UI;
using iFactr.UI.Controls;
using Color = iFactr.UI.Color;
using Control = System.Windows.Forms.Control;
using Size = iFactr.UI.Size;

namespace iFactr.Compact
{
    public class GridControl : Control, IGridBase, INotifyPropertyChanged
    {
        public GridControl()
        {
            Columns = new ColumnCollection();
            Rows = new RowCollection();
            TabStop = false;
        }

        private bool _forceLayout;

        public ColumnCollection Columns
        {
            get;
            private set;
        }

        public RowCollection Rows
        {
            get;
            private set;
        }

        public IEnumerable<IElement> Children
        {
            get
            {
                return _children;
            }
        }

        private readonly List<IElement> _children = new List<IElement>();

        public void AddChild(IElement element)
        {
            _children.Add(element);

            if (element is Control || element.Pair is Control)
            {
                var control = CompactFactory.GetNativeObject<Control>(element, "element");
                if (control != null)
                {
                    control.Parent = this;
                }
            }
            else if (element is IPaintable || element.Pair is IPaintable)
            {
                var paint = CompactFactory.GetNativeObject<IPaintable>(element, "element");
                if (paint != null)
                {
                    paint.SetParent(this);
                }
            }
            _forceLayout = true;
        }

        public void RemoveChild(IElement element)
        {
            _children.Remove(element);

            if (element is Control || element.Pair is Control)
            {
                var control = CompactFactory.GetNativeObject<Control>(element, "element");
                if (control != null)
                {
                    control.Parent = null;
                }
            }

            _forceLayout = true;
        }

        internal void Redraw()
        {
            _forceLayout = true;
            if (CellBitmap != null)
            {
                var bit = CellBitmap;
                Bitmaps.Remove(this);
                bit.Dispose();
                mem -= Width * Height * 4;
            }
            Invalidate();
        }

        private void Draw(Graphics grp)
        {
            foreach (var paint in Children
                .Select(element => element is IPaintable ||
                        element.Pair is IPaintable ? CompactFactory.GetNativeObject<IPaintable>(element, "element") : null)
                .Where(paint => paint != null))
            {
                paint.Paint(grp);
            }
        }

        public virtual void SetBackground(Color color)
        {
            BackgroundHexCode = color.HexCode;
            Redraw();
        }

        protected string BackgroundHexCode;

        public virtual void SetBackground(string imagePath, ContentStretch stretch)
        {
            BackgroundHexCode = null;
            Backer = new ImageControl { Size = Size.ToSize(), Stretch = stretch, };
            Backer.Loaded += (o, ev) => Redraw();
            Backer.FilePath = imagePath;
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_forceLayout)
            {
                var minSize = new Size(MinWidth, MinHeight);
                var maxSize = new Size(MaxWidth, MaxHeight);
                var size = this.PerformLayout(minSize, maxSize).ToSize();
                _forceLayout = false;

                if (size != Size)
                {
                    Size = size;
                    _width = size.Width;
                    return;
                }
            }

            var backgroundView = Parent.Parent as SmoothListbox;
            if (backgroundView == null || backgroundView.BackgroundBitmap == null)
            {
                if (InitializeBitmap())
                {
                    using (var g = Graphics.FromImage(CellBitmap))
                    {
                        if (BackgroundHexCode != null)
                        {
                            g.Clear(new Color(BackgroundHexCode).ToColor());
                        }
                        else if (Backer != null)
                        {
                            Backer.Paint(g);
                        }
                        else
                        {
                            g.Clear(CompactFactory.Instance.Style.LayerItemBackgroundColor.ToColor());
                        }
                        Draw(g);
                    }
                }

                if (CellBitmap != null)
                {
                    e.Graphics.DrawImage(CellBitmap, 0, 0);
                }
            }
            else
            {
                //Transparent cells
                var rect = new Rectangle(0, 0, Width, Height);
                if (backgroundView.BackgroundBitmap != null)
                {
                    var backgroundRect = new Rectangle(0, Top + backgroundView.Controls[0].Top, Width, Height);
                    e.Graphics.DrawImage(backgroundView.BackgroundBitmap, rect, backgroundRect, GraphicsUnit.Pixel);
                }

                if (InitializeBitmap())
                {
                    _imageAttr = new ImageAttributes();
                    _imageAttr.SetColorKey(System.Drawing.Color.Transparent, System.Drawing.Color.Transparent);

                    using (var grp = Graphics.FromImage(CellBitmap))
                    {
                        grp.Clear(System.Drawing.Color.Transparent);
                        Draw(grp);
                    }
                }
                e.Graphics.DrawImage(CellBitmap, rect, 0, 0, Width, Height, GraphicsUnit.Pixel, _imageAttr);
            }
        }

        public virtual double MinWidth
        {
            get;
            set;
        }

        public virtual double MinHeight
        {
            get;
            set;
        }

        public virtual double MaxWidth
        {
            get { return _maxWidth; }
            set { _maxWidth = value; }
        }
        private double _maxWidth = double.PositiveInfinity;

        public virtual double MaxHeight
        {
            get { return _maxHeight; }
            set { _maxHeight = value; }
        }
        private double _maxHeight = double.PositiveInfinity;

        private bool InitializeBitmap()
        {
            if (!_forceLayout && CellBitmap != null && CellBitmap.Width == Width && CellBitmap.Height == Height)
            {
                return false;
            }

            if (CellBitmap != null)
            {
                mem -= CellBitmap.Width * CellBitmap.Height * 4;
                CellBitmap.Dispose();
            }

            //Peg to 10MB
            const int megs = 10;
            if (mem + (Width * Height * 4) > megs * 1024 * 1024)
            {
                // decimate memory usage
                while (mem + (Width * Height * 4) > 1024 * 1024)
                {
                    var bit = Bitmaps.FirstOrDefault();
                    if (bit.Value == null) break;
                    mem -= bit.Value.Width * bit.Value.Height * 4;
                    Bitmaps.Remove(bit.Key);
                    bit.Value.Dispose();
                }
            }

            try
            {
                Bitmaps[this] = new Bitmap(Width, Height);
                mem += Width * Height * 4;
            }
            catch (OutOfMemoryException)
            {
                return false;
            }
            return true;
        }

        internal static int mem;

        internal static readonly Dictionary<GridControl, Bitmap> Bitmaps = new Dictionary<GridControl, Bitmap>();

        internal Bitmap CellBitmap
        {
            get { return Bitmaps.GetValueOrDefault(this, null); }
        }
        private ImageAttributes _imageAttr;
        internal ImageControl Backer;

        private int _width;
        protected override void OnResize(EventArgs e)
        {
            var t = CompactFactory.MetricStopwatch.ElapsedTicks;
            if (Parent == null || Width == _width) return;
            var minSize = new Size(MinWidth, MinHeight);
            var maxSize = new Size(MaxWidth, MaxHeight);
            var size = this.PerformLayout(minSize, maxSize);

            if (Backer != null && Backer.Size != size)
            {
                Backer.Size = size;
                if (CellBitmap != null)
                {
                    var bit = CellBitmap;
                    Bitmaps.Remove(this);
                    bit.Dispose();
                    mem -= Width * Height * 4;
                }
            }

            if (size.ToSize() == Size)
            {
                Debug.WriteLine(string.Format("Unnecessary resize at {0}", size));
                return;
            }
            Debug.WriteLine(string.Format("Resize {0} -> {1}", Size.ToSize(), size));

            if (Size.Height != (int)size.Height)
            {
                Size = size.ToSize();
                if (Parent != null && Parent.Parent is SmoothListbox)
                {
                    ((SmoothListbox)Parent.Parent).LayoutItems(this);
                }
            }
            else
            {
                Size = size.ToSize();
            }

            _width = Size.Width;

            Debug.WriteLineIf(CompactFactory.MetricStopwatch.IsRunning, string.Format("[{1}] Grid resize took {0}ms", new TimeSpan(CompactFactory.MetricStopwatch.ElapsedTicks - t).TotalMilliseconds, CompactFactory.MetricStopwatch.ElapsedMilliseconds));
        }

        public MetadataCollection Metadata
        {
            get { return _metadata ?? (_metadata = new MetadataCollection()); }
        }
        private MetadataCollection _metadata;

        public Thickness Padding { get; set; }

        #region Submission

        private IDictionary<string, string> GetSubmissions()
        {
            var list = Parent.Parent as IListView;
            if (list != null)
                return list.GetSubmissionValues();

            var grid = Parent.Parent as IGridView;
            return grid == null ? null : grid.GetSubmissionValues();
        }

        internal void SetSubmission(string id, string value)
        {
            if (id == null || Parent == null) return;
            var values = GetSubmissions();
            if (values != null) values[id] = value;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void Dispose(bool disposing)
        {
            if (CellBitmap != null)
            {
                var bit = CellBitmap;
                Bitmaps.Remove(this);
                mem -= bit.Width * bit.Height * 4;
                bit.Dispose();
            }
            if (Backer != null)
            {
                Backer.Dispose();
                Backer = null;
            }
            base.Dispose(disposing);
        }
    }
}