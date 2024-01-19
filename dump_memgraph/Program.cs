using System.Text;
using System.Text.RegularExpressions;
using CommandLine;
using System.Collections;
using System.Diagnostics;
using System.Security.Cryptography;
using Core;

/*
 * 想法整理：自动dump出xcode mem graph中的内容
 *  1. heap -SortBySize xxx.memgraph > 1.txt
 *  2. 读取1.txt，取出前3的内容
 *  3. heap xxx.memgraph -addresses all |grep 前3的内容 > 2-x.txt
 *  4. 解析2-x.txt每行，取出内存地址$address，然后执行malloc_history xxx.memgraph $address
 *  5. 解析调用栈，计算md5，然后导出到csv表：内存占用值，md5值，调用栈信息
 * 
 */


/// <summary>
/// 设置pendinglist
/// </summary>

class Program
{
    class Options
    {
        [Option("file", Default = @"", Required = true,
            HelpText = "workspace example: 1.memgraph")]
        public string File { get; set; }

        [Option("test", Default = false, Required = false,
            HelpText = "test, bool")] 
        public bool Test { get; set; }
    }

    static void Main(string[] args)
    {
        var parser = new Parser(x =>
        {
            x.IgnoreUnknownArguments = true;
            x.CaseSensitive = false;
        });
        var options = parser.ParseArguments<Options>(args).WithParsed(Run);
        Console.WriteLine("Done...");
    }

    static void Test(string rootFolder)
    {
        ParseMallocHistory($"{rootFolder}", "day2-1.4.txt");
        
        var marks = Parse1($"{rootFolder}", "1.txt");
        foreach (var mark in marks)
        {
            var lists = Parse2($"{rootFolder}", "day2-1.6.txt", mark);
        }
    }

    static void Run(Options options)
    {
        if (!File.Exists(options.File))
        {
            throw new Exception($"无法找到文件:{options.File}");
        }

        var rootFolder = Path.GetDirectoryName(options.File);
        var fileName = Path.GetFileName(options.File);
        if (!Directory.Exists(rootFolder))
        {
            throw new Exception("无法找到文件夹！");
        }
        
        // 调试
        if(options.Test)
        {
            Test(rootFolder);
            return;
        }
        

        var tempFolder = "temp";
        var workspace = rootFolder;
        // heap -SortBySize xxx.memgraph > 1.txt
        var ret = ProcessHelper.Start("", workspace, "heap", "-SortBySize", fileName, ">", $"{tempFolder}/1.txt");
        Debug.Assert(ret == 0);

        var marks = Parse1($"{rootFolder}/{tempFolder}", "1.txt");
        
        // all命令
        ret = ProcessHelper.Start("", workspace, "heap", fileName, "-addresses", "all", ">", $"{tempFolder}/2.txt");
        Debug.Assert(ret == 0);
        int markIdx = 0;
        foreach(var mark in marks)
        {
            markIdx++;

            var historyList = new List<MallocHistoryOne>();
            var addressLit = Parse2($"{rootFolder}/{tempFolder}", "2.txt", mark);
            foreach (var address in addressLit)
            {
                ret = ProcessHelper.Start("", workspace, "malloc_history", fileName, address.Address, ">", $"{tempFolder}/3-temp-{markIdx}.txt");
                Debug.Assert(ret == 0);
                
                // 再次解析
                var oneHistory = ParseMallocHistory($"{rootFolder}/{tempFolder}", $"3-temp-{markIdx}.txt");
                historyList.Add(oneHistory);
            }
            
            // 整合
            var historyDict = new Dictionary<string, MallocHistoryOne>();
            foreach (var historyOne in historyList)
            {
                if (!historyDict.ContainsKey(historyOne.Md5))
                {
                    historyDict.Add(historyOne.Md5, historyOne);
                }

                var one = historyDict[historyOne.Md5];
                one.TotalSize += one.Size;
            }
            
            // 保存内容
            JsonUtils.SaveIfDirty(historyList, $"{rootFolder}/{tempFolder}/3-{markIdx}.list.txt");
            JsonUtils.SaveIfDirty(historyDict, $"{rootFolder}/{tempFolder}/3-{markIdx}.dict.txt");
        }
    }

    static List<string> Parse1(string folder, string file)
    {
        var lines = File.ReadAllLines($"{folder}/{file}");
        int contentLine = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line ==
                "   COUNT      BYTES       AVG   CLASS_NAME                                        TYPE    BINARY")
            {
                // 取前3行
                contentLine = i + 2;
                break;
            }
        }

        if (contentLine == -1)
        {
            throw new Exception("结果不对!");
        }

        /* 大概长这样
 1990110  413942880     208.0   MTLTextureDescriptorInternal                      ObjC    Metal
  403422  250823968     621.7   malloc in AnsiRealloc(void*, unsigned long, unsigned int)  C       TestNiagara
  227164  106509696     468.9   malloc in FMallocAnsi::Malloc(unsigned long, unsigned int)  C++     TestNiagara          
         */
        List<string> ret = new List<string>();
        for (int i = contentLine; i < contentLine + 3 && i < lines.Length; i++)
        {
            var line = lines[i];
            var list = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // 前3个
            var one = string.Join(' ', list, 3, list.Length - 3 - 2);
            ret.Add(one);
        }
        Debug.Assert(ret.Count > 0);
        File.WriteAllLines($"{folder}/{file}.r1.txt", ret);

        return ret;
    }

    class AddressInfo
    {
        public string Address;
        public string Mark;
        public int Size;
        public List<string> Origin;

        public void Parse()
        {
            Debug.Assert(Origin.Count == 3);
            Address = Origin[0].Trim(' ', ':');
            Mark = Origin[1];
            var sizeStr = Origin[2].Trim('(', ')');
            sizeStr = sizeStr.Substring(0, sizeStr.Length);
            Size = int.Parse(sizeStr);
            Debug.Assert(Size > 0);
        }
    }

    static List<AddressInfo> Parse2(string folder, string file, string mark)
    {
        List<AddressInfo> Ret = new List<AddressInfo>();
        var lines = File.ReadAllLines($"{folder}/{file}");
        foreach (var line in lines)
        {
            if (line.Contains(mark) && line.Contains("bytes"))
            {
                AddressInfo info = new AddressInfo();
                // 类似这种：0x11822c370: caulk::alloc::darwin_resource in (anonymous namespace)::EABLImpl::create(unsigned int, unsigned int, bool) (80 bytes)
                var list = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var one = string.Join(' ', list, 1, list.Length - 1 - 2);
                
                info.Origin = new List<string>();
                info.Origin.Add(list[0]);
                info.Origin.Add(one);
                info.Origin.Add(list[list.Length-2]);
                info.Parse();
                Ret.Add(info);
            }
        }

        return Ret;
    }

    class MallocHistoryOne
    {
        public string Mark;
        public List<string> Callstack;
        public List<string> OriginCallstack;
        public string Md5;
        public int Size = 0;
        public int TotalSize = 0;
    }

    static MallocHistoryOne ParseMallocHistory(string folder, string file)
    {
        MallocHistoryOne one = new MallocHistoryOne();

        var lines = File.ReadAllLines($"{folder}/{file}");
        Debug.Assert(lines.Length > 0);
        Debug.Assert(lines[0].Contains("malloc_history Report Version"));

        // 大概长这样：ALLOC 0x282e30e40-0x282e30eff [size=192]:  0x1b1c108d4 (dyld) start | 0x1b1c1266c (dyld) dyld4::prepare(dyld4::APIs&, dyld3::MachOAnalyzer const*) | 0x1b1c3d244 (dyld) dyld4::APIs::runAllInitializersForMain()        
        var callstackStr = lines[1];
        var idx1 = callstackStr.IndexOf("]:");
        Debug.Assert(idx1 >= 0 && idx1 < 100);

        var str1 = callstackStr.Substring(idx1 + 4);
        Debug.Assert(str1.StartsWith("0x"));

        one.Callstack = new List<string>();
        one.OriginCallstack = new List<string>();
        var callstackList = str1.Split(" | ");
        foreach (var callstack in callstackList)
        {
            var cs = callstack.Trim(' ');
            one.OriginCallstack.Add(cs);
            var list = cs.Split(' ');
            Debug.Assert(list.Length > 2);
            Debug.Assert(list[0].StartsWith("0x"));
            // Debug.Assert(list[1] == ("(dyld)"));
            var simplify = string.Join(' ', list, 2, list.Length - 2);
            one.Callstack.Add(simplify);
        }
        
        one.Md5 = CalculateMD5Hash(string.Join('\n', one.Callstack));
        
        return one;
    }
    
    static public string CalculateMD5Hash(string input)
    {
        // step 1, calculate MD5 hash from input
  
        MD5 md5 = System.Security.Cryptography.MD5.Create();

        byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);

        byte[] hash = md5.ComputeHash(inputBytes);

        // step 2, convert byte array to hex string

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < hash.Length; i++)
        {
            sb.Append(hash[i].ToString("X2"));
        }

        return sb.ToString();
    }
}