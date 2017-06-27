using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using iFactr.Core;
using iFactr.Core.Layers;
using iFactr.Core.Native;
using iFactr.Core.Styles;
using iFactr.Core.Targets.Settings;
using MonoCross.Utilities;
using MonoCross.Utilities.ImageComposition;
using System.Text;
using System.Runtime.InteropServices;
using iFactr.UI.Instructions;
using Microsoft.WindowsCE.Forms;
using MonoCross;
using MonoCross.Navigation;
using iFactr.UI;
using iFactr.UI.Controls;
using Color = iFactr.UI.Color;
using Control = System.Windows.Forms.Control;
using Font = iFactr.UI.Font;
using Link = iFactr.Core.Controls.Link;
using Size = iFactr.UI.Size;

namespace iFactr.Compact
{
    public class CompactFactory : NativeFactory
    {
        #region Properties, constructors, and singletons

        public override Instructor Instructor
        {
            get { return _instructor ?? (_instructor = new CompactInstructor()); }
            set { _instructor = value; }
        }
        private Instructor _instructor;

        public Form RootForm { get; private set; }

        public static void Initialize()
        {
            Device.Initialize(new CompactDevice());
            Initialize(new RootForm());

            IntPtr HWND_BROADCAST = (IntPtr)0xFFFF;
            const int WM_Fontchange = 0x001D;
            IntPtr thir = (IntPtr)0;
            IntPtr fourth = (IntPtr)0;
            var fontName = Font.PreferredLabelFont.Name + ".ttf";
            var fontPath = "\\Windows\\Fonts\\".AppendPath(fontName);
            if (!Device.File.Exists(fontPath))
            {
                var fontStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("iFactr.Compact.Resources." + fontName);
                if (fontStream != null)
                {
                    Device.File.Save(fontPath, fontStream);
                }
            }

            if (Device.File.Exists(fontPath))
            {
                CoreDll.AddFontResource(fontPath);
                CoreDll.SendMessage(HWND_BROADCAST, WM_Fontchange, thir, fourth);
            }
            CoreDll.SystemParametersInfo(CoreDll.SPI_SETFONTSMOOTHING, -1, IntPtr.Zero, 0);

            var context = new iApp.AppNavigationContext { ActivePane = Pane.Master };
            PaneManager.Instance.AddStack(new HistoryStack { Context = context }, context);
            context = new iApp.AppNavigationContext { ActivePane = Pane.Popover };
            PaneManager.Instance.AddStack(new HistoryStack { Context = context }, context);
        }

        public static Stopwatch MetricStopwatch = new Stopwatch();

        /// <summary>
        /// Initializes the factory singleton.
        /// </summary>
        /// <param name="rootForm">The root form</param>
        public static void Initialize(Form rootForm)
        {
            if (!IsInitialized)
            {
                if (rootForm == null) throw new ArgumentNullException("rootForm");

                Initialize(new CompactFactory());
                CompactDevice.Instance.DispatcherSource = Instance.RootForm = rootForm;

                using (var g = Instance.RootForm.CreateGraphics())
                    Instance.DpiScale = g.DpiX / 96;

                Instance.Margin = (int)(Instance.DpiScale * 7);
                Instance.Style = new Style
                {
                    SectionHeaderColor = new Color(102, 102, 102),
                    SectionHeaderTextColor = Color.White,
                    LayerBackgroundColor = SystemColors.Control.ToColor(),
                    LayerItemBackgroundColor = Color.White,
                    SelectionColor = SystemColors.Highlight.ToColor(),
                    DefaultLabelStyle = new LabelStyle
                    {
                        FontFamily = Font.PreferredLabelFont.Name,
                        FontSize = Font.PreferredLabelFont.Size,
                    },
                };
            }
        }

        public override double GetDisplayScale()
        {
            return DpiScale;
        }
        internal double DpiScale;

        /// <summary>
        /// Gets the factory instance.
        /// </summary>
        /// <value>The instance.</value>
        public static new CompactFactory Instance
        {
            get
            {
                if (!IsInitialized)
                {
                    Initialize(new CompactFactory());
                }
                return (CompactFactory)MXContainer.Instance;
            }
        }

        private CompactFactory()
        {
            _indicator.Elapsed += _indicator_Elapsed;
        }

        #endregion

        public new static void Navigate(Link location)
        {
            Instance.Thread.ExecuteOnMainThread(() =>
            {
                Navigate(location, ((RootForm)Instance.RootForm).ActiveView);
            });
        }

        public new static void Navigate(Link location, IMXView fromView)
        {
            if (location == null || NavigatedAddresses.Contains(location.Address))
            {
                return;
            }

            NavigatedAddresses.Add(location.Address);
            Instance.Thread.QueueWorker(o => iApp.Navigate(location, fromView));
        }

        internal static List<string> NavigatedAddresses = new List<string>();

        public int Margin { get; private set; }
        protected override void OnSetDefinitions()
        {
            Register<IPlatformDefaults>(typeof(CompactDefaults));
            Register<ITimer>(typeof(Timer));
            Register<IAlert>(typeof(Alert));
            Register<IImageData>(typeof(BitmapImage));
            //Register<IExifData>(typeof(ExifData));

            Register<ICanvasView>(typeof(CanvasView));
            Register<IBrowserView>(typeof(BrowserView));
            Register<IListView>(typeof(SmoothListbox));
            Register<IGridView>(typeof(GridView));
            Register<ITabView>(typeof(MenuTabView));

            Register<ISearchBox>(typeof(SearchBar));
            //Register<IGrid>(typeof(Grid));
            Register<IGridCell>(typeof(GridCell));
            Register<IRichContentCell>(typeof(RichText));
            Register<ISectionHeader>(typeof(HeaderControl));
            Register<ISectionFooter>(typeof(FooterControl));

            Register<IMenu>(typeof(ActionMenu));
            Register<IMenuButton>(typeof(MenuButton));
            Register<ITabItem>(typeof(TabItem));

            Register<IImage>(typeof(ImageControl));
            Register<ILabel>(typeof(TransparentLabel));
            Register<ITextBox>(typeof(TextBoxWithPrompt));
            Register<ITextArea>(typeof(TextArea));
            Register<IPasswordBox>(typeof(PasswordBox));
            Register<ISelectList>(typeof(SelectList));
            Register<IButton>(typeof(ButtonControl));
            Register<IDatePicker>(typeof(DatePicker));
            Register<ITimePicker>(typeof(TimePicker));
            Register<ISlider>(typeof(SliderControl));
            Register<ISwitch>(typeof(Switch));

            Register<IToolbar>(typeof(Toolbar));
            Register<IToolbarButton>(typeof(ToolbarButton));
            Register<IToolbarSeparator>(typeof(ToolbarSeparator));
        }

        protected override bool OnOutputLayer(iLayer layer)
        {
            var browser = layer as Browser;
            if (browser != null)
            {
                string url = browser.Url;
                if (url.StartsWith("image") ||
                    url.StartsWith("geoloc") ||
                    url.StartsWith("compass") ||
                    url.StartsWith("accel") ||
                    url.StartsWith("video") ||
                    url.StartsWith("audio") ||
                    url.StartsWith("voice"))
                {
                    //TODO: Launch integration
                    return true;
                }
            }
            return false;
        }

        protected override void OnShowLoadIndicator(string title)
        {
            RootForm.Invoke(new Action(() =>
            {
                Cursor.Current = Cursors.WaitCursor;
                _showIndicator = false;
                _indicator.Start();
            }));
        }

        protected override void OnShowImmediateLoadIndicator()
        {
            MetricStopwatch.Reset();
            MetricStopwatch.Start();

            _lastNavTime = DateTime.UtcNow.Ticks;
        }
        long _lastNavTime;

        protected override void OnHideLoadIndicator()
        {
            _showIndicator = true;
            if (!_indicator.IsEnabled)
            {
                RootForm.Invoke(new Action(() => { Cursor.Current = Cursors.Default; }));
            }

            var elapse = DateTime.UtcNow.Ticks - _lastNavTime;
            Device.Log.Metric(string.Format("Rendering the layer cost {0}ms", new TimeSpan(elapse).TotalMilliseconds));
        }

        void _indicator_Elapsed(object sender, EventArgs e)
        {
            _indicator.Stop();
            if (_showIndicator)
            {
                RootForm.Invoke(new Action(() => { Cursor.Current = Cursors.Default; }));
            }
        }

        private readonly Timer _indicator = new Timer { Interval = 500 };
        private bool _showIndicator;

        /// <summary>
        /// Allows implementation of ICustomItem in container
        /// </summary>
        [Obsolete("Use GetCustomItem instead.")]
        public Func<ICustomItem, Style, Control> CustomItemRequested { get; set; }

        /// <summary>
        /// Allows implementation of ICustomItem in container
        /// </summary>
        public Func<ICustomItem, iLayer, IListView, Control, Control> GetCustomItem { get; set; }

        protected override object OnGetCustomItem(ICustomItem item, iLayer layer, IListView view, object recycledCell)
        {
            var ci = GetCustomItem;
            if (ci != null) return ci(item, layer, view, recycledCell as Control);
#pragma warning disable 618
            var cir = CustomItemRequested;
            return cir == null ? null : cir(item, layer.LayerStyle);
#pragma warning restore 618
        }

        #region ITargetFactory members

        protected override void ShouldNavigate(Link link, Pane pane, Action handler)
        {
            RootForm.Invoke(new Action(() =>
            {
                if (PaneManager.Instance.ShouldNavigate(link, pane, NavigationType.Forward))
                {
                    base.ShouldNavigate(link, pane, handler);
                }
            }));
        }

        public override ICompositor Compositor
        {
            get { return _compositor ?? (_compositor = Resolve<ICompositor>()); }
        }

        public override ISettings Settings
        {
            get { return _settings ?? (_settings = new BasicSettingsDictionary()); }
        }

        public override MobileTarget Target { get { return MobileTarget.Compact; } }

        protected override double GetLineHeight(Font font)
        {
            return CoreDll.MeasureString("Wq", font.ToFont(), new Size(1000, 1000), false, true).Height;
        }

        public override string DeviceId
        {
            get
            {
                byte[] outbuff = new byte[20];
                Int32 dwOutBytes;
                Int32 nBuffSize = outbuff.Length;
                BitConverter.GetBytes(nBuffSize).CopyTo(outbuff, 0);
                dwOutBytes = 0;
                while (!CoreDll.KernelIoControl(CoreDll.IOCTL_HAL_GET_DEVICEID, IntPtr.Zero, 0, outbuff, nBuffSize, ref dwOutBytes))
                {
                    int error = Marshal.GetLastWin32Error();
                    switch (error)
                    {
                        case CoreDll.ERROR_NOT_SUPPORTED:
                            throw new NotSupportedException("IOCTL_HAL_GET_DEVICEID is not supported on this device", new Win32Exception(error));
                        case CoreDll.ERROR_INSUFFICIENT_BUFFER:
                            nBuffSize = BitConverter.ToInt32(outbuff, 0);
                            outbuff = new byte[nBuffSize];
                            BitConverter.GetBytes(nBuffSize).CopyTo(outbuff, 0);
                            break;
                        default:
                            throw new Win32Exception(error, "Unexpected error");
                    }
                }
                Int32 dwPresetIDOffset = BitConverter.ToInt32(outbuff, 0x4);
                Int32 dwPresetIDSize = BitConverter.ToInt32(outbuff, 0x8);
                Int32 dwPlatformIDOffset = BitConverter.ToInt32(outbuff, 0xc);
                Int32 dwPlatformIDSize = BitConverter.ToInt32(outbuff, 0x10);
                StringBuilder sb = new StringBuilder();
                for (int i = dwPresetIDOffset; i < dwPresetIDOffset + dwPresetIDSize; i++)
                    sb.Append(String.Format("{0:X2}", outbuff[i]));
                sb.Append("-"); for (int i = dwPlatformIDOffset; i < dwPlatformIDOffset + dwPlatformIDSize; i++)
                    sb.Append(String.Format("{0:X2}", outbuff[i]));
                return sb.ToString();
            }
        }

        public static int TopPadding
        {
            get { return SystemSettings.Platform == WinCEPlatform.WinCEGeneric ? SystemInformation.MenuHeight : 0; }
        }

        public Func<double, double, double, double, double> EasingFunction
        {
            get;
            set;
        }

        #endregion

        internal static T GetNativeObject<T>(object obj, string objName) where T : class
        {
            if (obj == null) { return null; }

            obj = GetNativeObject(obj, objName, typeof(T));

            return obj as T;
        }
    }
}