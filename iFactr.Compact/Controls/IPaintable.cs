using System.Drawing;
using Point = iFactr.UI.Point;
using Size = iFactr.UI.Size;

namespace iFactr.Compact
{
    internal interface IPaintable
    {
        Point Location { get; set; }
        Size Size { get; set; }
        void Paint(Graphics g);
        void SetParent(GridControl gridControl);
    }
}