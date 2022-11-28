using MemberSite.Service.Model.Interfaces;

namespace MemberSite.Service.Model
{
    public class PageInfo : IPageInfo
    {
        public int TotalPages { get; set; } = 0;
        public int PageNumber { get; set; } = 0;
        public int RowCount { get; set; } = 0;
    }
}
