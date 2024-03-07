using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace End
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string command = " /delete /tn \"WeatherDesktop\" /f";
            Process process = new Process();
            process.StartInfo.FileName = "schtasks";
            process.StartInfo.Arguments = command;
            process.Start();
            Console.WriteLine("SUCCESS,Press Enter to exit");
            Console.ReadLine();
            // 按进程名称查找要终止的进程
            Process[] processes = Process.GetProcessesByName("LiveDesktop");

            // 遍历找到的进程并终止它们
            foreach (Process p in processes)
            {
                process.Kill();
                Console.WriteLine("Process {0} has been terminated.", p.ProcessName);
            }
        }
    }
}
