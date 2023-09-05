using System.Drawing;

using LazZiya.ImageResize;

using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace MemberSite.Service.BLL
{
    public class ImageEditorBLL
    {
        private const double BaseSize = 500.0;
        private double _xCell = 1;

        public async Task<byte[]> Resize(Bitmap originImage)
        {
            if (originImage.Width > BaseSize || originImage.Height > BaseSize)
            {
                _xCell = BaseSize / Math.Max(originImage.Width, originImage.Height);
            }

            var newWidth = Convert.ToInt32(originImage.Width * _xCell);
            var newHeight = Convert.ToInt32(originImage.Height * _xCell);

            var bitmap = new Bitmap(newWidth, newHeight);
            var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            graphics.DrawImage(originImage, 0, 0, newWidth, newHeight);

            await using var ms = new System.IO.MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return ms.ToArray();
        }

        public async Task<byte[]> Resize(Image uploadedImage)
        {
            Image img;
            if (uploadedImage.Width > BaseSize || uploadedImage.Height > BaseSize)
            {
                if (uploadedImage.Width > uploadedImage.Height)
                {
                    _xCell = BaseSize / uploadedImage.Width;
                }
                else
                {
                    _xCell = BaseSize / uploadedImage.Height;
                }
            }

            if (uploadedImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Undefined ||
                uploadedImage.PixelFormat == System.Drawing.Imaging.PixelFormat.DontCare ||
                uploadedImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Format16bppArgb1555 ||
                uploadedImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Format1bppIndexed ||
                uploadedImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Format4bppIndexed ||
                uploadedImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                var bitmap = new Bitmap(Convert.ToInt32(uploadedImage.Width * _xCell), Convert.ToInt32(uploadedImage.Height * _xCell));
                var graphics = Graphics.FromImage(bitmap);
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                graphics.DrawImage(uploadedImage, 0, 0, Convert.ToInt32(uploadedImage.Width * _xCell), Convert.ToInt32(uploadedImage.Height * _xCell));
                img = Image.FromHbitmap(bitmap.GetHbitmap());
            }
            else
            {
                var graphicOptions = new GraphicOptions
                {
                    CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed,
                    SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed
                };
                img = ImageResize.Scale(uploadedImage, Convert.ToInt32(uploadedImage.Width * _xCell), Convert.ToInt32(uploadedImage.Height * _xCell), graphicOptions);
            }
            await using var ms = new System.IO.MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return ms.ToArray();
        }

        public async Task<byte[]> Resize(SixLabors.ImageSharp.Image originImage)
        {
            if (originImage.Width > BaseSize || originImage.Height > BaseSize)
            {
                _xCell = BaseSize / Math.Max(originImage.Width, originImage.Height);
            }

            var newWidth = Convert.ToInt32(originImage.Width * _xCell);
            var newHeight = Convert.ToInt32(originImage.Height * _xCell);

            originImage.Mutate(x => x.Resize(newWidth, newHeight));

            await using var ms = new System.IO.MemoryStream();
            await originImage.SaveAsync(ms, new JpegEncoder());
            return ms.ToArray();
        }
    }
}