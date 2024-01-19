using System;
using System.IO;
using Newtonsoft.Json;


public class JsonUtils
{
    public static string ToBeautifulJson(object obj)
    {
        var d2 = JsonConvert.SerializeObject(obj, Formatting.Indented);
        return d2;
    }

    public static void SaveIfDirty(object obj, string fullPath)
    {
        var d2 = JsonConvert.SerializeObject(obj, Formatting.Indented);
        var d1 = "";
        try
        {
            d1 = File.ReadAllText(fullPath);
        }
        catch (Exception e)
        {
                
        }
        if (d1 != d2)
        {
            File.WriteAllText(fullPath, d2);
        }
    }
    public static T LoadJson<T>(string fullPath)
    {
        T info = default;

        if (!File.Exists(fullPath))
        {
            return info;
        }

        try 
        { 
            string d = File.ReadAllText(fullPath);
        
            info = JsonConvert.DeserializeObject<T>(d);
        }
        catch (Exception e)
        {
            throw e;
        }

        return info;
    }
}