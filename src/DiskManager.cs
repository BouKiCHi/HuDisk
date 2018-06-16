using System;

namespace Disk
{
    class DiskManager
    {
        RunModeType RunMode = RunModeType.List;
        public bool FormatImage = false;
        public string EntryName = "";
        public string EntryPath = "";


        public enum RunModeType
        {
            None,
            Add,
            Extract,
            List,
            Delete
        };

        public enum OptionType
        {
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
            Path
        }

        public virtual void AppendInfoForAdd()
        {
        }

        public int ReadValue(string s)
        {
            return Convert.ToInt32(s, 16);
        }
        public virtual bool CheckOptionExternal(MiniOption.OptionData o)
        {
            return true;
        }
        public virtual void Usage()
        {
        }

        public bool CheckOption(MiniOption miniopt)
        {
            foreach (var o in miniopt.Result)
            {
                switch (o.Type)
                {
                    case (int)OptionType.Add:
                        RunMode = RunModeType.Add;
                        break;
                    case (int)OptionType.Extract:
                        RunMode = RunModeType.Extract;
                        break;
                    case (int)OptionType.List:
                        RunMode = RunModeType.List;
                        break;
                    case (int)OptionType.Delete:
                        RunMode = RunModeType.Delete;
                        break;

                    case (int)OptionType.Format:
                        RunMode = RunModeType.Add;
                        FormatImage = true;
                        break;
                    case (int)OptionType.Help:
                        Usage();
                        return false;
                    default:
                        if (!CheckOptionExternal(o)) return false;
                        break;
                }
            }
            return true;
        }

        public void RunDiskEdit(MiniOption Options, DiskImage Image)
        {
            if (!Image.ChangeDirectory(EntryPath)) {
                Console.WriteLine("Directory open error!");
                return;
            }

            switch (RunMode)
            {
                case RunModeType.Add:
                    Console.WriteLine("Add files:");

                    for (var i = 1; i < Options.Files.Count; i++)
                    {
                        string s = Options.Files[i];
                        Image.AddFile(s, EntryName);
                    }
                    if (Options.Files.Count == 1)
                    {
                        Console.WriteLine("No files to add.");
                    }

                    Image.DisplayFreeSpace();
                    Image.Write();

                    break;
                case RunModeType.List:
                    Console.WriteLine("List files:");
                    Image.ListFiles();
                    Image.DisplayFreeSpace();
                    break;
                case RunModeType.Extract:
                    Console.WriteLine("Extract files:");
                    Image.EntryName = EntryName;
                    EditFiles(Options,Image,true,false);
                    break;

                case RunModeType.Delete:
                    Console.WriteLine("Delete files:");
                    EditFiles(Options,Image,false,true);
                    Image.Write();
                    break;
            }
        }

        private void EditFiles(MiniOption Options, DiskImage Image, bool Extract, bool Delete)
        {
            if (Options.Files.Count == 1)
            {
              if (Delete) Image.DeleteFiles("*");
                return;
            }
            for (var i = 1; i < Options.Files.Count; i++)
            {
                string s = Options.Files[i];
                if (Extract) Image.ExtractFiles(s);
                if (Delete) Image.DeleteFiles(s);
            }
        }
    }
}