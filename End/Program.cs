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
            Console.OutputEncoding = Encoding.UTF8;
            Process process = new Process();
            process.StartInfo.FileName = "schtasks";
            process.StartInfo.Arguments = command;
            process.Start();
            Console.WriteLine("删除定时任务成功");
            Console.ReadLine();
        }
    }
}
