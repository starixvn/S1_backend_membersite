using FluentAssertions;
using MemberSite.Service.BLL;
using MemberSite.Service.DAL.Interface;
using MemberSite.Service.Library;
using MemberSite.Service.Model;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

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
            eventsSvc.GetTickets(Arg.Any<int>(), Arg.Any<int>()).Returns(x => 
            {
                return new List<AngPowTicket>();
            });
            
            var cacheHelper = new CacheHelper(_serviceProvider);
            var angPao = new EventsBLL(eventsSvc , cacheHelper);
            var custId = 250032;
            var currencyId = 12;
            var expect = 102;

            var (error, ticketCount) = await angPao.CheckDrawPrize(custId, currencyId);

            error.Should().Be(expect);
        }

        [Test]
        public async Task User_Has_2_Tickets_And_1_Tickets_Can_Use() 
        {
            var eventsSvc = Substitute.For<IEventsService>();
            _ = eventsSvc.GetTickets(Arg.Any<int>(), Arg.Any<int>()).Returns(x =>
              {
                  return new List<AngPowTicket>()
                  {
                    new AngPowTicket() { CustId = 250032, IsUsed = true, TicketNo =1 },
                    new AngPowTicket() { CustId = 250032, IsUsed = false, TicketNo =2 }
                  };
              });
            var cacheHelper = new CacheHelper(_serviceProvider);
            var angPao = new EventsBLL(eventsSvc, cacheHelper);
            var custId = 250032;
            var currencyId = 12;
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
            var angPao = new EventsBLL(eventsSvc, cacheHelper);
            var custId = 250032;
            var currencyId = 12;
            var expect = 103;
            var expectTicket = 0;

            var (error, ticketCount) = await angPao.CheckDrawPrize(custId, currencyId);

            error.Should().Be(expect);
            ticketCount.Should().Be(expectTicket);
        }
    }
}
