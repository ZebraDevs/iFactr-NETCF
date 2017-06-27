using MonoCross;
using MonoCross.Utilities;

namespace iFactr.Compact
{
    public class ImageManager
    {
        public static IImageData GetBitmapData(string uri, bool skipCache)
        {
            if (uri == null) return null;
            var image = Device.ImageCache.Get(uri);
            if (image != null)
            {
                return image;
            }

            var b = new BitmapImage(uri);
            if (!skipCache && b.GetBytes() != null)
            {
                Device.ImageCache.Add(uri, b);
            }
            return b;
        }
    }
}