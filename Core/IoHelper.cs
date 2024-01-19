/**
 * Auth :   liubo
 * Date :   2023-01-10 17:09:31
 * Comment: IO相关的操作
 */

using System;
using System.Diagnostics;
using System.IO;
using Ionic.Zip;

namespace Core
{
    public class IoHelper
    {
        public static void CopyFolder(string src, string dst)
        {
            var fullpath = Path.GetFullPath(src);
            if (!Directory.Exists(fullpath))
            {
                Logger.LogWarnning($"无法找到文件夹：{src}");
                return;
            }

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(fullpath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(fullpath, dst));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(fullpath, "*.*",SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(fullpath, dst), true);
            }
            
        }

        public static void Copy(string src, string dst)
        {
            if (Directory.Exists(src))
            {
                CopyFolder(src, dst);
            }
            else
            {
                CopyFile(src, dst);
            }
        }

        public static void CopyFile(string src, string dst)
        {
            if (!File.Exists(src))
            {
                Logger.LogWarnning($"拷贝失败，没有此文件：{src}");
                return;
            }

            var folder = Path.GetDirectoryName(dst);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            File.Copy(src, dst, true);
        }

        public static bool HasFile(string filePath)
        {
            try
            {
                return File.Exists(filePath);
            }
            catch (Exception e)
            {
                Logger.LogWarnning($"权限错误？e={e.Message}");
            }

            return false;
        }
        public static bool HasFolder(string folderPath)
        {
            try
            {
                return Directory.Exists(folderPath);
            }
            catch (Exception e)
            {
                Logger.LogWarnning($"权限错误？e={e.Message}");
            }

            return false;
        }

        public static void DeleteFile(string filePath)
        {
            if (!HasFile(filePath))
            {
                return;
            }

            try
            {
                File.Delete(filePath);
            }
            catch (Exception e)
            {
                Logger.LogWarnning($"删除文件失败:{filePath}");       
            }
        }

        public static void DeleteFolder(string folderPath)
        {
            if (!HasFolder(folderPath))
            {
                return;
            }
            
            try
            {
                Directory.Delete(folderPath, true);
            }
            catch (Exception e)
            {
                Logger.LogWarnning($"删除文件夹失败:{folderPath}");       
            }
        }

        public static void Delete(string path)
        {
            if (Directory.Exists(path))
            {
                DeleteFolder(path);
            }
            else
            {
                DeleteFile(path);
            }
        }

        public static void ReplaceAllString(string file, string src, string target)
        {
            if (!HasFile(file))
            {
                Logger.LogWarnning($"没有此文件：{file}");
                return;
            }

            var str = File.ReadAllText(file);
            str = str.Replace(src, target);
            File.WriteAllText(file, str);
        }
        
        public static string GetLatestFolderName(string root)
        {
            string dir = "";
            DateTime lastTime = new DateTime(); 
            var directories = Directory.GetDirectories(root);
            if (directories != null && directories.Length > 0)
            {
                foreach (var it in directories)
                {
                    var info = new DirectoryInfo(it);
                    if (info.LastWriteTime > lastTime)
                    {
                        dir = it;
                        lastTime = info.LastWriteTime;
                    }
                }
            }

            var dirName = Path.GetFileName(dir);
            return dirName;
        }
        public static string GetLatestFile(string root, string pattern)
        {
            string file = "";
            DateTime lastTime = new DateTime(); 
            var files = Directory.GetFiles(root, pattern);
            if (files != null && files.Length > 0)
            {
                foreach (var it in files)
                {
                    var info = new FileInfo(it);
                    if (info.LastWriteTime > lastTime)
                    {
                        file = it;
                        lastTime = info.LastWriteTime;
                    }
                }
            }

            return file;
        }
        
        public static void MakeLastest(string fileOrFolder)
        {
            if (File.Exists(fileOrFolder))
            {
                var info = new FileInfo(fileOrFolder);
                info.LastWriteTime = DateTime.Now;
            }
            else if (Directory.Exists(fileOrFolder))
            {
                var info = new DirectoryInfo(fileOrFolder);
                info.LastWriteTime = DateTime.Now;;
            }
            else
            {
                Logger.LogWarnning($"Not Find:{fileOrFolder}");
            }
        }

        public static void ZipFolder(string folder, string zipFileName)
        {
            if (!Directory.Exists(folder))
            {
                Logger.LogWarnning($"no Folder:{folder}");
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(zipFileName));
            }
            catch (Exception e)
            {
                
            }

            using (ZipFile zip = new ZipFile())
            {
                zip.UseUnicodeAsNecessary= true;  // utf-8
                zip.AddDirectory(folder);
                // zip.AddDirectory(folder, Path.GetFileName(folder));
                // zip.Comment = "This zip was created at " + System.DateTime.Now.ToString("G") ; 
                zip.Save(zipFileName);
            }
        }
    }
}