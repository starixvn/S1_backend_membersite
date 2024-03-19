using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using MemberSite.Service.BLL;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using NUnit.Framework;

namespace MemberSiteTests.Service.BLL
{
    [TestFixture]
    public class AngPaoBLLTests
    {
        private ServiceProvider _serviceProvider;
        [SetUp]
        public void SetUp()
        {
            var services = new ServiceCollection();
            services.AddTransient(p => Substitute.For<IMemoryCache>());
            _serviceProvider = services.BuildServiceProvider();
        }
        [Test]
        public async Task User_Has_No_Ticktes()
        {
            var eventsSvc = Substitute.For<IEventsService>();
            var angPaoPro = Substitute.For<AngPaoProcessor>();
            eventsSvc.GetTickets(Arg.Any<int>(), Arg.Any<int>()).Returns(x =>
            {
                return new List<AngPowTicket>();
            });

            var cacheHelper = new CacheHelper(_serviceProvider);
            var angPao = new AngPaoBLL(eventsSvc, angPaoPro);
            var custId = 250032;
            var currencyId = 2;
            var expect = 102;

            var (error, ticketCount) = await angPao.CheckDrawPrize(custId, currencyId);

            error.Should().Be(expect);
        }

        [Test]
        public async Task User_Has_2_Tickets_And_1_Tickets_Can_Use()
        {
            var eventsSvc = Substitute.For<IEventsService>();
            var angPaoPro = Substitute.For<AngPaoProcessor>();
            _ = eventsSvc.GetTickets(Arg.Any<int>(), Arg.Any<int>()).Returns(x =>
              {
                  return new List<AngPowTicket>()
                  {
                    new AngPowTicket() { CustId = 250032, IsUsed = true, TicketNo =1 },
                    new AngPowTicket() { CustId = 250032, IsUsed = false, TicketNo =2 }
                  };
              });
            var cacheHelper = new CacheHelper(_serviceProvider);
            var angPao = new AngPaoBLL(eventsSvc, angPaoPro);
            var custId = 250032;
            var currencyId = 2;
            var expect = 0;
            var expectTicket = 1;

            var (error, ticketCount) = await angPao.CheckDrawPrize(custId, currencyId);

            error.Should().Be(expect);
            ticketCount.Should().Be(expectTicket);
        }

        [Test]
        public async Task User_Has_No_Can_Be_Used_Ticket()
        {
            var eventsSvc = Substitute.For<IEventsService>();
            var angPaoPro = Substitute.For<AngPaoProcessor>();
            _ = eventsSvc.GetTickets(Arg.Any<int>(), Arg.Any<int>()).Returns(x =>
            {
                return new List<AngPowTicket>()
                  {
                    new AngPowTicket() { CustId = 250032, IsUsed = true, TicketNo =1 },
                    new AngPowTicket() { CustId = 250032, IsUsed = true, TicketNo =2 },
                    new AngPowTicket() { CustId = 250032, IsUsed = true, TicketNo =2 }
                  };
            });
            var cacheHelper = new CacheHelper(_serviceProvider);
            var angPao = new AngPaoBLL(eventsSvc, angPaoPro);
            var custId = 250032;
            var currencyId = 12;
            var expect = 103;
            var expectTicket = 0;

            var (error, ticketCount) = await angPao.CheckDrawPrize(custId, currencyId);

            error.Should().Be(expect);
            ticketCount.Should().Be(expectTicket);
        }

        [Test]
        public async Task User_Have_Not_Filled_AccountName_And_Verified_Phone()
        {
            var customer = new Customer()
            {
                CustId = 250032,
                PhoneVerified = false
            };

            var eventsSvc = Substitute.For<IEventsService>();
            var angPaoPro = Substitute.For<AngPaoProcessor>();
            eventsSvc.GetPrizes(Arg.Any<int>(), Arg.Any<int>()).Returns(x =>
            {
                return new List<Prize>()
                {
                    new Prize() { PrizeId  = 118 ,PrizeType = 1, PrizeName = "2 MYR", Amount = 2m, StartTime = new DateTime(2023,1,5), EndTime = new DateTime(2023,2,6), Quota = 10, Jackpot = 0 },
                    new Prize() { PrizeId  = 119 ,PrizeType = 1, PrizeName = "3 MYR", Amount = 3m, StartTime = new DateTime(2023,1,5), EndTime = new DateTime(2023,2,6), Quota = 5, Jackpot = 0 },
                    new Prize() { PrizeId  = 120 ,PrizeType = 1, PrizeName = "8 MYR", Amount = 8m, StartTime = new DateTime(2023,1,5), EndTime = new DateTime(2023,2,6), Quota = 1, Jackpot = 0 }
                };
            });
            var angPao = new AngPaoBLL(eventsSvc, angPaoPro);
            var eventId = 12;
            var expect = 104;
            var time = new DateTime(2023, 1, 10, 10, 22, 00);
            var (error, _) = await angPao.DrawPrize(eventId, customer, time);

            error.Should().Be(expect);
        }

        [Test]
        public async Task AngPao_Have_Not_Started()
        {
            var customer = new Customer()
            {
                CustId = 250032,
                PhoneVerified = true,
                AccountName = "Xidada"
            };
            var eventsSvc = Substitute.For<IEventsService>();
            var angPaoPro = Substitute.For<AngPaoProcessor>();
            eventsSvc.GetPrizes(Arg.Any<int>(), Arg.Any<int>()).Returns(x =>
            {
                return new List<Prize>();
            });
            eventsSvc.GetTickets(Arg.Any<int>(), Arg.Any<int>()).Returns(x =>
            {
                return new List<AngPowTicket>();
            });

            var cacheHelper = new CacheHelper(_serviceProvider);
            var angPao = new AngPaoBLL(eventsSvc, angPaoPro);
            var eventId = 12;
            var expect = 101;
            var time = new DateTime(2023, 1, 10, 10, 22, 00);
            var (error, _) = await angPao.DrawPrize(eventId, customer, time);

            error.Should().Be(expect);
        }

        [Test]
        public async Task AngPao_Only_1_To_24_Clock_Can_Drawing()
        {
            var customer = new Customer()
            {
                CustId = 250032,
                CurrencyId = 2,
                PhoneVerified = true,
                AccountName = "Xidada"
            };
            var eventsSvc = Substitute.For<IEventsService>();
            var angPaoPro = Substitute.For<AngPaoProcessor>();
            eventsSvc.GetPrizes(Arg.Any<int>(), Arg.Any<int>()).Returns(x =>
            {
                return new List<Prize>()
                {
                    new Prize() { PrizeId  = 118 ,PrizeType = 1, PrizeName = "2 MYR", Amount = 2m, StartTime = new DateTime(2023,1,5), EndTime = new DateTime(2023,2,6), Quota = 10, Jackpot = 0 },
                    new Prize() { PrizeId  = 119 ,PrizeType = 1, PrizeName = "3 MYR", Amount = 3m, StartTime = new DateTime(2023,1,5), EndTime = new DateTime(2023,2,6), Quota = 5, Jackpot = 0 },
                    new Prize() { PrizeId  = 120 ,PrizeType = 1, PrizeName = "8 MYR", Amount = 8m, StartTime = new DateTime(2023,1,5), EndTime = new DateTime(2023,2,6), Quota = 1, Jackpot = 0 }
                };
            });
            eventsSvc.GetTickets(Arg.Any<int>(), Arg.Any<int>()).Returns(x =>
            {
                return new List<AngPowTicket>();
            });

            var cacheHelper = new CacheHelper(_serviceProvider);
            var angPao = new AngPaoBLL(eventsSvc, angPaoPro);
            var eventId = 12;
            var expect = 105;
            var time = new DateTime(2023, 1, 10, 0, 22, 00);
            var (error, _) = await angPao.DrawPrize(eventId, customer, time);

            error.Should().Be(expect);
        }

        [Test]
        public async Task User_Has_No_Ticktes_When_Drawing()
        {
            var customer = new Customer()
            {
                CustId = 250032,
                PhoneVerified = true,
                AccountName = "Xidada"
            };
            var eventsSvc = Substitute.For<IEventsService>();
            var angPaoPro = Substitute.For<AngPaoProcessor>();
            eventsSvc.GetPrizes(Arg.Any<int>(), Arg.Any<int>()).Returns(x =>
            {
                return new List<Prize>()
                {
                    new Prize() { PrizeId  = 118 ,PrizeType = 1, PrizeName = "2 MYR", Amount = 2m, StartTime = new DateTime(2023,1,5), EndTime = new DateTime(2023,2,6), Quota = 10, Jackpot = 0 },
                    new Prize() { PrizeId  = 119 ,PrizeType = 1, PrizeName = "3 MYR", Amount = 3m, StartTime = new DateTime(2023,1,5), EndTime = new DateTime(2023,2,6), Quota = 5, Jackpot = 0 },
                    new Prize() { PrizeId  = 120 ,PrizeType = 1, PrizeName = "8 MYR", Amount = 8m, StartTime = new DateTime(2023,1,5), EndTime = new DateTime(2023,2,6), Quota = 1, Jackpot = 0 }
                };
            });
            eventsSvc.GetTickets(Arg.Any<int>(), Arg.Any<int>()).Returns(x =>
            {
                return new List<AngPowTicket>();
            });

            var cacheHelper = new CacheHelper(_serviceProvider);
            var angPao = new AngPaoBLL(eventsSvc, angPaoPro);
            var eventId = 12;
            var expect = 102;
            var time = new DateTime(2023, 1, 10, 10, 22, 00);
            var (error, _) = await angPao.DrawPrize(eventId, customer, time);

            error.Should().Be(expect);
        }

        [Test]
        public async Task User_Drawing_And_Get_2_MYR_Prize()
        {
            var customer = new Customer()
            {
                CustId = 250032,
                CurrencyId = 2,
                SiteId = 1,
                PhoneVerified = true,
                AccountName = "Xidada"
            };
            var eventsSvc = Substitute.For<IEventsService>();
            var angPaoPro = Substitute.For<AngPaoProcessor>();
            angPaoPro.Drawing(Arg.Any<List<Prize>>()).Returns(x =>
            {
                return new Prize() { PrizeId = 118, PrizeType = 1, PrizeName = "2 MYR", Amount = 2m, StartTime = new DateTime(2023, 1, 5), EndTime = new DateTime(2023, 2, 6), Quota = 5, Jackpot = 0 };
            });
            eventsSvc.GetTickets(Arg.Any<int>(), Arg.Any<int>()).Returns(x =>
            {
                return new List<AngPowTicket>()
                  {
                    new AngPowTicket() { CustId = 250032, IsUsed = false, TicketNo = 1 }
                  };
            });
            eventsSvc.GetPrizes(Arg.Is(12), Arg.Is(2)).Returns(x =>
            {
                return new List<Prize>()
                {
                    new Prize() { PrizeId  = 118 ,PrizeType = 1, PrizeName = "2 MYR", Amount = 2m, StartTime = new DateTime(2023,1,5), EndTime = new DateTime(2023,2,6), Quota = 10, Jackpot = 0 },
                    new Prize() { PrizeId  = 119 ,PrizeType = 1, PrizeName = "3 MYR", Amount = 3m, StartTime = new DateTime(2023,1,5), EndTime = new DateTime(2023,2,6), Quota = 5, Jackpot = 0 },
                    new Prize() { PrizeId  = 120 ,PrizeType = 1, PrizeName = "8 MYR", Amount = 8m, StartTime = new DateTime(2023,1,5), EndTime = new DateTime(2023,2,6), Quota = 1, Jackpot = 0 }
                };
            });
            eventsSvc.GetCustPrizeOnToday(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).ReturnsNull();
            eventsSvc.PayoutPrize(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<decimal>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>()).Returns(x => 1);
            var cacheHelper = new CacheHelper(_serviceProvider);
            var angPao = new AngPaoBLL(eventsSvc, angPaoPro);
            var eventId = 12;
            var expect = 1;
            var expectAmount = 2m;
            var time = new DateTime(2023, 1, 10, 10, 22, 00);
            var (error, prize) = await angPao.DrawPrize(eventId, customer, time);

            error.Should().Be(expect);
            prize.Amount.Should().Be(expectAmount);
        }
    }
}
