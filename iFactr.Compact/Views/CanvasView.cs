using System.ComponentModel;
using iFactr.UI;

namespace iFactr.Compact
{
    public class CanvasView : GridView, ICanvasView
    {
        private readonly Canvas _canvas;

        public CanvasView()
        {
            Columns.Add(Column.OneStar);
            Rows.Add(Row.OneStar);
            Rows.Add(Row.AutoSized);

            _canvas = new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            _canvas.DrawingSaved += Canvas_DrawingSaved;
            _canvas.PropertyChanged += Canvas_PropertyChanged;
            AddChild(_canvas);
        }

        private void Canvas_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "StrokeColor":
                case "StrokeThickness":
                    OnPropertyChanged(e.PropertyName);
                    return;
            }
        }

        private void Canvas_DrawingSaved(object sender, SaveEventArgs args)
        {
            var save = DrawingSaved;
            if (save != null) save(this, args);
        }

        public Color StrokeColor
        {
            get { return _canvas.StrokeColor; }
            set { _canvas.StrokeColor = value; }
        }

        public double StrokeThickness
        {
            get { return _canvas.StrokeThickness; }
            set { _canvas.StrokeThickness = value; }
        }

        public IToolbar Toolbar
        {
            get { return _toolbar; }
            set
            {
                if (_toolbar == value) return;
                var tool = _toolbar is Toolbar || _toolbar != null && _toolbar.Pair is Toolbar
                    ? CompactFactory.GetNativeObject<Toolbar>(_toolbar, "Toolbar")
                    : null;
                if (tool != null)
                {
                    RemoveChild(tool);
                }
                _toolbar = value;
                if (_toolbar != null)
                {
                    tool = _toolbar is Toolbar || _toolbar != null && _toolbar.Pair is Toolbar
                        ? CompactFactory.GetNativeObject<Toolbar>(_toolbar, "Toolbar")
                        : null;
                    if (tool != null)
                    {
                        AddChild(tool);
                        tool.Layout();
                    }
                }
                OnPropertyChanged("Toolbar");
            }
        }

        private IToolbar _toolbar;

        public void Clear()
        {
            _canvas.Clear();
        }

        public void Load(string fileName)
        {
            _canvas.Load(fileName);
        }

        public void Save(bool compositeBackground)
        {
            _canvas.Save(compositeBackground);
        }

        public void Save(string fileName)
        {
            _canvas.Save(fileName);
        }

        public void Save(string fileName, bool compositeBackground)
        {
            _canvas.Save(fileName, compositeBackground);
        }

        public override void SetBackground(Color color)
        {
            _canvas.BackColor = color.ToColor();
            _canvas.Clear();
            base.SetBackground(color);
        }

        public event SaveEventHandler DrawingSaved;

        public override void SetBackground(string imagePath, ContentStretch stretch)
        {
            _canvas.BackColor = System.Drawing.Color.Transparent;
            base.SetBackground(imagePath, stretch);
        }

        public void InvalidateBackground()
        {
            _canvas.InvalidateBackground();
        }
    }
}