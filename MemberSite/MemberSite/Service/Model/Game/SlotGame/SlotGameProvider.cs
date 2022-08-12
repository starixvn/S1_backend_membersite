namespace MemberSite.Service.Model.Game
{
    public class SlotGameProvider : GameProvider
    {
        public SlotGameProvider(GameProvider gameProvider, string displayText)
        {
            TpId = gameProvider.TpId;
            TpName = gameProvider.TpName;
            PlayFree = gameProvider.PlayFree;
            DisplayText = displayText;
        }

        public string DisplayText { get; set; }
    }
}
