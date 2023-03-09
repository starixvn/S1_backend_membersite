namespace MemberSite.Service.Model
{
    public class News
    {
        public int NewsId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageURL { get; set; }
        public DateTime PostTime { get; set; }
        public int Sort { get; set; }
    }
}
