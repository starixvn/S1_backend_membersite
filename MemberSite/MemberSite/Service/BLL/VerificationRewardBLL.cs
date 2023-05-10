using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Model;
using MemberSite.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace MemberSite.Service.BLL
{
    public class VerificationRewardBLL
    {
        private readonly RabbitMQConfig _config;
        public VerificationRewardBLL(IOptions<RabbitMQConfig> config)
        {
            _config = config.Value;
        }
        public void PhoneReward(int custId)
        {
            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                UserName = _config.UserName,
                Password = _config.Password
            };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            var data = new
            {
                CustId = custId,
                OTPType = (int)OTPType.SMS
            };
            var message = JsonConvert.SerializeObject(data);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = channel.CreateBasicProperties();
            properties.ContentType = "application/json";

            channel.BasicPublish(exchange: "MemberExchange",
                                 routingKey: "verify.phone",
                                 basicProperties: properties,
                                 body: body);
        }

        public void EmailReward(int custId)
        {
            var factory = new ConnectionFactory
            {
                HostName = _config.HostName, 
                UserName = _config.UserName, 
                Password = _config.Password
            };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            var data = new
            {
                CustId = custId,
                OTPType = (int)OTPType.Email
            };
            var message = JsonConvert.SerializeObject(data);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = channel.CreateBasicProperties();
            properties.ContentType = "application/json";

            channel.BasicPublish(exchange: "MemberExchange",
                                 routingKey: "verify.email",
                                 basicProperties: properties,
                                 body: body);
        }
    }
}