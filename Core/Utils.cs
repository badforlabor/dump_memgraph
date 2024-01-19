/**
 * Auth :   liubo
 * Date :   2023-02-12 15:17:41
 * Comment: 常用的工具
 */

using System;

namespace Core
{
    public class Utils
    {
        public static string JoinString(string[] arr, string ch, int cnt)
        {
            var idx = 0;
            string ret = "";
            for (int i = 0; i < cnt; i++)
            {
                if (i > 0)
                {
                    ret += ch;
                }

                ret += arr[i];
            }

            return ret;
        }
        
        public static string ToHumanReadable(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = bytes / Math.Pow(1024, place);//Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString("F2") + suf[place];
        }
    }
}