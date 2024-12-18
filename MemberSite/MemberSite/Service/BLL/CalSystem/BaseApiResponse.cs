namespace MemberSite.Service.BLL.CalSystem
{
    public class BaseApiResponse<T> : BaseApiResponse
    {
        public T Data { get; set; }
    }
    
    public class BaseApiResponse
    {
        public ApiErrorCode[] Errors { get; set; }
        public string Success { get; set; }
    }
}