using iFactr.Core;
using iFactr.UI;
using iFactr.UI.Controls;
using Control = System.Windows.Forms.Control;

namespace iFactr.Compact
{
    public sealed class SearchBar : Grid, ISearchBox
    {
        private readonly ITextBox _box;
        private readonly IButton _button;

        public SearchBar()
        {
            Columns.Add(Column.OneStar);
            Columns.Add(Column.AutoSized);

            _box = new TextBox
            {
                Placeholder = iApp.Factory.GetResourceString("SearchHint"),
            };
            _box.TextChanged += BoxOnTextChanged;
            AddChild(_box);

            _button = new Button(iApp.Factory.GetResourceString("Clear"))
            {
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            Rows.Add(new Row(((Control)_box.Pair).Height / CompactFactory.Instance.DpiScale, LayoutUnitType.Absolute));
            _button.Clicked += (o, e) => _box.Text = string.Empty;
            AddChild(_button);
        }

        private void BoxOnTextChanged(object sender, ValueChangedEventArgs<string> args)
        {
            var visible = string.IsNullOrEmpty(args.NewValue) ? Visibility.Collapsed : Visibility.Visible;
            if (_button.Visibility != visible)
            {
                _button.Visibility = visible;
                var size = Size.ToSize();
                this.PerformLayout(size, size);
            }
            OnSearchPerformed(new SearchEventArgs(args.NewValue));
        }

        public Color BackgroundColor
        {
            get { return _box.BackgroundColor; }
            set
            {
                if (_box.BackgroundColor == value) return;
                _box.BackgroundColor = value;
                OnPropertyChanged("BackgroundColor");
            }
        }

        public Color BorderColor
        {
            get { return new Color(); }
            set { }
        }

        public Color ForegroundColor
        {
            get { return _box.ForegroundColor; }
            set
            {
                if (_box.ForegroundColor == value) return;
                _box.ForegroundColor = value;
                OnPropertyChanged("ForegroundColor");
            }
        }

        public string Placeholder
        {
            get { return _box.Placeholder; }
            set
            {
                if (_box.Placeholder == value) return;
                _box.Placeholder = value;
                OnPropertyChanged("Placeholder");
            }
        }

        public TextCompletion TextCompletion
        {
            get { return _box.TextCompletion; }
            set
            {
                if (_box.TextCompletion == value) return;
                _box.TextCompletion = value;
                OnPropertyChanged("Placeholder");
            }
        }

        public override string Text
        {
            get { return _box.Text; }
            set
            {
                if (value == _box.Text) return;
                _box.Text = value;
                OnPropertyChanged("Text");
            }
        }

        public event SearchEventHandler SearchPerformed;

        private void OnSearchPerformed(SearchEventArgs args)
        {
            SearchEventHandler handler = SearchPerformed;
            if (handler != null) handler(this, args);
        }

        public new void Focus()
        {
            _box.Focus();
        }
    }
}