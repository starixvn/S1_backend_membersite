using Dapper;
using MemberSite.Service.Model;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MemberSite.Service.DAL
{
    public class GameCasinoService
    {
        private readonly IConfiguration configuration;
        public GameCasinoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public List<GameCasinoListData> GetGameCasinoList()
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            int IsStage = configuration.GetValue<int>("IsStage");
            string SPName = "Member_GameCasinoList_Get";
            if (IsStage == 1)
            {
                SPName = "Member_GameCasinoListStag_Get";
            }
            return conn.Query<GameCasinoListData>(SPName, null, commandType: CommandType.StoredProcedure).ToList();
        }
        public List<GameCasinoListData> GetGameCasinoListByDevice(int deviceType)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            int IsStage = configuration.GetValue<int>("IsStage");
            string SPName = "Member_GameCasinoListByDevice_Get";
            if (IsStage == 1)
            {
                SPName = "Member_GameCasinoListByDeviceStag_Get";
            }
            return conn.Query<GameCasinoListData>(SPName, new { DeviceType = deviceType }, commandType: CommandType.StoredProcedure).ToList();
        }
    }
}
