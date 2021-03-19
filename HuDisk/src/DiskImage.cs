using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Disk {


    public class DiskImage {

        const int DefaultHeaderSize = 0x2b0;

        const int DefaultSectorSize = 256;

        const int MaxTrack = 164;

        public int TrackPerSector = 0;


        public string DiskName;
        public bool IsWriteProtect;

        public DiskType DiskType { get; }

        public long ImageSize;
        private readonly string ImageFile;

        protected long[] TrackAddress = new long[MaxTrack];

        protected long CurrentHeaderSize = 0;


        protected List<SectorData> Sectors = new List<SectorData>();

        public bool Verbose;

        public bool PlainFormat = false;

        public string EntryName;

        public Encoding TextEncoding { get; }

        public DiskImage(Context Context) {
            TextEncoding = Context.TextEncoding;
            this.ImageFile = Context.ImageFile;
            Verbose = false;
            DiskName = "";
            IsWriteProtect = false;
            DiskType = Context.DiskType;

        }


        public DataController GetDataControllerForWrite(int Sector) {
            Sectors[Sector].IsDirty = true;
            return new DataController(Sectors[Sector].Data);
        }
        public DataController GetDataControllerForRead(int Sector) {
            return new DataController(Sectors[Sector].Data);
        }

        public byte[] GetSectorDataForWrite(int Sector) {
            Sectors[Sector].IsDirty = true;
            return Sectors[Sector].Data;
        }

        public SectorData GetSector(int Sector) => Sectors[Sector];

        public void Format() {
            var tf = DiskType.CurrentTrackFormat;
            TrackPerSector = tf.TrackPerSector;
            TrackFormat(tf.TrackMax, tf.TrackPerSector);
            CurrentHeaderSize = 0;
        }

        private void TrackFormat(int TrackMax, int TrackPerSector) {
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



        public void WriteDisk() {
            var fs = new FileStream(ImageFile,
            FileMode.OpenOrCreate,
            FileAccess.Write,
            FileShare.ReadWrite);

            var RebuildImage = IsRewriteImage();
            if (Verbose) Console.WriteLine("RebuildImage:" + RebuildImage.ToString());
            if (!PlainFormat) {
                if (RebuildImage) WriteHeader(fs);
            }
            WriteSectors(fs, RebuildImage);
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

        private void WriteHeader(FileStream fs) {
            byte[] header = new byte[DefaultHeaderSize];
            ImageSize = header.Length;
            int t = 0;
            foreach (SectorData s in Sectors) {
                if (s.Sector == 0x01) TrackAddress[t++] = ImageSize;
                ImageSize += s.GetLength();
            }

            var dc = new DataController(header);
            dc.SetCopy(0, TextEncoding.GetBytes(this.DiskName), 0x10);
            dc.SetByte(0x1a, IsWriteProtect ? 0x10 : 0x00);
            dc.SetByte(0x1b, DiskType.ImageTypeByte);
            dc.SetLong(0x1c, (ulong)ImageSize);

            // トラック分のアドレスを出力する
            for (var i = 0; i < MaxTrack; i++) {
                var a = TrackAddress[i];
                if (a == 0x00) break;
                dc.SetLong(0x20 + (i * 4), (ulong)a);
            }
            fs.Write(header, 0, header.Length);
        }

        private void WriteSectors(FileStream fs, bool isRebuild) {
            var Length = fs.Length;
            var Position = TrackAddress[0];
            var Skip = true;

            foreach (SectorData s in Sectors) {
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

        public bool Read() {
            if (!File.Exists(ImageFile)) return false;
            var fs = new FileStream(ImageFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (!PlainFormat) ReadHeader(fs);
            TrackPerSector = DiskType.GetTrackPerSector();
            ReadSectors(fs);

            fs.Close();
            return true;
        }

        void ReadHeader(FileStream fs) {
            byte[] header = new byte[0x2b0];
            fs.Read(header, 0, header.Length);

            var dc = new DataController(header);
            this.DiskName = TextEncoding.GetString(dc.Copy(0, 17)).TrimEnd((Char)0);
            IsWriteProtect = dc.GetByte(0x1a) != 0x00;
            var t = dc.GetByte(0x1b);
            DiskType.SetImageTypeFromHeader(t);
            ImageSize = (long)dc.GetLong(0x1c);

            CurrentHeaderSize = 0;

            for (var i = 0; i < MaxTrack; i++) {
                var a = (long)dc.GetLong(0x20 + (i * 4));
                TrackAddress[i] = a;
                if (i == 0) CurrentHeaderSize = a;
            }
        }

        public void DiskDescription() {
            Console.WriteLine("DiskName:{0}", this.DiskName);
            Console.Write("IsWriteProtect:{0}", IsWriteProtect ? "Yes" : "No");
            Console.Write(" DiskType:{0}", DiskType.GetTypeName());
            Console.WriteLine(" ImageSize:{0}", ImageSize);
        }

        void ReadSectors(FileStream fs) {
            int Track = 0;
            int SectorCount = 1;
            var Address = PlainFormat ? 0x00 : TrackAddress[Track];
            if (!PlainFormat && Address == 0x00) return;
            fs.Seek(Address, SeekOrigin.Begin);

            while (true) {
                Address = fs.Position;
                var Sector = new SectorData();
                if (!Sector.Read(PlainFormat, fs)) break;
                if (!PlainFormat) {
                    SectorCount = Sector.Sector;
                }
                if (SectorCount == 0x01) {
                    if (PlainFormat) TrackAddress[Track] = Address;
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
