using MemberSite.Service.Model.enums;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace MemberSite.Service.BLL
{
    public class VerificationRewardBLL
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<VerificationRewardBLL> _logger;
        public VerificationRewardBLL(IConnectionFactory connectionFactory, ILogger<VerificationRewardBLL> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }
        public void PhoneReward(int custId)
        {
            var data = new
            {
                CustId = custId,
                OTPType = (int)OTPType.SMS
            };
            var message = JsonConvert.SerializeObject(data);
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using var connection = _connectionFactory.CreateConnection();
                    using var channel = connection.CreateModel();

                    var body = Encoding.UTF8.GetBytes(message);
                    var properties = channel.CreateBasicProperties();
                    properties.ContentType = "application/json";

                    channel.BasicPublish(exchange: "MemberExchange",
                                         routingKey: "verify.phone",
                                         basicProperties: properties,
                                         body: body);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"publish fail MemberExchange, verify.phone, {message}");
                }
            }
        }

        public void EmailReward(int custId)
        {
            var data = new
            {
                CustId = custId,
                OTPType = (int)OTPType.Email
            };
            var message = JsonConvert.SerializeObject(data);
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using var connection = _connectionFactory.CreateConnection();
                    using var channel = connection.CreateModel();

                    var body = Encoding.UTF8.GetBytes(message);
                    var properties = channel.CreateBasicProperties();
                    properties.ContentType = "application/json";

                    channel.BasicPublish(exchange: "MemberExchange",
                                         routingKey: "verify.email",
                                         basicProperties: properties,
                                         body: body);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"publish fail MemberExchange, verify.email, {message}");
                }
            }
        }
    }
}