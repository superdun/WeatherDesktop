using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Start
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string executablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"WeatherDesktop.exe");
            string command = $"/Create  /TN WeatherDesktop /TR \"{executablePath}\" /sc minute /mo 15 /st 00:01";
            Process process = new Process();
            process.StartInfo.FileName = "schtasks";
            process.StartInfo.Arguments = command;
            process.Start();
            Console.WriteLine("设置定时任务成功，每15分钟更新一次....");
            Console.ReadLine();
        }
    }
}
