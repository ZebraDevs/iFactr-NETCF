using iFactr.UI;
using iFactr.UI.Controls;

namespace iFactr.Compact
{
    public class HeaderControl : GridControl, ISectionHeader
    {
        public override double MinWidth
        {
            get { return Parent.Width; }
            set { }
        }

        public override double MaxWidth
        {
            get { return Parent.Width; }
            set { }
        }

        readonly Label _headerLabel = new Label();
        public HeaderControl()
        {
            _headerLabel.Margin = new Thickness(Thickness.LeftMargin, 0, 0, 0);
            _headerLabel.HorizontalAlignment = HorizontalAlignment.Stretch;
            _headerLabel.Font = Font.PreferredHeaderFont;
            _headerLabel.Lines = 1;
            _headerLabel.RowIndex = 0;
            _headerLabel.ColumnIndex = 0;
            AddChild(_headerLabel);
        }

        public Color BackgroundColor
        {
            get { return BackgroundHexCode != null ? new Color(BackgroundHexCode) : new Color(); }
            set
            {
                if (BackgroundHexCode == value.HexCode) return;
                BackgroundHexCode = value.HexCode;
                OnPropertyChanged("BackgroundColor");
            }
        }

        public Color ForegroundColor
        {
            get { return _headerLabel.ForegroundColor; }
            set
            {
                if (_headerLabel.ForegroundColor == value) return;
                _headerLabel.ForegroundColor = value;
                OnPropertyChanged("ForegroundColor");
            }
        }

        public new Font Font
        {
            get { return _headerLabel.Font; }
            set
            {
                if (_headerLabel.Font == value) return;
                _headerLabel.Font = value;
                OnPropertyChanged("Font");
            }
        }

        public new string Text
        {
            get { return _headerLabel.Text; }
            set
            {
                if (_headerLabel.Text == value) return;
                _headerLabel.Text = value;
                OnPropertyChanged("Text");
            }
        }

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
    }
}