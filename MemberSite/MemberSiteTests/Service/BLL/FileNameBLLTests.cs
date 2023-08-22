using NUnit.Framework;

namespace MemberSite.Service.BLL.Tests
{
    [TestFixture]
    public class FileNameBLLTests
    {
        [Test]
        public void Exclude_Path_When_FileName_With_Path()
        {
            var fileName = @"C:\\Users\\HulkChou\\Pictures\\hulk-avengers-5.jpeg";
            var fileNameBll = new FileNameBLL();
            var expected = @"hulk-avengers-5.jpeg";

            var actual = fileNameBll.SetFileName(fileName).ExcludePath().FileName;

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Change_Extension_PNG_To_JPG()
        {
            var fileName = @"hulk-avengers-5.png";
            var fileNameBll = new FileNameBLL();
            var expected = @"hulk-avengers-5.jpg";

            var actual = fileNameBll.SetFileName(fileName).SetFilenameExtension("jpg").FileName;

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Exclude_Path_And_Change_Extension_To_JPG()
        {
            var fileName = @"C:\\Users\\HulkChou\\Pictures\\hulk-avengers-5.png";
            var fileNameBll = new FileNameBLL();
            var expected = @"hulk-avengers-5.jpg";

            var actual = fileNameBll.SetFileName(fileName).ExcludePath().SetFilenameExtension("jpg").FileName;

            Assert.AreEqual(expected, actual);
        }
    }
}