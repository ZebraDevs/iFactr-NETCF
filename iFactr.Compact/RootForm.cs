using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using iFactr.Core;
using iFactr.Core.Layers;
using Microsoft.WindowsCE.Forms;
using MonoCross.Navigation;
using iFactr.UI;
using MonoCross.Utilities;
using Point = System.Drawing.Point;
using iFactr.UI.Controls;
using Button = iFactr.Core.Controls.Button;
using Control = System.Windows.Forms.Control;

namespace iFactr.Compact
{
    public partial class RootForm : Form, IDisposable
    {
        private readonly HookKeys _hook;

        public RootForm()
        {
            InitializeComponent();
            _inputPanel = new InputPanel(new System.ComponentModel.Container());
            _inputPanel.EnabledChanged += inputPanel_EnabledChanged;

            Closing += RootForm_Closing;
            Deactivate += RootForm_Deactivate;
            Activated += RootForm_Activated;

            _hook = new HookKeys();
            _hook.HookEvent += HookEvent;
        }

        private void inputPanel_EnabledChanged(object sender, EventArgs e)
        {
            var display = Controls.Count > 0 ? Controls[Controls.Count - 1] : null;
            if (display == null) return;
            int inputHeight = _inputPanel.Bounds.Height;
            if (_inputPanel.Enabled)
            {
                display.Height -= inputHeight;
            }
            else
            {
                var height = Screen.PrimaryScreen.WorkingArea.Height - SystemInformation.MenuHeight;
                if (Controls.Count > 1)
                {
                    height -= Controls.Cast<Control>().LastOrDefault().Height;
                }
                display.Height = height;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            var height = Screen.PrimaryScreen.WorkingArea.Height - SystemInformation.MenuHeight;
            for (int i = 0; i < Controls.Count; i++)
            {
                var control = Controls[i];
                control.Width = Width;
                if (i != Controls.Count - 1)
                    height -= control.Height;
                else
                    control.Height = height;
            }
        }

        private readonly InputPanel _inputPanel;

        internal void Attach(IMXView view, HistoryStack stack)
        {
            if (GridControl.Bitmaps.Any())
            {
                var bitmaps = new Dictionary<GridControl, System.Drawing.Bitmap>(GridControl.Bitmaps);
                GridControl.Bitmaps.Clear();
                GridControl.mem = 0;
                foreach (var bit in bitmaps)
                {
                    bit.Value.Dispose();
                }
            }

            Device.Thread.ExecuteOnMainThread(() =>
            {
                if (view == null) return;
                iApp.CurrentNavContext.ActivePane = stack.Context.ActivePane;
                var t = CompactFactory.MetricStopwatch.ElapsedTicks;
                var absView = view as IView;
                var list = view as IListView;
                var grid = view as IGridView;
                var browser = view as IBrowserView;
                var history = view as IHistoryEntry;
                var concreteView = view as System.Windows.Forms.Control ?? (view is IPairable ? ((IPairable)view).Pair as System.Windows.Forms.Control : null);

                if (absView != null)
                {
                    Text = absView.Title;
                }
                var listbox = ActiveView as SmoothListbox;
                if (listbox != null)
                {
                    listbox.animationTimer.Enabled = false;
                }
                Controls.Clear();
                int top = CompactFactory.TopPadding;
                if (list != null && list.SearchBox != null)
                {
                    var box = CompactFactory.GetNativeObject<System.Windows.Forms.Control>(list.SearchBox, "SearchBox");
                    box.Parent = this;
                    var min = new Size(Width, 0);
                    var max = new Size(Width, 50 * CompactFactory.Instance.DpiScale);
                    box.Size = ((IGridBase)box).PerformLayout(min, max).ToSize();
                    box.Top = top;
                    top = box.Bottom;
                }

                if (concreteView != null)
                {
                    var h = Screen.PrimaryScreen.WorkingArea.Height - SystemInformation.MenuHeight;
                    concreteView.Size = new System.Drawing.Size(Width, h);
                    concreteView.Parent = this;
                    concreteView.Location = new Point(0, top);
                }

                Menu.MenuItems.Clear();

                var back = history == null ? null : history.BackLink;
                var backButton = new MenuItem { Text = iApp.Factory.GetResourceString("Back") };
                backButton.Click += (sender, args) => stack.HandleBackLink(back, stack.Context.ActivePane);
                Menu.MenuItems.Add(backButton);
                var layer = view.GetModel() as iLayer;
                backButton.Enabled = stack.DisplayBackButton(back) || stack.Context.ActivePane == Pane.Popover && !(layer is LoginLayer) && stack.Views.Count() < 2;
                if (MenuTabView.Instance != null && !(layer is LoginLayer) && (layer == null || stack.Context.ActivePane != Pane.Popover))
                {
                    Menu.MenuItems.Add(MenuTabView.Instance);
                }

                var menu = (list != null ? list.Menu : grid != null ? grid.Menu : browser != null ? browser.Menu : null);
                if (menu != null)
                {
                    MenuItem menuButton;
                    if (menu.ButtonCount == 1)
                    {
                        menuButton = CompactFactory.GetNativeObject<MenuItem>(menu.GetButton(0), "menu[0]");
                    }
                    else
                    {
                        menuButton = new MenuItem { Text = iApp.Factory.GetResourceString("Menu") };
                        for (int i = 0; i < menu.ButtonCount; i++)
                        {
                            menuButton.MenuItems.Add(CompactFactory.GetNativeObject<MenuItem>(menu.GetButton(i), "menu[" + i + "]"));
                        }
                    }
                    Menu.MenuItems.Add(menuButton);
                }

                MinimizeBox = layer == null || layer.ActionButtons.All(b => b.Action != Button.ActionType.Submit);

                listbox = ActiveView as SmoothListbox;
                if (listbox != null)
                {
                    listbox.animationTimer.Enabled = true;
                }

                CompactFactory.NavigatedAddresses.Clear();

                Debug.WriteLineIf(CompactFactory.MetricStopwatch.IsRunning, string.Format("[{1}] Attach IMXView took {0}ms", new TimeSpan(CompactFactory.MetricStopwatch.ElapsedTicks - t).TotalMilliseconds, CompactFactory.MetricStopwatch.ElapsedMilliseconds));
                Debug.WriteLineIf(CompactFactory.MetricStopwatch.IsRunning, string.Format("Total elapsed layer load time: {0}ms", CompactFactory.MetricStopwatch.ElapsedMilliseconds));

                CompactFactory.MetricStopwatch.Stop();
            });
        }

        private void RootForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MinimizeBox) { return; }
            e.Cancel = true;
            var layer = ActiveView.GetModel() as iLayer;
            if (layer == null) return;
            var panel = ActiveView as IListView;
            if (panel != null)
            {
                panel.Submit(layer.ActionButtons.FirstOrDefault(b => b.Action == Button.ActionType.Submit));
                return;
            }
            var grid = ActiveView as IGridView;
            if (grid != null)
            {
                grid.Submit(layer.ActionButtons.FirstOrDefault(b => b.Action == Button.ActionType.Submit));
            }
        }

        void RootForm_Activated(object sender, EventArgs e)
        {
            _hook.Start();
        }

        void RootForm_Deactivate(object sender, EventArgs e)
        {
            _hook.Stop();
            var c = ActiveView as CanvasView;
            if (c != null)
            {
                c.InvalidateBackground();
            }
        }

        void IDisposable.Dispose()
        {
            _hook.Stop();
            Dispose();
        }

        #region Keyboard Helper Methods

        internal IMXView ActiveView
        {
            get { return Controls.OfType<IMXView>().FirstOrDefault(); }
        }

        private void HookEvent(HookEventArgs hookEventArgs, KeyBoardInfo keyBoardInfo)
        {
            var vkey = (Keys)keyBoardInfo.vkCode;
            if (!_keys.Contains(vkey)) _keys.Add(vkey);

            if (_collecting) return;
            _collecting = true;

            var layer = ActiveView == null ? null : ActiveView.GetModel() as iLayer;
            var list = ActiveView as SmoothListbox;
            Device.Thread.QueueWorker(o =>
            {
                Thread.Sleep(40);
                Device.Thread.ExecuteOnMainThread(new Action(() =>
                {
                    var currentKeystroke = new Keystroke(_keys.Cast<int>());
                    var candidateKeys = new List<Keystroke> { currentKeystroke };
                    candidateKeys.AddRange(_keys.Select(k => new Keystroke((int)k)));

                    if (layer != null)
                    {
                        foreach (var key in candidateKeys)
                        {
                            var keyChord = new Gesture(key);

                            var link = layer.ShortcutGestures.GetValueOrDefault(keyChord, null);
                            if (link == null && _lastKeystroke != null)
                            {
                                keyChord = new Gesture(_lastKeystroke, key);
                                link = layer.ShortcutGestures.GetValueOrDefault(keyChord, null);
                            }

                            if (link == null) { continue; }
                            _keys.Clear();
                            currentKeystroke = null;
                            CompactFactory.Navigate(link, ActiveView);
                            break;
                        }
                    }

                    if (_keys.Contains(Keys.Tab))
                    {
                        if (list == null || list.IsDisposed) return;
                        if (_keys.Contains(Keys.ShiftKey))
                        { list.HighlightPrevious(); }
                        else { list.HighlightNext(); }
                        _keys.Clear();
                        currentKeystroke = null;
                    }
                    else if (_keys.Any())
                    {
                        UI.Controls.TextBox text = null;
                        UI.Controls.TextArea area = null;
                        UI.Controls.PasswordBox pass = null;
                        UI.Controls.DatePicker date = null;
                        UI.Controls.TimePicker time = null;

                        var item = list == null || list.IsDisposed ? null : list.SelectedItem;
                        if (item != null)
                        {
                            text = item.GetChild<UI.Controls.TextBox>();
                            area = item.GetChild<UI.Controls.TextArea>();
                            pass = item.GetChild<UI.Controls.PasswordBox>();
                            date = item.GetChild<UI.Controls.DatePicker>();
                            time = item.GetChild<UI.Controls.TimePicker>();
                        }

                        if (item == null || text == null && area == null && pass == null)
                        {
                            foreach (var key in _keys)
                            {
                                switch (key)
                                {
                                    case Keys.Home:
                                        if (list != null && !list.IsDisposed)
                                        {
                                            list.ScrollToHome(true);
                                            _keys.Clear();
                                            currentKeystroke = null;
                                        }
                                        break;
                                    case Keys.End:
                                        if (list != null && !list.IsDisposed)
                                        {
                                            list.ScrollToEnd(true);
                                            _keys.Clear();
                                            currentKeystroke = null;
                                        }
                                        break;
                                    case Keys.F23:
                                    case Keys.Select:
                                    case Keys.Enter:
                                    case Keys.Space:
                                        if (item != null) item.Select();
                                        _keys.Clear();
                                        currentKeystroke = null;
                                        break;
                                }

                                if (currentKeystroke == null) break; // _keys was modified
                            }
                        }

                        if (item == null || area == null && date == null && time == null)
                        {
                            foreach (var key in _keys)
                            {
                                switch (key)
                                {
                                    case Keys.Up:
                                        if (list != null && !list.IsDisposed)
                                        {
                                            list.HighlightPrevious();
                                            _keys.Clear();
                                            currentKeystroke = null;
                                        }
                                        break;
                                    case Keys.Down:
                                        if (list != null && !list.IsDisposed)
                                        {
                                            list.HighlightNext();
                                            _keys.Clear();
                                            currentKeystroke = null;
                                        }
                                        break;
                                }

                                if (currentKeystroke == null) break; // _keys was modified
                            }
                        }

                        if (area == null && text != null && (_keys.Contains(Keys.Enter) || _keys.Contains(Keys.F23)))
                        {
                            list.HighlightNext();
                            _keys.Clear();
                            currentKeystroke = null;
                        }

                        CommonKeys(list == null || list.IsDisposed ? null : list, ref currentKeystroke);
                    }

                    _lastKeystroke = currentKeystroke;

                    Device.Thread.QueueWorker(p =>
                    {
                        Thread.Sleep(40);
                        _keys.Clear();
                        _collecting = false;
                    });
                }));
            });
        }

        private bool _collecting;
        private Keystroke _lastKeystroke;
        private readonly List<Keys> _keys = new List<Keys>();

        private void CommonKeys(SmoothListbox list, ref Keystroke current)
        {
            foreach (var key in _keys)
            {
                switch (key)
                {
                    case Keys.Cancel:
                    case Keys.Escape:
                        var view = ActiveView as IHistoryEntry;
                        if (view != null)
                        {
                            var stack = view.Stack;
                            stack.HandleBackLink(view.BackLink, view.OutputPane);

                            _keys.Clear();
                            current = null;
                        }
                        break;
                    case Keys.PageUp:
                        if (list != null)
                        {
                            list.ScrollPageUp(true);
                            _keys.Clear();
                            current = null;
                        }
                        break;
                    case Keys.PageDown:
                        if (list != null)
                        {
                            list.ScrollPageDown(true);
                            _keys.Clear();
                            current = null;
                        }
                        break;
                    case Keys.Play:
                        iApp.Navigate("audio://?command=pause");
                        break;
                }
                if (current == null) break;
            }
        }
        #endregion

    }
}