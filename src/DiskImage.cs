using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Disk
{
    class DiskImage
    {
        public enum DiskType
        {
            Disk2D = 0x00,
            Disk2DD = 0x10,
            Disk2HD = 0x20
        }

        const int DefaultHeaderSize = 0x2b0;

        const int DefaultSectorSize = 256;

        const int MaxTrack = 164;

        const int TrackPerSector2D = 16;
        const int TrackPerSector2DD = 16;
        const int TrackPerSector2HD = 26;
        const int TrackMax2D = 80;
        const int TrackMax2DD = 160;
        const int TrackMax2HD = 154;

        public int TrackPerSector = 0;


        public string DiskName;
        public bool IsWriteProtect;
        public DiskType ImageType;
        public long ImageSize;
        private string ImageFile;

        protected long[] TrackAddress = new long[MaxTrack];

        protected long CurrentHeaderSize = 0;


        protected List<SectorData> Sectors = new List<SectorData>();

        protected Encoding TextEncoding;
        public bool Verbose;

        public bool PlainFormat = false;

        public string EntryName;

        public DiskImage(string ImageFilePath)
        {
            this.ImageFile = ImageFilePath;
            Verbose = false;
            DiskName = "";
            IsWriteProtect = false;
            ImageType = DiskType.Disk2D;
#if RELEASE
            // Console.WriteLine("Encoding:932");
            TextEncoding = System.Text.Encoding.GetEncoding(932);
#else
            // Console.WriteLine("Encoding:ASCII");
            TextEncoding = System.Text.Encoding.ASCII;
#endif
            var ext = Path.GetExtension(ImageFilePath);
            if (ext.StartsWith(".")) ext = ext.Substring(1);
            ext = ext.ToUpper();
            CheckExtension(ext);
        }

        private void CheckExtension(string ext)
        {
            if (ext != "2D" && ext != "2HD") return;
            PlainFormat = true;
            ImageType = ext == "2D" ? DiskType.Disk2D : DiskType.Disk2HD;
        }

        public virtual void Format2D()
        {
            ImageType = DiskType.Disk2D;
            TrackFormat(TrackMax2D, TrackPerSector2D);
        }

        private void TrackFormat(int TrackMax, int TrackPerSector)
        {
            var Position = PlainFormat ? 0x0 : DefaultHeaderSize;
            for (var t = 0; t < TrackMax; t++) {
                TrackAddress[t] = Position;
                for (var s = 0; s < TrackPerSector; s++) {
                    var Sector = new SectorData();
                    Sector.Make(t >> 1, t & 1, s + 1, 1, TrackPerSector, 0, false, 0, DefaultSectorSize);
                    Sectors.Add(Sector);
                    Position += DefaultSectorSize;
                }
            }
        }

        public virtual void Format2DD()
        {
            ImageType = DiskType.Disk2DD;
            TrackFormat(TrackMax2DD, TrackPerSector2DD);
        }

        public virtual void Format2HD()
        {
            ImageType = DiskType.Disk2HD;
            TrackFormat(TrackMax2HD, TrackPerSector2HD);
        }

        public void Format() {
            SetDiskSetting();
            switch(ImageType) {
                case DiskType.Disk2D:
                    Format2D();
                break;
                case DiskType.Disk2HD:
                    Format2HD();
                break;
                case DiskType.Disk2DD:
                    Format2DD();
                break;
            }
            CurrentHeaderSize = 0;
        }

        public void Write()
        {
            var fs = new FileStream(ImageFile,
            FileMode.OpenOrCreate,
            FileAccess.Write,
            FileShare.ReadWrite);

            var RebuildImage = IsRewriteImage();
            if (Verbose) Console.WriteLine("RebuildImage:" + RebuildImage.ToString());
            if (!PlainFormat) {
                if (RebuildImage) WriteHeader(fs);
            }
            WriteSectors(fs,RebuildImage);
            fs.Close();
        }

        public bool IsRewriteImage() {
            var LastTrack = -1;
            var MaxDirtyTrack = 0;
            foreach (SectorData s in Sectors) {
                if (s.IsDirty) MaxDirtyTrack = s.Track;
            }

            for (var i = 0; i < MaxTrack; i++) {
                if (TrackAddress[i] == 0x0) break;
                LastTrack = i;
            }
            if (!PlainFormat && CurrentHeaderSize != DefaultHeaderSize) return true;
            if (LastTrack < MaxDirtyTrack) return true;

            return false;
        }

        private void WriteHeader(FileStream fs)
        {
            byte[] header = new byte[DefaultHeaderSize];
            ImageSize = header.Length;
            int t = 0;
            foreach (SectorData s in Sectors) {
                if (s.Sector == 0x01) TrackAddress[t++] = ImageSize; 
                ImageSize += s.GetLength();
            }

            var dc = new DataController(header);
            dc.SetCopy(0, TextEncoding.GetBytes(this.DiskName),0x10);
            dc.SetByte(0x1a, IsWriteProtect ? 0x10 : 0x00);
            dc.SetByte(0x1b, (int)ImageType);
            dc.SetLong(0x1c, (ulong)ImageSize);

            // トラック分のアドレスを出力する
            long a = 0;
            for (var i = 0; i < MaxTrack; i++)
            {
                a = TrackAddress[i];
                if (a == 0x00) break;
                dc.SetLong(0x20 + (i * 4), (ulong)a);
            }
            fs.Write(header, 0, header.Length);
        }

        private void WriteSectors(FileStream fs,bool isRebuild)
        {
            var Length = fs.Length;
            var Position = TrackAddress[0];
            var Skip = true;

            foreach (SectorData s in Sectors)
            {
                if (!isRebuild) {
                    // 変更セクタまでスキップする
                    if (Position < Length && !s.IsDirty) {
                        Position += s.GetLength();
                        Skip = true;
                        continue;
                    }
                    if (Skip) {
                        fs.Position = Position;
                        Skip = false;
                    }
                    Position += s.GetLength();
                }
                byte[] d = PlainFormat ? s.Data : s.GetBytes();
                fs.Write(d, 0, d.Length);
            }
        }

        public bool Read()
        {
            if (!File.Exists(ImageFile)) return false;
            var fs = new FileStream(ImageFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
            if (!PlainFormat) ReadHeader(fs);
            SetDiskSetting();
            ReadSectors(fs);

            fs.Close();
            return true;
        }

        private void SetDiskSetting()
        {
            switch(ImageType) {
                case DiskType.Disk2D:
                    TrackPerSector = TrackPerSector2D;
                    break;
                case DiskType.Disk2DD:
                    TrackPerSector = TrackPerSector2DD;
                    break;
                case DiskType.Disk2HD:
                    TrackPerSector = TrackPerSector2HD;
                break;
            }
        }

        string GetDiskTypeName()
        {
            switch (ImageType)
            {
                case DiskType.Disk2D:
                    return "2D";
                case DiskType.Disk2DD:
                    return "2DD";
                case DiskType.Disk2HD:
                    return "2HD";
                default:
                    return "Unknown";
            }
        }

        void ReadHeader(FileStream fs) {
            byte[] header = new byte[0x2b0];
            fs.Read(header, 0, header.Length);

            var dc = new DataController(header);
            this.DiskName = TextEncoding.GetString(dc.Copy(0, 17)).TrimEnd((Char)0);
            IsWriteProtect = dc.GetByte(0x1a) != 0x00;
            var t = dc.GetByte(0x1b);
            ImageType = (DiskType)Enum.ToObject(typeof(DiskType), t);
            ImageSize = (long)dc.GetLong(0x1c);

            CurrentHeaderSize = 0;

            for (var i = 0; i < MaxTrack; i++) {
                var a = (long)dc.GetLong(0x20 + (i * 4));
                TrackAddress[i] = a;
                if (i == 0) CurrentHeaderSize = a;
            }
        }

        public void DiskDescription()
        {
            Console.WriteLine("DiskName:{0}", this.DiskName);
            Console.Write("IsWriteProtect:{0}", IsWriteProtect ? "Yes" : "No");
            Console.Write(" DiskType:{0}", GetDiskTypeName());
            Console.WriteLine(" ImageSize:{0}", ImageSize);
        }

        void ReadSectors(FileStream fs)
        {
            int Track = 0;
            int SectorCount = 1;
            var Address = PlainFormat ? 0x00 : TrackAddress[Track];
            if (!PlainFormat && Address == 0x00) return;
            fs.Seek(Address, SeekOrigin.Begin);
 
            while (true)
            {
                Address = fs.Position;
                var Sector = new SectorData();
                if (!Sector.Read(PlainFormat, fs)) break;
                if (!PlainFormat) {
                    SectorCount = Sector.Sector;
                }
                if (SectorCount == 0x01)
                {
                    if (PlainFormat) TrackAddress[Track] = Address;
                    if (Verbose) Console.WriteLine("Track:{0} Pos:{1:X} Address:{2:X}", Track, Address, TrackAddress[Track]);
                    Track++;
                }
                Sectors.Add(Sector);
                SectorCount++;
                if (SectorCount > TrackPerSector) SectorCount = 1;
            }
        }

        public virtual bool ChangeDirectory(string Path) {
            return false;
        }


        public virtual bool AddFile(string FilePath,string EntryName) {
            return false;
        }

        public virtual void ListFiles(string Directory = "")
        {
        }

        public virtual void DisplayFreeSpace() {
        }


        public virtual void ExtractFiles(string Pattern) {
        }

        public virtual void DeleteFiles(string Pattern) {
        }

    }
}
