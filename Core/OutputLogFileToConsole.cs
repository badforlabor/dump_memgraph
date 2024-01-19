/**
 * Auth :   liubo
 * Date :   2023-01-17 15:21:34
 * Comment: 输出日志文件到控制台
 *      解决jenkins无法显示unity日志的问题！
 */
using System;
using System.IO;
using System.Threading;

namespace Core
{
    public class OutputLogFileToConsole
    {
        public static OutputLogFileToConsole StartWatch(string logFilePath)
        {
            var w = new OutputLogFileToConsole(logFilePath);
            w.StartWatch();
            return w;
        }
        public void StopWatch(bool dumpLog)
        {
            EndWatchLogFile();
            if (dumpLog)
            {
                // 如果上一次，没有成功，那么打印最后100条日志
                if (!readFileSucc)
                {
                    try
                    {
                        var file = File.Open(logFile, FileMode.Open);
                        if (file != null)
                        {
                            var bufferCnt = 1024;
                            var buffer = new byte[bufferCnt];

                            while (true)
                            {
                                var cnt = file.Read(buffer, 0, bufferCnt);
                                if (cnt > 0)
                                {
                                    var str = System.Text.Encoding.Default.GetString(buffer, 0, cnt);
                                    Console.Write(str);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            Console.Write("\n");
                            file.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        
                    }
                }
            }
        }
        
        
        private OutputLogFileToConsole(string logFilePath)
        {
            logFile = logFilePath;
        }

        void StartWatch()
        {
            WatchLogFile(logFile);
        }

        Thread WatchLogFile(string logFilePath)
        {
            logFile = Path.GetFullPath(logFilePath);
            
            File.Delete(logFile);
            bStop = false;
            readFileSucc = false;
            Logger.Log($"[watcher] start. file={logFile}");
            Thread t = new Thread(this.ThreadFunc);
            t.Start();
            return t;
        }
        void EndWatchLogFile()
        {
            bStop = true;
            int cnt = 10;
            while (cnt > 0)
            {
                if (bQuit)
                {
                    break;
                }
                
                Thread.Sleep(100);
                cnt--;
            }

            if (!bQuit)
            {
                Console.Error.Write($"线程没有终止！ logFile={logFile}");
            }
        }

        private bool bStop = false;
        private bool bQuit = false;
        private string logFile = "";
        private bool readFileSucc;
        void ThreadFunc()
        {
            Console.WriteLine("Child thread starts");

            bQuit = false;
            
            FileStream file = null;
            long offset = 0;
            var bufferCnt = 1024;
            var buffer = new byte[bufferCnt];
            int showErrCnt = 10;
            while (!bStop)
            {
                Thread.Sleep(10);
                if (!File.Exists(logFile))
                {
                    Logger.Log($"[watcher] not find file, retry. file={logFile}");
                    Thread.Sleep(100);
                    continue;
                }

                if (file == null)
                {
                    // var access = new FileAccess[] {FileAccess.Read, FileAccess.Write, FileAccess.ReadWrite};
                    // var share = new FileShare[]
                    // {
                    //     FileShare.None, FileShare.Read, FileShare.Write, FileShare.ReadWrite, FileShare.Delete,
                    //     FileShare.Inheritable
                    // };
                    var access = new FileAccess[] {FileAccess.Read, FileAccess.ReadWrite};
                    var share = new FileShare[]
                    {
                        FileShare.None, FileShare.Read,FileShare.ReadWrite
                    };


                    foreach (var a in access)
                    {
                        if (file != null)
                        {
                            break;
                        }

                        foreach (var s in share)
                        {
                            try
                            {
                                file = File.Open(logFile, FileMode.Open, a, s);
                                if (file != null)
                                {
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                if (showErrCnt >= 0)
                                {
                                    Logger.Log($"[watcher] open file failed, retry. file={e.Message}");
                                    showErrCnt--;
                                }
                            }
                            
                        }
                    }
                }

                if (file == null)
                {
                    Thread.Sleep(100);
                    if (showErrCnt > 0)
                    {
                        Logger.Log($"[watcher] 2 not find file, retry. file={logFile}");
                        showErrCnt--;
                    }
                    continue;
                }

                // file.Seek(offset, SeekOrigin.Begin);
                try
                {
                    readFileSucc = true;
                    var cnt = file.Read(buffer, 0, bufferCnt);
                    if (cnt > 0)
                    {
                        offset += cnt;
                        
                        var str = System.Text.Encoding.Default.GetString(buffer, 0, cnt);
                        Console.Write(str);
                    }
                }
                catch (Exception e)
                {
                    
                }
            }

            if (file != null)
            {
                file.Close();
            }

            bQuit = true;
        }
    }
}