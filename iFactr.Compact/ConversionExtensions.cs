using System;
using System.Drawing;
using System.Drawing.Imaging;
using MonoCross;
using iFactr.UI;
using Color = System.Drawing.Color;
using Font = iFactr.UI.Font;
using Point = System.Drawing.Point;
using Size = iFactr.UI.Size;

namespace iFactr.Compact
{
    public static class ConversionExtensions
    {
        public static Font ToFont(this System.Drawing.Font font)
        {
            if (font == null) { return new Font(); }

            return new Font(font.Name, font.Size, (FontFormatting)font.Style);
        }

        public static System.Drawing.Font ToFont(this Font UiFont)
        {
            return new System.Drawing.Font(UiFont.Name, (float)UiFont.Size, (FontStyle)UiFont.Formatting);
        }

        public static Color ToColor(this UI.Color color)
        {
            return Color.FromArgb(color.GetHashCode());
        }

        public static UI.Color ToColor(this Color color)
        {
            return new UI.Color(color.A, color.R, color.G, color.B);
        }

        public static System.Drawing.Size ToSize(this Size size)
        {
            return new System.Drawing.Size((int)size.Width, (int)size.Height);
        }

        public static Size ToSize(this System.Drawing.Size size)
        {
            return new Size(size.Width, size.Height);
        }

        public static Point ToPoint(this UI.Point size)
        {
            return new Point((int)size.X, (int)size.Y);
        }

        public static UI.Point ToPoint(this Point size)
        {
            return new UI.Point(size.X, size.Y);
        }

        public static ImageFileFormat ToFormat(this ImageFormat format)
        {
            if (format == ImageFormat.Jpeg)
                return ImageFileFormat.JPEG;
            return ImageFileFormat.PNG;
        }

        public static ImageFormat ToFormat(this ImageFileFormat format)
        {
            switch (format)
            {
                case ImageFileFormat.JPEG:
                    return ImageFormat.Jpeg;
                default:
                case ImageFileFormat.PNG:
                    return ImageFormat.Png;
            }
        }
    }
}