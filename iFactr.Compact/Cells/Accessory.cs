using System;
using System.IO;
using iFactr.UI;

namespace iFactr.Compact
{
    public class Accessory : ImageControl, IHighlight
    {
        private static BitmapImage DefaultBitmap;
        private static BitmapImage HighlightBitmap;

        static Accessory()
        {
            var assembly = typeof(Accessory).Assembly;

            using (var stream = assembly.GetManifestResourceStream("iFactr.Compact.Resources.Next.png"))
                if (stream != null)
                    DefaultBitmap = new BitmapImage(ReadToEnd(stream), "png");

            using (var stream = assembly.GetManifestResourceStream("iFactr.Compact.Resources.NextSelect.png"))
                if (stream != null)
                    HighlightBitmap = new BitmapImage(ReadToEnd(stream), "png");
        }

        public Accessory()
        {
            BitmapData = DefaultBitmap;
            Margin = new Thickness(0, -Thickness.TopMargin, -Thickness.RightMargin, -Thickness.BottomMargin);
            HorizontalAlignment = HorizontalAlignment.Right;
            VerticalAlignment = VerticalAlignment.Center;
        }

        public bool Highlight
        {
            get { return _highlight; }
            set
            {
                _highlight = value;
                BitmapData = value ? HighlightBitmap : DefaultBitmap;
                OnPropertyChanged("Highlight");
            }
        }

        private bool _highlight;

        private static byte[] ReadToEnd(Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }
    }
}