using System;
using iFactr.UI;
using iFactr.UI.Controls;

namespace iFactr.Compact
{
    public class Grid : GridControl, IElement
    {
        public Grid()
        {
            ColumnIndex = -1;
            RowIndex = -1;
            ColumnSpan = 1;
            RowSpan = 1;
        }

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

        private VerticalAlignment _verticalAlignment;

        public Size Measure(Size constraints)
        {
            return new Size(constraints.Width, Math.Min((int)constraints.Height, Height));
        }

        public void SetLocation(UI.Point location, Size size)
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

        public new object Parent
        {
            get { return base.Parent; }
        }

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

        public bool Equals(IElement other)
        {
            var control = other as Element;
            return control == null ? ReferenceEquals(this, other) : control.Equals(this);
        }

        #endregion
    }
}