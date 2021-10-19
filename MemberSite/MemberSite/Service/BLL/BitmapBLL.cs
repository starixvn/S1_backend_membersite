using System;
using System.Drawing;
using LazZiya.ImageResize;

namespace MemberSite.Service.BLL
{
    public class BitmapBLL
    {
        private const double xSize = 500.0;
        private double xCell = 1;
        
        public Bitmap Resize(Bitmap originImage)
        {
            if (originImage.Width > xSize || originImage.Height > xSize)
            {
                xCell = xSize / Math.Max(originImage.Width, originImage.Height);
            }

            var newWidth = Convert.ToInt32(originImage.Width * xCell);
            var newHeight = Convert.ToInt32(originImage.Height * xCell);
            
            var bitmap = new Bitmap(newWidth, newHeight);
            var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            graphics.DrawImage(originImage, 0, 0, newWidth, newHeight);
            return bitmap;
        }

        public Image Resize(Image uploadedImage)
        {
            Image img;
            if (uploadedImage.Width > xSize || uploadedImage.Height > xSize)
            {
                if (uploadedImage.Width > uploadedImage.Height)
                {
                    xCell = xSize / uploadedImage.Width;
                }
                else
                {
                    xCell = xSize / uploadedImage.Height;
                }
            }

            if (uploadedImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Undefined ||
                uploadedImage.PixelFormat == System.Drawing.Imaging.PixelFormat.DontCare ||
                uploadedImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Format16bppArgb1555 ||
                uploadedImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Format1bppIndexed ||
                uploadedImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Format4bppIndexed ||
                uploadedImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                var bitmap = new Bitmap(Convert.ToInt32(uploadedImage.Width * xCell), Convert.ToInt32(uploadedImage.Height * xCell));
                var graphics = Graphics.FromImage(bitmap);
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                graphics.DrawImage(uploadedImage, 0, 0, Convert.ToInt32(uploadedImage.Width * xCell), Convert.ToInt32(uploadedImage.Height * xCell));
                img = Image.FromHbitmap(bitmap.GetHbitmap());
            }
            else
            {
                var graphicOptions = new GraphicOptions
                {
                    CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed,
                    SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed
                };
                img = ImageResize.Scale(uploadedImage, Convert.ToInt32(uploadedImage.Width * xCell), Convert.ToInt32(uploadedImage.Height * xCell), graphicOptions);
            }

            return img;
        }
    }
}