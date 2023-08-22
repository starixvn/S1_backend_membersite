using System;

using MemberSite.Service.Model.enums;

using NUnit.Framework;

namespace MemberSite.Service.Model.Tests
{
    [TestFixture()]
    public class TpMaintainTests
    {
        private DateTime now;

        [SetUp]
        public void Setup()
        {
            now = Convert.ToDateTime("2021-05-25 14:30");
        }

        [TestCase("2021-01-01 00:00", "2022-01-01 00:00", ExpectedResult = true)]
        [TestCase("2021-05-25 14:00", "2021-05-25 15:00", ExpectedResult = true)]
        [TestCase("2022-01-01 00:00", "2023-01-01 00:00", ExpectedResult = false)]
        public bool Date_MaintainCheck_Test(DateTime beginDate, DateTime endDate)
            => new TpMaintain()
            {
                BeginDate = beginDate,
                EndDate = endDate,
                MaintainType = MaintainType.Date
            }
            .MaintainCheck(now);

        [TestCase("14:29", "14:31", ExpectedResult = true)]
        [TestCase("10:00", "15:00", ExpectedResult = true)]
        [TestCase("10:00", "11:00", ExpectedResult = false)]
        [TestCase("00:00", "00:00", ExpectedResult = false)]
        public bool Day_MaintainCheck_Test(string beginTime, string endTime)
            => new TpMaintain()
            {
                BeginDate = new DateTime(2021, 01, 01),
                EndDate = new DateTime(2099, 01, 01),
                MaintainType = MaintainType.Day,
                BeginTime = beginTime,
                EndTime = endTime,
            }
            .MaintainCheck(now);


        [TestCase((int)DayOfWeek.Sunday, ExpectedResult = false)]
        [TestCase((int)DayOfWeek.Monday, ExpectedResult = false)]
        [TestCase((int)DayOfWeek.Tuesday, ExpectedResult = true)]
        public bool Week_MaintainCheck_Test(int dayOfWeek)
            => new TpMaintain()
            {
                BeginDate = new DateTime(2021, 01, 01),
                EndDate = new DateTime(2099, 01, 01),
                MaintainType = MaintainType.Week,
                TypeValue = dayOfWeek,
                BeginTime = "14:00",
                EndTime = "15:00",
            }
            .MaintainCheck(now);


        [TestCase(21, ExpectedResult = false)]
        [TestCase(23, ExpectedResult = false)]
        [TestCase(25, ExpectedResult = true)]
        public bool Month_MaintainCheck_Test(int day)
            => new TpMaintain()
            {
                BeginDate = new DateTime(2021, 01, 01),
                EndDate = new DateTime(2099, 01, 01),
                MaintainType = MaintainType.Month,
                TypeValue = day,
                BeginTime = "14:00",
                EndTime = "15:00",
            }
            .MaintainCheck(now);
    }
}