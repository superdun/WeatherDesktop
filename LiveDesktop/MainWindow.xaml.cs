using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LiveDesktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        private static readonly string FileFolder = "Images";
        private static readonly string ImgFolder = System.IO.Path.Combine(Directory.GetCurrentDirectory(), FileFolder);
        private int currentIndex = 0;
        private DispatcherTimer playTimer;
        private List<BitmapImage> imageFiles = new List<BitmapImage>();
        private int playTimeSpan = 90;
        private int checkTimeSpan = 10000;
        public MainWindow()
        {
            InitializeComponent();
            playImages();
        }
        private void Play_Timer_Tick(object sender, EventArgs e)
        {
            ShowNextImage();
        }


        private void playImages()
        {
            playTimer = new DispatcherTimer();
            playTimer.Interval = TimeSpan.FromMilliseconds(playTimeSpan); // 设置每张图片显示的时间间隔（这里设置为2秒）
            playTimer.Tick += Play_Timer_Tick;
            playTimer.Start();
        }

        private void ShowNextImage()
        {
            
            if (currentIndex < imageFiles.Count())
            {
                BitmapImage bitmap = imageFiles[currentIndex];
                imageControl.Source = bitmap;
                currentIndex++;
            }
            else
            {
                imageFiles = GetExsitingFiles();
                if (imageFiles.Count()==0)
                {
                    playTimer.Interval = TimeSpan.FromMilliseconds(checkTimeSpan);
                }
                else
                {
                   playTimer.Interval = TimeSpan.FromMilliseconds(playTimeSpan);
                }
                currentIndex = 0; // 重置索引以重新播放动画
            }
        }
        private static List<BitmapImage> GetExsitingFiles()
        {
            List<BitmapImage> result = new List<BitmapImage>();
            if (Directory.Exists(ImgFolder))
            {
                var files = Directory.GetFiles(ImgFolder).ToList(); // 获取 "imgs" 文件夹下所有文件的路径
                files?.Sort();
                foreach (string file in files)
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        bitmapImage.BeginInit();
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.StreamSource = fileStream;
                        bitmapImage.EndInit();
                    }

                    result.Add(bitmapImage); // 获取文件名并添加到列表中
                }

            }
            else
            {
                Directory.CreateDirectory(ImgFolder);

            }

            return result;
        }
    }
}
