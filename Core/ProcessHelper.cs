using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Core
{
    public class ProcessHelper
    {
        public const int ExitSucc = 0;
        
        public static int Start(string exePath, string workspace, params string[] args)
        {
            string strPathExe = exePath;
            var process = new System.Diagnostics.Process();
            process.EnableRaisingEvents = true;
            process.Exited += new EventHandler(myProcess_Exited);
            process.StartInfo.FileName = strPathExe;
            process.StartInfo.WorkingDirectory = workspace;
            process.StartInfo.Arguments = string.Join(" ", args);
            Logger.Debug($@"启动进程, exe=[{process.StartInfo.FileName}], StartTime=[{DateTime.Now}], workspace=[{process.StartInfo.WorkingDirectory}], args=[{process.StartInfo.Arguments}]");
            
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = false;  //true
            process.StartInfo.RedirectStandardOutput = true;  //true
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.StandardOutputEncoding = System.Text.UTF8Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = System.Text.UTF8Encoding.UTF8;
            process.ErrorDataReceived += build_ErrorDataReceived;
            process.OutputDataReceived += build_LogDataReceived;
            process.Start();
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            process.WaitForExit();
            Logger.Debug($"退出Code={process.ExitCode}");
            if (process.ExitCode != 0)
            {
                Logger.Error($"进程结束异常, exec=[{process.StartInfo.FileName}], ExitCode={process.ExitCode}");
            }

            return process.ExitCode;
        }
        static void build_LogDataReceived(object sender, DataReceivedEventArgs e)
        {
            Logger.Log(e.Data);
        } 
        static void build_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Logger.Error(e.Data);
        } 
        static void myProcess_Exited(object sender, System.EventArgs e)
        {
            var p = (Process) sender;
            Logger.Log($"exeFileName={p.StartInfo.FileName}, ExitCode={p.ExitCode}, ExitTime={p.ExitTime}");
        }

        public static Tuple<int, string> Start2(string exePath, string workspace, params string[] args)
        {
            string strPathExe = exePath;
            var process = new System.Diagnostics.Process();
            process.EnableRaisingEvents = true;
            process.StartInfo.FileName = strPathExe;
            process.StartInfo.WorkingDirectory = workspace;
            process.StartInfo.Arguments = string.Join(" ", args);
            Logger.Debug($@"启动进程, exe=[{process.StartInfo.FileName}], StartTime=[{DateTime.Now}], workspace=[{process.StartInfo.WorkingDirectory}], args=[{process.StartInfo.Arguments}]");
            
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = false;  //true
            process.StartInfo.RedirectStandardOutput = true;  //true
            process.StartInfo.RedirectStandardError = true;
            
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            int timeout = 5*1000;
            using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
            using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                };
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // if (process.WaitForExit(timeout) &&
                //     outputWaitHandle.WaitOne(timeout) &&
                //     errorWaitHandle.WaitOne(timeout))
                // {
                //     // Process completed. Check process.ExitCode here.
                // }
                // else
                // {
                //     // Timed out.
                // }
                process.WaitForExit();
            }
            
            if (process.ExitCode != 0)
            {
                Logger.Error($"进程结束异常, exec=[{process.StartInfo.FileName}], ExitCode={process.ExitCode}");
            }
            
            var str = output.ToString();

            return new Tuple<int, string>(process.ExitCode, str);
        }
        
        public static int Start3(string exePath, string workspace, params string[] args)
        {
            string strPathExe = exePath;
            var process = new System.Diagnostics.Process();
            process.EnableRaisingEvents = true;
            process.StartInfo.FileName = strPathExe;
            process.StartInfo.WorkingDirectory = workspace;
            process.StartInfo.Arguments = string.Join(" ", args);
            Logger.Debug($@"启动进程, exe=[{process.StartInfo.FileName}], StartTime=[{DateTime.Now}], workspace=[{process.StartInfo.WorkingDirectory}], args=[{process.StartInfo.Arguments}]");
            
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.RedirectStandardInput = false;  //true
            process.StartInfo.RedirectStandardOutput = false;  //true
            process.StartInfo.RedirectStandardError = false;
            
            process.Start();

            process.WaitForExit();
            
            if (process.ExitCode != 0)
            {
                Logger.Error($"进程结束异常, exec=[{process.StartInfo.FileName}], ExitCode={process.ExitCode}");
            }

            return process.ExitCode;
        }
        
        public static void KillProcess(string exeName)
        {
            bool bFind = false;
            var processList = System.Diagnostics.Process.GetProcesses();
            foreach (var process in processList)
            {
                // 忽略大小写
                if (process.ProcessName.ToLower() == exeName.ToLower())
                {
                    try
                    {
                        bFind = true;
                        Logger.Log($"kill process: {process.ProcessName}");
                        process.Kill();
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarnning($"kill process exception: {e.Message}");
                    }
                }
            }

            if (!bFind)
            {
                Logger.Log($"kill process, can't find process:{exeName}");
            }
        }
        public static void ShowProcessList()
        {
            Logger.Log($"process list start...");
            bool bFind = false;
            var processList = System.Diagnostics.Process.GetProcesses();
            foreach (var process in processList)
            {
                try
                {
                    Logger.Log($"process list name={process.ProcessName}, fullName={process.MainModule.FileName}");
                }
                catch (Exception e)
                {
                    Logger.Log($"process list name={process.ProcessName}");
                }
            }
            Logger.Log($"process list end...");
        }
        
    }
}