using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WeatherDesktop
{
    internal class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
        static async Task Main(string[] args)
        {
            //string xmlUrl = @"http://img.nsmc.org.cn/PORTAL/NSMC/XML/FY4A/FY4A_AGRI_IMG_DISK_MTCC_NOM.xml";
            string xmlUrl = @"http://img.nsmc.org.cn/PORTAL/NSMC/XML/FY4B/FY4B_AGRI_IMG_DISK_GCLR_NOM.xml";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage xmlResponse = await client.GetAsync(xmlUrl);
                if (xmlResponse.IsSuccessStatusCode)
                {
                    string xmlString = await xmlResponse.Content.ReadAsStringAsync();
                    var imageUrl = XDocument.Parse(xmlString).Descendants("image").First().Attribute("url").Value;
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        Console.WriteLine("Can not get image url");
                        return;
                    }
                    HttpResponseMessage imgResponse = await client.GetAsync(imageUrl);
                    if (imgResponse.IsSuccessStatusCode)
                    {
                        using (Stream stream = await imgResponse.Content.ReadAsStreamAsync())
                        {
                            var path = MakeImage(stream);
                            SystemParametersInfo(0x0014, 0, path, 0x01 | 0x02);
                        }
                        Console.WriteLine("Image downloaded successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"Error: {imgResponse.StatusCode}");
                    }
                }
                else
                {
                    Console.WriteLine($"Error: {xmlResponse.StatusCode}");
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
            Image originalImage = Image.FromStream(stream); 
            var resizedImg = ResizeImage(originalImage, 1245, 1350);
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
