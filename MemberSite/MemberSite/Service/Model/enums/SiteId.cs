using System.ComponentModel;

namespace MemberSite.Service.Model.enums
{
    public enum SiteId
    {
        Nova88 = 1,
        Goal123 = 2,
        Bwing = 3,
        Ivorysport = 5,
        Yashbet = 6
    }

    public enum SiteEnum
    {
        [Description("Nova88")]
        NOVA88 = 1,
        [Description("Goal123")]
        GOAL123 = 2,
        [Description("Bwing")]
        BWING = 3,
        [Description("Soon88")]
        SOON88 = 4,
        [Description("Ivorysport")]
        IVORYSPORT = 5,
        [Description("Yashbet")]
        YASHBET = 6
    }
}