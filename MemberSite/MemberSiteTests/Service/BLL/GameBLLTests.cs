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
                new TpMaintain() { TpId = 2, TpGameType = 2 },
                new TpMaintain() { TpId = 3, TpGameType = 3 },
            };
        }

        [TestCase(1,1, ExpectedResult = true)]
        [TestCase(1,2, ExpectedResult = false)]
        public bool Maintain_Test(int tpId, int tpGameType)
        {
            return _gameBLL.IsMaintain(_tpMaintainList, tpId, tpGameType.ToString(), out var _);
        }

        [TestCase(1, 1, ExpectedResult = false)]
        [TestCase(1, 2, ExpectedResult = false)]
        public bool MaintainList_Null_Test(int tpId, int tpGameType)
        {
            return _gameBLL.IsMaintain(null, tpId, tpGameType.ToString(), out var _);
        }

        [TestCase(1,ExpectedResult = false)]
        public bool TpGameType_Null_Test(int tpId)
        {
            return _gameBLL.IsMaintain(_tpMaintainList, tpId, null, out var _);
        }
    
        [TestCase(4,1, ExpectedResult = true)]
        [TestCase(4,2, ExpectedResult = true)]
        public bool MaintainList_All_Test(int tpId, int tpGameType)
        {
            _tpMaintainList.Add(new TpMaintain() { TpId = 4, TpGameType = 0 });
            return _gameBLL.IsMaintain(_tpMaintainList, tpId, tpGameType.ToString(), out var _);
        }

        [TestCase(1,1, ExpectedResult = true)]
        [TestCase(2,2, ExpectedResult = true)]
        [TestCase(2,3, ExpectedResult = false)]
        public bool TpGameType_MaintainList_Test(int tpId, int tpGameType) 
        { 
            _tpMaintainList.Add(new TpMaintain() { TpId = 4, TpGameType = 0 });
            return _gameBLL.IsMaintain(_tpMaintainList, tpId, tpGameType.ToString(), out _);
        }
    }
}