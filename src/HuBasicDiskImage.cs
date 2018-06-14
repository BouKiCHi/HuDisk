using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Disk
{
    class HuBasicDiskImage : DiskImage
    {
        const int AllocationTable2DSector = 14;
        const int EntrySector2D = 16;
        const int MaxCluster2D = 80;

        const int AllocationTable2HDSector = 28;
        const int EntrySector2HD = 32;
        const int MaxCluster2HD = 160;

        const int AllocationTable2DDSector = 14;
        const int EntrySector2DD = 16;
        const int MaxCluster2DD = 160;


        const int ClusterPerSector = 16;
        const int SectorSize = 256;

        const int BinaryFileMode = 0x01;
        const int DirectoryFileMode = 0x80;

        const int EntryEnd = 0xFF;
        const int EntryDelete = 0x00;

        const int PasswordNoUse = 0x20;
        const int FileEntrySize = 0x20;
        private const int EntriesInSector = 8;
        public int AllocationTableStart;
        public int EntrySector = 0;
        public int MaxCluster = 0;


        DataController[] AllocationController;

        public string IplName;
        public bool IplMode;
        public int ExecuteAddress;
        public int LoadAddress;

        public bool X1SMode;

        public HuBasicDiskImage(string Path) : base(Path)
        {
        }

        private void FillFileSystem()
        {
            var dc = new DataController(Sectors[AllocationTableStart].Data);
            dc.Fill(0);
            dc.SetByte(0, 0x01);
            dc.SetByte(1, 0x8f);

            switch(ImageType) {
                case DiskType.Disk2D:
                    for (var i = 0x50; i < 0x80; i++) dc.SetByte(i, 0x8f);
                break;
                case DiskType.Disk2DD:
                    dc.SetBuffer(Sectors[AllocationTableStart + 1].Data);
                    dc.Fill(0);
                   for (var i = 0x20; i < 0x80; i++) dc.SetByte(i, 0x8f);
                break;
                case DiskType.Disk2HD:
                    dc.SetByte(2, 0x8f);
                    dc.SetBuffer(Sectors[AllocationTableStart + 1].Data);
                    dc.Fill(0);
                    for (var i = 0x7a; i < 0x80; i++) dc.SetByte(i, 0x8f);
                break;
            }
            FormatEntry(dc);
        }

        private void FormatEntry(DataController dc)
        {
            for (var i = 0; i < ClusterPerSector; i++)
            {
                dc.SetBuffer(Sectors[EntrySector + i].Data);
                dc.Fill(0xff);
            }
        }

        public void ReadOrFormat()
        {
            if (!Read()) {
                FormatDisk();
                return;
            }
            SetDiskParameter();
            SetAllocateController();
        }

        public bool CheckFormat() {
            if (ImageType != DiskType.Disk2D) return false;
            return true;
        }

        public void FormatDisk() {
            Format();
            SetDiskParameter();
            FillFileSystem();
            SetAllocateController();
        }

        private void SetDiskParameter() {
            switch(ImageType) {
                case DiskType.Disk2D:
                    AllocationTableStart = AllocationTable2DSector;
                    EntrySector = EntrySector2D;
                    MaxCluster = MaxCluster2D;
                break;
                case DiskType.Disk2HD:
                    AllocationTableStart = AllocationTable2HDSector;
                    EntrySector = EntrySector2HD;
                    MaxCluster = MaxCluster2HD;
                break;
                case DiskType.Disk2DD:
                    AllocationTableStart = AllocationTable2DDSector;
                    EntrySector = EntrySector2DD;
                    MaxCluster = MaxCluster2DD;
                break;

            }
        }

        private void SetAllocateController() {
            AllocationController = new DataController[2];
            AllocationController[0] = new DataController(Sectors[AllocationTableStart].Data);
            if (ImageType == DiskType.Disk2DD || ImageType ==DiskType.Disk2HD) {
                AllocationController[1] = new DataController(Sectors[AllocationTableStart+1].Data);
            }
        }


        public int GetNextFreeCluster(int Step=1) {
            for(var i=0; i<MaxCluster; i++) {
                var end = IsEndCluster(i);
                var ptr = ClusterValue(i);
                if (!end && ptr == 0x00) {
                    Step--;
                    if (Step == 0) return i;
                }
            }
            return -1;
        }

        public int GetNextFreeSerialCluster(int Clusters) {
            int FreeCount = 0;
            int FreeStart = 0;
            for(var i=0; i<MaxCluster; i++) {
                var end = IsEndCluster(i);
                var ptr = ClusterValue(i);
                if (!end && ptr == 0x00) {
                    if (FreeCount == 0) {
                        FreeStart=i;
                    }
                    FreeCount++;
                    if (FreeCount == Clusters) return FreeStart;
                } else {
                    FreeCount = 0;
                }
            }
            return -1;
        }

        public int GetFreeClusters() {
            var Result = 0;
            for(var i=0; i<MaxCluster; i++) {
                var end = IsEndCluster(i);
                var ptr = ClusterValue(i);
                if (!end && ptr == 0x00) Result++;
            }
            return Result;
        }

        public void RemoveAllocation(int StartCluster) {
            int c = StartCluster;
            while(true) {
                var next = ClusterValue(c);
                var end = IsEndCluster(c);
                // 0x00 = 既に解放済み
                if (next == 0x00) break;
                SetClusterValue(c,0x00);
                var FillLength = end ? (next & 0x0f) + 1 : ClusterPerSector;

                for(var i=0; i < FillLength; i++) {
                    (new DataController(Sectors[(c*ClusterPerSector)+i].Data)).Fill(0);
                }
                // 0x8x = 最後のクラスタ
                if (end) break;
                c = next;
            }
        }

        private void SetClusterValue(int pos, int value,bool end = false) {
            var low = (value & 0x7f);
            low |= end ? 0x80 : 0x00;
            int offset = pos / 0x80;
            pos &= 0x7f;
            AllocationController[offset].SetByte(pos, low);
            AllocationController[offset].SetByte(pos+0x80,(value)>>7);
        }

        private int ClusterValue(int pos) {
            int offset = pos / 0x80;
            pos &= 0x7f;
            int Result = AllocationController[offset].GetByte(pos);
            Result |= (AllocationController[offset].GetByte(pos+0x80)<<7);
            return Result;
        }

        private bool IsEndCluster(int pos) {
            int offset = pos / 0x80;
            pos &= 0x7f;
            return ((AllocationController[offset].GetByte(pos) & 0x80) != 0x00);
        }

        public HuFileEntry GetEntry(DataController dc,int sector, int pos) {
            var fe = new HuFileEntry();
            string Name = TextEncoding.GetString(dc.Copy(pos + 0x01, HuFileEntry.MaxNameLength)).TrimEnd((Char)0x20);
            string Extension = TextEncoding.GetString(dc.Copy(pos + 0x0e, HuFileEntry.MaxExtensionLength)).TrimEnd((Char)0x20);
            fe.Name = Name;
            fe.Extension = Extension;
            fe.EntrySector = sector;
            fe.EntryPosition = pos;
            fe.Mode = dc.GetByte(pos);
            fe.Password = dc.GetByte(pos + 0x11);
            fe.Size = dc.GetWord(pos + 0x12);
            fe.LoadAddress = dc.GetWord(pos + 0x14);
            fe.ExecuteAddress = dc.GetWord(pos + 0x16);
            fe.DateTimeData = dc.Copy(pos + 0x18,6);
            fe.StartCluster = dc.GetByte(pos + 0x1e);
            fe.StartCluster |= dc.GetByte(pos + 0x1f) << 7;
            return fe;
        }

        public List<HuFileEntry> GetEntriesFromSector(int Sector)
        {
            List<HuFileEntry> FileList = new List<HuFileEntry>();
            for (int i = 0; i < ClusterPerSector; i++,Sector++)
            {
                var dc = new DataController(Sectors[Sector].Data);
                for (var j = 0; j < 8; j++)
                {
                    int pos = (j * 0x20);
                    var mode = dc.GetByte(pos);
                    if (mode == EntryEnd) return FileList;
                    if (mode == EntryDelete) continue;
                    FileList.Add(GetEntry(dc,Sector,pos));
                }
            }
            return FileList;
        }

        public HuFileEntry GetFileEntry(string Filename, int EntrySector)
        {
            int Sector = EntrySector;
            Filename = Filename.ToUpper();
            // 名前
            string Name = Path.GetFileNameWithoutExtension(Filename);
            if (Name.Length > HuFileEntry.MaxNameLength) Name = Name.Substring(0,HuFileEntry.MaxNameLength);

            // 拡張子
            string Extension = Path.GetExtension(Filename);
            if (Extension.Length > 0) Extension = Extension.Substring(1);
            if (Extension.Length > HuFileEntry.MaxExtensionLength) Extension = Extension.Substring(0,HuFileEntry.MaxExtensionLength);

            Filename = Name + "." + Extension;

            for (int i = 0; i < ClusterPerSector; i++,Sector++)
            {
                var dc = new DataController(Sectors[Sector].Data);
                for (var j = 0; j < EntriesInSector; j++)
                {
                    int pos = (j * FileEntrySize);
                    var mode = dc.GetByte(pos);
                    if (mode == EntryEnd) return null;
                    string EntryName = TextEncoding.GetString(dc.Copy(pos + 0x01, HuFileEntry.MaxNameLength)).TrimEnd((Char)0x20);
                    string EntryExtension = TextEncoding.GetString(dc.Copy(pos + 0x0e, HuFileEntry.MaxExtensionLength)).TrimEnd((Char)0x20);
                    string EntryFilename = (EntryName + "." + EntryExtension).ToUpper();
                    if (Filename != EntryFilename) continue;
                    return GetEntry(dc,Sector,pos);
                }
            }
            return null;
        }

        public HuFileEntry GetNewFileEntry(int sector)
        {
            for (int i = 0; i < ClusterPerSector; i++)
            {
                var dc = new DataController(Sectors[sector + i].Data);
                for (var j = 0; j < EntriesInSector; j++)
                {
                    int pos = (j * FileEntrySize);
                    var mode = dc.GetByte(pos);
                    if (mode != EntryEnd && mode != EntryDelete) continue;
                    var fe = new HuFileEntry();
                    fe.EntrySector = sector + i;
                    fe.EntryPosition = pos;
                    return fe;
                }
            }
            return null;
        }

        public void FileEntryNormalize(HuFileEntry fe) {
            if (fe.Name.Length > HuFileEntry.MaxNameLength) {
                fe.Name = fe.Name.Substring(0,HuFileEntry.MaxNameLength);
            }
            if (fe.Extension.StartsWith(".")) {
                fe.Extension = fe.Extension.Substring(1);
            }
            if (fe.Extension.Length > HuFileEntry.MaxExtensionLength) {
                fe.Extension = fe.Extension.Substring(0,HuFileEntry.MaxExtensionLength);
            }
        }

        // ファイルエントリ書き出し
        public void WriteFileEntry(HuFileEntry fe) {
            FileEntryNormalize(fe);
            var dc = new DataController(Sectors[fe.EntrySector].Data);
            WriteEntry(dc,fe,fe.EntryPosition ,fe.StartCluster,false);
        }

        // IPLエントリ書き出し
        public void WriteIplEntry(HuFileEntry fe) {
            var dc = new DataController(Sectors[0].Data);
            WriteEntry(dc,fe,0x00,fe.StartCluster * ClusterPerSector,true);
        }

        private void WriteEntry(DataController dc,HuFileEntry fe,int pos,int start,bool ipl) {
            dc.Fill(0x20,pos + 0x01,HuFileEntry.MaxNameLength);
            dc.Fill(0x20,pos + 0x0e,HuFileEntry.MaxExtensionLength);

            if (ipl) {
                dc.SetByte(pos,0x01);
                dc.SetCopy(pos + 0x01,TextEncoding.GetBytes(IplName));
                dc.SetCopy(pos + 0x0e,TextEncoding.GetBytes("Sys"));
            } else {
                dc.SetByte(pos,fe.Mode);
                dc.SetCopy(pos + 0x01,TextEncoding.GetBytes(fe.Name));
                dc.SetCopy(pos + 0x0e,TextEncoding.GetBytes(fe.Extension));
            }
            dc.SetByte(pos + 0x11,fe.Password);

            dc.SetWord(pos + 0x12,fe.Size);
            dc.SetWord(pos + 0x14,fe.LoadAddress);
            dc.SetWord(pos + 0x16,fe.ExecuteAddress);
            dc.SetCopy(pos + 0x18,fe.DateTimeData);

            // 最上位は未調査
            dc.SetByte(pos + 0x1d,(start>>14) &0x7f);
            dc.SetByte(pos + 0x1e,start & 0x7f);
            dc.SetByte(pos + 0x1f,(start>>7) & 0x7f);
        }

        public override void ListFiles(string Directory = "")
        {
            var Files = GetEntriesFromSector(EntrySector);
            foreach(var f in Files) {
                f.Description();
            }
        }

        public override void DisplayFreeSpace() {
            Console.WriteLine("Free:{0} Cluster(s)",GetFreeClusters());
        }

        public void ExtractFileFromCluster(string OutputFile,int StartCluster,int Size) {
            
            Stream fs;
            if (OutputFile == "-") {
                fs = Console.OpenStandardOutput();
            } else {
                fs = new FileStream(OutputFile,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.ReadWrite);
            }

            int c = StartCluster;
            while(true) {
                var end = IsEndCluster(c);
                var next = ClusterValue(c);
                if (next == 0x00) {
                    Console.WriteLine("WARNING: Wrong cluster chain!!");
                    break;
                }
                var FillLength = end ? (next & 0x0f) + 1 : ClusterPerSector;

                for(var i=0; i < FillLength; i++) {
                    var Length = SectorSize;
                    if (end && (i + 1) == FillLength) {
                       Length = Size % SectorSize; 
                    }
                    fs.Write(Sectors[(c*ClusterPerSector)+i].Data,0,Length);
                }
                if (end) break;
                c = next;
            }
            fs.Close();
        }

        public void WriteFileToImage(string InputFile,int StartCluster,int Filesize) {

            var fs = new FileStream(InputFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

            int Size = Filesize;
            int c = StartCluster;
            while(true) {
                var sc = 0;
                var s = (c*ClusterPerSector);
                var LastSector = 0;
                for(sc=0; sc < ClusterPerSector; sc++,s++) {
                    var Length = Size < SectorSize ? Size : SectorSize;
                    Sectors[s].Fill(0x00);
                    if (Size == 0) continue;
                    fs.Read(Sectors[s].Data,0,Length);
                    Size-=Length;
                    if (Length > 0) LastSector = sc;
                }
                if (Size == 0) {
                    if (X1SMode && LastSector > 0) {
                        LastSector--;
                        if ((Filesize & 0xff) == 0) LastSector++;
                    }
                    SetClusterValue(c,LastSector,true);
                    break;
                }
                var next = GetNextFreeCluster(2);
                SetClusterValue(c,next);
                c = next;
            }
            fs.Close();
        }

        public override bool AddFile(string FilePath,string EntryName) {
            if (!File.Exists(FilePath)) return false;
            var fi = new FileInfo(FilePath);

            // Filename = エントリ上のファイル名
            var Filename = EntryName.Length > 0 ? EntryName : Path.GetFileName(FilePath);
            var Size = (int)fi.Length;
            var FileDate = File.GetLastWriteTime(FilePath);
            Console.WriteLine("Add:{0} Size:{1} {2}",Filename, Size, IplMode ? "IPL" : "");

            var fe = GetFileEntry(Filename,EntrySector);
            // エントリに確保されていたクラスタを解放する
            if (fe != null) {
                RemoveAllocation(fe.StartCluster);
            } else {
                fe = GetNewFileEntry(EntrySector);
            }

            if (fe == null) {
                Console.WriteLine("no entry space!");
                return false;
            }

            if (fe == null) return false;
            fe.SetTime(FileDate);
            fe.Mode = BinaryFileMode;
            fe.Size = Size;
            fe.ExecuteAddress = ExecuteAddress;
            fe.LoadAddress = LoadAddress;
            fe.Name = Path.GetFileNameWithoutExtension(Filename);
            fe.Extension = Path.GetExtension(Filename);
            fe.Password = PasswordNoUse;

            int fc = -1;
            if (IplMode) {
                int Cluster = (fe.Size / (ClusterPerSector * SectorSize)) + 1;
                fc = GetNextFreeSerialCluster(Cluster);
            } else {
                fc = GetNextFreeCluster();
            }
            if (fc < 0) {
                Console.WriteLine("no free cluster!");
                return false;
            }
            fe.StartCluster = fc;
            if (IplMode) {
                Console.WriteLine("IPL Name:{0}",IplName);
                WriteFileEntry(fe);
                WriteIplEntry(fe);
                IplMode = false;
            } else {
                WriteFileEntry(fe);
            }
            WriteFileToImage(FilePath, fc, Size);

            return true;
        }

        private string PatternToRegex(string Pattern) {
            return  "^" +  Regex.Escape(Pattern).Replace(@"\*", ".*" ).Replace( @"\?", "." ) + "$";
        }

        private void PatternFiles(string Pattern,bool Extract,bool Delete) {
            var r = new Regex(PatternToRegex(Pattern),RegexOptions.IgnoreCase);
            var Files = GetEntriesFromSector(EntrySector);
            foreach(var fe in Files) {
                if (!r.IsMatch(fe.GetFilename())) continue;
                fe.Description();
                if (Extract) {
                    string name =!string.IsNullOrEmpty(OutputName) ? OutputName : fe.GetFilename();
                    ExtractFileFromCluster(name,fe.StartCluster,fe.Size);
                }
                if (Delete) {
                    fe.SetDelete();
                    WriteFileEntry(fe);
                    RemoveAllocation(fe.StartCluster);
                }
            }
        }

        public override void ExtractFiles(string Pattern) {
            PatternFiles(Pattern,true,false);
        }

        public override void DeleteFiles(string Pattern) {
            PatternFiles(Pattern,false,true);
        }
    }
}
