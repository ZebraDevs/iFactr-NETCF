using System;
using System.ComponentModel;
using System.Windows.Forms;
using iFactr.UI;
using iFactr.UI.Controls;
using HorizontalAlignment = iFactr.UI.HorizontalAlignment;

namespace iFactr.Compact
{
    class Switch : CheckBox, ISwitch, INotifyPropertyChanged
    {
        public Switch()
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
            ValueChanged = null;
            Validating = null;
        }

        protected override void OnCheckStateChanged(EventArgs e)
        {
            base.OnCheckStateChanged(e);
            var clicked = ValueChanged;
            if (clicked != null) clicked(this, new ValueChangedEventArgs<bool>(!Value, Value));
            OnPropertyChanged("StringValue");
            OnPropertyChanged("Value");
            OnPropertyChanged("Checked");
            var parent = Parent as GridControl;
            if (parent != null) parent.SetSubmission(SubmitKey, StringValue);
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

        public string StringValue { get { return Checked.ToString().ToLower(); } }

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

        public Color FalseColor { get; set; }
        public Color TrueColor { get; set; }

        public bool Value
        {
            get { return Checked; }
            set { Checked = value; }
        }
        public event ValueChangedEventHandler<bool> ValueChanged;

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
            var height = 15 * CompactFactory.Instance.DpiScale;
            return new Size(height, height);
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