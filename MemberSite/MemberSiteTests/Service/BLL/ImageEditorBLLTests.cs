using System.Drawing;
using System.IO;

using FluentAssertions;

using NUnit.Framework;

namespace MemberSite.Service.BLL.Tests
{
    [TestFixture]
    public class ImageEditorBLLTests
    {
        [Test]
        public void Resize_Jpeg()
        {
            var srcPic = SixLabors.ImageSharp.Image.Load(@"./Service/TestData/test1.jpeg");
            var imageEditorBll = new ImageEditorBLL();

            var byteArray = imageEditorBll.Resize(srcPic).Result;
            var ms = new MemoryStream(byteArray);
            var img = new Bitmap(ms);
            img.Width.Should().Be(500);
            img.Height.Should().Be(375);
        }

        [Test]
        public void Resize_Jeg()
        {
            var srcPic = SixLabors.ImageSharp.Image.Load(@"./Service/TestData/test2.jpg");
            var imageEditorBll = new ImageEditorBLL();

            var byteArray = imageEditorBll.Resize(srcPic).Result;
            var ms = new MemoryStream(byteArray);
            var img = new Bitmap(ms);
            img.Width.Should().Be(500);
            img.Height.Should().Be(375);
        }

        [Test]
        public void Resize_4K_Jpg()
        {
            var srcPic = SixLabors.ImageSharp.Image.Load(@"./Service/TestData/test3.jpg");
            var imageEditorBll = new ImageEditorBLL();

            var byteArray = imageEditorBll.Resize(srcPic).Result;
            var ms = new MemoryStream(byteArray);
            var img = new Bitmap(ms);
            img.Width.Should().Be(500);
            img.Height.Should().Be(333);
        }

        [Test]
        public void Resize_Normal_Size_Png()
        {
            var srcPic = SixLabors.ImageSharp.Image.Load(@"./Service/TestData/test4.png");
            var imageEditorBll = new ImageEditorBLL();

            var byteArray = imageEditorBll.Resize(srcPic).Result;
            var ms = new MemoryStream(byteArray);
            var img = new Bitmap(ms);
            img.Width.Should().Be(320);
            img.Height.Should().Be(240);
        }
    }
}