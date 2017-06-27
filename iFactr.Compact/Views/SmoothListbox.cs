using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using iFactr.Compact.Annotations;
using iFactr.Core.Layers;
using iFactr.UI;
using iFactr.Core;
using Color = iFactr.UI.Color;
using Point = iFactr.UI.Point;
using Size = iFactr.UI.Size;
using View = iFactr.UI.View;

namespace iFactr.Compact
{
    public partial class SmoothListbox : Control, IListView, INotifyPropertyChanged
    {
        #region List animation members

        /// <summary>
        /// Makes sure rendering of the list occurs only once per animation tick.
        /// </summary>
        private bool _renderLockFlag;

        private Point _mouseDownPoint;
        private Point _previousPoint;
        private bool _scrolling;
        private bool _mouseIsDown;
        private bool _mouseDragLock;
        private double _pixelsPerFrame;
        private int _frame;
        private double _destination = double.NaN;
        private int _panelHeight;
        private int _windowHeight;
        private readonly Queue<double> _velocityPoints = new Queue<double>();

        public event EventHandler Scrolling;

        #endregion

        public SmoothListbox()
        {
            InitializeComponent();
            Sections = new SectionCollection();
            ValidationErrors = new ValidationErrorCollection();
            _submitValues = new Dictionary<string, string>();
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            PerformAnimation();
            _renderLockFlag = false;
        }

        /// <summary>
        /// This method calculates the new velocity and the distance the list items
        /// should scroll. It also handles snapback if the list has been 
        /// scrolled out of bounds.
        /// </summary>
        private void PerformAnimation()
        {
            if (Parent == null || _renderLockFlag)
            {
                _frame++;
                return;
            }

            if (CoreDll.GetAsyncKeyState(Keys.LButton) == -1 != _mouseIsDown)
            {
                _mouseIsDown = !_mouseIsDown;
                if (_mouseIsDown)
                {
                    _destination = double.NaN;
                    _previousPoint = _mouseDownPoint = CoreDll.MousePosition.ToPoint();
                }
                else
                {
                    var absolutePoint = CoreDll.MousePosition;
                    if (absolutePoint.Y < 0)
                    {
                        _velocityPoints.Enqueue(0);
                    }
                    Fling();
                }
            }

            if (_mouseIsDown)
            {
                var absolutePoint = CoreDll.MousePosition;
                if (absolutePoint.Y < 0)
                {
                    _velocityPoints.Enqueue(0);
                }
                else
                {
                    if (_mouseDragLock)
                    {
                        if (Math.Abs(absolutePoint.Y - _mouseDownPoint.Y) > 7 * CompactFactory.Instance.DpiScale)
                        {
                            _mouseDragLock = false;
                            _previousPoint = _mouseDownPoint = absolutePoint.ToPoint();
                        }
                        return;
                    }

                    var delta = absolutePoint.Y - _previousPoint.Y;
                    _previousPoint = absolutePoint.ToPoint();

                    // Don't allow drag outside top screen limit
                    if (_panelHeight <= _windowHeight || itemsPanel.Top + delta > 0)
                        delta = -itemsPanel.Top;

                    // or outside bottom screen limit
                    if (_panelHeight > _windowHeight && itemsPanel.Bottom + delta < _windowHeight)
                        delta = _windowHeight - itemsPanel.Top - _panelHeight;

                    if (!_mouseDragLock)
                    {
                        ScrollItems((int)delta);
                    }
                    _velocityPoints.Enqueue(delta);
                }
                while (_velocityPoints.Count > 5)
                {
                    _velocityPoints.Dequeue();
                }
                return;
            }

            // If the velocity induced by the user dragging the list
            // results in a deltaDistance greater than 1.0f pixels 
            // then scroll the items that distance.
            if (Math.Abs(_pixelsPerFrame) > 1.0f)
            {
                ScrollItems((int)_pixelsPerFrame);
                _pixelsPerFrame *= .85f;
            }
            else if (!double.IsNaN(_destination))
            {
                const int animationDuration = 300;
                var totalDistance = (int)(_mouseDownPoint.Y + _destination);
                var currentTimeMs = ++_frame * animationTimer.Interval;
                var currentDistance = _mouseDownPoint.Y + itemsPanel.Top;
                var framePixels = EasingFunction(currentTimeMs, _mouseDownPoint.Y, totalDistance, animationDuration);

                if (currentTimeMs >= animationDuration)
                {
                    ScrollItems((int)_destination - itemsPanel.Top);
                    _destination = double.NaN;
                }
                else
                {
                    ScrollItems((int)(framePixels - currentDistance - _mouseDownPoint.Y));
                }
            }
            else
            {
                _pixelsPerFrame = 0;
                _mouseDragLock = true;
                _scrolling = false;

                if (itemsPanel.Top > 0)
                {
                    _destination = 0;
                    _frame = 0;
                }

                if (_panelHeight > _windowHeight && itemsPanel.Top + _panelHeight < _windowHeight)
                {
                    _destination = _windowHeight - _panelHeight;
                    _frame = 0;
                }
            }
        }

        public static int EasingFunction(double milliseconds, double beginning, double totalDistance, double totalTimeMs)
        {
            if (CompactFactory.Instance.EasingFunction != null)
            {
                return (int)CompactFactory.Instance.EasingFunction(milliseconds, beginning, totalDistance, totalTimeMs);
            }
            return (int)ExpoEaseOut(milliseconds, beginning, totalDistance, totalTimeMs);
        }

        private void Fling()
        {
            if (!_velocityPoints.Any()) return;

            var delta = _velocityPoints.Average();
            var top = 0;
            var rootForm = CompactFactory.Instance.RootForm as RootForm;
            if (rootForm != null) { top = CompactFactory.TopPadding; }

            if (itemsPanel.Top + delta + top < 0 && itemsPanel.Bottom + delta >= _windowHeight)
            {
                _pixelsPerFrame = delta;
            }
            _velocityPoints.Clear();
        }

        #region Mouse handlers

        /// <summary>
        /// Handles the mouse up event and determines if the list needs to animate 
        /// after it has been "released".
        /// </summary>
        private void MouseUpHandler(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _mouseIsDown = false;

            var mouseUpPoint = new System.Drawing.Point(e.X, e.Y + Top);

            // If the mouse was lifted from the same location it was pressed down on 
            // then this is not a drag but a click, do item selection logic instead
            // of dragging logic.
            if (!_mouseDragLock)
            {
                for (var iterator = (Control)sender; iterator != this; iterator = iterator.Parent)
                {
                    mouseUpPoint.Offset(iterator.Left, iterator.Top);
                }

                _velocityPoints.Enqueue(mouseUpPoint.Y - _previousPoint.Y);
                Fling();
                return;
            }

            // Get the list item (regardless if it was a child Control that was clicked). 
            Control item = null;
            if (sender != this && sender != itemsPanel)
            {
                var parent = sender as Control;
                while (parent != null && parent.Parent != itemsPanel)
                    parent = parent.Parent;
                item = parent;
            }

            if (item != null)
            {
                ResetSelection(item);
                if (SelectedItem != null)
                {
                    SelectedItem.SelectControlAt(mouseUpPoint);
                }
            }
            _pixelsPerFrame = 0;
        }

        internal void ResetSelection(Control select)
        {
            for (int index = 0; index < itemsPanel.Controls.Count; index++)
            {
                var listItem = itemsPanel.Controls[index];
                var selected = listItem == select;

                var cell = listItem as GridCell;
                if (cell != null)
                {
                    if (selected)
                    {
                        _selectedIndex = index;
                        cell.Highlight();
                    }
                    else cell.Deselect();
                }
                else if (selected)
                {
                    listItem.Focus();
                }
            }
        }

        #endregion

        #region Scroll members

        private void SmoothScrollItems(int offset)
        {
            var target = Math.Min(Math.Max(offset + itemsPanel.Top, _windowHeight - _panelHeight), 0);
            if (target == itemsPanel.Top) return;
            _destination = target;
            _frame = 0;
            _mouseDownPoint = new Point(0, -itemsPanel.Top);
        }

        /// <summary>
        /// Scrolls the member itemsPanel by offset.
        /// </summary>
        private void ScrollItems(int offset)
        {
            if (_renderLockFlag) return;
            _renderLockFlag = true;

            if (!_scrolling && offset != 0)
            {
                _scrolling = true;
                var scrollEvent = Scrolling;
                if (scrollEvent != null) scrollEvent(this, EventArgs.Empty);
            }

            SuspendLayout();
            itemsPanel.Top += offset;
            AddVirtualCells();
            ResumeLayout(false);
        }

        public void ScrollToCell(int section, int index, bool animated)
        {
            var cellIndex = GetPosition(section, index);
            while (itemsPanel.Controls.Count <= cellIndex)
            {
                AddVirtualCells(true);
            }
            int destination = -itemsPanel.Controls[cellIndex].Top - itemsPanel.Top;
            if (animated) SmoothScrollItems(destination);
            else ScrollItems(destination);
        }

        public void ScrollToEnd(bool animated)
        {
            int sectionIndex = Sections.Count - 1;
            var section = Sections[sectionIndex];

            int index = section.ItemCount;
            if (section.Footer == null || string.IsNullOrEmpty(section.Footer.Text))
                index--;
            int cellIndex = GetPosition(sectionIndex, index);
            while (itemsPanel.Controls.Count <= cellIndex)
            {
                AddVirtualCells(true);
            }
            int destination = -itemsPanel.Controls[cellIndex].Bottom - itemsPanel.Top;
            if (animated) SmoothScrollItems(destination);
            else ScrollItems(destination);
        }

        public void ScrollToHome(bool animated)
        {
            int destination = -itemsPanel.Top;
            if (animated) SmoothScrollItems(destination);
            else ScrollItems(destination);
        }

        public void ScrollPageUp(bool animated)
        {
            int destination = ClientSize.Height;
            if (animated) SmoothScrollItems(destination);
            else ScrollItems(destination);
        }

        public void ScrollPageDown(bool animated)
        {
            int destination = -ClientSize.Height;
            if (animated) SmoothScrollItems(destination);
            else ScrollItems(destination);
        }

        public void HighlightPrevious()
        {
            int begin = _selectedIndex;
            if (SelectedItem != null)
                SelectedItem.Deselect();

            int sectionIndex = Sections.Count - 1;
            var section = Sections[sectionIndex];

            int cellIndex = section.ItemCount;
            if (section.Footer == null || string.IsNullOrEmpty(section.Footer.Text))
                cellIndex--;
            do
            {
                _selectedIndex--;
                if (_selectedIndex == begin) return;
                if (_selectedIndex < 0)
                {
                    _selectedIndex = GetPosition(sectionIndex, cellIndex);
                }
            } while (SelectedItem == null);

            SelectedItem.Highlight();

            if (SelectedItem.Top + itemsPanel.Top < 0 ||
                SelectedItem.Bottom + itemsPanel.Top > _windowHeight)
            {
                SmoothScrollItems(-(SelectedItem.Top + itemsPanel.Top));
            }
        }

        public void HighlightNext()
        {
            int begin = _selectedIndex;
            if (_selectedIndex > -1)
                SelectedItem.Deselect();

            int sectionIndex = Sections.Count - 1;
            var section = Sections[sectionIndex];

            int cellIndex = section.ItemCount;
            if (section.Footer == null || string.IsNullOrEmpty(section.Footer.Text))
                cellIndex--;

            do
            {
                _selectedIndex++;
                if (_selectedIndex == begin) return;
                if (_selectedIndex > GetPosition(sectionIndex, cellIndex))
                {
                    if (begin < 1)
                    {
                        _selectedIndex = begin;
                        return;
                    }
                    _selectedIndex = 0;
                }
            } while (SelectedItem == null);

            SelectedItem.Highlight();

            if (SelectedItem.Top + itemsPanel.Top < 0 ||
                SelectedItem.Bottom + itemsPanel.Top > _windowHeight)
            {
                SmoothScrollItems(-SelectedItem.Top - itemsPanel.Top);
            }
        }

        public GridCell SelectedItem
        {
            get
            {
                while (itemsPanel.Controls.Count <= _selectedIndex)
                {
                    AddVirtualCells(true);
                }
                return _selectedIndex < 0 ? null : itemsPanel.Controls[_selectedIndex] as GridCell;
            }
            internal set { _selectedIndex = itemsPanel.Controls.IndexOf(value); }
        }
        private int _selectedIndex = -1;

        #endregion

        internal int GetPosition(int section, int index)
        {
            int position = 0;
            for (int i = 0; i <= section; i++)
            {
                var s = Sections[i];
                if (s.Header != null && !string.IsNullOrEmpty(s.Header.Text))
                    position++;
                if (i == section) break;
                position += s.ItemCount;
                if (s.Footer != null && !string.IsNullOrEmpty(s.Footer.Text))
                    position++;
            }
            return position + index;
        }

        /// <summary>
        /// Layout the items and make sure they line up properly as they 
        /// can change size during runtime.
        /// </summary>
        public void LayoutItems(Control adjustAfter)
        {
            int top = adjustAfter.Bottom;
            for (int i = itemsPanel.Controls.IndexOf(adjustAfter) + 1; i < itemsPanel.Controls.Count; i++)
            {
                var c = itemsPanel.Controls[i];
                c.Location = new Point(0, top).ToPoint();
                c.Width = itemsPanel.ClientSize.Width;
                top = c.Bottom;
            }
            AddVirtualCells();
        }

        private void AddVirtualCells()
        {
            AddVirtualCells(false);
        }

        private void AddVirtualCells(bool readAhead)
        {
            if (Sections != null && Sections.Any() && Parent != null)
            {
                int top = itemsPanel.Controls.Count == 0 ? 0 : itemsPanel.Controls.Count == 0 ? 0 : itemsPanel.Controls.Cast<Control>().Last().Bottom;
                var height = top;
                while (_section != -1 && _section < Sections.Count && top + itemsPanel.Top < (readAhead ? height + CompactFactory.Instance.RootForm.Height : CompactFactory.Instance.RootForm.Height * 2))
                {
                    if (_cell > Sections[_section].ItemCount)
                    {
                        _cell = -1;
                        _section++;
                        if (_section >= Sections.Count)
                        {
                            _section = -1;
                            break;
                        }
                    }
                    var control = GetControl(_section, _cell++, null);
                    if (control == null) continue;

                    control.Width = itemsPanel.Width;

                    control.Location = new Point(0, itemsPanel.Controls.Count == 0 ? 0 : itemsPanel.Controls.Cast<Control>().Last().Bottom).ToPoint();
                    control.Parent = itemsPanel;
                    SetHandlers(control, MouseUpHandler);
                    top = control.Bottom;
                }
            }
            _panelHeight = itemsPanel.Height = itemsPanel.Controls.Count == 0 ? 0 : itemsPanel.Controls.Cast<Control>().Last().Bottom;
        }

        private int _section, _cell = -1;

        private Control GetControl(int sectionIndex, int cellIndex, ICell recycledCell)
        {
            Control control = null;
            if (cellIndex == -1)
            {
                var header = Sections[sectionIndex].Header;
                if (header == null) return null;
                control = CompactFactory.GetNativeObject<HeaderControl>(header, "header");
            }
            else if (cellIndex == Sections[sectionIndex].ItemCount)
            {
                var footer = Sections[sectionIndex].Footer;
                if (footer == null) return null;
                control = CompactFactory.GetNativeObject<FooterControl>(footer, "footer");
            }
            else if (cellIndex < Sections[sectionIndex].ItemCount)
            {
                ICell abCell;

                var sectionHandler = Sections[sectionIndex].CellRequested;
                if (sectionHandler != null)
                {
                    abCell = sectionHandler.Invoke(cellIndex, recycledCell);
                }
                else
                {
                    var listHandler = CellRequested;
                    abCell = listHandler == null ? null : listHandler.Invoke(sectionIndex, cellIndex, recycledCell);
                }

                if (abCell is CustomItemContainer)
                {
                    var item = ((CustomItemContainer)abCell).CustomItem;
                    control = item as Control;
                    abCell = item as ICell;
                }
                if (abCell is IGridCell || abCell != null && abCell.Pair is IGridCell)
                {
                    var concreteCell = CompactFactory.GetNativeObject<GridCell>(abCell, "abCell");
                    if (concreteCell == null) return null;
                    foreach (var s in concreteCell.Controls.OfType<SelectList>())
                    {
                        //Local variable as compiler hint
                        var select = s;
                        Scrolling += (sender, args) => select.HideList();
                    }
                    control = concreteCell;
                }
                else if (abCell is IRichContentCell || abCell != null && abCell.Pair is IRichContentCell)
                {
                    var concreteCell = CompactFactory.GetNativeObject<RichText>(abCell, "abCell");
                    if (concreteCell == null) return null;
                    control = concreteCell;
                    concreteCell.View = this;
                    concreteCell.Load();
                }
            }

            return control;
        }

        /// <summary>
        /// When resizing the list box the iternal items has to 
        /// be resized as well.
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            int top = 0;
            itemsPanel.Location = new Point(0, itemsPanel.Location.Y).ToPoint();
            itemsPanel.Width = ClientSize.Width;
            _windowHeight = ClientSize.Height;
            for (int i = 0; i < itemsPanel.Controls.Count; i++)
            {
                var control = itemsPanel.Controls[i];
                if (control.Width == itemsPanel.ClientSize.Width) continue;
                var grid = control as GridControl;
                if (grid != null)
                {
                    var minsize = new Size(itemsPanel.Width, grid.MinHeight);
                    var maxsize = new Size(itemsPanel.Width, grid.MaxHeight);
                    grid.Size = grid.PerformLayout(minsize, maxsize).ToSize();
                }
                else
                {
                    control.Width = itemsPanel.Width;
                }
                control.Location = new Point(0, top).ToPoint();
                top = control.Bottom;
            }
            AddVirtualCells();
            ScrollItems(0);
            PrepareBackground(_backer);
        }

        #region IListView Members

        public Color SeparatorColor
        {
            get;
            set;
        }

        public ListViewStyle Style
        {
            get { return ListViewStyle.Default; }
        }

        public IMenu Menu
        {
            get { return _menu; }
            set
            {
                if (_menu == value) return;
                _menu = value;
                OnPropertyChanged("Menu");
            }
        }
        private IMenu _menu;

        public ISearchBox SearchBox
        {
            get
            {
                return _searchBox;
            }
            set
            {
                if (_searchBox == value) return;
                _searchBox = value;
                OnPropertyChanged("SearchBox");
            }
        }

        private ISearchBox _searchBox;

        public SectionCollection Sections
        {
            get;
            private set;
        }

        public ValidationErrorCollection ValidationErrors
        {
            get;
            private set;
        }

        public CellDelegate CellRequested
        {
            get;
            set;
        }

        public ItemIdDelegate ItemIdRequested
        {
            get;
            set;
        }

        public event SubmissionEventHandler Submitting;

        public IDictionary<string, string> GetSubmissionValues() { return _submitValues; }
        private readonly Dictionary<string, string> _submitValues;

        public void ReloadSections()
        {
            foreach (var item in itemsPanel.Controls.Cast<Control>())
            {
                item.Parent = null;
            }
            _section = 0;
            _cell = -1;
            ScrollItems(0);
        }

        public IEnumerable<ICell> GetVisibleCells()
        {
            var retval = new List<ICell>();
            var offset = itemsPanel.Top;
            int cellIndex = 0;
            while (offset < 0)
            {
                offset += itemsPanel.Controls[cellIndex++].Height;
            }
            while (offset < Height && cellIndex < itemsPanel.Controls.Count)
            {
                var control = itemsPanel.Controls[cellIndex++];
                var cell = control as ICell ?? new CustomItemContainer(control);
                if (cell.Pair != null) cell = cell.Pair as ICell;
                retval.Add(cell);
                offset += control.Height;
            }
            return retval;
        }

        public void Submit(string url)
        {
            Submit(new Link(url));
        }

        public void Submit(Link link)
        {
            var submitValues = (_model is iLayer) ? ((iLayer)_model).GetFieldValues() : GetSubmissionValues();

            var args = new SubmissionEventArgs(link, ValidationErrors);
            Submitting.Raise(this, args);

            if (args.Cancel) return;
            link.Parameters.AddRange(submitValues);
            CompactFactory.Navigate(link, this);
        }

        public ColumnMode ColumnMode
        {
            get;
            set;
        }

        #endregion

        #region IView Members

        public Color HeaderColor
        {
            get;
            set;
        }

        public new double Height
        {
            get { return base.Height; }
        }

        public MetadataCollection Metadata
        {
            get { return _metadata ?? (_metadata = new MetadataCollection()); }
        }
        private MetadataCollection _metadata;

        public PreferredOrientation PreferredOrientations
        {
            get;
            set;
        }

        public string Title
        {
            get;
            set;
        }

        public Color TitleColor
        {
            get;
            set;
        }

        public new double Width
        {
            get { return base.Width; }
        }

        public event EventHandler Rendering;

        public void SetBackground(Color color)
        {
            _backgroundColor = color.HexCode;
            _backer = null;
        }

        public void SetBackground(string imagePath, ContentStretch stretch)
        {
            _backer = new ImageControl { Size = new Size(Width, Height), Stretch = stretch, };
            _backer.Loaded += (o, ev) => PrepareBackground(_backer);
            _backer.FilePath = imagePath;
            _backgroundColor = null;
        }

        private void PrepareBackground(IPaintable backgroundImage)
        {
            if (backgroundImage == null)
            {
                return;
            }

            if (BackgroundBitmap != null)
            {
                BackgroundBitmap.Dispose();
            }

            BackgroundBitmap = new Bitmap((int)Width, (int)Height);
            backgroundImage.Size = new Size(Width, Height);
            using (var graphics = Graphics.FromImage(BackgroundBitmap))
            {
                backgroundImage.Paint(graphics);
            }
        }

        internal Bitmap BackgroundBitmap;
        private string _backgroundColor;
        private ImageControl _backer;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (BackgroundBitmap == null)
            {
                var color = new Color(_backgroundColor ?? CompactFactory.Instance.Style.LayerBackgroundColor.HexCode).ToColor();
                e.Graphics.Clear(color);
            }
            else
            {
                e.Graphics.DrawImage(BackgroundBitmap, ClientRectangle, ClientRectangle, GraphicsUnit.Pixel);
            }
        }

        #endregion

        #region IMXView Members

        private object _model;

        public object GetModel()
        {
            return _model;
        }

        public void SetModel(object model)
        {
            _model = model;
        }

        public Type ModelType
        {
            get { return _model == null ? null : _model.GetType(); }
        }

        public void Render()
        {
            var rendering = Rendering;
            if (rendering != null) { rendering(Pair ?? this, EventArgs.Empty); }
            ReloadSections();
        }

        #endregion

        #region IHistoryEntry Members

        public Link BackLink
        {
            get;
            set;
        }

        public string StackID
        {
            get;
            set;
        }

        public Pane OutputPane
        {
            get;
            set;
        }

        public PopoverPresentationStyle PopoverPresentationStyle
        {
            get;
            set;
        }

        public ShouldNavigateDelegate ShouldNavigate
        {
            get;
            set;
        }

        public IHistoryStack Stack
        {
            get
            {
                return PaneManager.Instance.FromNavContext(OutputPane, PaneManager.Instance.CurrentTab);
            }
        }

        public event EventHandler Activated;

        public event EventHandler Deactivated;

        #endregion

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

        public bool Equals(IView other)
        {
            var view = other as View;
            if (view != null)
            {
                return view.Equals(this);
            }

            return base.Equals(other);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.itemsPanel = new System.Windows.Forms.Control();
            this.animationTimer = new System.Windows.Forms.Timer();
            this.SuspendLayout();

            this.itemsPanel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.itemsPanel.Location = new System.Drawing.Point(0, 0);
            this.itemsPanel.Name = "itemsPanel";
            this.itemsPanel.Size = new System.Drawing.Size(0, CompactFactory.Instance.RootForm.Width);

            this.animationTimer.Enabled = false;
            this.animationTimer.Interval = 30;
            this.animationTimer.Tick += new System.EventHandler(this.AnimationTick);

            this.Controls.Add(this.itemsPanel);
            this.Name = "SmoothListbox";
            this.Size = CompactFactory.Instance.RootForm.Size;
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Control itemsPanel;
        internal System.Windows.Forms.Timer animationTimer;

        #endregion

        public static void SetHandlers(Control control, MouseEventHandler mouseUpEventHandler)
        {
            RemoveHandlers(control, mouseUpEventHandler);

            control.MouseUp += mouseUpEventHandler;

            foreach (Control childControl in control.Controls)
            {
                SetHandlers(childControl, mouseUpEventHandler);
            }
        }

        public static void RemoveHandlers(Control control, MouseEventHandler mouseUpEventHandler)
        {
            control.MouseUp -= mouseUpEventHandler;

            foreach (Control childControl in control.Controls)
            {
                RemoveHandlers(childControl, mouseUpEventHandler);
            }
        }


        #region Equations

        #region Linear

        /// <summary>
        /// Easing equation function for a simple linear tweening, with no easing.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double Linear(double t, double b, double c, double d)
        {
            return c * t / d + b;
        }

        #endregion

        #region Expo

        /// <summary>
        /// Easing equation function for an exponential (2^t) easing out: 
        /// decelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double ExpoEaseOut(double t, double b, double c, double d)
        {
            return (t == d) ? b + c : c * (-Math.Pow(2, -10 * t / d) + 1) + b;
        }

        /// <summary>
        /// Easing equation function for an exponential (2^t) easing in: 
        /// accelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double ExpoEaseIn(double t, double b, double c, double d)
        {
            return (t == 0) ? b : c * Math.Pow(2, 10 * (t / d - 1)) + b;
        }

        /// <summary>
        /// Easing equation function for an exponential (2^t) easing in/out: 
        /// acceleration until halfway, then deceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double ExpoEaseInOut(double t, double b, double c, double d)
        {
            if (t == 0)
                return b;

            if (t == d)
                return b + c;

            if ((t /= d / 2) < 1)
                return c / 2 * Math.Pow(2, 10 * (t - 1)) + b;

            return c / 2 * (-Math.Pow(2, -10 * --t) + 2) + b;
        }

        /// <summary>
        /// Easing equation function for an exponential (2^t) easing out/in: 
        /// deceleration until halfway, then acceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double ExpoEaseOutIn(double t, double b, double c, double d)
        {
            if (t < d / 2)
                return ExpoEaseOut(t * 2, b, c / 2, d);

            return ExpoEaseIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region Circular

        /// <summary>
        /// Easing equation function for a circular (sqrt(1-t^2)) easing out: 
        /// decelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double CircEaseOut(double t, double b, double c, double d)
        {
            return c * Math.Sqrt(1 - (t = t / d - 1) * t) + b;
        }

        /// <summary>
        /// Easing equation function for a circular (sqrt(1-t^2)) easing in: 
        /// accelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double CircEaseIn(double t, double b, double c, double d)
        {
            return -c * (Math.Sqrt(1 - (t /= d) * t) - 1) + b;
        }

        /// <summary>
        /// Easing equation function for a circular (sqrt(1-t^2)) easing in/out: 
        /// acceleration until halfway, then deceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double CircEaseInOut(double t, double b, double c, double d)
        {
            if ((t /= d / 2) < 1)
                return -c / 2 * (Math.Sqrt(1 - t * t) - 1) + b;

            return c / 2 * (Math.Sqrt(1 - (t -= 2) * t) + 1) + b;
        }

        /// <summary>
        /// Easing equation function for a circular (sqrt(1-t^2)) easing in/out: 
        /// acceleration until halfway, then deceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double CircEaseOutIn(double t, double b, double c, double d)
        {
            if (t < d / 2)
                return CircEaseOut(t * 2, b, c / 2, d);

            return CircEaseIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region Quad

        /// <summary>
        /// Easing equation function for a quadratic (t^2) easing out: 
        /// decelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double QuadEaseOut(double t, double b, double c, double d)
        {
            return -c * (t /= d) * (t - 2) + b;
        }

        /// <summary>
        /// Easing equation function for a quadratic (t^2) easing in: 
        /// accelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double QuadEaseIn(double t, double b, double c, double d)
        {
            return c * (t /= d) * t + b;
        }

        /// <summary>
        /// Easing equation function for a quadratic (t^2) easing in/out: 
        /// acceleration until halfway, then deceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double QuadEaseInOut(double t, double b, double c, double d)
        {
            if ((t /= d / 2) < 1)
                return c / 2 * t * t + b;

            return -c / 2 * ((--t) * (t - 2) - 1) + b;
        }

        /// <summary>
        /// Easing equation function for a quadratic (t^2) easing out/in: 
        /// deceleration until halfway, then acceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double QuadEaseOutIn(double t, double b, double c, double d)
        {
            if (t < d / 2)
                return QuadEaseOut(t * 2, b, c / 2, d);

            return QuadEaseIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region Sine

        /// <summary>
        /// Easing equation function for a sinusoidal (sin(t)) easing out: 
        /// decelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double SineEaseOut(double t, double b, double c, double d)
        {
            return c * Math.Sin(t / d * (Math.PI / 2)) + b;
        }

        /// <summary>
        /// Easing equation function for a sinusoidal (sin(t)) easing in: 
        /// accelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double SineEaseIn(double t, double b, double c, double d)
        {
            return -c * Math.Cos(t / d * (Math.PI / 2)) + c + b;
        }

        /// <summary>
        /// Easing equation function for a sinusoidal (sin(t)) easing in/out: 
        /// acceleration until halfway, then deceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double SineEaseInOut(double t, double b, double c, double d)
        {
            if ((t /= d / 2) < 1)
                return c / 2 * (Math.Sin(Math.PI * t / 2)) + b;

            return -c / 2 * (Math.Cos(Math.PI * --t / 2) - 2) + b;
        }

        /// <summary>
        /// Easing equation function for a sinusoidal (sin(t)) easing in/out: 
        /// deceleration until halfway, then acceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double SineEaseOutIn(double t, double b, double c, double d)
        {
            if (t < d / 2)
                return SineEaseOut(t * 2, b, c / 2, d);

            return SineEaseIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region Cubic

        /// <summary>
        /// Easing equation function for a cubic (t^3) easing out: 
        /// decelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double CubicEaseOut(double t, double b, double c, double d)
        {
            return c * ((t = t / d - 1) * t * t + 1) + b;
        }

        /// <summary>
        /// Easing equation function for a cubic (t^3) easing in: 
        /// accelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double CubicEaseIn(double t, double b, double c, double d)
        {
            return c * (t /= d) * t * t + b;
        }

        /// <summary>
        /// Easing equation function for a cubic (t^3) easing in/out: 
        /// acceleration until halfway, then deceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double CubicEaseInOut(double t, double b, double c, double d)
        {
            if ((t /= d / 2) < 1)
                return c / 2 * t * t * t + b;

            return c / 2 * ((t -= 2) * t * t + 2) + b;
        }

        /// <summary>
        /// Easing equation function for a cubic (t^3) easing out/in: 
        /// deceleration until halfway, then acceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double CubicEaseOutIn(double t, double b, double c, double d)
        {
            if (t < d / 2)
                return CubicEaseOut(t * 2, b, c / 2, d);

            return CubicEaseIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region Quartic

        /// <summary>
        /// Easing equation function for a quartic (t^4) easing out: 
        /// decelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double QuartEaseOut(double t, double b, double c, double d)
        {
            return -c * ((t = t / d - 1) * t * t * t - 1) + b;
        }

        /// <summary>
        /// Easing equation function for a quartic (t^4) easing in: 
        /// accelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double QuartEaseIn(double t, double b, double c, double d)
        {
            return c * (t /= d) * t * t * t + b;
        }

        /// <summary>
        /// Easing equation function for a quartic (t^4) easing in/out: 
        /// acceleration until halfway, then deceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double QuartEaseInOut(double t, double b, double c, double d)
        {
            if ((t /= d / 2) < 1)
                return c / 2 * t * t * t * t + b;

            return -c / 2 * ((t -= 2) * t * t * t - 2) + b;
        }

        /// <summary>
        /// Easing equation function for a quartic (t^4) easing out/in: 
        /// deceleration until halfway, then acceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double QuartEaseOutIn(double t, double b, double c, double d)
        {
            if (t < d / 2)
                return QuartEaseOut(t * 2, b, c / 2, d);

            return QuartEaseIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region Quintic

        /// <summary>
        /// Easing equation function for a quintic (t^5) easing out: 
        /// decelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double QuintEaseOut(double t, double b, double c, double d)
        {
            return c * ((t = t / d - 1) * t * t * t * t + 1) + b;
        }

        /// <summary>
        /// Easing equation function for a quintic (t^5) easing in: 
        /// accelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double QuintEaseIn(double t, double b, double c, double d)
        {
            return c * (t /= d) * t * t * t * t + b;
        }

        /// <summary>
        /// Easing equation function for a quintic (t^5) easing in/out: 
        /// acceleration until halfway, then deceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double QuintEaseInOut(double t, double b, double c, double d)
        {
            if ((t /= d / 2) < 1)
                return c / 2 * t * t * t * t * t + b;
            return c / 2 * ((t -= 2) * t * t * t * t + 2) + b;
        }

        /// <summary>
        /// Easing equation function for a quintic (t^5) easing in/out: 
        /// acceleration until halfway, then deceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double QuintEaseOutIn(double t, double b, double c, double d)
        {
            if (t < d / 2)
                return QuintEaseOut(t * 2, b, c / 2, d);
            return QuintEaseIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region Elastic

        /// <summary>
        /// Easing equation function for an elastic (exponentially decaying sine wave) easing out: 
        /// decelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double ElasticEaseOut(double t, double b, double c, double d)
        {
            if ((t /= d) == 1)
                return b + c;

            double p = d * .3;
            double s = p / 4;

            return (c * Math.Pow(2, -10 * t) * Math.Sin((t * d - s) * (2 * Math.PI) / p) + c + b);
        }

        /// <summary>
        /// Easing equation function for an elastic (exponentially decaying sine wave) easing in: 
        /// accelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double ElasticEaseIn(double t, double b, double c, double d)
        {
            if ((t /= d) == 1)
                return b + c;

            double p = d * .3;
            double s = p / 4;

            return -(c * Math.Pow(2, 10 * (t -= 1)) * Math.Sin((t * d - s) * (2 * Math.PI) / p)) + b;
        }

        /// <summary>
        /// Easing equation function for an elastic (exponentially decaying sine wave) easing in/out: 
        /// acceleration until halfway, then deceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double ElasticEaseInOut(double t, double b, double c, double d)
        {
            if ((t /= d / 2) == 2)
                return b + c;

            double p = d * (.3 * 1.5);
            double s = p / 4;

            if (t < 1)
                return -.5 * (c * Math.Pow(2, 10 * (t -= 1)) * Math.Sin((t * d - s) * (2 * Math.PI) / p)) + b;
            return c * Math.Pow(2, -10 * (t -= 1)) * Math.Sin((t * d - s) * (2 * Math.PI) / p) * .5 + c + b;
        }

        /// <summary>
        /// Easing equation function for an elastic (exponentially decaying sine wave) easing out/in: 
        /// deceleration until halfway, then acceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double ElasticEaseOutIn(double t, double b, double c, double d)
        {
            if (t < d / 2)
                return ElasticEaseOut(t * 2, b, c / 2, d);
            return ElasticEaseIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region Bounce

        /// <summary>
        /// Easing equation function for a bounce (exponentially decaying parabolic bounce) easing out: 
        /// decelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double BounceEaseOut(double t, double b, double c, double d)
        {
            if ((t /= d) < (1 / 2.75))
                return c * (7.5625 * t * t) + b;
            else if (t < (2 / 2.75))
                return c * (7.5625 * (t -= (1.5 / 2.75)) * t + .75) + b;
            else if (t < (2.5 / 2.75))
                return c * (7.5625 * (t -= (2.25 / 2.75)) * t + .9375) + b;
            else
                return c * (7.5625 * (t -= (2.625 / 2.75)) * t + .984375) + b;
        }

        /// <summary>
        /// Easing equation function for a bounce (exponentially decaying parabolic bounce) easing in: 
        /// accelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double BounceEaseIn(double t, double b, double c, double d)
        {
            return c - BounceEaseOut(d - t, 0, c, d) + b;
        }

        /// <summary>
        /// Easing equation function for a bounce (exponentially decaying parabolic bounce) easing in/out: 
        /// acceleration until halfway, then deceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double BounceEaseInOut(double t, double b, double c, double d)
        {
            if (t < d / 2)
                return BounceEaseIn(t * 2, 0, c, d) * .5 + b;
            else
                return BounceEaseOut(t * 2 - d, 0, c, d) * .5 + c * .5 + b;
        }

        /// <summary>
        /// Easing equation function for a bounce (exponentially decaying parabolic bounce) easing out/in: 
        /// deceleration until halfway, then acceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double BounceEaseOutIn(double t, double b, double c, double d)
        {
            if (t < d / 2)
                return BounceEaseOut(t * 2, b, c / 2, d);
            return BounceEaseIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region Back

        /// <summary>
        /// Easing equation function for a back (overshooting cubic easing: (s+1)*t^3 - s*t^2) easing out: 
        /// decelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double BackEaseOut(double t, double b, double c, double d)
        {
            return c * ((t = t / d - 1) * t * ((1.70158 + 1) * t + 1.70158) + 1) + b;
        }

        /// <summary>
        /// Easing equation function for a back (overshooting cubic easing: (s+1)*t^3 - s*t^2) easing in: 
        /// accelerating from zero velocity.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double BackEaseIn(double t, double b, double c, double d)
        {
            return c * (t /= d) * t * ((1.70158 + 1) * t - 1.70158) + b;
        }

        /// <summary>
        /// Easing equation function for a back (overshooting cubic easing: (s+1)*t^3 - s*t^2) easing in/out: 
        /// acceleration until halfway, then deceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double BackEaseInOut(double t, double b, double c, double d)
        {
            double s = 1.70158;
            if ((t /= d / 2) < 1)
                return c / 2 * (t * t * (((s *= (1.525)) + 1) * t - s)) + b;
            return c / 2 * ((t -= 2) * t * (((s *= (1.525)) + 1) * t + s) + 2) + b;
        }

        /// <summary>
        /// Easing equation function for a back (overshooting cubic easing: (s+1)*t^3 - s*t^2) easing out/in: 
        /// deceleration until halfway, then acceleration.
        /// </summary>
        /// <param name="t">Current time in seconds.</param>
        /// <param name="b">Starting value.</param>
        /// <param name="c">Final value.</param>
        /// <param name="d">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double BackEaseOutIn(double t, double b, double c, double d)
        {
            if (t < d / 2)
                return BackEaseOut(t * 2, b, c / 2, d);
            return BackEaseIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #endregion
    }
}