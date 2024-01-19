/**
 * Auth :   liubo
 * Date :   2023-02-06 16:47:43
 * Comment: Http工具
 */

namespace Core;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

public class HttpHelper
{
    static void SplitUrl(string url, out string host, out string other)
    {
        var arr = url.Split("//");
        if (arr.Length != 2)
        {
            throw new Exception($"url是非法格式:{url}");
        }

        var a1 = arr[0];
        var a2 = arr[1];
        var idx = a2.IndexOf("/");
        if (idx >= 0)
        {
            host = a2.Substring(0, idx);
            other = a2.Substring(idx + 1);
        }
        else
        {
            host = a2;
            other = "";
        }

        host = a1 + "//" + host;
    }

    /// <summary>
    /// 上传某个文件夹所有内容到host。
    /// host=http://172.19.146.19:12380
    /// </summary>
    /// <param name="folder"></param>
    /// <param name="host"></param>
    public static void UploadFolder(string url, string localFolder, string remoteFolder1)
    {
        SplitUrl(url, out string host, out string other);
        var remoteFolder = string.IsNullOrEmpty(other) ? "" : $"{other}/" + remoteFolder1;
        
        if (host.EndsWith("/") || remoteFolder.StartsWith("/") || remoteFolder1.StartsWith("/"))
        {
            throw new Exception($"非法地址:{host}");
        }

        if (!Directory.Exists(localFolder))
        {
            throw new Exception($"没有文件夹：{localFolder}");
        }

        var files = Directory.GetFiles(localFolder, "*", SearchOption.AllDirectories);

        HashSet<string> processed = new HashSet<string>();
        
        foreach (var f in files)
        {
            var relativePath = f.Substring(localFolder.Length + 1);
            var remoteFile = remoteFolder + "/" + relativePath.Replace('\\', '/');
            if (remoteFile.StartsWith("/"))
            {
                throw new Exception($"非法地址:{remoteFile}");
            }
            
            // 一个文件夹，只上传一个文件！
            var dir = Path.GetDirectoryName(relativePath);
            if (false && processed.Contains(dir))
            {
                continue;
            }
            processed.Add(dir);

            using (var fs = File.Open(f, FileMode.Open))
            {
                var t = HttpUploadFile(host + "/?upload", fs, "dirfile", $"{remoteFile}");
                if (t == null)
                {
                    Logger.Log($"upload failed. remoteFile={remoteFile}");
                }
                else
                {
                    t?.Wait();
                    Logger.Log($"upload succ. remoteFile={remoteFile}");
                }
            }
        }
        Logger.Log($"upload done");
    }

    public static void UploadUTF8(string host, string msg, string remoteFile)
    {
        var buff  = Encoding.UTF8.GetBytes(msg);
        UploadData(host, buff, remoteFile);
    }
    public static void UploadAnsi(string host, string msg, string remoteFile)
    {
        var buff  = Encoding.ASCII.GetBytes(msg);
        UploadData(host, buff, remoteFile);
    }

    public static void UploadData(string url, byte[] buff, string remoteFile1)
    {
        SplitUrl(url, out string host, out string other);
        var remoteFile = string.IsNullOrEmpty(other) ? "" : $"{other}/" + remoteFile1;
        
        if (host.EndsWith("/") || remoteFile.StartsWith("/") || remoteFile1.StartsWith("/"))
        {
            throw new Exception($"非法地址:{host}");
        }
        
        using (var fs = new MemoryStream(buff))
        {
            var t = HttpUploadFile(host + "/?upload", fs, "dirfile", $"{remoteFile}");
            if (t == null)
            {
                Logger.Log($"upload failed. remoteFile={remoteFile}");
            }
            else
            {
                t?.Wait();
                Logger.Log($"upload succ. remoteFile={remoteFile}");
            }
        }
        
        Logger.Log($"upload done");
    }
    public static void UploadFile(string url, string src, string remoteFile1)
    {
        SplitUrl(url, out string host, out string other);
        var remoteFile = string.IsNullOrEmpty(other) ? "" : $"{other}/" + remoteFile1;
        
        if (host.EndsWith("/") || remoteFile.StartsWith("/") || remoteFile1.StartsWith("/"))
        {
            throw new Exception($"非法地址:{host}");
        }
        
        using (var fs = File.Open(src, FileMode.Open))
        {
            var t = HttpUploadFile(host + "/?upload", fs, "dirfile", $"{remoteFile}");
            if (t == null)
            {
                Logger.Log($"upload failed. remoteFile={remoteFile}");
            }
            else
            {
                t?.Wait();
                Logger.Log($"upload succ. remoteFile={remoteFile}");
            }
        }
        
        Logger.Log($"upload done");
    }

    public static async Task<string> HttpUploadFile(string actionUrl, Stream paramFileStream, string mode,
        string remote_file_path)
    {
        HttpContent fileStreamContent = new StreamContent(paramFileStream);
        // using (var handler = new HttpClientHandler { Credentials = new NetworkCredential("liubo", "123") })
        using (var client = new HttpClient())
        using (var formData = new MultipartFormDataContent())
        {
            var byteArray = Encoding.UTF8.GetBytes("liubo:123");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            // client.DefaultRequestHeaders.Add("Basic", Convert.ToBase64String(byteArray));
            // var encodedAuthString = Convert.ToBase64String(Encoding.ASCII.GetBytes("liubo:123"));
            // client.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedAuthString}");

            formData.Add(fileStreamContent, mode, remote_file_path);

            var response = await client.PostAsync(actionUrl, formData);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
    
    public static void FtpUpload(string ftpHost, string ftpUsername, string ftpPassword, string localFile, string remoteFile)
    {
        if (!File.Exists(localFile))
        {
            throw new Exception($"Not Exist File:{localFile}");
        }

        if (!ftpHost.StartsWith("ftp://"))
        {
            throw new Exception($"Error Host:{ftpHost}");
        }

        remoteFile = remoteFile.Replace('\\', '/');

        if(remoteFile.Contains("/"))
        {
            var folders = remoteFile.Split('/');
            int idx = 1;
            while (idx < folders.Length)
            {
                var folder = Utils.JoinString(folders, "/", idx);
                var fullUrl = $"{ftpHost}/{folder}";
                if (!FtpDirectoryIsExist(fullUrl, ftpUsername, ftpPassword))
                {
                    WebRequest request = WebRequest.Create(fullUrl);
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                    using (var resp = (FtpWebResponse) request.GetResponse())
                    {
                        Console.WriteLine(resp.StatusCode);
                    }
                }

                idx++;
            }
        }

        using (var client = new WebClient())
        {
            client.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
            client.UploadFile($"{ftpHost}/{remoteFile}", WebRequestMethods.Ftp.UploadFile, localFile);
            Logger.Log($"上传文件成功:{ftpHost}/{remoteFile}");
        }
    }
    static bool FtpDirectoryIsExist(string fullUrl, string ftpUsername, string ftpPassword)
    {
        try
        {
            WebRequest request = WebRequest.Create(fullUrl);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
            using (var resp = (FtpWebResponse) request.GetResponse())
            {
                // Console.WriteLine(resp.StatusCode);
            }
            return true;
        }
        catch(WebException ex)
        {
            return false;
        }
    }

    public static void PostJson(string url, string json)
    {
        if (url.StartsWith("http") || url.StartsWith("https"))
        {
            
        }
        else
        {
            throw new Exception("非法参数！");
        }

        var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.Method = "POST";

        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
        {
            // json = "{\"user\":\"test\"," +
            //               "\"password\":\"bla\"}";

            streamWriter.Write(json);
        }

        var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
        using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
        {
            var result = streamReader.ReadToEnd();
        }
    }
    public static void PostKim(string url, string msg)
    {
        /*
         {
            "msgtype": "text",
             "text" : {
                "content": "123<@=username(liubo11)=>456"
            }
        }
         */
        
        // var msg = "123<@=username(liubo11)=>456";
        HttpHelper.PostJson(url, "{\"msgtype\":\"text\", \"text\":{\"content\": \""+msg+"\"}}");
    }
}