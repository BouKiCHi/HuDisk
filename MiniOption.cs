using System;
using System.Collections.Generic;

namespace Disk
{
    class MiniOption {
    public List<string> Files;

    List<DefineData> DefineList;

    public List<OptionData> Result;

    public MiniOption()
    {
      Files = new List<string>();
      Result = new List<OptionData>();
      DefineList = new List<DefineData>();
    }

    public class DefineData {
      public int Type;
      public string Short;
      public string Long;
      public bool IsNeedArgument;
      public DefineData(int Type, string ShortOption, string LongOption, bool NeedArgument) {
        this.Type = Type;
        this.Short = ShortOption;
        this.Long = LongOption;
        this.IsNeedArgument = NeedArgument;
      }
    }

    public class OptionData {
      public string Key;
      public string Value;
      public int Type;
    }

    public void AddOptionDefines(DefineData[] opt) {
      DefineList.AddRange(opt);
    }

    int ParseIndex = 0;
    string ParseArgument = null;
    DefineData ParseOption = null;

    bool CheckLongOption(string OptionText) {
        string Key = OptionText.Substring(2);
        ParseArgument = null;
        ParseOption = null;
        foreach(DefineData o in DefineList) {
          if (o.Long != null && Key.StartsWith(o.Long)) {
            ParseOption = o;
            ParseArgument = Key.Substring(o.Long.Length);
            return true;
          }
        }
        Console.WriteLine("Invalid Option:{0}",OptionText);
        return false;
    }

    bool CheckOption(string OptionText) {
        if (OptionText.StartsWith("--")) 
          return CheckLongOption(OptionText);

        string Key = OptionText.Substring(1);
        ParseArgument = null;
        ParseOption = null;
        foreach(DefineData o in DefineList) {
          if (o.Short != null && Key.StartsWith(o.Short)) {
            ParseOption = o;
            ParseArgument = Key.Substring(o.Short.Length);
            return true;
          }
        }
        Console.WriteLine("Invalid Option:{0}",OptionText);
        return false;
    }

    OptionData MakeOption(string OptionText, string[] args) {
       OptionData od = new OptionData();
        if (!ParseOption.IsNeedArgument) {
          od.Type = ParseOption.Type;
          od.Key = ParseOption.Short;
          od.Value = null;
          return od;
        }
        if (ParseArgument.Length == 0) {
          ParseIndex++;
          if (ParseIndex >= args.Length) {
            Console.WriteLine("Missing Argument:{0}",OptionText);
            return null;
          }
          ParseArgument = args[ParseIndex];
        }
        od.Type = ParseOption.Type;
        od.Key = ParseOption.Short;
        od.Value = ParseArgument; 
        return od;
    }

    public bool Parse(string[] args) {
      for(ParseIndex=0; ParseIndex< args.Length; ParseIndex++) {
        if (!args[ParseIndex].StartsWith("-")) {
          Files.Add(args[ParseIndex]);
          continue;
        }

        if (!CheckOption(args[ParseIndex])) return false;
        var od = MakeOption(args[ParseIndex],args);
        if (od == null) return false;
        Result.Add(od); 
      }
      return true;
    }
  }

}
