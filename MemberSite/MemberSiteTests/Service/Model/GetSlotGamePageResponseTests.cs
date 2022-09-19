using NUnit.Framework;
using System;
using MemberSite.Service.Model.enums;
using MemberSite.Service.Model.Game;
using System.Collections.Generic;

namespace MemberSite.Service.Model.Tests
{
    [TestFixture()]
    public class GetSlotGamePageResponseTests
    {
        private string _imgUrl;
        private List<GameProvider> _providerList;
        private List<SlotGameInfo> _slotGameList;
        private List<UserGameInfo> _favorite;
        private List<UserGameInfo> _recentlyPlayed;
        private List<DisableTpGame> _disableGame;

        [SetUp]
        public void Setup()
        {
            _imgUrl = "test";
            _providerList = new List<GameProvider>
            {
                new GameProvider() { TpId = 1, TpName = "1" },
                new GameProvider() { TpId = 2, TpName = "2" },
                new GameProvider() { TpId = 3, TpName = "3" },
                new GameProvider() { TpId = 4, TpName = "4" },
                new GameProvider() { TpId = 5, TpName = "5" },
                new GameProvider() { TpId = 6, TpName = "6" },
            };
            _slotGameList = new List<SlotGameInfo>
            {
                new SlotGameInfo(){ TpId=1, GameId = "1", LaunchDate = DateTime.Now , SlotGameCategoryList = new List<int> { (int)SlotGameCategory.Featured, (int)SlotGameCategory.Megaways } },
                new SlotGameInfo(){ TpId=1, GameId = "11", LaunchDate = DateTime.Now , SlotGameCategoryList = new List<int> { (int)SlotGameCategory.Featured, (int)SlotGameCategory.Megaways } },
                new SlotGameInfo(){ TpId=2, GameId = "2", LaunchDate = DateTime.Now.AddMonths(-2) ,SlotGameCategoryList = new List<int> { (int)SlotGameCategory.Jackpot, (int)SlotGameCategory.Megaways } },
                new SlotGameInfo(){ TpId=3, GameId = "3", LaunchDate = DateTime.Now.AddMonths(-2) ,SlotGameCategoryList = new List<int> { (int)SlotGameCategory.TableAndCards, (int)SlotGameCategory.Megaways } },
                new SlotGameInfo(){ TpId=4, GameId = "4", LaunchDate = DateTime.Now ,SlotGameCategoryList = new List<int> { (int)SlotGameCategory.BuyFeaturesGames, (int)SlotGameCategory.Megaways } },
                new SlotGameInfo(){ TpId=5, GameId = "5", LaunchDate = DateTime.Now ,SlotGameCategoryList = new List<int> { (int)SlotGameCategory.Megaways} },
                new SlotGameInfo(){ TpId=5, GameId = "55", LaunchDate = DateTime.Now ,SlotGameCategoryList = new List<int> { (int)SlotGameCategory.Megaways } },
                new SlotGameInfo(){ TpId=6, GameId = "6", LaunchDate = DateTime.Now ,SlotGameCategoryList = new List<int> { (int)SlotGameCategory.Megaways } },
            };
            _favorite = new List<UserGameInfo>
            {
                new UserGameInfo(){ TpId=1, GameId = "1"},
                new UserGameInfo(){ TpId=2, GameId = "2"},
                new UserGameInfo(){ TpId=3, GameId = "33"},
                new UserGameInfo(){ TpId=5, GameId = "55"},
            };
            _recentlyPlayed = new List<UserGameInfo>
            {
                new UserGameInfo(){ TpId=3, GameId = "3"},
                new UserGameInfo(){ TpId=3, GameId = "33"},
                new UserGameInfo(){ TpId=4, GameId = "4"},
                new UserGameInfo(){ TpId=6, GameId = "6"},
            };
            _disableGame = new List<DisableTpGame>
            {
                new DisableTpGame(){ TpId = 5, GameList = new List<string> { "55"} },
                new DisableTpGame(){ TpId = 6 },
            };
        }

        [Test]
        public void NoLoginSlotGamePageTests()
        {
            var actual = new GetSlotGamePageResponse(_imgUrl, _providerList, _slotGameList);

            Assert.AreEqual("test", actual.ImgUrl);
            Assert.AreEqual(6, actual.ProviderList.Count);

            Assert.AreEqual(8, actual.All.Count);
            Assert.AreEqual(6, actual.Random.Count);

            Assert.AreEqual(0, actual.MyFavorite.Count);
            Assert.AreEqual(0, actual.RecentlyPlayed.Count);

            Assert.AreEqual(6, actual.New.Count);
            Assert.AreEqual(2, actual.Featured.Count);
            Assert.AreEqual(1, actual.Jackpot.Count);
            Assert.AreEqual(1, actual.TableAndCards.Count);
            Assert.AreEqual(1, actual.BuyFeaturesGames.Count);
            Assert.AreEqual(8, actual.Megaways.Count);
        }


        [Test]
        public void GetSlotGamePageTests()
        {
           var actual = new GetSlotGamePageResponse(_imgUrl, _providerList, _slotGameList, _favorite, _recentlyPlayed, _disableGame);

           Assert.AreEqual("test", actual.ImgUrl);
           Assert.AreEqual(5, actual.ProviderList.Count);
           
           Assert.AreEqual(6, actual.All.Count);
           Assert.AreEqual(5, actual.Random.Count);

           Assert.AreEqual(2, actual.MyFavorite.Count);
           Assert.AreEqual(2, actual.RecentlyPlayed.Count);

           Assert.AreEqual(4, actual.New.Count);
           Assert.AreEqual(2, actual.Featured.Count);
           Assert.AreEqual(1, actual.Jackpot.Count);
           Assert.AreEqual(1, actual.TableAndCards.Count);
           Assert.AreEqual(1, actual.BuyFeaturesGames.Count);
           Assert.AreEqual(6, actual.Megaways.Count);
        }

    }
}