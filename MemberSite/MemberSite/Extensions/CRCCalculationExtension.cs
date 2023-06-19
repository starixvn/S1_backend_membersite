using System.Text;

namespace MemberSite.Extensions;

public static class CRCCalculationExtension
{
    public static string CalculateCRC0xFFFF(string data)
    {
        var bytes = Encoding.ASCII.GetBytes(data);
        ushort crc = 0xFFFF;

        foreach (var b in bytes)
        {
            crc ^= (ushort)(b << 8);

            for (var i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) > 0)
                {
                    crc = (ushort)((crc << 1) ^ 0x1021);
                }
                else
                {
                    crc <<= 1;
                }
            }
        }

        return crc.ToString("X4");
    }
}