using System;
using System.ComponentModel;
using System.Drawing;
using MonoCross;
using iFactr.UI;
using Point = iFactr.UI.Point;
using Size = iFactr.UI.Size;

namespace iFactr.Compact
{
    public class ImageControl : UI.Controls.IImage, IPaintable, INotifyPropertyChanged, IDisposable
    {
        public ImageControl()
        {
            ColumnIndex = -1;
            RowIndex = -1;
            ColumnSpan = 1;
            RowSpan = 1;
        }

        public void NullifyEvents()
        {
            Validating = null;
        }

        #region Value

        protected BitmapImage BitmapData;

        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                if (_filePath == value) return;
                _filePath = value;
                var bitmapData = ImageManager.GetBitmapData(_filePath, false) as BitmapImage;
                if (bitmapData == null || bitmapData.GetBytes() == null) return;
                BitmapData = bitmapData;
                var handler = Loaded;
                if (handler != null)
                    handler(this, EventArgs.Empty);
                OnPropertyChanged("FilePath");
                var par = Parent as GridControl;
                if (par != null) par.Redraw();
            }
        }
        private string _filePath;

        #endregion

        #region Submission

        public string StringValue { get { return FilePath; } }

        public string SubmitKey { get; set; }

        public event ValidationEventHandler Validating;

        public bool Validate(out string[] errors)
        {
            var handler = Validating;
            if (handler != null)
            {
                var args = new ValidationEventArgs(SubmitKey, FilePath, StringValue);
                handler(Pair ?? this, args);

                if (args.Errors.Count > 0)
                {
                    errors = new string[args.Errors.Count];
                    args.Errors.CopyTo(errors, 0);
                    return false;
                }
            }

            errors = null;
            return true;
        }

        #endregion

        public Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                if (_visibility == value) return;
                _visibility = value;
                OnPropertyChanged("Visibility");
            }
        }
        private Visibility _visibility;

        public bool IsEnabled { get; set; }

        public int ColumnIndex { get; set; }

        public int ColumnSpan { get; set; }

        public int RowIndex { get; set; }

        public int RowSpan { get; set; }

        public Thickness Margin
        {
            get;
            set;
        }

        public HorizontalAlignment HorizontalAlignment { get; set; }

        public VerticalAlignment VerticalAlignment { get; set; }

        public Size Dimensions
        {
            get
            {
                if (BitmapData == null || BitmapData.GetBytes() == null) return new Size();
                var b = BitmapData.GetBitmap();
                var retval = new Size(b.Width, b.Height);
                b.Dispose();
                return retval;
            }
        }

        public ContentStretch Stretch
        {
            get { return _stretch; }
            set
            {
                if (_stretch == value) return;
                _stretch = value;
                OnPropertyChanged("Stretch");
            }
        }
        private ContentStretch _stretch;

        public event EventHandler Clicked;

        public event EventHandler Loaded;

        public IImageData GetImageData()
        {
            return BitmapData;
        }

        #region Layout

        public Size Measure(Size constraints)
        {
            var originalSize = Dimensions;
            var ratio = originalSize.Width / originalSize.Height;
            double width = Math.Min(originalSize.Width, constraints.Width);
            double height = Math.Min(originalSize.Height, constraints.Height);

            if (width / height < ratio)
            {
                height = width / ratio;
            }
            else
            {
                width = height * ratio;
            }

            return new Size(width, height);
        }

        /// <summary>
        /// Sets the location and size of the control within its parent grid.
        /// This is called by the underlying grid layout system and should not be used in application logic.
        /// </summary>
        /// <param name="location">The X and Y coordinates of the upper left corner of the control.</param>
        /// <param name="size">The width and height of the control.</param>
        public void SetLocation(Point location, Size size)
        {
            Location = location;
            Size = size;
            var par = Parent as GridControl;
            if (par != null) par.Redraw();
        }

        public void SetParent(GridControl gridControl)
        {
            Parent = gridControl;
        }

        public Size Size { get; set; }

        public Point Location { get; set; }

        #endregion

        #region Identity

        public string ID { get; set; }

        public object Parent { get; set; }

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

        public bool Equals(UI.Controls.IElement other)
        {
            var control = other as UI.Controls.Element;
            return control == null ? ReferenceEquals(this, other) : control.Equals(this);
        }

        #endregion

        public void Paint(Graphics g)
        {
            if (Visibility != Visibility.Visible || BitmapData == null || BitmapData.GetBytes() == null) return;

            BitmapImage.ImageInfo imgInfo;
            var image = BitmapData.CreateImage();
            image.GetImageInfo(out imgInfo);

            var bitmapSize = new Size(imgInfo.Width, imgInfo.Height);

            var dest = new Rect
            {
                Left = (int)Location.X,
                Top = (int)Location.Y,
                Right = (int)(Location.X + Size.Width),
                Bottom = (int)(Location.Y + Size.Height),
            };
            var source = new Rectangle(0, 0, (int)bitmapSize.Width, (int)bitmapSize.Height);

            if (_stretch == ContentStretch.UniformToFill)
            {
                var ratio = Size.Width / Size.Height;
                double width = source.Width;
                double height = source.Height;

                if (width / height < ratio)
                {
                    height = width / ratio;
                }
                else
                {
                    width = height * ratio;
                }

                var xdiff = (source.Width - width) / 2;
                var ydiff = (source.Height - height) / 2;
                source = new Rectangle((int)xdiff, (int)ydiff, (int)width, (int)height);
            }

            double scaleFactorX = 1 / imgInfo.Xdpi * 2540;
            double scaleFactorY = 1 / imgInfo.Ydpi * 2540;
            var rcScaled = new Rect
            {
                Left = (int)(source.X * scaleFactorX),
                Top = (int)(source.Y * scaleFactorY),
                Right = (int)((source.X + source.Width) * scaleFactorX),
                Bottom = (int)((source.Y + source.Height) * scaleFactorY),
            };

            // Draw the image, with alpha channel if any
            IntPtr hdcDest = g.GetHdc();
            image.Draw(hdcDest, ref dest, ref rcScaled);
            g.ReleaseHdc(hdcDest);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            BitmapData.Dispose();
        }
    }
}