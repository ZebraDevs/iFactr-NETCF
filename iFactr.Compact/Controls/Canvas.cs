using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using iFactr.Core;
using MonoCross.Utilities;
using iFactr.UI;
using iFactr.UI.Controls;
using Color = iFactr.UI.Color;
using Control = System.Windows.Forms.Control;
using HorizontalAlignment = iFactr.UI.HorizontalAlignment;
using Point = System.Drawing.Point;
using Size = iFactr.UI.Size;

namespace iFactr.Compact
{
    class Canvas : Control, IElement, INotifyPropertyChanged
    {
        public Canvas()
        {
            ColumnIndex = -1;
            RowIndex = -1;
            ColumnSpan = 1;
            RowSpan = 1;
        }

        private bool _backgroundInvalidated;
        private Bitmap _signatureBitmap;
        private Graphics _graphics;
        private Point _lastPoint = Point.Empty;
        private bool _drawPoint = true;

        protected override void OnPaintBackground(PaintEventArgs e) { }

        internal void InvalidateBackground()
        {
            _backgroundInvalidated = true;
        }

        private void PaintBackground(Graphics g)
        {
            if (BackColor == System.Drawing.Color.Transparent)
            {
                ((CanvasView)Parent).Backer.Paint(g);
            }
            else
            {
                g.Clear(BackColor);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // create memory bitmap if don't have one or the size changed
            if (_signatureBitmap == null || _signatureBitmap.Width != Width || _signatureBitmap.Height != Height)
            {
                InitMemoryBitmap();
            }

            if (_backgroundInvalidated)
            {
                PaintBackground(e.Graphics);
                _backgroundInvalidated = false;
            }

            var attr = new ImageAttributes();
            var transKey = System.Drawing.Color.Lime;
            attr.SetColorKey(transKey, transKey);

            var dstRect = new Rectangle(0, 0, (int)Width, (int)Height);
            e.Graphics.DrawImage(_signatureBitmap, dstRect, 0, 0, (int)Width, (int)Height, GraphicsUnit.Pixel, attr);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _lastPoint = new Point(e.X, e.Y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_graphics != null && _drawPoint)
            {
                // draw the new point on the memory bitmap
                var d = (int)_pen.Width;
                using (var b = new SolidBrush(_pen.Color))
                    _graphics.FillEllipse(b, e.X, e.Y, d, d);
                Invalidate();
            }

            _drawPoint = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // process if drawing signature
            if (_lastPoint.X != e.X && _lastPoint.Y != e.Y && _graphics != null)
            {
                _drawPoint = false;

                // draw the new segment on the memory bitmap
                _graphics.DrawLine(_pen, _lastPoint.X, _lastPoint.Y, e.X, e.Y);

                // update the current position
                _lastPoint.X = e.X;
                _lastPoint.Y = e.Y;

                // display the updated bitmap
                Invalidate();
            }
        }

        /// <summary>
        /// Clear the signature.
        /// </summary>
        public void Clear()
        {
            Load(null);
        }

        protected override void Dispose(bool disposing)
        {
            if (_pen != null)
            {
                _pen.Dispose();
                _pen = null;
            }

            if (_graphics != null)
            {
                _graphics.Dispose();
                _graphics = null;
            }

            if (_signatureBitmap != null)
            {
                _signatureBitmap.Dispose();
                _signatureBitmap = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Create a memory bitmap that is used to draw the signature.
        /// </summary>
        private void InitMemoryBitmap()
        {
            _backgroundInvalidated = true;
            if (_signatureBitmap != null)
                _signatureBitmap.Dispose();

            if (_graphics != null)
                _graphics.Dispose();

            _signatureBitmap = new Bitmap(base.Width, base.Height);
            _graphics = Graphics.FromImage(_signatureBitmap);

            if (_canvasFile == null)
            {
                _graphics.Clear(System.Drawing.Color.Lime);
            }
            else
            {
                using (var b = ((BitmapImage)ImageManager.GetBitmapData(_canvasFile, true)).GetBitmap())
                {
                    if (b != null)
                    {
                        // load the foreground image
                        _graphics.DrawImage(b, 0, 0);
                    }
                }
            }
        }

        public new double Height { get { return base.Height; } }
        public new double Width { get { return base.Width; } }

        public void Load(string fileName)
        {
            _canvasFile = fileName;

            if (_signatureBitmap != null)
            {
                _signatureBitmap.Dispose();
                _signatureBitmap = null;
            }

            Invalidate();
        }
        private string _canvasFile;

        public Color StrokeColor
        {
            get { return _pen.Color.ToColor(); }
            set
            {
                var val = value.ToColor();
                if (_pen.Color == val) return;
                _pen.Color = value.IsDefaultColor ? System.Drawing.Color.Black : val;
                OnPropertyChanged("StrokeColor");
            }
        }
        private Pen _pen = new Pen(System.Drawing.Color.Black);

        public double StrokeThickness
        {
            get { return _pen.Width; }
            set
            {
                if (Math.Abs(_pen.Width - value) < double.Epsilon) return;
                _pen.Width = (float)value;
                OnPropertyChanged("StrokeColor");
            }
        }

        public void Save(bool compositeBackground)
        {
            Save(Path.Combine(CompactFactory.Instance.TempPath, Guid.NewGuid() + ".png"), compositeBackground);
        }

        public void Save(string fileName)
        {
            Save(fileName, false);
        }

        public void Save(string fileName, bool compositeBackground)
        {
            iApp.Factory.ActivateLoadTimer("Saving...");
            Device.Thread.Start(() =>
            {
                iApp.File.EnsureDirectoryExists(fileName);
                using (var temp = new Bitmap(_signatureBitmap.Width, _signatureBitmap.Height))
                using (var g = Graphics.FromImage(temp))
                {
                    g.Clear(System.Drawing.Color.Transparent);
                    if (compositeBackground) PaintBackground(g);
                    var attr = new ImageAttributes();
                    var transKey = System.Drawing.Color.Lime;
                    attr.SetColorKey(transKey, transKey);
                    var dstRect = new Rectangle(0, 0, temp.Width, temp.Height);
                    g.DrawImage(_signatureBitmap, dstRect, 0, 0, temp.Width, temp.Height, GraphicsUnit.Pixel, attr);
                    temp.Save(fileName, ImageFormat.Png);
                }

                Device.Thread.ExecuteOnMainThread(() =>
                {
                    var save = DrawingSaved;
                    if (save != null) save(this, new SaveEventArgs(fileName));
                });
                iApp.Factory.StopBlockingUserInput();
            });
        }

        public event SaveEventHandler DrawingSaved;

        public Visibility Visibility { get; set; }

        #region Layout

        public Thickness Margin
        {
            get { return _margin; }
            set
            {
                if (_margin == value) return;
                _margin = value;
                OnPropertyChanged("Margin");
            }
        }
        private Thickness _margin;

        public int ColumnIndex
        {
            get { return _columnIndex; }
            set
            {
                if (value == _columnIndex) return;
                _columnIndex = value;
                OnPropertyChanged("ColumnIndex");
            }
        }
        private int _columnIndex;

        public int ColumnSpan
        {
            get { return _columnSpan; }
            set
            {
                if (value == _columnSpan) return;
                _columnSpan = value;
                OnPropertyChanged("ColumnSpan");
            }
        }
        private int _columnSpan = 1;

        public int RowIndex
        {
            get { return _rowIndex; }
            set
            {
                if (value == _rowIndex) return;
                _rowIndex = value;
                OnPropertyChanged("RowIndex");
            }
        }
        private int _rowIndex;

        public int RowSpan
        {
            get { return _rowSpan; }
            set
            {
                if (_rowSpan == value) return;
                _rowSpan = value;
                OnPropertyChanged("RowSpan");
            }
        }
        private int _rowSpan = 1;

        public HorizontalAlignment HorizontalAlignment
        {
            get { return _horizontalAlignment; }
            set
            {
                if (value == _horizontalAlignment) return;
                _horizontalAlignment = value;
                OnPropertyChanged("HorizontalAlignment");
            }
        }
        private HorizontalAlignment _horizontalAlignment;

        public VerticalAlignment VerticalAlignment
        {
            get { return _verticalAlignment; }
            set
            {
                if (value == _verticalAlignment) return;
                _verticalAlignment = value;
                OnPropertyChanged("VerticalAlignment");
            }
        }
        private VerticalAlignment _verticalAlignment;

        public Size Measure(Size constraints)
        {
            return new Size(constraints.Width, Math.Min((int)constraints.Height, Height));
        }

        public void SetLocation(UI.Point location, Size size)
        {
            base.Width = (int)size.Width;
            base.Height = (int)size.Height;
            Location = location.ToPoint();
        }

        #endregion

        #region Identity

        public string ID
        {
            get { return _id; }
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged("ID");
            }
        }
        private string _id;

        public new object Parent { get { return base.Parent; } }

        public IPairable Pair
        {
            get { return _pair; }
            set
            {
                if (_pair != null || value == null) return;
                _pair = value;
                _pair.Pair = this;
            }
        }
        private IPairable _pair;

        public MetadataCollection Metadata
        {
            get { return _metadata ?? (_metadata = new MetadataCollection()); }
        }
        private MetadataCollection _metadata;

        public bool Equals(IElement other)
        {
            var control = other as Element;
            return control == null ? ReferenceEquals(this, other) : control.Equals(this);
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            var prop = PropertyChanged;
            if (prop != null) prop(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}