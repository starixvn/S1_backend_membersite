using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Domain;
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
            var expected = true;
            var affCodeValidate = new AffCodeObject(affCode);
            // Act
            var actual = affCodeValidate.IsValid();
            // Assert
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public void Invalid_AffCode_Test()
        {
            // Arrange
            var affCode = "123456789012345678901234567890";
            var expected = false;
            var affCodeValidate = new AffCodeObject(affCode);
            // Act
            var actual = affCodeValidate.IsValid();
            // Assert
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public void Normal_AffCode_Test()
        {
            // Arrange
            var siteId = 1;
            var affCode = "666666";
            var affiliateService = Substitute.For<IAffiliateService>();
            affiliateService.ExistsAffiliateCode(Arg.Any<int>(), Arg.Any<string>()).Returns(true);
            var expected = true;
            var affCodeValidate = new AffCodeObject(affCode);
            // Act
            var actual = affCodeValidate.IsExists(siteId, affiliateService.ExistsAffiliateCode);
            // Assert
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public void NotFound_AffCode_Test()
        {
            // Arrange
            var siteId = 1;
            var affCode = "111777";
            var affiliateService = Substitute.For<IAffiliateService>();
            affiliateService.ExistsAffiliateCode(Arg.Any<int>(), Arg.Any<string>()).Returns(false);
            var expected = false;
            var affCodeValidate = new AffCodeObject(affCode);
            // Act
            var actual = affCodeValidate.IsExists(siteId, affiliateService.ExistsAffiliateCode);
            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
