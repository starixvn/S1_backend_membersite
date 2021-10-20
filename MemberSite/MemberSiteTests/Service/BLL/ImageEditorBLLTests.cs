using System.Drawing;
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
            var srcPic = new Bitmap(@"./Service/TestData/test1.jpeg");
            var imageEditorBll = new ImageEditorBLL();

            var actual = imageEditorBll.Resize(srcPic);

            actual.Width.Should().Be(500);
            actual.Height.Should().Be(375);
        }
        
        [Test]
        public void Resize_Jeg()
        {
            var srcPic = new Bitmap(@"./Service/TestData/test2.jpg");
            var imageEditorBll = new ImageEditorBLL();

            var actual = imageEditorBll.Resize(srcPic);

            actual.Width.Should().Be(500);
            actual.Height.Should().Be(375);
        }
        
        [Test]
        public void Resize_4K_Jpg()
        {
            var srcPic = new Bitmap(@"./Service/TestData/test3.jpg");
            var imageEditorBll = new ImageEditorBLL();

            var actual = imageEditorBll.Resize(srcPic);

            actual.Width.Should().Be(500);
            actual.Height.Should().Be(333);
        }
        
        [Test]
        public void Resize_Normal_Size_Png()
        {
            var srcPic = new Bitmap(@"./Service/TestData/test4.png");
            var imageEditorBll = new ImageEditorBLL();

            var actual = imageEditorBll.Resize(srcPic);

            actual.Width.Should().Be(320);
            actual.Height.Should().Be(240);
        }
    }
}