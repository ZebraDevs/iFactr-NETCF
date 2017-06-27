using System.ComponentModel;
using System.Drawing;
using iFactr.UI;
using Color = iFactr.UI.Color;
using Font = iFactr.UI.Font;
using Point = iFactr.UI.Point;
using Size = iFactr.UI.Size;

namespace iFactr.Compact
{
    public class ToolbarButton : ButtonControl, IToolbarButton
    {
        public ToolbarButton()
        {
            Font = Font.PreferredButtonFont;
        }

        public bool Equals(IToolbarButton other)
        {
            var item = other as UI.ToolbarButton;
            return item == null ? ReferenceEquals(this, other) : item.Equals(this);
        }

        public string ImagePath { get; set; }
    }

    public class ToolbarSeparator : IToolbarSeparator, INotifyPropertyChanged, IPaintable
    {
        public ToolbarSeparator()
        {
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

        public bool Equals(IToolbarSeparator other)
        {
            var item = other as UI.ToolbarSeparator;
            return item == null ? ReferenceEquals(this, other) : item.Equals(this);
        }

        public Point Location
        {
            get;
            set;
        }

        public Size Size
        {
            get;
            set;
        }

        public void Paint(Graphics g)
        {
            g.DrawLine(new Pen(ForegroundColor.IsDefaultColor ? System.Drawing.Color.Black : ForegroundColor.ToColor()),
                (int)Location.X, (int)Location.Y,
                (int)Location.X, (int)(Location.Y + Size.Height));
        }

        public void SetParent(GridControl gridControl) { }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            var prop = PropertyChanged;
            if (prop != null) prop(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}