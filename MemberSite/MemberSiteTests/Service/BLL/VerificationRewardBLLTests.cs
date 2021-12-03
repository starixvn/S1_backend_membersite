using System.Threading.Tasks;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace MemberSite.Service.BLL.Tests
{
    [TestFixture]
    public class VerificationRewardBLLTests
    {
        [Test]
        public async Task New_User_Verify_Phone_And_Get_Reward_50()
        {
            var custId = 250032;
            var services = new ServiceCollection();
            var verificationRewardService = Substitute.For<IVerificationRewardService>();
            verificationRewardService.GetCustVerificationReward(Arg.Any<int>()).Returns(
                new CustVerificationRewardData()
            {
                CustId = 250032,
                SiteId = 1,
                UserName = "hulkchou",
                CurrencyId = 2,
            });
            verificationRewardService.GetVerificationReward(Arg.Any<int>(), Arg.Any<int>()).Returns(
                new VerificationRewardData()
                {
                    SiteId = 1,
                    CurrencyId = 2,
                    VerificationType = 0,
                    PhoneReward = 50,
                    EmailReward = 30
                });
            verificationRewardService.PayoutVerificationReward(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<int>());
            services.AddTransient(p => verificationRewardService);
            
            var serviceProvider = services.BuildServiceProvider();
            var verificationRewardBll = new VerificationRewardBLL(serviceProvider);

            await verificationRewardBll.PhoneReward(custId);
        }
    }
}