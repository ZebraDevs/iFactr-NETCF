using System;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using iFactr.UI;
using iFactr.UI.Controls;
using Color = System.Drawing.Color;
using HorizontalAlignment = iFactr.UI.HorizontalAlignment;
using TextBox = System.Windows.Forms.TextBox;

namespace iFactr.Compact
{
    public class TextBase : TextBox, INotifyPropertyChanged, IPairable
    {
        public TextBase()
        {
            Font = Font.PreferredTextBoxFont;
            ColumnIndex = -1;
            RowIndex = -1;
            ColumnSpan = 1;
            RowSpan = 1;
        }

        #region TextBoxWithPrompt

        // originated at http://danielmoth.com/Blog

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            var p = Parent as GridCell;
            if (p != null)
            {
                ((SmoothListbox)p.Parent.Parent).ResetSelection(this);
            }
            _isFocused = true;
            UsePrompt = false;
        }

        protected override void OnLostFocus(EventArgs e)
        {
            if (TextLength == 0)
                UsePrompt = true;
            base.OnLostFocus(e);
            _isFocused = false;
        }

        public string Placeholder
        {
            get { return _placeholder; }
            set
            {
                _placeholder = FixNewLines(value);
                if (!UsePrompt || string.IsNullOrEmpty(_placeholder)) return;
                ForeColor = PlaceholderColor.IsDefaultColor ? Color.Gray : PlaceholderColor.ToColor();
                PasswordChar = new char();
                base.Text = value;
            }
        }
        protected string _placeholder;

        public bool IsFocused { get { return _isFocused; } }
        private bool _isFocused;

        private bool UsePrompt
        {
            get { return _usePrompt; }
            set
            {
                if (value == _usePrompt) return;
                if (string.IsNullOrEmpty(_placeholder))
                {
                    PasswordChar = _passwordChar;
                    return;
                }
                _usePrompt = value;
                if (_usePrompt)
                {
                    ForeColor = PlaceholderColor.IsDefaultColor ? Color.Gray : PlaceholderColor.ToColor();
                    PasswordChar = new char();
                    base.Text = Placeholder;
                }
                else
                {
                    ForeColor = ForegroundColor.IsDefaultColor ? Color.Black : ForegroundColor.ToColor();
                    base.Text = string.Empty;
                }
            }
        }
        private bool _usePrompt;

        public bool IsPassword
        {
            get { return _passwordChar == '*'; }
            set { _passwordChar = value ? '*' : new char(); }
        }
        private char _passwordChar;

        protected override void OnParentChanged(EventArgs e)
        {
            if (string.IsNullOrEmpty(Text))
            {
                UsePrompt = true;
            }
            base.OnParentChanged(e);
        }

        #endregion

        #region Methods

        private string FixNewLines(string rawInput)
        {
            if (string.IsNullOrEmpty(rawInput))
                return rawInput;
            string[] lines = rawInput.Split('\n');
            var b = new StringBuilder(lines[0]);
            for (int i = 1; i < lines.Length; i++)
            {
                b.Append("\r\n");
                b.Append(lines[i].Trim('\r', '\n'));
            }
            return b.ToString();
        }

        public new void Focus()
        {
            base.Focus();
        }

        public virtual void NullifyEvents()
        {
            TextChanged = null;
        }

        #endregion

        #region Value

        public override string Text
        {
            get
            {
                return UsePrompt ? string.Empty : base.Text;
            }
            set
            {
                value = FixNewLines(value);
                if (Text != value && (!(UsePrompt = string.IsNullOrEmpty(value)) || _placeholder == null))
                {
                    base.Text = value ?? string.Empty;
                }
            }
        }

        protected string _oldValue;
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            if (base.Text != _placeholder)
            {
                if (base.Text != _oldValue && !(_expression == null || _expression.IsMatch(base.Text)))
                {
                    int p = SelectionStart;
                    base.Text = _oldValue;
                    SelectionStart = Math.Min(p, base.Text.Length);
                    return;
                }
                var parent = Parent as GridControl;
                if (parent != null) parent.SetSubmission(SubmitKey, StringValue);
            }
            OnPropertyChanged("Text");
            var textChanged = TextChanged;
            if (textChanged != null) textChanged(this, new ValueChangedEventArgs<string>(_oldValue, Text));
            _oldValue = base.Text == _placeholder ? string.Empty : Text;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var ret = ReturnKeyPressed;
                var args = new EventHandledEventArgs();
                if (ret != null) ret(Pair, args);
                if (args.IsHandled)
                {
                    e.Handled = true;
                    return;
                }
            }
            base.OnKeyUp(e);
        }

        public event EventHandler<EventHandledEventArgs> ReturnKeyPressed;

        public new event ValueChangedEventHandler<string> TextChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            var prop = PropertyChanged;
            if (prop != null) prop(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Style

        public KeyboardReturnType KeyboardReturnType
        {
            get { return _keyboardReturnType; }
            set
            {
                if (_keyboardReturnType == value) return;
                _keyboardReturnType = value;
                OnPropertyChanged("KeyboardReturnType");
            }
        }
        private KeyboardReturnType _keyboardReturnType;

        public string Expression
        {
            get { return _expression == null ? null : _expression.ToString(); }
            set
            {
                if (_expression == null && value == null ||
                    _expression != null && _expression.ToString() == value) return;
                _expression = value == null ? null : new Regex(value);
                OnPropertyChanged("Expression");
            }
        }
        private Regex _expression;

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

        public KeyboardType KeyboardType
        {
            get { return _keyboardType; }
            set
            {
                if (_keyboardType == value) return;
                _keyboardType = value;
                OnPropertyChanged("KeyboardType");
            }
        }
        private KeyboardType _keyboardType;

        public TextAlignment TextAlignment
        {
            get { return _textAlignment; }
            set
            {
                if (_textAlignment == value) return;
                _textAlignment = value;
                OnPropertyChanged("TextAlignment");
            }
        }
        private TextAlignment _textAlignment;

        public TextCompletion TextCompletion
        {
            get { return _textCompletion; }
            set
            {
                if (_textCompletion == value) return;
                _textCompletion = value;
                OnPropertyChanged("TextCompletion");
            }
        }
        private TextCompletion _textCompletion;

        public UI.Color BackgroundColor
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
        private UI.Color _backgroundColor;

        public UI.Color PlaceholderColor
        {
            get { return _placeholderColor; }
            set
            {
                if (_placeholderColor == value) return;
                _placeholderColor = value;
                if (!_placeholderColor.IsDefaultColor)
                {
                    BackColor = _placeholderColor.ToColor();
                }
                OnPropertyChanged("PlaceholderColor");
            }
        }
        private UI.Color _placeholderColor;

        public UI.Color ForegroundColor
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
        private UI.Color _foregroundColor;

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
            return new Size(Math.Min(Width, constraints.Width), Math.Min(Height, constraints.Height));
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