using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Disk {

    class HuBasicDiskEntry {

        const int EntryEnd = 0xFF;
        const int EntryDelete = 0x00;

        const int FileEntrySize = 0x20;

        const int EntriesInSector = 8;

        const int DefaultSectorBytes = 256;

        public DiskParameter DiskParameter { get; private set; }

        public int CurrentEntrySector;
        public int AllocationTableStart => DiskParameter.AllocationTableStart;
        public int MaxCluster => DiskParameter.MaxCluster;
        public int ClusterPerSector => DiskParameter.ClusterPerSector;


        DataController[] AllocationController;

        public DiskImage DiskImage { get; }
        public Setting Setting { get; }
        public DiskType DiskType { get; }
        public DiskTypeEnum ImageType { get; }
        public Encoding TextEncoding { get; }
        public Log Log { get; }

        public HuBasicDiskEntry(Context Context) {
            DiskImage = new DiskImage(Context);
            Setting = Context.Setting;

            DiskType = Setting.DiskType;
            ImageType = Setting.DiskType.ImageType;
            TextEncoding = Context.TextEncoding;

            Log = Context.Log;
            if (Setting.FormatImage) FormatDisk(); else ReadOrFormat();
        }

        public void WriteDisk() {
            DiskImage.WriteDisk();
        }

        public void ReadOrFormat() {
            if (!DiskImage.ReadImage()) {
                FormatDisk();
                return;
            }

            SetParameter(false);
        }
        public void FormatDisk() {
            DiskImage.FormatImage();
            SetParameter(true);
        }

        private void SetParameter(bool FillAllocation) {
            SetDiskParameter();
            if (FillAllocation) FillAllocationTable();
            SetAllocateController();
        }

        private void SetDiskParameter() {
            DiskParameter = DiskType.DiskParameter;
            CurrentEntrySector = DiskType.DiskParameter.EntrySectorStart;
        }

        public bool IsOk(OpenEntryResult result) => result == OpenEntryResult.Ok;

        public List<HuFileEntry> GetEntries() => GetEntriesFromSector(CurrentEntrySector);

        public int GetFreeBytes(int FreeCluster) => FreeCluster * ClusterPerSector * DefaultSectorBytes;

        public int CountFreeClusters() {
            var Result = 0;
            for (var i = 0; i < MaxCluster; i++) {
                var end = IsEndCluster(i);
                var ptr = GetClusterValue(i);
                if (!end && ptr == 0x00) Result++;
            }
            return Result;
        }



        /// <summary>
        /// ファイル展開
        /// </summary>
        public void ExtractFile(Stream fs, int StartCluster, int Size) {
            int c = StartCluster;
            int LeftSize = Size;

            int TotalOutputBytes = 0;

            bool SectorWriteMode = Size == 0;
            bool AsciiMode = Setting.AsciiMode;

            while (true) {
                var end = IsEndCluster(c);
                var next = GetClusterValue(c);
                if (next == 0x00) {
                    Log.Warning("WARNING: Wrong cluster chain!!");
                    break;
                }
                // セクタ数
                var SectorCount = end ? (next & 0x0f) + 1 : ClusterPerSector;

                for (var i = 0; i < SectorCount; i++) {
                    var CurrentSector = (c * ClusterPerSector) + i;
                    var Sector = DiskImage.GetSector(CurrentSector);
                    
                    var Data = Sector.Data;
                    var Eof = false;
                    if (AsciiMode) {
                        var AsciiData = ConvertAscii(Sector.Data);
                        Eof = AsciiData.Eof;
                        Data = AsciiData.Data;
                    }

                    Log.Verbose($"Cluster:{c} Sector:{CurrentSector} Position:0x{Sector.Position:X}");

                    var OutputBytes = Data.Length;

                    // セクタ書き込みモード
                    if (!SectorWriteMode) {
                        // セクタサイズか残りのバイト数を書き出す
                        if (LeftSize < OutputBytes) OutputBytes = LeftSize;
                        LeftSize -= OutputBytes;
                    }

                    fs.Write(Data, 0, OutputBytes);
                    TotalOutputBytes += OutputBytes;

                    // 次のクラスタに進む
                    if (Eof) break;
                }
                if (end) break;
                c = next;
            }
        }

        class AsciiData {
            public byte[] Data;
            public bool Eof;

            public AsciiData(byte[] Data, bool Eof) {
                this.Data = Data;
                this.Eof = Eof;
            }
        }

        private AsciiData ConvertAscii(byte[] Data) {
            bool Eof = false;
            List<byte> Result = new List<byte>(Data.Length * 2);
            foreach(var b in Data) {
                if (b == 0x1a) {
                    Eof = true;
                    break;
                }
                Result.Add(b);
                if (b == 0x0d) {
                    Result.Add(0x0a);
                }
            }
            return new AsciiData(Result.ToArray(), Eof);
        }

        private void FillAllocationTable() {
            var dc = DiskImage.GetDataControllerForWrite(AllocationTableStart);
            dc.Fill(0);
            dc.SetByte(0, 0x01);
            dc.SetByte(1, 0x8f);

            switch (ImageType) {
                case DiskTypeEnum.Disk2D:
                    for (var i = 0x50; i < 0x80; i++) dc.SetByte(i, 0x8f);
                    break;
                case DiskTypeEnum.Disk2DD:
                    dc.SetBuffer(DiskImage.GetSectorDataForWrite(AllocationTableStart + 1));
                    dc.Fill(0);
                    for (var i = 0x20; i < 0x80; i++) dc.SetByte(i, 0x8f);
                    break;
                case DiskTypeEnum.Disk2HD:
                    dc.SetByte(2, 0x8f);
                    dc.SetBuffer(DiskImage.GetSectorDataForWrite(AllocationTableStart + 1));
                    dc.Fill(0);
                    for (var i = 0x7a; i < 0x80; i++) dc.SetByte(i, 0x8f);
                    break;
            }
            FormatEntry(CurrentEntrySector);
        }

        /// <summary>
        /// ファイルの削除
        /// </summary>

        public void DeleteFile(HuFileEntry fe) {
            fe.SetDelete();
            WriteFileEntry(fe);
            RemoveAllocation(fe.StartCluster);
        }

        private void FormatEntry(int Sector) {
            for (var i = 0; i < ClusterPerSector; i++) {
                var dc = DiskImage.GetDataControllerForWrite(Sector + i);
                dc.Fill(0xff);
            }
        }


        public HuFileEntry GetEntry(DataController dc, int sector, int pos) {
            var fe = new HuFileEntry();
            string Name = TextEncoding.GetString(dc.Copy(pos + 0x01, HuFileEntry.MaxNameLength)).TrimEnd((Char)0x20);
            string Extension = TextEncoding.GetString(dc.Copy(pos + 0x0e, HuFileEntry.MaxExtensionLength)).TrimEnd((Char)0x20);
            fe.SetEntryFromSector(dc, sector, pos, Name, Extension);
            return fe;
        }



        public List<HuFileEntry> GetEntriesFromSector(int Sector) {
            List<HuFileEntry> FileList = new List<HuFileEntry>();
            for (int i = 0; i < ClusterPerSector; i++, Sector++) {
                var dc = DiskImage.GetDataControllerForRead(Sector);
                for (var j = 0; j < 8; j++) {
                    int pos = (j * 0x20);
                    var mode = dc.GetByte(pos);
                    if (mode == EntryEnd) return FileList;
                    if (mode == EntryDelete) continue;
                    FileList.Add(GetEntry(dc, Sector, pos));
                }
            }
            return FileList;
        }

        // ファイルエントリ書き出し
        public void WriteFileEntry(HuFileEntry fe) {
            FileEntryNormalize(fe);
            var dc = DiskImage.GetDataControllerForWrite(fe.EntrySector);
            WriteEntry(dc, fe, fe.EntryPosition, fe.StartCluster, false);

            if (fe.IsIplEntry) WriteIplEntry(fe);
        }


        private void FileEntryNormalize(HuFileEntry fe) {
            if (fe.Name.Length > HuFileEntry.MaxNameLength) {
                fe.Name = fe.Name.Substring(0, HuFileEntry.MaxNameLength);
            }
            if (fe.Extension.StartsWith(".")) {
                fe.Extension = fe.Extension.Substring(1);
            }
            if (fe.Extension.Length > HuFileEntry.MaxExtensionLength) {
                fe.Extension = fe.Extension.Substring(0, HuFileEntry.MaxExtensionLength);
            }
        }

        // IPLエントリ書き出し
        public void WriteIplEntry(HuFileEntry fe) {
            var dc = DiskImage.GetDataControllerForWrite(0);
            WriteEntry(dc, fe, 0x00, fe.StartCluster * ClusterPerSector, true);
        }


        private void WriteEntry(DataController dc, HuFileEntry fe, int pos, int start, bool ipl) {
            dc.Fill(0x20, pos + 0x01, HuFileEntry.MaxNameLength);
            dc.Fill(0x20, pos + 0x0e, HuFileEntry.MaxExtensionLength);

            if (ipl) {
                WriteIplName(dc, pos);
            } else {
                WriteEntryName(dc, fe, pos);
            }
            dc.SetByte(pos + 0x11, fe.Password);

            dc.SetWord(pos + 0x12, fe.Size);
            dc.SetWord(pos + 0x14, fe.LoadAddress);
            dc.SetWord(pos + 0x16, fe.ExecuteAddress);
            dc.SetCopy(pos + 0x18, fe.DateTimeData);

            // 最上位は未調査
            dc.SetByte(pos + 0x1d, (start >> 14) & 0x7f);
            dc.SetByte(pos + 0x1e, start & 0x7f);
            dc.SetByte(pos + 0x1f, (start >> 7) & 0x7f);
        }

        private void WriteEntryName(DataController dc, HuFileEntry fe, int pos) {
            dc.SetByte(pos, fe.FileMode);
            dc.SetCopy(pos + 0x01, TextEncoding.GetBytes(fe.Name));
            dc.SetCopy(pos + 0x0e, TextEncoding.GetBytes(fe.Extension));
        }

        private void WriteIplName(DataController dc, int pos) {
            dc.SetByte(pos, 0x01);
            dc.SetCopy(pos + 0x01, TextEncoding.GetBytes(Setting.IplName));
            dc.SetCopy(pos + 0x0e, TextEncoding.GetBytes("Sys"));
        }

        private HuFileEntry GetFileEntry(string Filename, int EntrySector) {
            int Sector = EntrySector;
            Filename = Filename.ToUpper();
            // 名前
            string Name = Path.GetFileNameWithoutExtension(Filename);
            if (Name.Length > HuFileEntry.MaxNameLength) Name = Name.Substring(0, HuFileEntry.MaxNameLength);

            // 拡張子
            string Extension = Path.GetExtension(Filename);
            if (Extension.Length > 0) Extension = Extension.Substring(1);
            if (Extension.Length > HuFileEntry.MaxExtensionLength) Extension = Extension.Substring(0, HuFileEntry.MaxExtensionLength);

            Filename = Name + "." + Extension;

            for (int i = 0; i < ClusterPerSector; i++, Sector++) {
                var dc = DiskImage.GetDataControllerForRead(Sector);

                for (var j = 0; j < EntriesInSector; j++) {

                    int pos = (j * FileEntrySize);
                    var mode = dc.GetByte(pos);
                    if (mode == EntryEnd) return null;

                    string EntryName = TextEncoding.GetString(dc.Copy(pos + 0x01, HuFileEntry.MaxNameLength)).TrimEnd((Char)0x20);
                    string EntryExtension = TextEncoding.GetString(dc.Copy(pos + 0x0e, HuFileEntry.MaxExtensionLength)).TrimEnd((Char)0x20);
                    string EntryFilename = (EntryName + "." + EntryExtension).ToUpper();
                    if (Filename != EntryFilename) continue;

                    return GetEntry(dc, Sector, pos);
                }
            }
            return null;
        }

        private HuFileEntry GetNewFileEntry(int Sector) {
            for (int i = 0; i < ClusterPerSector; i++) {
                var dc = DiskImage.GetDataControllerForRead(Sector + i);
                for (var j = 0; j < EntriesInSector; j++) {
                    int pos = (j * FileEntrySize);
                    var mode = dc.GetByte(pos);
                    if (mode != EntryEnd && mode != EntryDelete) continue;

                    return new HuFileEntry {
                        EntrySector = Sector + i,
                        EntryPosition = pos
                    };
                }
            }
            return null;
        }

        public enum OpenEntryResult {
            Ok,
            NotDirectory,
            NoEntrySpace,
            NoFreeCluster,
        } 

        /// <summary>
        /// イメージのディレクトリを開く
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public OpenEntryResult OpenEntryDirectory(string Name) {
            var fe = GetFileEntry(Name, CurrentEntrySector);
            if (fe != null) {
                if (!fe.IsDirectory) {
                    Log.Error($"ERROR: {Name} is not directory!");
                    return OpenEntryResult.NotDirectory;
                }

                // エントリセクタを変更
                CurrentEntrySector = (fe.StartCluster * ClusterPerSector);
                return OpenEntryResult.Ok;
            }

            fe = GetNewFileEntry(CurrentEntrySector);
            if (fe == null) {
                Log.Error("ERROR:No entry space!");
                return OpenEntryResult.NotDirectory;
            }

            fe.SetNewDirectoryEntry(Name);

            int fc = GetNextFreeCluster();
            if (fc < 0) {
                Log.Error("ERROR:No free cluster!");
                return OpenEntryResult.NoFreeCluster;
            }

            fe.StartCluster = fc;
            WriteFileEntry(fe);
            CurrentEntrySector = fc * ClusterPerSector;
            FormatEntry(CurrentEntrySector);
            SetClusterValue(fc, 0x0f, true);
            return OpenEntryResult.Ok;
        }


        public bool WriteStream(Stream fs, int StartCluster, int Filesize) {

                Log.Info("StartCluster:" + StartCluster.ToString());

                int Size = Filesize;
                int c = StartCluster;
                while (true) {
                    var s = c * ClusterPerSector;
                    var LastSector = 0;
                    for (var sc = 0; sc < ClusterPerSector; sc++, s++) {
                        var Length = Size < DefaultSectorBytes ? Size : DefaultSectorBytes;
                        DiskImage.GetSector(s).Fill(0x00);
                        if (Size == 0) continue;

                        var SectorBuffer = DiskImage.GetSectorDataForWrite(s);
                        fs.Read(SectorBuffer, 0, Length);
                        Size -= Length;
                        if (Length > 0) LastSector = sc;
                    }
                    if (Size == 0) {
                        if (Setting.X1SMode && LastSector > 0) {
                            LastSector--;
                            if ((Filesize & 0xff) == 0) LastSector++;
                        }
                        SetClusterValue(c, LastSector, true);
                        break;
                    }
                    var next = GetNextFreeCluster(2);
                    if (next < 0) {
                        Log.Error($"Too big filesize!: LastClaster={c}");
                        SetClusterValue(c, LastSector, true);
                        return false;
                    }
                    SetClusterValue(c, next);
                    c = next;
            }

            return true;
        }

        public int GetFreeCluster(HuFileEntry fe) {
            if (Setting.IplMode) {
                int Cluster = (fe.Size / (ClusterPerSector * DefaultSectorBytes)) + 1;
                return GetNextFreeSerialCluster(Cluster);
            } else {
                return GetNextFreeCluster();
            }
        }

        public HuFileEntry GetWritableEntry(string Filename) {
            var fe = GetFileEntry(Filename, CurrentEntrySector);
            // エントリに確保されていたクラスタを解放する
            if (fe != null) {
                RemoveAllocation(fe.StartCluster);
            } else {
                fe = GetNewFileEntry(CurrentEntrySector);
            }

            return fe;
        }

        private void SetAllocateController() {
            AllocationController = new DataController[2];
            AllocationController[0] = DiskImage.GetDataControllerForWrite(AllocationTableStart);
            if (DiskType.IsNot2D) {
                AllocationController[1] = DiskImage.GetDataControllerForWrite(AllocationTableStart + 1);
            }
        }


        private int GetNextFreeCluster(int Step = 1) {
            for (var i = 0; i < MaxCluster; i++) {
                var end = IsEndCluster(i);
                var ptr = GetClusterValue(i);
                if (!end && ptr == 0x00) {
                    Step--;
                    if (Step == 0) return i;
                }
            }
            return -1;
        }

        private int GetNextFreeSerialCluster(int Clusters) {
            int FreeCount = 0;
            int FreeStart = 0;
            for (var i = 0; i < MaxCluster; i++) {
                var end = IsEndCluster(i);
                var ptr = GetClusterValue(i);
                if (!end && ptr == 0x00) {
                    if (FreeCount == 0) {
                        FreeStart = i;
                    }
                    FreeCount++;
                    if (FreeCount == Clusters) return FreeStart;
                } else {
                    FreeCount = 0;
                }
            }
            return -1;
        }

        /// <summary>
        /// アロケーションテーブルの開放
        /// </summary>
        /// <param name="StartCluster"></param>
        public void RemoveAllocation(int StartCluster) {
            int c = StartCluster;
            while (true) {
                var next = GetClusterValue(c);
                var end = IsEndCluster(c);
                // 0x00 = 既に解放済み
                if (next == 0x00) break;
                SetClusterValue(c, 0x00);
                var FillLength = end ? (next & 0x0f) + 1 : ClusterPerSector;

                for (var i = 0; i < FillLength; i++) {
                    DiskImage.GetDataControllerForWrite((c * ClusterPerSector) + i).Fill(0);
                }
                // 0x8x = 最後のクラスタ
                if (end) break;
                c = next;
            }
        }

        private void SetClusterValue(int pos, int value, bool end = false) {
            var low = (value & 0x7f);
            low |= end ? 0x80 : 0x00;
            int offset = pos / 0x80;
            pos &= 0x7f;
            AllocationController[offset].SetByte(pos, low);
            AllocationController[offset].SetByte(pos + 0x80, (value) >> 7);
        }

        private int GetClusterValue(int pos) {
            int offset = pos / 0x80;
            pos &= 0x7f;
            int Result = AllocationController[offset].GetByte(pos);
            Result |= (AllocationController[offset].GetByte(pos + 0x80) << 7);
            return Result;
        }

        private bool IsEndCluster(int pos) {
            int offset = pos / 0x80;
            pos &= 0x7f;
            return (AllocationController[offset].GetByte(pos) & 0x80) != 0x00;
        }
    }
}
