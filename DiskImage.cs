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

        const int MaxTrack = 164;

        public int TrackPerSector = 16;

        public string Name;
        public bool IsWriteProtect;
        public DiskType DensityType;
        public long ImageSize;
        private string ImageFile;

        protected long[] TrackAddress = new long[MaxTrack];


        protected List<SectorData> Sectors = new List<SectorData>();

        protected Encoding TextEncoding;
        public bool Verbose;

        public bool Plain2DFormat = false;

        public DiskImage(string ImageFilePath)
        {
            this.ImageFile = ImageFilePath;
            Verbose = false;
            Name = "";
            IsWriteProtect = false;
            DensityType = DiskType.Disk2D;
#if NET40
            TextEncoding = System.Text.Encoding.GetEncoding(932);
#else
            TextEncoding = System.Text.Encoding.ASCII;
#endif
            var ext = Path.GetExtension(ImageFilePath);
            if (ext.StartsWith(".")) ext = ext.Substring(1);
            ext = ext.ToUpper();
            if (ext == "2D") Plain2DFormat = true;
        }

        public void Format2D()
        {
            DensityType = DiskType.Disk2D;
            for (var t = 0; t < 80; t++)
            {
                for (var s = 0; s < 16; s++)
                {
                    var Sector = new SectorData();
                    Sector.Make(t >> 1, t & 1, s + 1, 1, 16, 0, false, 0, 256);
                    Sectors.Add(Sector);
                }
            }
        }

        public void ReadOrFormat()
        {
            if (!Read()) Format2D();
        }

        public void Write()
        {
            var fs = new FileStream(ImageFile,
            FileMode.OpenOrCreate,
            FileAccess.Write,
            FileShare.ReadWrite);

            if (!Plain2DFormat) WriteHeader(fs);
            WriteSectors(fs);
            fs.Close();
        }

        private void WriteHeader(FileStream fs)
        {
            byte[] header = new byte[0x2b0];
            ImageSize = header.Length;
            int t = 0;
            foreach (SectorData s in Sectors)
            {
                if (s.Sector == 0x01)
                {
                    TrackAddress[t++] = ImageSize;
                }
                ImageSize += s.GetLength();
            }

            var dc = new DataController(header);
            dc.SetCopy(0, TextEncoding.GetBytes(this.Name));
            dc.SetByte(0x1a, IsWriteProtect ? 0x10 : 0x00);
            dc.SetByte(0x1b, (int)DensityType);
            dc.SetLong(0x1c, (ulong)ImageSize);

            long a = 0;
            for (var i = 0; i < MaxTrack; i++)
            {
                a = TrackAddress[i];
                if (a == 0x00) break;
                dc.SetLong(0x20 + (i * 4), (ulong)a);
            }
            fs.Write(header, 0, header.Length);
        }

        private void WriteSectors(FileStream fs)
        {

            foreach (SectorData s in Sectors)
            {
                byte[] d = Plain2DFormat ? s.Data : s.GetBytes();
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
            if (!Plain2DFormat) ReadHeader(fs);
            ReadSectors(fs);
            fs.Close();
            return true;
        }

        string GetDiskTypeName()
        {
            switch (DensityType)
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

        void ReadHeader(FileStream fs)
        {
            byte[] header = new byte[0x2b0];
            fs.Read(header, 0, header.Length);

            var dc = new DataController(header);
            this.Name = TextEncoding.GetString(dc.Copy(0, 17)).TrimEnd((Char)0);
            IsWriteProtect = dc.GetByte(0x1a) != 0x00;
            var t = dc.GetByte(0x1b);
            DensityType = (DiskType)Enum.ToObject(typeof(DiskType), t);
            ImageSize = (long)dc.GetLong(0x1c);

            for (var i = 0; i < MaxTrack; i++)
            {
                TrackAddress[i] = (long)dc.GetLong(0x20 + (i * 4));
            }
        }

        public void DiskDescription()
        {
            Console.WriteLine("DiskName:{0}", this.Name);
            Console.Write("IsWriteProtect:{0}", IsWriteProtect ? "Yes" : "No");
            Console.Write(" DiskType:{0}", GetDiskTypeName());
            Console.WriteLine(" ImageSize:{0}", ImageSize);
            for (var i = 0; i < MaxTrack; i++)
            {
                if (TrackAddress[i] == 0x0) break;
                // Console.WriteLine("No:{0} Address:{1} ${1:X}", i, TrackAddress[i]);
            }
        }

        void ReadSectors(FileStream fs)
        {
            int Track = 0;
            int SectorCount = 1;
            var Address = Plain2DFormat ? 0x00 : TrackAddress[Track];
            if (!Plain2DFormat && Address == 0x00) return;
            fs.Seek(Address, SeekOrigin.Begin);
            while (true)
            {
                Address = fs.Position;
                var Sector = new SectorData();
                if (!Sector.Read(Plain2DFormat, fs)) break;
                if (!Plain2DFormat) SectorCount = Sector.Sector;
                if (SectorCount == 0x01)
                {
                    if (Plain2DFormat) TrackAddress[Track] = Address;
                    if (Verbose) Console.WriteLine("Track:{0} Pos:{1:X} Address:{2:X}", Track, Address, TrackAddress[Track]);
                    Track++;
                }
                Sectors.Add(Sector);
                SectorCount++;
                if (SectorCount > TrackPerSector) SectorCount = 1;
            }
        }
    }
}
