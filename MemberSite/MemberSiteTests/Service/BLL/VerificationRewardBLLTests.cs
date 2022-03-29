using System.Threading.Tasks;
using MemberSite.Service.DAL.Interface;
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
            services.AddTransient(p => verificationRewardService);
            
            var serviceProvider = services.BuildServiceProvider();
            var verificationRewardBll = new VerificationRewardBLL(serviceProvider);

            await verificationRewardBll.PhoneReward(custId);
        }
    }
}