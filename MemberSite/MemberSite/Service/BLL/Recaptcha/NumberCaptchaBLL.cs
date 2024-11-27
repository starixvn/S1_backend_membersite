using System.Drawing.Imaging;
using System.Drawing;
using MemberSite.Service.Library;
using MemberSite.Service.Model;

namespace MemberSite.Service.BLL;

public class NumberCaptchaBLL
{

    private readonly ICacheHelper _redisHelper;
    private readonly ILogger<NumberCaptchaBLL> _logger;
    private readonly string _numberCaptchaPartten = string.Empty;
    private readonly int _expiredMins = 0;
    private readonly int _length = 0;
    private readonly NumberCaptchaSetting _recaptchaBaseSettingConfig;

    public NumberCaptchaBLL(IServiceProvider provider)
    {
        _redisHelper = provider.GetRequiredService<ICacheHelper>();
        _logger = provider.GetRequiredService<ILogger<NumberCaptchaBLL>>();
        var configBll = provider.GetRequiredService<ConfigBLL>();
        _recaptchaBaseSettingConfig = configBll.GetConfigValue<NumberCaptchaSetting>("NumberCaptchaSetting");

        _numberCaptchaPartten = _recaptchaBaseSettingConfig.RedisKeyPrefix;
        _expiredMins = _recaptchaBaseSettingConfig.ExpiredMins;
        _length = _recaptchaBaseSettingConfig.Length;
    }

    public bool GenPicture(string ip, string action, out byte[] picture, out int seconds)
    {
        var captchaCode = CreateRandomCode(_length);
        picture = GenerateCaptchaImageComplex(captchaCode);
        var key = $"{_numberCaptchaPartten}:{action}:{ip}";
        seconds = _expiredMins * 60;
        if (_redisHelper.KeyExists(key))
        {
            _logger.LogInformation($"ReSet CaptchaCode. RedisKey={key}. captchaCode={captchaCode}");
            return _redisHelper.StringSet(key, captchaCode, TimeSpan.FromMinutes(_expiredMins));
        }
        else
        {
            _logger.LogInformation($"Create CaptchaCode. RedisKey={key}. captchaCode={captchaCode}");
            return _redisHelper.Create(key, captchaCode, TimeSpan.FromMinutes(_expiredMins));
        }
    }

    public bool Verify(string ip, string captchaCode, string action)
    {
        var key = $"{_numberCaptchaPartten}:{action}:{ip}";
        if (!_redisHelper.TryGetValue(key, out string redisCode))
        {
            _logger.LogInformation($"NumberCaptcha Verify Fail. CaptchaCode={captchaCode}. RedisCode={redisCode}");
            return false;
        }
        if (captchaCode != redisCode)
        {
            _logger.LogInformation($"NumberCaptcha Verify Fail. CaptchaCode={captchaCode}. RedisCode={redisCode}");
            return false;
        }
        return true;
    }

    private string CreateRandomCode(int codeCount)
    {
        string randomCode;
        var upperLimit = Convert.ToInt32(string.Empty.PadLeft(codeCount, '9'));
        var rand = new Random(Guid.NewGuid().GetHashCode());
        do
        {
            randomCode = rand.Next(0, upperLimit).ToString().PadLeft(codeCount, '0');
        } while (CheckCodeIsHyphen(randomCode));
        return randomCode;
    }

    /// <summary>
    /// 檢查random是否為連續數字
    /// </summary>
    /// <param name="code">code</param>
    /// <returns>code</returns>
    private bool CheckCodeIsHyphen(string code)
    {
        var @char = code.ToCharArray();
        for (var i = 0; i < @char.Length - 1; i++)
        {
            if (@char[i].Equals(@char[i + 1]))
            {
                return true;
            }
        }
        return false;
    }

    private byte[] GenerateCaptchaImageComplex(string captchaCode)
    {
        // 圖片的寬度與高度
        int width = 180;
        int height = 60;

        // 創建一個新的 Bitmap
        var bmp = new Bitmap(width, height);
        var g = Graphics.FromImage(bmp);

        // 設置背景顏色，這裡使用淺灰色作為背景
        g.Clear(Color.LightGray);

        // 設置隨機數生成器
        Random random = new Random();

        // 畫一些隨機雜點（增加干擾）
        AddNoise(g, width, height, random);

        // 設置字體
        var font = new System.Drawing.Font("Arial", 28, FontStyle.Bold);
        var brush = new SolidBrush(Color.DarkBlue);

        // 隨機設定數字的起始位置與高度，讓數字之間不會完全對齊
        int xPosition = 20;
        for (int i = 0; i < captchaCode.Length; i++)
        {
            // 隨機調整每個字符的垂直位置（使數字不在同一行）
            int yPosition = random.Next(10, height - 40);  // 垂直位置在 10 到 height-40 之間

            // 隨機調整每個字符的大小（使每個數字略有不同）
            float size = random.Next(25, 30);
            var randomFont = new System.Drawing.Font("Arial", size, FontStyle.Bold);

            // 隨機調整每個字符的旋轉角度
            float angle = random.Next(-30, 30);

            // 隨機顏色（每個數字的顏色會不同）
            //var randomColor = Color.FromArgb(random.Next(100, 255), random.Next(100, 255), random.Next(100, 255));
            //var brush = new SolidBrush(randomColor);

            // 創建矩陣來旋轉文字
            var rotateMatrix = new System.Drawing.Drawing2D.Matrix();
            rotateMatrix.RotateAt(angle, new PointF(xPosition + 10, yPosition + 10));

            g.Transform = rotateMatrix;

            // 在圖片上繪製數字
            g.DrawString(captchaCode[i].ToString(), randomFont, brush, new PointF(xPosition, yPosition));

            // 更新 X 位置，讓數字之間的間距隨機變化
            xPosition += random.Next(30, 40); // 隨機增加數字之間的距離

            // 還原繪圖的變換
            g.ResetTransform();
        }

        // 將圖片轉換為字節數組並返回
        using (var ms = new MemoryStream())
        {
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }

    /// <summary>
    /// 增加隨機雜點或線條
    /// </summary>
    /// <param name="g"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="random"></param>
    private void AddNoise(Graphics g, int width, int height, Random random)
    {
        // 隨機畫點
        for (int i = 0; i < 100; i++)
        {
            int x = random.Next(0, width);
            int y = random.Next(0, height);
            g.FillEllipse(new SolidBrush(Color.FromArgb(random.Next(100, 255), random.Next(100, 255), random.Next(100, 255))), x, y, 2, 2);
        }

        // 隨機畫直線或曲線（增加干擾）
        for (int i = 0; i < 10; i++)
        {
            var pen = new Pen(Color.FromArgb(random.Next(100, 255), random.Next(100, 255), random.Next(100, 255)), 6);
            int startX = random.Next(0, width);
            int startY = random.Next(0, height);
            int endX = random.Next(0, width);
            int endY = random.Next(0, height);
            g.DrawLine(pen, startX, startY, endX, endY);
        }
    }
}
