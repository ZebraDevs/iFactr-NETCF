using System;
using System.ComponentModel;
using iFactr.Core;
using iFactr.UI;
using iFactr.UI.Controls;
using MonoCross.Navigation;
using Button = System.Windows.Forms.Button;
using Color = iFactr.UI.Color;
using Font = iFactr.UI.Font;
using Point = iFactr.UI.Point;
using Size = iFactr.UI.Size;

namespace iFactr.Compact
{
    public class ButtonControl : Button, INotifyPropertyChanged, IButton
    {
        public ButtonControl()
        {
            ColumnIndex = -1;
            RowIndex = -1;
            ColumnSpan = 1;
            RowSpan = 1;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            var p = Parent as GridCell;
            if (p != null) p.Highlight();
        }

        public void NullifyEvents()
        {
            Clicked = null;
            Validating = null;
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            var clicked = Clicked;
            if (clicked != null)
            {
                clicked(this, e);
            }
            else
            {
                IMXView view;
                var parent = Parent as IGridBase;
                if (parent is IMXView)
                {
                    view = parent as IMXView;
                }
                else
                {
                    var control = CompactFactory.GetNativeObject<GridCell>(parent, "Parent");
                    view = control.Parent as IMXView ?? control.Parent.Parent as IMXView;
                }
                CompactFactory.Navigate(NavigationLink, view);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            var prop = PropertyChanged;
            if (prop != null) prop(this, new PropertyChangedEventArgs(propertyName));
        }

        public Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                if (_visibility == value) return;
                _visibility = value;
                Visible = value == Visibility.Visible;
                OnPropertyChanged("Visibility");
            }
        }
        private Visibility _visibility;

        public bool IsEnabled
        {
            get { return Enabled; }
            set
            {
                if (Enabled == value) return;
                Enabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }

        public string StringValue { get { return Title; } }

        public string SubmitKey
        {
            get { return _submitKey; }
            set
            {
                if (_submitKey == value) return;
                _submitKey = value;
                OnPropertyChanged("SubmitKey");
            }
        }
        private string _submitKey;

        public new event ValidationEventHandler Validating;

        public bool Validate(out string[] errors)
        {
            var handler = Validating;
            if (handler != null)
            {
                var args = new ValidationEventArgs(SubmitKey, StringValue, StringValue);
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

        public string Title
        {
            get { return base.Text; }
            set
            {
                if (_title == value) return;
                _title = value;
                base.Text = value;
                if (_hasMeasured)
                    FitText();
                OnPropertyChanged("Title");
            }
        }
        private string _title;

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            OnPropertyChanged("StringValue");
            OnPropertyChanged("Title");
        }

        public new Font Font
        {
            get { return _font; }
            set
            {
                if (_font == value) return;
                _font = value;
                base.Font = _font.ToFont();
                OnPropertyChanged("Font");
            }
        }
        private Font _font;

        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (_backgroundColor == value) return;
                _backgroundColor = value;
                if (!_backgroundColor.IsDefaultColor)
                {
                    BackColor = _backgroundColor.ToColor();
                }
                OnPropertyChanged("BackgroundColor");
            }
        }
        private Color _backgroundColor;

        public Color ForegroundColor
        {
            get { return _foregroundColor; }
            set
            {
                if (_foregroundColor == value) return;
                _foregroundColor = value;
                if (!_foregroundColor.IsDefaultColor)
                {
                    ForeColor = _foregroundColor.ToColor();
                }
                OnPropertyChanged("ForegroundColor");
            }
        }
        private Color _foregroundColor;

        public UI.Controls.IImage Image { get; set; }

        public Link NavigationLink
        {
            get { return _navigationLink; }
            set
            {
                if (value == _navigationLink) return;
                _navigationLink = value;
                OnPropertyChanged("NavigationLink");
            }
        }
        private Link _navigationLink;
        public event EventHandler Clicked;

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
        private int _columnSpan;

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
        private int _rowSpan;

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

        private Size _margins = new Size(Thickness.LargeHorizontalSpacing, 0);
        public Size Measure(Size constraints)
        {
            var measure = CoreDll.MeasureString(" " + _title + " ", Font.ToFont(), constraints, false, true) + _margins;
            return new Size(measure.Width, Math.Max(measure.Height, 33 * CompactFactory.Instance.DpiScale));
        }

        public void SetLocation(Point location, Size size)
        {
            Width = (int)size.Width;
            Height = (int)size.Height;
            _hasMeasured = size.Height > 0 && size.Width > 0;
            FitText();
            Location = location.ToPoint();
        }
        private bool _hasMeasured;

        public void FitText()
        {
            if (!_hasMeasured)
                return;
            const string ellipsisChars = "... ";
            var constraints = Size.ToSize();
            Size s = CoreDll.MeasureString(" " + _title + " ", Font.ToFont(), constraints, false, true) + _margins;

            // control is large enough to display the whole text 
            if (s.Width <= Width)
                return;

            int len = 0;
            int seg = _title.Length;
            string fit = string.Empty;

            // find the longest string that fits into the control boundaries using bisection method 
            while (seg > 1)
            {
                seg -= seg / 2;

                int left = len + seg;
                int right = _title.Length;

                if (left > right)
                    continue;

                // build and measure a candidate string with ellipsis
                string tst = " " + _title.Substring(0, left).TrimEnd() + ellipsisChars;

                s = CoreDll.MeasureString(tst, Font.ToFont(), constraints, false, true) + _margins;

                // candidate string fits into control boundaries, 
                // try a longer string
                // stop when seg <= 1 
                if (s.Width <= Width)
                {
                    len += seg;
                    fit = tst;
                }
            }

            base.Text = len == 0 ? ellipsisChars : fit;
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
    }
}