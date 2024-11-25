namespace MemberSite.Service.Model;

public class PixelRequest
{
    public string TransId { get; set; }
    public int SiteId { get; set; }
    public int CustId { get; set; }
    public string Url { get; set; }
    public int CurrencyId { get; set; }
    public decimal Value { get; set; }
    public IEnumerable<FaceBookPixel> PixelEvent { get; set; }
}

public class CustFacebookData
{
    public int CustId { get; set; }
    public string TransId { get; set; }
    public string Fbc { get; set; }
    public string Fbp { get; set; }
    public string UserAgent { get; set; }
    public string Ip { get; set; }
 
}

public enum FaceBookPixel
{
    registerSubmit = 1,
    completeRegistration = 2,
    depositSubmit = 3,
    firstDeposit = 4,
    withdraw = 5,
    firstDepositArrival = 6,
    purchase = 7,
    redeposit = 8,
    startTrial = 9
}