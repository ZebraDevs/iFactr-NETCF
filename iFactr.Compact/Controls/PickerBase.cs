using System;
using System.ComponentModel;
using System.Windows.Forms;
using iFactr.UI;
using iFactr.UI.Controls;
using HorizontalAlignment = iFactr.UI.HorizontalAlignment;

namespace iFactr.Compact
{
    class PickerBase : DateTimePicker, INotifyPropertyChanged, IElement
    {
        public PickerBase()
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

        public virtual void NullifyEvents()
        {
            Validating = null;
        }

        public virtual void ShowPicker()
        {
            Focus();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            var prop = PropertyChanged;
            if (prop != null) prop(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Style

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

        #endregion

        #region Submission

        public string StringValue
        {
            get { return Text; }
        }

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


        public Size Measure(Size constraints)
        {
            var s = CoreDll.MeasureString(Text, Font.ToFont(), constraints, false, true);
            return new Size(constraints.Width, s.Height);
        }

        public void SetLocation(Point location, Size size)
        {
            Width = (int)size.Width;
            Height = (int)size.Height;
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
    }
}