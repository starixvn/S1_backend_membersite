using MemberSite.Service.BLL.Interface;
using MemberSite.Service.DAL.Interface;

namespace MemberSite.Service.BLL
{
    public class HotGameBLL: IHotGameBLL
    {
        private readonly IHotGameService _hotGameService;

        public HotGameBLL(IHotGameService hotGameService)
        {
            this._hotGameService = hotGameService;
        }



    }
}
