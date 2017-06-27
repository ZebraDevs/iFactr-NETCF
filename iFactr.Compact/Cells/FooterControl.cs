using iFactr.UI;
using iFactr.UI.Controls;

namespace iFactr.Compact
{
    public class FooterControl : GridControl, ISectionFooter
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

        readonly Label _footerLabel = new Label();
        public FooterControl()
        {
            _footerLabel.Margin = new Thickness(Thickness.LeftMargin, 0, Thickness.RightMargin, Thickness.BottomMargin);
            _footerLabel.Font = Font.PreferredSmallFont;
            _footerLabel.HorizontalAlignment = HorizontalAlignment.Stretch;
            _footerLabel.TextAlignment = TextAlignment.Center;
            _footerLabel.RowIndex = 0;
            _footerLabel.ColumnIndex = 0;
            _footerLabel.Visibility = Visibility.Collapsed;
            AddChild(_footerLabel);
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
            get { return _footerLabel.ForegroundColor; }
            set
            {
                if (_footerLabel.ForegroundColor == value) return;
                _footerLabel.ForegroundColor = value;
                OnPropertyChanged("ForegroundColor");
            }
        }

        public new Font Font
        {
            get { return _footerLabel.Font; }
            set
            {
                if (_footerLabel.Font == value) return;
                _footerLabel.Font = value;
                OnPropertyChanged("Font");
            }
        }

        public new string Text
        {
            get { return _footerLabel.Text; }
            set
            {
                if (_footerLabel.Text == value) return;
                _footerLabel.Text = value;
                _footerLabel.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
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