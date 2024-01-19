using System;

namespace Core
{
    public class Logger
    {
        public static void Debug(string str)
        {
            Console.WriteLine(str);
        }
        public static void Log(string str)
        {
            Console.WriteLine(str);
        }
        public static void LogWarnning(string str)
        {
            Console.WriteLine(str);
        }
        public static void Error(string str)
        {
            Console.Error.WriteLine(str);
        }
    }
}