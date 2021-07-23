using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MemberSite.Service.Library
{
    public class IBCDBLibrary
    {
        private readonly IConfiguration configuration;

        public IBCDBLibrary(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public string GetConnectionString(string ConnectionName)
        {
            var connection_str = configuration.GetConnectionString(ConnectionName);
            if (!string.IsNullOrEmpty(connection_str))
            {
                var SCSB = new MySqlConnectionStringBuilder(connection_str);
                SCSB.Password = Decrypt(SCSB.Password, "&％#@?,:*!~^.");
                return SCSB.ConnectionString;
            }
            else
            {
                return null;
            }
        }
        public string SetConnectionString(string ConnectionName)
        {
            var connection_str = configuration.GetConnectionString(ConnectionName);
            if (!string.IsNullOrEmpty(connection_str))
            {
                var SCSB = new MySqlConnectionStringBuilder(connection_str);
                SCSB.Password = Encrypt(SCSB.Password, "&％#@?,:*!~^.");
                return SCSB.ConnectionString;
            }
            else
            {
                return null;
            }
        }
        public string Decrypt(string strText, string sDecrKey)
        {
            var array = new byte[0];
            var rgbKey = new byte[8]
            {
                18,
                52,
                86,
                120,
                144,
                171,
                205,
                239
            };
            var array2 = new byte[strText.Length];
            array = Encoding.UTF8.GetBytes(sDecrKey);
            var dESCryptoServiceProvider = new DESCryptoServiceProvider();
            array2 = Convert.FromBase64String(strText);
            var memoryStream = new MemoryStream();
            var cryptoStream = new CryptoStream(memoryStream, dESCryptoServiceProvider.CreateDecryptor(rgbKey, array), CryptoStreamMode.Write);
            cryptoStream.Write(array2, 0, array2.Length);
            cryptoStream.FlushFinalBlock();
            var uTF = Encoding.UTF8;
            return uTF.GetString(memoryStream.ToArray());
        }
        public string Encrypt(string strText, string strEncrKey)
        {
            var array = new byte[0];
            var rgbKey = new byte[8]
            {
                18,
                52,
                86,
                120,
                144,
                171,
                205,
                239
            };
            array = Encoding.UTF8.GetBytes(strEncrKey);
            var dESCryptoServiceProvider = new DESCryptoServiceProvider();
            var bytes = Encoding.UTF8.GetBytes(strText);
            var memoryStream = new MemoryStream();
            var cryptoStream = new CryptoStream(memoryStream, dESCryptoServiceProvider.CreateEncryptor(rgbKey, array), CryptoStreamMode.Write);
            cryptoStream.Write(bytes, 0, bytes.Length);
            cryptoStream.FlushFinalBlock();
            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }
}
