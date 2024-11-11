namespace MemberSite.Service.Model;

public class HotMatchDto
{
    public string SportLeaguesTitle { get; set; }
    public int MatchId { get; set; }

    public string HomeTeamName { get; set; }

    public string HomeIconUrl { get; set; }

    public string AwayTeamName { get; set; }

    public string AwayIconUrl { get; set; }

    public DateTime BattleTime { get; set; }

    public decimal? HomeOdds { get; set; }

    public decimal? AwayOdds { get; set; }

    public decimal? Handicap { get; set; }
}
