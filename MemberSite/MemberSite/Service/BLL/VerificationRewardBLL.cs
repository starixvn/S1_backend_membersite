using MemberSite.Service.Model.enums;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace MemberSite.Service.BLL
{
    public class VerificationRewardBLL
    {
        private readonly IConnectionFactory _connectionFactory;
        public VerificationRewardBLL(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }
        public void PhoneReward(int custId)
        {
            using var connection = _connectionFactory.CreateConnection();
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
            using var connection = _connectionFactory.CreateConnection();
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