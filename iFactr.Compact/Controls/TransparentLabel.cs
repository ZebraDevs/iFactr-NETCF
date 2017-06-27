using System.ComponentModel;
using System.Drawing;
using iFactr.UI;
using iFactr.UI.Controls;
using System;
using Color = iFactr.UI.Color;
using Font = iFactr.UI.Font;
using Point = iFactr.UI.Point;
using Size = iFactr.UI.Size;

namespace iFactr.Compact
{
    public class TransparentLabel : ILabel, INotifyPropertyChanged, IPaintable, IHighlight
    {
        public TransparentLabel()
        {
            HighlightColor = SystemColors.HighlightText.ToColor();
            ColumnIndex = -1;
            RowIndex = -1;
            ColumnSpan = 1;
            RowSpan = 1;
        }

        public string ID { get; set; }

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

        #region ILabel Members

        public Font Font
        {
            get { return _font; }
            set
            {
                if (_font == value) return;
                _font = value;
                OnPropertyChanged("Font");
            }
        }
        private Font _font;

        public Color ForegroundColor
        {
            get { return _foregroundColor; }
            set
            {
                if (_foregroundColor == value) return;
                _foregroundColor = value;
                OnPropertyChanged("ForegroundColor");
            }
        }
        private Color _foregroundColor;

        public Color HighlightColor
        {
            get;
            set;
        }

        public int Lines
        {
            get { return _lines; }
            set
            {
                if (_lines == value) return;
                _lines = value;
                OnPropertyChanged("Lines");
            }
        }

        private int _lines;

        public string Text
        {
            get { return _text; }
            set
            {
                if (_text == value) return;
                var old = _text;
                _text = value;

                var ce = ValueChanged;
                if (ce != null)
                    ce(_pair ?? this, new ValueChangedEventArgs<string>(old, Text));

                OnPropertyChanged("Text");
            }
        }
        private string _text;

        public event ValueChangedEventHandler<string> ValueChanged;

        #endregion

        #region IPairable Members

        public IPairable Pair
        {
            get { return _pair; }
            set
            {
                if (_pair == null && value != null)
                {
                    _pair = value;
                    _pair.Pair = this;
                }
            }
        }
        private IPairable _pair;

        #endregion

        #region IControl Members

        public int ColumnIndex
        {
            get { return _columnIndex; }
            set
            {
                if (_columnIndex == value) return;
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
                if (_columnSpan == value) return;
                _columnSpan = value;
                OnPropertyChanged("ColumnSpan");
            }
        }
        private int _columnSpan;

        public int RowIndex
        {
            get { return _rowIndex; }
            set
            {
                if (_rowIndex == value) return;
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
        private int _rowSpan;

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

        public HorizontalAlignment HorizontalAlignment
        {
            get;
            set;
        }

        public object Parent { get; set; }

        public MetadataCollection Metadata
        {
            get { return _metadata ?? (_metadata = new MetadataCollection()); }
        }
        private MetadataCollection _metadata;

        public string StringValue
        {
            get { return Text; }
        }

        public string SubmitKey
        {
            get;
            set;
        }

        public VerticalAlignment VerticalAlignment
        {
            get;
            set;
        }

        public event ValidationEventHandler Validating;

        public Size Measure(Size constraints)
        {
            return CoreDll.MeasureString(Text, Font.ToFont(), constraints, Lines != 1, false);
        }

        public void NullifyEvents()
        {
            ValueChanged = null;
            Validating = null;
        }

        public void SetLocation(Point location, Size size)
        {
            Location = location;
            Size = size;
            var par = Parent as GridControl;
            if (par != null) par.Redraw();
        }

        public bool Validate(out string[] errors)
        {
            var handler = Validating;
            if (handler != null)
            {
                var args = new ValidationEventArgs(SubmitKey, Text, StringValue);
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

        #region IEquatable<IControl> Members

        public bool Equals(IElement other)
        {
            var control = other as Element;
            if (control != null)
            {
                return control.Equals(this);
            }

            return base.Equals(other);
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public Size Size
        {
            get;
            set;
        }

        public Point Location
        {
            get;
            set;
        }

        public void Paint(Graphics g)
        {
            if (Visibility != Visibility.Visible || string.IsNullOrEmpty(Text)) return;

            IntPtr hdcTemp = IntPtr.Zero;
            IntPtr oldFont = IntPtr.Zero;
            IntPtr currentFont = IntPtr.Zero;

            try
            {
                hdcTemp = g.GetHdc();
                if (hdcTemp != IntPtr.Zero)
                {
                    currentFont = Font.ToFont().ToHfont();
                    oldFont = CoreDll.SelectObject(hdcTemp, currentFont);

                    var rect = new Rect
                    {
                        Left = (int)Location.X,
                        Top = (int)Location.Y,
                        Right = (int)(Location.X + Size.Width),
                        Bottom = (int)(Location.Y + Size.Height),
                    };
                    var color = (Highlight ? HighlightColor : ForegroundColor).ToColor();
                    CoreDll.SetTextColor(hdcTemp, color.R | (color.G << 8) | (color.B << 16));
                    CoreDll.SetBkMode(hdcTemp, 1);
                    var flags = CoreDll.DT_END_ELLIPSIS | CoreDll.DT_NOPREFIX;
                    if (Lines != 1) flags += CoreDll.DT_WORDBREAK;
                    CoreDll.DrawText(hdcTemp, Text, Text.Length, ref rect, flags);
                }
            }
            finally
            {
                if (oldFont != IntPtr.Zero)
                {
                    CoreDll.SelectObject(hdcTemp, oldFont);
                }

                if (hdcTemp != IntPtr.Zero)
                {
                    g.ReleaseHdc(hdcTemp);
                }

                if (currentFont != IntPtr.Zero)
                {
                    CoreDll.DeleteObject(currentFont);
                }
            }
        }

        public void SetParent(GridControl gridControl)
        {
            Parent = gridControl;
        }

        #region ILabel Members


        public TextAlignment TextAlignment
        {
            get;
            set;
        }

        #endregion

        #region IControl Members

        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }

        public bool Highlight { get; set; }

        private bool _isEnabled;

        #endregion
    }
}