using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Disk {



    public class Context {
        const string ProgramTitle = "HuDisk";
        const string ProgramVersion = "1.20";

        public string IplName = "";

        public bool IplMode = false;
        public bool X1SMode = false;

        public int ExecuteAddress = 0x00;
        public int LoadAddress = 0x00;

        public RunModeTypeEnum RunMode = RunModeTypeEnum.List;
        public bool FormatImage = false;
        public string EntryName = "";
        public string EntryPath = "";

        public string ImageFile;

        public List<string> Files { get; private set; }
        public string Extension { get; private set; }

        public DiskType DiskType = new DiskType();

        public Encoding TextEncoding;

        public Context() {
            TextEncoding = GetEncoding();
        }

        private Encoding GetEncoding() {
#if USE_ASCII
            // Console.WriteLine("Encoding:ASCII");
            return System.Text.Encoding.ASCII;
#else
            // Console.WriteLine("Encoding:932");
            return System.Text.Encoding.GetEncoding(932);
#endif        
        }

        public bool Parse(string[] args) {
            var miniopt = GetOptionData();
            if (!miniopt.Parse(args)) return false;

            if (miniopt.Files.Count < 1) {
                Usage();
                return false;
            }

            if (!CheckOption(miniopt)) return false;

            ImageFile = miniopt.Files[0];
            Files = miniopt.Files;

            Extension = ExtractExtension(ImageFile);
            DiskType.SetTypeFromExtension(Extension);

            Console.WriteLine("ImageFile:{0}", ImageFile);

            return true;
        }

        private string ExtractExtension(string Filename) {
            var ext = Path.GetExtension(Filename);
            if (ext.StartsWith(".")) ext = ext.Substring(1);
            ext = ext.ToUpper();
            return ext;
        }


        private static MiniOption GetOptionData() {
            var miniopt = new MiniOption();
            miniopt.AddOptionDefines(new MiniOption.DefineData[] {
                new MiniOption.DefineData(OptionType.Add,"a","add",false),
                new MiniOption.DefineData(OptionType.Extract,"x","extract",false),
                new MiniOption.DefineData(OptionType.List,"l","list",false),
                new MiniOption.DefineData(OptionType.Delete,"d","delete",false),

                new MiniOption.DefineData(OptionType.Go, "g","go",true),
                new MiniOption.DefineData(OptionType.Read,"r","read",true),
                new MiniOption.DefineData(OptionType.Ipl,"i","ipl",true),


                new MiniOption.DefineData(OptionType.ImageType,null,"type",true),
                new MiniOption.DefineData(OptionType.Format,null,"format",false),
                new MiniOption.DefineData(OptionType.X1S,null,"x1s",false),
                new MiniOption.DefineData(OptionType.EntryName,null,"name",true),
                new MiniOption.DefineData(OptionType.Path,null,"path",true),

                new MiniOption.DefineData(OptionType.Help,"h","help",false),
                new MiniOption.DefineData(OptionType.Help,"?",null,false)
              });
            return miniopt;
        }

        public bool CheckOption(MiniOption miniopt) {
            foreach (var o in miniopt.Result) {
                switch (o.Type) {
                    case OptionType.Add:
                        RunMode = RunModeTypeEnum.Add;
                        break;
                    case OptionType.Extract:
                        RunMode = RunModeTypeEnum.Extract;
                        break;
                    case OptionType.List:
                        RunMode = RunModeTypeEnum.List;
                        break;
                    case OptionType.Delete:
                        RunMode = RunModeTypeEnum.Delete;
                        break;

                    case OptionType.Format:
                        RunMode = RunModeTypeEnum.Add;
                        FormatImage = true;
                        break;
                    case OptionType.Help:
                        Usage();
                        return false;
                    default:
                        if (!CheckOptionExternal(o)) return false;
                        break;
                }
            }
            return true;
        }

        public void Usage() {
            Console.WriteLine("{0} ver {1}", ProgramTitle, ProgramVersion);
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

        public void ShowAddressInfo() {
            Console.Write("Load:{0:X} Exec:{1:X}", LoadAddress, ExecuteAddress);
            if (X1SMode) Console.Write(" X1S Mode");
            Console.WriteLine(IplMode ? " IPL Mode" : "");
        }


        public int ReadValue(string s) => Convert.ToInt32(s, 16);


        public bool CheckOptionExternal(MiniOption.OptionData o) {
            switch (o.Type) {
                case OptionType.Go:
                    ExecuteAddress = ReadValue(o.Value);
                    break;
                case OptionType.Read:
                    LoadAddress = ReadValue(o.Value);
                    break;
                case OptionType.Ipl:
                    IplMode = true;
                    IplName = o.Value;
                    break;
                case OptionType.EntryName:
                    EntryName = o.Value;
                    break;
                case OptionType.Path:
                    EntryPath = o.Value;
                    break;
                case OptionType.X1S:
                    X1SMode = true;
                    break;
                case OptionType.ImageType:
                    DiskType.SetDiskTypeFromOption(o.Value);
                    break;
            }
            return true;
        }


    }
}