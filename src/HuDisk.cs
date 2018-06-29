using System;

namespace Disk
{
    class HuDisk : DiskManager {
    const string ProgramTitle = "HuDisk";
    const string ProgramVersion = "1.15";

    string IplName = "";
    bool IplMode = false;
    bool X1SMode = false;
    int ExecuteAddress = 0x00;
    int LoadAddress = 0x00;

    DiskImage.DiskType ImageType = DiskImage.DiskType.Disk2D;
    bool SelectImageType = false;
    
    public override void Usage() {
        Console.WriteLine("{0} ver {1}",ProgramTitle, ProgramVersion);
        Console.WriteLine("Usage HuDisk IMAGE.D88 [Files..] [Options...]");
        Console.WriteLine();
        Console.WriteLine(" Options...");
        Console.WriteLine(" -a,--add <files...>   Add file(s)");
        Console.WriteLine(" -x,--extract [files...] Extract file(s)");
        Console.WriteLine(" -l,--list     List file(s)");
        Console.WriteLine(" -d,--delete   Delete file(s)");
        Console.WriteLine();
        Console.WriteLine(" --format    Format image file");
        Console.WriteLine(" --type <type> Determine Image type (2d/2dd/2hd)");
        Console.WriteLine(" -i,--ipl <iplname>    Added file as a IPL binary");
        Console.WriteLine(" -r,--read  <address>    Set load address");
        Console.WriteLine(" -g,--go  <address>    Set execute address");
        Console.WriteLine(" --x1s    Set x1save.exe compatible mode");
        Console.WriteLine(" --name <name>   Set entry name as <name>");
        Console.WriteLine(" --path <path>   Change directory in image");
        Console.WriteLine();
        Console.WriteLine(" -h,-?,--help  This one");
    }

    public override void AppendInfoForAdd() {
        Console.Write("Load:{0:X} Exec:{1:X}",LoadAddress,ExecuteAddress);
        if (X1SMode) Console.Write(" X1S Mode");
        Console.WriteLine(IplMode ? " IPL Mode" : "");
    }

    public bool Run(string[] args)
    {
      var miniopt = new MiniOption();
      miniopt.AddOptionDefines(new MiniOption.DefineData[] {
        new MiniOption.DefineData((int)OptionType.Add,"a","add",false),
        new MiniOption.DefineData((int)OptionType.Extract,"x","extract",false),
        new MiniOption.DefineData((int)OptionType.List,"l","list",false),
        new MiniOption.DefineData((int)OptionType.Delete,"d","delete",false),

        new MiniOption.DefineData((int)OptionType.Go, "g","go",true),
        new MiniOption.DefineData((int)OptionType.Read,"r","read",true),
        new MiniOption.DefineData((int)OptionType.Ipl,"i","ipl",true),


        new MiniOption.DefineData((int)OptionType.ImageType,null,"type",true),
        new MiniOption.DefineData((int)OptionType.Format,null,"format",false),
        new MiniOption.DefineData((int)OptionType.X1S,null,"x1s",false),
        new MiniOption.DefineData((int)OptionType.EntryName,null,"name",true),
        new MiniOption.DefineData((int)OptionType.Path,null,"path",true),

        new MiniOption.DefineData((int)OptionType.Help,"h","help",false),
        new MiniOption.DefineData((int)OptionType.Help,"?",null,false)
      });

      if (!miniopt.Parse(args)) return false;

      if (miniopt.Files.Count < 1)
      {
        Usage();
        return false;
      }

      if (!CheckOption(miniopt)) return false;

      var ImageFile = miniopt.Files[0];
      Console.WriteLine("ImageFile:{0}",ImageFile);
      var d = new HuBasicDiskImage(ImageFile);

      if (IplMode && IplName.Length > 13) IplName = IplName.Substring(0, 13);
      d.IplMode = IplMode;
      d.IplName = IplName;
      d.X1SMode = X1SMode;
      d.LoadAddress = LoadAddress;
      d.ExecuteAddress = ExecuteAddress;
      if (SelectImageType) {
        d.ImageType = ImageType;
      }

      if (FormatImage) d.FormatDisk(); else d.ReadOrFormat();

      RunDiskEdit(miniopt,d);

      return true;
    }

     public override bool CheckOptionExternal(MiniOption.OptionData o) {
        switch(o.Type) {
            case (int)OptionType.Go:
              ExecuteAddress = ReadValue(o.Value);
            break;
            case (int)OptionType.Read:
              LoadAddress = ReadValue(o.Value);
            break;
            case (int)OptionType.Ipl:
              IplMode = true;
              IplName = o.Value;
            break;
            case (int)OptionType.EntryName:
              EntryName = o.Value;
            break;
            case (int)OptionType.Path:
              EntryPath = o.Value;
            break;
            case (int)OptionType.X1S:
              X1SMode=true;
            break;
            case (int)OptionType.ImageType:
              SetDiskType(o.Value);
              break;
        }
        return true;
    }

        private void SetDiskType(string value)
        {
          SelectImageType = true;
          switch(value.ToLower()) {
            case "2d":
              ImageType = DiskImage.DiskType.Disk2D;
              break;              
            case "2dd":
              ImageType = DiskImage.DiskType.Disk2DD;
              break;              
            case "2hd":
              ImageType = DiskImage.DiskType.Disk2HD;
            break;
            default:
                Console.WriteLine("Unknown DiskType!!");
            break;
          }
        }
    }
}