using MemberSite.Service.DAL.Interface;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using RabbitMQ.Client;

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
            var optionsMock = new ConnectionFactory()
            {
                HostName = "18.139.54.64",
                UserName = "ailab",
                Password = "VynvPGTm7N"
            };
            services.AddTransient(p => verificationRewardService);
            
            var serviceProvider = services.BuildServiceProvider();
            var verificationRewardBll = new VerificationRewardBLL(optionsMock);

            verificationRewardBll.PhoneReward(custId);
        }
    }
}