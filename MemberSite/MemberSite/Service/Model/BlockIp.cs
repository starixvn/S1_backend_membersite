namespace MemberSite.Service.Model
{
    public class BlockIP
    {
        public string IP { get; set; }
    }

    public class BlockIPForUpload
    {
        public string IP
        {
            get => $"{_IP} 1;";
            set => _IP = value;
        }

        private string _IP { get; set; }
    }

    public class BlockIpRequest
    {
        public string IP { get; set; }
        public string Memo { get; set; }
        public string Operator { get; set; }
    }
}