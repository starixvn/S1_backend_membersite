using System.Threading.Tasks;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace MemberSite.Service.BLL.Tests
{
    [TestFixture]
    public class VerificationRewardBLLTests
    {
        [Test]
        public void New_User_Verify_Phone_And_Get_Reward_50()
        {
            var custId = 250032;
            var services = new ServiceCollection();
            var verificationRewardService = Substitute.For<IVerificationRewardService>();
            var optionsMock = Substitute.For<IOptions<RabbitMQConfig>>();
            var myOptions = new RabbitMQConfig
            {
                HostName = "18.139.54.64",
                UserName = "ailab",
                Password = "VynvPGTm7N"
            };
            optionsMock.Value.Returns(myOptions);
            services.AddTransient(p => verificationRewardService);
            
            var serviceProvider = services.BuildServiceProvider();
            var verificationRewardBll = new VerificationRewardBLL(optionsMock);

            verificationRewardBll.PhoneReward(custId);
        }
    }
}