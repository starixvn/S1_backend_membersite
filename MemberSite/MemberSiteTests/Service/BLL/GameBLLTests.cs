using NUnit.Framework;
using System.Collections.Generic;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL.Tests
{
    [TestFixture()]
    public class GameBLLTests
    {
        private GameBLL _gameBLL;
        private List<TpMaintain> _tpMaintainList;

        [SetUp]
        public void Setup()
        {
            _gameBLL = new GameBLL();
            _tpMaintainList = new List<TpMaintain>()
            {
                new TpMaintain() { TpId = 1, TpGameType = 1 },
            };
        }

        [TestCase(1, ExpectedResult = true)]
        [TestCase(2, ExpectedResult = false)]
        public bool Maintain_Test(int tpGameType)
        {

            return _gameBLL.IsMaintain(_tpMaintainList, new Dictionary<string, string>()
            {
                { "tpGameType",tpGameType.ToString() }
            }, out var tpMaintain);
        }

        [TestCase(1, ExpectedResult = false)]
        [TestCase(2, ExpectedResult = false)]
        public bool MaintainList_Null_Test(int tpGameType)
        {
            return _gameBLL.IsMaintain(null, new Dictionary<string, string>()
            {
                { "tpGameType",tpGameType.ToString() }
            }, out var tpMaintain);
        }

        [TestCase(ExpectedResult = false)]
        public bool TpGameType_Null_Test()
        {
            return _gameBLL.IsMaintain(_tpMaintainList, new Dictionary<string, string>(), out var tpMaintain);
        }

        [TestCase(1, ExpectedResult = true)]
        [TestCase(2, ExpectedResult = true)]
        public bool MaintainList_All_Test(int tpGameType)
        {
            _tpMaintainList.Add(new TpMaintain() { TpId = 1, TpGameType = 0 });
            return _gameBLL.IsMaintain(_tpMaintainList, new Dictionary<string, string>()
            {
                { "tpGameType",tpGameType.ToString() }
            }, out var tpMaintain);
        }

        [TestCase(1, ExpectedResult = true)]
        [TestCase(2, ExpectedResult = true)]
        [TestCase(3, ExpectedResult = false)]
        public bool TpGameType_MaintainList_Test(int tpGameType)
        {
            _tpMaintainList.Add(new TpMaintain() { TpId = 1, TpGameType = 2 });
            return _gameBLL.IsMaintain(_tpMaintainList, new Dictionary<string, string>()
            {
                { "tpGameType",tpGameType.ToString() }
            }, out var tpMaintain);
        }
    }
}