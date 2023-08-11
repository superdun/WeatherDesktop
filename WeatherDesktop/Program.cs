using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace WeatherDesktop
{
    internal class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
        static void Main(string[] args)
        {
            string imageUrl = "https://img.nsmc.org.cn/CLOUDIMAGE/FY4A/MTCC/FY4A_DISK.JPG"; 

            using (WebClient client = new WebClient())
            {
                using (Stream stream = client.OpenRead(imageUrl))
                {
                    var path = MakeImage(stream);
                    SystemParametersInfo(0x0014, 0, path, 0x01 | 0x02);
                }
            }
        }
        public static Image ResizeImage(Image originalImage, int width, int height)
        {
     
            Bitmap resizedBitmap = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(resizedBitmap))
            {

                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(originalImage, 0, 0, width, height);
            }

            return resizedBitmap;
        }
        static string MakeImage(Stream stream)
        {
            // 读取原始图片
            Image originalImage = Image.FromStream(stream); // 替换为你的图片路径
            var resizedImg = ResizeImage(originalImage, 1350, 1350);
            // 创建一个圆形遮罩层
            Bitmap maskedBitmap = new Bitmap(2560, 1440);
            using (Graphics graphics = Graphics.FromImage(maskedBitmap))
            {
                graphics.Clear(Color.Black);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                GraphicsPath path = new GraphicsPath();
                var startX = (maskedBitmap.Width - resizedImg.Width) / 2;
                path.AddEllipse(startX, 0, resizedImg.Width, resizedImg.Height);
                graphics.SetClip(path);
                graphics.DrawImage(resizedImg, startX,0);
            }

            string tempDirectory = Path.GetTempPath();
            var tempFilaname = Path.Combine(tempDirectory, "weather.jpg");
            // 保存合成后的圆形图片
            maskedBitmap.Save(tempFilaname);

            return tempFilaname;
        }
    }
}
