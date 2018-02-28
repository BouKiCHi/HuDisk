using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Disk
{
    class HuBasicDiskImage : DiskImage
    {
        const int AllocationTableSector = 14;
        const int FileEntrySector = 16;
        const int MaxCluster = 80;
        const int ClusterPerSector = 16;
        const int SectorSize = 256;

        const int BinaryFileMode = 0x01;
        const int DirectoryFileMode = 0x80;

        const int EntryEnd = 0xFF;
        const int EntryDelete = 0x00;

        DataController AllocationController;

        public string IplName;
        public bool IplMode;
        public int ExecuteAddress;
        public int LoadAddress;

        public HuBasicDiskImage(string Path) : base(Path)
        {
        }

        public new void Format2D()
        {
            base.Format2D();
            var dc = new DataController(Sectors[AllocationTableSector].Data);
            dc.Fill(0);
            dc.SetByte(0, 0x01);
            dc.SetByte(1, 0x8f);
            for (var i = 0; i < 0x30; i++) dc.SetByte(0x50 + i, 0x8f);

            for (var i = 0; i < ClusterPerSector; i++)
            {
                dc.SetBuffer(Sectors[FileEntrySector + i].Data);
                dc.Fill(0xff);
            }
        }

        public new void ReadOrFormat()
        {
            if (!Read()) Format2D();
            SetAllocateController();
        }

        public bool CheckFormat() {
            if (DensityType != DiskType.Disk2D) return false;
            return true;
        }

        public void Format() {
            Format2D();
            SetAllocateController();
        }

        private void SetAllocateController() {
            AllocationController = new DataController(Sectors[AllocationTableSector].Data);
        }


        public int GetNextFreeCluster(int Step=1) {
            for(var i=0; i<MaxCluster; i++) {
                var ptr = AllocationController.GetByte(i);
                if (ptr == 0x00) {
                    Step--;
                    if (Step == 0) return i;
                }
            }
            return -1;
        }

        public int GetFreeClusters() {
            var Result = 0;
            for(var i=0; i<MaxCluster; i++) {
                var ptr = AllocationController.GetByte(i);
                if (ptr == 0x00) Result++;
            }
            return Result;
        }

        public void RemoveAllocation(int StartCluster) {
            int c = StartCluster;
            while(true) {
                var next = AllocationController.GetByte(c);
                AllocationController.SetByte(c,0x00);
                var FillLength = (next & 0x80) != 0x00 ? (next & 0x0f) + 1 : ClusterPerSector;

                for(var i=0; i < FillLength; i++) {
                    (new DataController(Sectors[(c*ClusterPerSector)+i].Data)).Fill(0);
                }
                if ((next & 0x80) != 0x00) break;
                c = next;
            }
        }

        public HuFileEntry GetEntry(DataController dc,int sector, int pos) {
            var fe = new HuFileEntry();
            string Name = TextEncoding.GetString(dc.Copy(pos + 0x01, 13)).TrimEnd((Char)0x20);
            string Extension = TextEncoding.GetString(dc.Copy(pos + 0x0e, 4)).TrimEnd((Char)0x20);
            fe.Name = Name;
            fe.Extension = Extension;
            fe.EntrySector = sector;
            fe.EntryPosition = pos;
            fe.Mode = dc.GetByte(pos);
            fe.Size = dc.GetWord(pos + 0x12);
            fe.LoadAddress = dc.GetWord(pos + 0x14);
            fe.ExecuteAddress = dc.GetWord(pos + 0x16);
            fe.DateTimeData = dc.Copy(pos + 0x18,6);
            fe.StartCluster = dc.GetWord(pos + 0x1e);
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
            for (int i = 0; i < ClusterPerSector; i++,Sector++)
            {
                var dc = new DataController(Sectors[Sector].Data);
                for (var j = 0; j < 8; j++)
                {
                    int pos = (j * 0x20);
                    var mode = dc.GetByte(pos);
                    if (mode == EntryEnd) return null;
                    string Name = TextEncoding.GetString(dc.Copy(pos + 0x01, 13)).TrimEnd((Char)0x20);
                    string Extension = TextEncoding.GetString(dc.Copy(pos + 0x0e, 4)).TrimEnd((Char)0x20);
                    string EntryFilename = (Name + "." + Extension).ToUpper();
                    if (Filename != EntryFilename) continue;
                    return GetEntry(dc,Sector,pos);
                }
            }
            return null;
        }

        public HuFileEntry GetNewFileEntry(int EntrySector)
        {
            for (int i = 0; i < 16; i++)
            {
                var dc = new DataController(Sectors[EntrySector + i].Data);
                for (var j = 0; j < 8; j++)
                {
                    int pos = (j * 32);
                    var mode = dc.GetByte(pos);
                    if (mode != EntryEnd && mode != EntryDelete) continue;
                    var fe = new HuFileEntry();
                    fe.EntrySector = EntrySector + i;
                    fe.EntryPosition = pos;
                    return fe;
                }
            }
            return null;
        }

        public void WriteFileEntry(HuFileEntry fe) {
            var dc = new DataController(Sectors[fe.EntrySector].Data);
            int pos = fe.EntryPosition;
            dc.SetByte(pos,fe.Mode);
            dc.Fill(0x20,pos + 0x01,13);
            dc.Fill(0x20,pos + 0x0e,4);
            dc.SetCopy(pos + 0x01,TextEncoding.GetBytes(fe.Name));
            dc.SetCopy(pos + 0x0e,TextEncoding.GetBytes(fe.Extension));
            dc.SetWord(pos + 0x12,fe.Size);
            dc.SetWord(pos + 0x14,fe.LoadAddress);
            dc.SetWord(pos + 0x16,fe.ExecuteAddress);
            dc.SetCopy(pos + 0x18,fe.DateTimeData);
            dc.SetWord(pos + 0x1e,fe.StartCluster);
        }

        public void WriteIplEntry(HuFileEntry fe) {
            var dc = new DataController(Sectors[0].Data);
            int pos = 0x00;
            dc.SetByte(pos,0x01);
            dc.Fill(0x20,pos + 0x01,13);
            dc.Fill(0x20,pos + 0x0e,4);
            dc.SetCopy(pos + 0x01,TextEncoding.GetBytes(fe.Name));
            dc.SetCopy(pos + 0x0e,TextEncoding.GetBytes("Sys"));
            dc.SetWord(pos + 0x12,fe.Size);
            dc.SetWord(pos + 0x14,fe.LoadAddress);
            dc.SetWord(pos + 0x16,fe.ExecuteAddress);
            dc.SetCopy(pos + 0x18,fe.DateTimeData);
            dc.SetWord(pos + 0x1e,fe.StartCluster * ClusterPerSector);
        }

        public void ListFiles(int Sector = 16,string Directory = "")
        {
            var Files = GetEntriesFromSector(Sector);
            foreach(var f in Files) {
                f.Description();
            }
        }

        public void DisplayFreeSpace() {
            Console.WriteLine("Free:{0} Cluster(s)",GetFreeClusters());
        }

        public void ExtractFile(string OutputFile,int StartCluster,int Size) {

            var fs = new FileStream(OutputFile,
            FileMode.OpenOrCreate,
            FileAccess.Write,
            FileShare.ReadWrite);

            int c = StartCluster;
            while(true) {
                var next = AllocationController.GetByte(c);
                var FillLength = (next & 0x80) != 0x00 ? (next & 0x0f) + 1 : ClusterPerSector;

                for(var i=0; i < FillLength; i++) {
                    var Length = Size < SectorSize ? Size : SectorSize;
                    fs.Write(Sectors[(c*ClusterPerSector)+i].Data,0,Length);
                    Size-=Length;
                }
                if ((next & 0x80) != 0x00) break;
                c = next;
            }
            fs.Close();
        }

        public void WriteFileToImage(string InputFile,int StartCluster,int Size) {

            var fs = new FileStream(InputFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

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
                    AllocationController.SetByte(c,0x80 + LastSector);
                    break;
                }
                var next = GetNextFreeCluster(2);
                AllocationController.SetByte(c,next);
                c = next;
            }
            fs.Close();
        }

        public bool AddFile(string FilePath,int EntrySector = FileEntrySector) {
            if (!File.Exists(FilePath)) return false;
            var fi = new FileInfo(FilePath);
            
            var Filename = Path.GetFileName(FilePath);
            var Size = (int)fi.Length;
            var FileDate = File.GetLastWriteTime(FilePath);
            Console.WriteLine("Add:{0} Size:{1} {2}",Filename, Size, IplMode ? "IPL" : "");

            if (Size > 0xFFFF) {
                Console.WriteLine("too big filesize!");
                return false;
            }

            var fe = GetFileEntry(Filename,EntrySector);
            if (fe != null) {
                RemoveAllocation(fe.StartCluster);
            } else {
                fe = GetNewFileEntry(EntrySector);
            }
            if (fe == null) return false;
            fe.SetTime(FileDate);
            fe.Mode = BinaryFileMode;
            fe.Size = Size;
            fe.ExecuteAddress = ExecuteAddress;
            fe.LoadAddress = LoadAddress;
            fe.Name = Path.GetFileNameWithoutExtension(FilePath);
            fe.Extension = Path.GetExtension(FilePath);
            if (fe.Extension.StartsWith(".")) {
                fe.Extension = fe.Extension.Substring(1);
            }
            var fc = GetNextFreeCluster();
            fe.StartCluster = fc;
            if (IplMode) {
                Console.WriteLine("IPL Name:{0}",IplName);
                fe.Name = IplName;
                fe.Extension = "";
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

        public void ExtractFiles(string Pattern,int Sector = FileEntrySector) {
            var r = new Regex(PatternToRegex(Pattern));
            var Files = GetEntriesFromSector(Sector);
            foreach(var f in Files) {
                if (!r.IsMatch(f.GetFilename())) continue;
                f.Description();
                ExtractFile(f.GetFilename(),f.StartCluster,f.Size);
            }
        }
    }
}
