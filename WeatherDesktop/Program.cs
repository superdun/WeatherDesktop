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
using System.Diagnostics;
using System.Drawing.Imaging;

namespace WeatherDesktop
{
    internal class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
        private static readonly int MaxImageCount = 130;
        private static readonly string FileFolder = "Images";
        private static readonly string TmpFileFolder = "TmpImages";
        private static readonly string CurrentFolder = Directory.GetCurrentDirectory();
        private static readonly string ImgFolder = Path.Combine(CurrentFolder, FileFolder);
        private static readonly string TmpImgFolder = Path.Combine(CurrentFolder, TmpFileFolder);
        private static readonly string ffmepgPath = Path.Combine(CurrentFolder, "ffmpeg.exe");
        private static readonly string videoPath = Path.Combine(CurrentFolder, "video.mp4");
        private static readonly string DesktopExePath = Path.Combine(CurrentFolder, "LiveDesktop.exe");
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
                    var exsitingFiles = GetExsitingFiles(ImgFolder);
                    var requiredFileUrls = srcImageUrlList.Where(x => !exsitingFiles.Exists((y => x.ToUpper().Contains(y.ToUpper())))).Take(Math.Min(srcImageUrlList.Count(), MaxImageCount)).ToList();
                    var downloadTasks = new List<Task>();
                    foreach (var requiredFileUrl in requiredFileUrls)
                    {

                        if (string.IsNullOrEmpty(requiredFileUrl))
                        {
                            Console.WriteLine("Can not get image url");
                            return;
                        }
                        //downloadTasks.Add(DownloadAndSaveImageAsync(client, requiredFileUrl));
                        await  DownloadAndSaveImageAsync(client, requiredFileUrl);
                    }
                    //await Task.WhenAll(downloadTasks);
                    exsitingFiles = GetExsitingFiles(ImgFolder);
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
                    if (requiredFileUrls.Count() > 0)
                    {
                        KillDesktopProcess();
                        MakeVideo();
                        StartDesktopProcess();
                    }
                }
                else
                {
                    Console.WriteLine($"Error: {xmlResponse.StatusCode}");
                }
            }
        }
        private static void KillDesktopProcess()
        {
            // 按进程名称查找要终止的进程
            Process[] processes = Process.GetProcessesByName("LiveDesktop");

            // 遍历找到的进程并终止它们
            foreach (Process process in processes)
            {
                process.Kill();
                Console.WriteLine("Process {0} has been terminated.", process.ProcessName);
            }
        }
        private static void StartDesktopProcess()
        {
            Process.Start(DesktopExePath);
        }
        private static async Task DownloadAndSaveImageAsync(HttpClient client, string imageUrl)
        {
            HttpResponseMessage imgResponse = await client.GetAsync(imageUrl);
            if (imgResponse.IsSuccessStatusCode)
            {
                using (Stream stream = await imgResponse.Content.ReadAsStreamAsync())
                {
                    var path = MakeImage(stream, Path.GetFileName(imageUrl));
                    // Consider re-adding SystemParametersInfo call here if needed
                }
                Console.WriteLine("Image downloaded successfully.");
            }
            else
            {
                Console.WriteLine($"Error downloading {imageUrl}: {imgResponse.StatusCode}");
            }
        }
        private static void MakeVideo()
        {

            // 检查目标文件夹是否存在，如果不存在，则创建
            if (!Directory.Exists(TmpImgFolder))
            {
                Directory.CreateDirectory(TmpImgFolder);
            }
            var files = GetExsitingFiles(ImgFolder);
            try
            {
                

                int fileIndex = 1; // 用于文件重命名的序号
                foreach (string file in files)
                {
                    string fullFileName = Path.Combine(ImgFolder, Path.GetFileName(file));
                    // 构建新的文件名，基于序号
                    string newFileName = $"{fileIndex}.jpg";
                    // 构建目标文件的完整路径
                    string destFile = Path.Combine(TmpImgFolder, newFileName);

                    // 复制文件
                    File.Copy(fullFileName, destFile, true); // true 表示如果文件存在，则覆盖

                    fileIndex++; // 增加文件序号
                }
                if (File.Exists(videoPath))
                {
                    File.Delete(videoPath);
                }
                string imageFiles = Path.Combine(TmpImgFolder, "%d.jpg");
                // FFmpeg命令，-framerate是帧率，-i是输入文件的格式
                string arguments = $"-framerate 12 -i \"{imageFiles}\" -pix_fmt yuv420p \"{videoPath}\"";

                // 创建一个ProcessStartInfo对象
                ProcessStartInfo startInfo = new ProcessStartInfo(ffmepgPath, arguments)
                {
                    CreateNoWindow = true, // 不创建窗口
                    UseShellExecute = false, // 不使用shell启动进程
                    RedirectStandardError = true // 重定向错误流
                };

                // 创建并启动进程
                using (var process = Process.Start(startInfo))
                {
                    // 读取错误流的内容（FFmpeg的输出大多数情况下是写入到错误流中的）
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit(); // 等待进程退出

                    // 处理FFmpeg的输出（错误信息）
                    Console.WriteLine(stderr);
                }

                Console.WriteLine("视频已生成");
            }
            finally
            {
                var pathes = GetExsitingFiles(TmpImgFolder).Select(f => Path.Combine(TmpImgFolder, f));
                foreach (var path in pathes)
                {
                    File.Delete(path);
                }
               
            }

        }
        private static Image ResizeImage(Image originalImage, int width, int height)
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
        static string MakeImage(Stream stream, string fileName)
        {
            // 读取原始图片
            using (Image originalImage = Image.FromStream(stream))
            {
                var resizedImg = ResizeImage(originalImage, 1245, 1350);
                //var resizedImg = ResizeImage(originalImage, 1350, 1350);
                // 创建一个圆形遮罩层
                using (Bitmap maskedBitmap = new Bitmap(2560, 1440))
                {
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
                    var tempFilaname = Path.Combine(ImgFolder, $"{Path.GetFileNameWithoutExtension(fileName)}.jpg");
                    // 保存合成后的圆形图片
                    maskedBitmap.Save(tempFilaname,ImageFormat.Jpeg);

                    return tempFilaname;
                }
            }
        }

        private static List<string> GetExsitingFiles(string folder)
        {
            List<string> result = new List<string>();
            if (Directory.Exists(folder))
            {
                string[] files = Directory.GetFiles(folder); // 获取 "imgs" 文件夹下所有文件的路径

                foreach (string file in files)
                {
                    result.Add(Path.GetFileName(file)); // 获取文件名并添加到列表中
                }

            }
            else
            {
                Directory.CreateDirectory(folder);

            }
            result.Sort();
            return result;
        }
    }
}
