using System;

namespace Disk {
  class HuDisk {
    const string ProgramTitle = "HuDisk";
    const string ProgramVersion = "1.02";
    
    void Usage() {
        Console.WriteLine("Usage HuDisk image.d88 [files..] [options]");
        Console.WriteLine();
        Console.WriteLine(" Options...");
        Console.WriteLine(" -a,--add files ...  Add file(s)");
        Console.WriteLine(" -x,--extract files ... Extract file(s)");
        Console.WriteLine(" -l,--list ... List file(s)");
        Console.WriteLine();
        Console.WriteLine(" --format ... format image file");
        Console.WriteLine(" -i,--ipl <iplname> ... added file as a IPL binary");
        Console.WriteLine(" -r,--read  <address> ... set load address");
        Console.WriteLine(" -g,--go  <address> ... set execute address");
        Console.WriteLine();
        Console.WriteLine(" -h,-?,--help ... this one");
    }

    
    enum RunModeType {
      None,
      Add,
      Extract,
      List
    };

    enum OptionType {
      None,
      Go,
      Read,
      Ipl,
      Add,
      Extract,
      List,
      Help,
      Format
    }

    int ReadValue(string s) {
      return Convert.ToInt32(s,16);
    }

    public bool Run(string[] args)
    {
      Console.WriteLine("{0} ver {1}",ProgramTitle, ProgramVersion);

      var miniopt = new MiniOption();
      miniopt.AddOptionDefines(new MiniOption.DefineData[] {
        new MiniOption.DefineData((int)OptionType.Go, "g","go",true),
        new MiniOption.DefineData((int)OptionType.Read,"r","read",true),
        new MiniOption.DefineData((int)OptionType.Ipl,"i","ipl",true),
        new MiniOption.DefineData((int)OptionType.Add,"a","add",false),
        new MiniOption.DefineData((int)OptionType.Extract,"x","extract",false),
        new MiniOption.DefineData((int)OptionType.Format,null,"format",false),
        new MiniOption.DefineData((int)OptionType.List,"l","list",false),
        new MiniOption.DefineData((int)OptionType.Help,"h","help",false),
        new MiniOption.DefineData((int)OptionType.Help,"?",null,false)
      });

      string IplName = "";
      bool IplMode = false;
      bool FormatImage = false;
      int ExecuteAddress = 0x00;
      int LoadAddress = 0x00;

      if (!miniopt.Parse(args)) return false;

      if (miniopt.Files.Count < 1)
      {
        Usage();
        return false;
      }

      RunModeType mode = RunModeType.List;

      foreach(var o in miniopt.Result) {
        switch(o.Type) {
          case (int)OptionType.Go:
            ExecuteAddress = ReadValue(o.Value);
          break;
          case (int)OptionType.Read:
            LoadAddress = ReadValue(o.Value);
          break;
          case (int)OptionType.Help:
            Usage();
            return false;
          case (int)OptionType.Ipl:
            IplMode = true;
            IplName = o.Value;
          break;
          case (int)OptionType.Add:
            mode = RunModeType.Add;
          break;
          case (int)OptionType.Extract:
            mode = RunModeType.Extract;
          break;
          case (int)OptionType.List:
            mode = RunModeType.List;
          break;
          case (int)OptionType.Format:
            mode = RunModeType.Add;
            FormatImage = true;
          break;
        }
      }

      var ImageFile = miniopt.Files[0];
      Console.WriteLine("ImageFile:{0}",ImageFile);
      var d = new HuBasicDiskImage(ImageFile);
      if (FormatImage) d.Format(); else d.ReadOrFormat();

      switch(mode) {
        case RunModeType.Add:
          Console.Write("Add files ");
          Console.Write("Load:{0:X} Exec:{1:X}",LoadAddress,ExecuteAddress);
          Console.WriteLine(IplMode ? " IPL Mode" : "");
          d.IplMode = IplMode;
          d.IplName = IplName;
          d.LoadAddress = LoadAddress;
          d.ExecuteAddress = ExecuteAddress;

          for(var i=1; i<miniopt.Files.Count; i++) {
            string s = miniopt.Files[i];
            d.AddFile(s);
          }
          if (miniopt.Files.Count == 1) {
            Console.WriteLine("No files to add.");
          }

          d.DisplayFreeSpace();
          d.Write();

        break;
        case RunModeType.List:
          Console.WriteLine("List files");
          d.ListFiles();
          d.DisplayFreeSpace();
        break;
        case RunModeType.Extract:
          Console.WriteLine("Extract files");
          if (miniopt.Files.Count == 1) {
            d.ExtractFiles("*");
            break;
          }
          for(var i=1; i<miniopt.Files.Count; i++) {
            string s = miniopt.Files[i];
            d.ExtractFiles(s);
          }
        break;
      }

      return true;
    }
  }
}