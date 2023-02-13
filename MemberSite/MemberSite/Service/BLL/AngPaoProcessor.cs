using MemberSite.Service.Model;

namespace MemberSite.Service.BLL
{
    public class AngPaoProcessor
    {
        public virtual Prize Drawing(IEnumerable<Prize> prizes) 
        {
            var drawPrize = new Prize();
            var quota = prizes.Sum(a => a.Quota);
            if (quota > 0)
            {
                var r = new Random();
                var LuckyNum = r.Next(quota) + 1;
                var LuckyCnt = 0;

                foreach (var prize in prizes.Where(x => x.Quota > 0))
                {
                    drawPrize = prize;
                    LuckyCnt += prize.Quota;
                    if (LuckyNum <= LuckyCnt)
                    {
                        break;
                    }
                }
            }
            //獎項抽完了,給最小獎
            if (drawPrize == null)
            {
                var prize = prizes.Aggregate((min, next) => min.Amount > next.Amount ? next : min);
                drawPrize = prize;
            }

            return drawPrize;
        }
    }
}
