using System;
using System.Collections.Generic;
using System.Text;

namespace Disk {

    public enum RunModeTypeEnum {
        None,
        Add,
        Extract,
        List,
        Delete
    };

    public enum OptionType {
        None,
        Go,
        Read,
        Ipl,
        Add,
        Extract,
        List,
        Help,
        Format,
        X1S,
        EntryName,
        Delete,
        ImageType,
        Path,
        Verbose,
        Ascii
    }

    public class Context {
        const string ProgramTitle = "HuDisk";
        const string ProgramVersion = "1.20";


        public RunModeTypeEnum RunMode = RunModeTypeEnum.List;


        public List<string> Files { get; private set; }


        public Encoding TextEncoding;

        public Setting Setting { get; }

        public Log Log;

        public Context() {
            Setting = new Setting();
            Log = new Log();
            TextEncoding = GetEncoding();
        }

        private Encoding GetEncoding() {
#if USE_ASCII
            // Console.WriteLine("Encoding:ASCII");
            return System.Text.Encoding.ASCII;
#else
            // Console.WriteLine("Encoding:932");
            return Encoding.GetEncoding(932);
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

            var ImageFilename = miniopt.Files[0];
            SetImageFilename(ImageFilename);

            Files = miniopt.Files;
            return true;
        }

        public void SetImageFilename(string Filename) {
            Setting.SetImageFilename(Filename);
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
                new MiniOption.DefineData(OptionType.Verbose,"v","verbose",false),
                new MiniOption.DefineData(OptionType.Ascii,null,"ascii",false),

                new MiniOption.DefineData(OptionType.Help,"h","help",false),
                new MiniOption.DefineData(OptionType.Help,"?",null,false)
              });
            return miniopt;
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
            Console.WriteLine(" -v,--verbose Change directory in image");
            Console.WriteLine(" --ascii Set ASCII Mode");
            Console.WriteLine();
            Console.WriteLine(" -h,-?,--help  This one");
        }


        public int ReadValue(string s) => Convert.ToInt32(s, 16);


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
                        Setting.FormatImage = true;
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

        public bool CheckOptionExternal(MiniOption.OptionData o) {
            switch (o.Type) {
                case OptionType.Go:
                    Setting.ExecuteAddress = ReadValue(o.Value);
                    break;
                case OptionType.Read:
                    Setting.LoadAddress = ReadValue(o.Value);
                    break;
                case OptionType.Ipl:
                    Setting.IplMode = true;
                    Setting.IplName = o.Value;
                    break;
                case OptionType.EntryName:
                    Setting.EntryName = o.Value;
                    break;
                case OptionType.Path:
                    Setting.EntryDirectory = o.Value;
                    break;
                case OptionType.X1S:
                    Setting.X1SMode = true;
                    break;

                case OptionType.Ascii:
                    Setting.AsciiMode = true;
                    break;

                case OptionType.Verbose:
                    Log.SetVerbose();
                    break;
                case OptionType.ImageType:
                    bool Result = Setting.SetImageType(o.Value);
                    if (!Result) {
                        Log.Error($"Image Type is Unknown... {o.Value}");
                    }
                    break;
            }
            return true;
        }


    }
}