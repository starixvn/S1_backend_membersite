using MemberSite.Service.BLL;
using MemberSite.Service.Model;
using NSubstitute;
using NUnit.Framework;

namespace MemberSiteTests.Service.BLL
{
    [TestFixture]
    public class AffCodeValidateTests
    {
        [Test]
        public void Valid_AffCode_Test()
        {
            // Arrange
            var affCode = "12345";
            var expected = 0;
            var langBLL = Substitute.For<ISelLang>();
            var affCodeValidate = new AffCodeValidate(langBLL);
            // Act
            var actual = affCodeValidate.Validate(affCode);
            // Assert
            Assert.AreEqual(expected, actual.errorCode);
        }
        [Test]
        public void Invalid_AffCode_Test()
        {
            // Arrange
            var affCode = "123456789012345678901234567890";
            var expected = -9;
            var langBLL = Substitute.For<ISelLang>();
            var affCodeValidate = new AffCodeValidate(langBLL);
            // Act
            var actual = affCodeValidate.Validate(affCode);
            // Assert
            Assert.AreEqual(expected, actual.errorCode);
        }
    }
}
