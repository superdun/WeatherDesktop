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
using System.Collections.Generic;
using System.Security.Policy;

namespace WeatherDesktop
{
    internal class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
        private static readonly int MaxImageCount = 96;
        private static readonly string FileFolder = "Images";
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
                    var srcImageUrlList = XDocument.Parse(xmlString).Descendants("image").Select(x => x.Attribute("url").Value).ToList();
                    var exsitingFiles = GetExsitingFiles();
                    var requiredFileUrls = srcImageUrlList.Where(x => !exsitingFiles.Exists((y => x.ToUpper().Contains(y.ToUpper())))).Take(Math.Min(srcImageUrlList.Count(), MaxImageCount)).ToList();
                    foreach (var requiredFileUrl in requiredFileUrls)
                    {

                        if (string.IsNullOrEmpty(requiredFileUrl))
                        {
                            Console.WriteLine("Can not get image url");
                            return;
                        }
                        HttpResponseMessage imgResponse = await client.GetAsync(requiredFileUrl);
                        if (imgResponse.IsSuccessStatusCode)
                        {
                            using (Stream stream = await imgResponse.Content.ReadAsStreamAsync())
                            {
                                var path = MakeImage(stream, Path.GetFileName(requiredFileUrl));
                                //SystemParametersInfo(0x0014, 0, path, 0x01 | 0x02);
                            }
                            Console.WriteLine("Image downloaded successfully.");
                        }
                        else
                        {
                            Console.WriteLine($"Error: {imgResponse.StatusCode}");
                        }
                    }
                    exsitingFiles = GetExsitingFiles();
                    var deletedCount = Math.Max((exsitingFiles.Count() - MaxImageCount), 0);
                    var deletedFiles = exsitingFiles.Take(deletedCount).ToList();
                    foreach (var deletedFile in deletedFiles)
                    {
                        var fullPath = Path.Combine(ImgFolder, deletedFile);
                        if (File.Exists(fullPath))
                        {
                            File.Delete(fullPath); // 删除文件

                            Console.WriteLine($"File deleted successfully.{fullPath}");
                        }
                        else
                        {
                            Console.WriteLine($"File does not exist.{fullPath}");
                        }
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
        static string MakeImage(Stream stream,string fileName)
        {
            // 读取原始图片
            Image originalImage = Image.FromStream(stream);
            var resizedImg = ResizeImage(originalImage, 1245, 1350);
            //var resizedImg = ResizeImage(originalImage, 1350, 1350);
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
                graphics.DrawImage(resizedImg, startX, 0);
            }
            var tempFilaname = Path.Combine(ImgFolder, fileName);
            // 保存合成后的圆形图片
            maskedBitmap.Save(tempFilaname);

            return tempFilaname;
        }
        private static readonly string ImgFolder = Path.Combine(Directory.GetCurrentDirectory(), FileFolder);
        private static List<string> GetExsitingFiles()
        {
            List<string> result = new List<string>();
            if (Directory.Exists(ImgFolder))
            {
                string[] files = Directory.GetFiles(ImgFolder); // 获取 "imgs" 文件夹下所有文件的路径

                foreach (string file in files)
                {
                    result.Add(Path.GetFileName(file)); // 获取文件名并添加到列表中
                }

            }
            else
            {
                Directory.CreateDirectory(ImgFolder);

            }
            result.Sort();
            return result;
        }
    }
}
