using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using MonoCross;
using MonoCross.Utilities;
using Size = iFactr.UI.Size;

namespace iFactr.Compact
{
    public class BitmapImage : IImageData, IDisposable
    {
        // The IImage, for alpha channel.
        public IImage CreateImage()
        {
            IImage i;
            Factory.CreateImageFromBuffer(_buffer, (uint)_buffer.Length, 0, out i);
            return i;
        }
        private byte[] _buffer;

        public ImageFileFormat Format { get; set; }

        public BitmapImage(byte[] bytes, string format)
        {
            _buffer = bytes;
            Format = format.ToLower() == "png" ? ImageFileFormat.PNG : ImageFileFormat.JPEG;
        }

        public BitmapImage(string uri)
        {
            string ext = string.Empty;
            byte[] bytes = null;
            if (uri.StartsWith("http") || uri.StartsWith("ftp"))
            {
                try
                {
                    bytes = Device.Network.GetBytes(uri);
                }
                catch (Exception ex)
                {
                    Device.Log.Error("Image download failed", ex);
                }
            }
            else if (uri.StartsWith("data:"))
            {
                bytes = ImageUtility.DecodeImageFromDataUri(uri, out ext);
            }
            else
            {
                try { bytes = Device.File.Read(uri); }
                catch (Exception e)
                {
                    bytes = null;
                    Device.Log.Warn("Exception reading file from :" + uri, e);
                }
            }

            if (string.IsNullOrEmpty(ext) && uri.ToLower().EndsWith("png"))
            {
                ext = "png";
            }

            if (bytes == null)
            {
                return;
            }

            _buffer = bytes;
            Format = ext.ToLower() == "png" ? ImageFileFormat.PNG : ImageFileFormat.JPEG;
        }

        public void Dispose()
        {
            _buffer = null;
        }

        #region IImageData Members

        public byte[] GetBytes()
        {
            return _buffer;
        }

        public IExifData GetExifData()
        {
            throw new NotImplementedException();
        }

        public void Save(string filePath, ImageFileFormat f)
        {
            Device.File.Save(filePath, _buffer);
        }

        #endregion

        public Bitmap GetBitmap()
        {
            if (_buffer == null) return null;
            var stream = new MemoryStream(_buffer);
            return new Bitmap(stream);
        }

        private static IImagingFactory Factory
        {
            get
            {
                return (IImagingFactory)Activator.CreateInstance(
                    Type.GetTypeFromCLSID(new Guid("327ABDA8-072B-11D3-9D7B-0000F81EF32E")));
            }
        }

        // Pulled from imaging.h in the Windows Mobile 5.0 Pocket PC SDK
        public struct ImageInfo
        {
            public uint GuidPart1; // I am being lazy here, I don't care at this point about the RawDataFormat GUID
            public uint GuidPart2; // I am being lazy here, I don't care at this point about the RawDataFormat GUID
            public uint GuidPart3; // I am being lazy here, I don't care at this point about the RawDataFormat GUID
            public uint GuidPart4; // I am being lazy here, I don't care at this point about the RawDataFormat GUID
            public int pixelFormat;
            public uint Width;
            public uint Height;
            public uint TileWidth;
            public uint TileHeight;
            public double Xdpi;
            public double Ydpi;
            public uint Flags;
        }

        // Pulled from imaging.h in the Windows Mobile 5.0 Pocket PC SDK
        [ComImport, Guid("327ABDA7-072B-11D3-9D7B-0000F81EF32E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        public interface IImagingFactory
        {
            uint CreateImageFromStream(); // This is a place holder, note the lack of arguments
            uint CreateImageFromFile(string filename, out IImage image);
            // We need the MarshalAs attribute here to keep COM interop from sending the buffer down as a Safe Array.
            uint CreateImageFromBuffer([MarshalAs(UnmanagedType.LPArray)] byte[] buffer, uint size, int disposalFlag,
                out IImage image);

            uint CreateNewBitmap(uint width, uint height, int pixelFormat, out IBitmapImage bitmap);

            uint CreateBitmapFromImage(IImage image, uint width, uint height, int pixelFormat, int hints,
                out IBitmapImage bitmap);

            uint CreateBitmapFromBuffer(); // This is a place holder, note the lack of arguments
            uint CreateImageDecoder(); // This is a place holder, note the lack of arguments
            uint CreateImageEncoderToStream(); // This is a place holder, note the lack of arguments
            uint CreateImageEncoderToFile(); // This is a place holder, note the lack of arguments
            uint GetInstalledDecoders(); // This is a place holder, note the lack of arguments
            uint GetInstalledEncoders(); // This is a place holder, note the lack of arguments
            uint InstallImageCodec(); // This is a place holder, note the lack of arguments
            uint UninstallImageCodec(); // This is a place holder, note the lack of arguments
        }

        // Pulled from imaging.h in the Windows Mobile 5.0 Pocket PC SDK
        [ComImport, Guid("327ABDA9-072B-11D3-9D7B-0000F81EF32E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        public interface IImage
        {
            uint GetPhysicalDimension(out UI.Size size);
            uint GetImageInfo(out ImageInfo info);
            uint SetImageFlags(uint flags);
            uint Draw(IntPtr hdc, ref Rect dstRect, ref Rect srcRect);
            uint PushIntoSink(); // This is a place holder, note the lack of arguments
            uint GetThumbnail(uint thumbWidth, uint thumbHeight, out IImage thumbImage);
        }

        // Pulled from imaging.h in the Windows Mobile 5.0 Pocket PC SDK
        [ComImport, Guid("327ABDAA-072B-11D3-9D7B-0000F81EF32E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        public interface IBitmapImage
        {
            uint GetSize(out Size size);
            uint GetPixelFormatID(out int pixelFormat);
            uint LockBits(ref Rectangle rect, uint flags, int pixelFormat, out BitmapData lockedBitmapData);
            uint UnlockBits(ref BitmapData lockedBitmapData);
            uint GetPalette(); // This is a place holder, note the lack of arguments
            uint SetPalette(); // This is a place holder, note the lack of arguments
        }
    }
}