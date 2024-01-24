using MemberSite.Service.DAL;
using MemberSite.Service.Model.enums;

namespace MemberSite.Service.BLL
{
    public class QueueJobBLL 
    {
        private readonly QueueJobService _queueJobService;
        
        public QueueJobBLL(IServiceProvider provider)
        {
            _queueJobService = provider.GetRequiredService<QueueJobService>();
        }
        
        public int InsertQueueJob(int custId, QueueJob type, string data)
        {
            var result = _queueJobService.InsertQueueJob(custId, type, data);
            return result;
        }
    }
}
