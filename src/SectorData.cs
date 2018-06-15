using System;
using System.IO;

namespace Disk
{
    class SectorData
    {
        public int Track;
        public int Side;
        public int Sector;
        public int NumOfSector;
        public int SectorsInTrack;

        public int Density;

        public bool IsDelete;
        public int Status;
        public int DataSize;

        public byte[] Data;
        byte[] Header;

        byte FillValue;
        public bool IsDirty;
        const int DefaultSectorSize = 256;

        public SectorData()
        {
            this.FillValue = 0xe5;
        }

        public SectorData(byte FillValue)
        {
            this.FillValue = FillValue;
        }

        public void Make(int Track, int Side, int Sector, int NumOfSector, int SectorsInTrack, int Density, bool Delete, int Status, int DataSize)
        {

            Header = new byte[0x10];
            var dc = new DataController(Header);

            this.Track = Track;
            this.Side = Side;
            this.Sector = Sector;
            this.NumOfSector = NumOfSector;
            this.SectorsInTrack = SectorsInTrack;
            this.Density = Density;
            this.IsDelete = Delete;
            this.Status = Status;
            this.DataSize = DataSize;
            this.IsDirty = true;

            dc.SetByte(0, Track);
            dc.SetByte(1, Side);
            dc.SetByte(2, Sector);
            dc.SetByte(3, NumOfSector);
            dc.SetWord(4, SectorsInTrack);
            dc.SetByte(6, Density);
            dc.SetByte(7, IsDelete ? 0x10 : 0x00);
            dc.SetByte(8, Status);
            dc.SetWord(0x0e, DataSize);

            Data = new byte[DataSize];
            dc.SetBuffer(Data);
            dc.Fill(FillValue);
        }

        public byte[] GetBytes()
        {
            byte[] result = new byte[Header.Length + Data.Length];
            Header.CopyTo(result, 0);
            Data.CopyTo(result, Header.Length);
            return result;
        }

        public int GetLength()
        {
            return Header.Length + Data.Length;
        }


        public bool Read(bool IsPlain,FileStream fs)
        {
            DataSize = DefaultSectorSize;
            if (!IsPlain && !ReadSectorHeader(fs)) return false;
            Data = new byte[DataSize];
            return (fs.Read(Data, 0, DataSize) == DataSize);
        }

        public bool ReadSectorHeader(FileStream fs) {

            Header = new byte[0x10];
            int s = fs.Read(Header, 0, 0x10);
            if (s < 0x10) return false;
            var dc = new DataController(Header);
            Track = dc.GetByte(0);
            Side = dc.GetByte(1);
            Sector = dc.GetByte(2);
            NumOfSector = dc.GetByte(3);
            SectorsInTrack = dc.GetWord(4);
            Density = dc.GetByte(6);
            IsDelete = dc.GetByte(7) != 0x00 ? true : false;
            Status = dc.GetByte(8);
            DataSize = dc.GetWord(0x0e);
            return true;
        }

        public void Description()
        {
            Console.Write("C:{0} H:{1} R:{2} N:{3}", Track, Side, Sector, NumOfSector);
            Console.Write(" SectorsInTrack:{0} Density:{1}", SectorsInTrack, Density);
            Console.WriteLine(" DeleteFlag:{0} Status:{1} DataSize:{2}", IsDelete.ToString(), Status, DataSize);
        }

        public void Fill(int Value)
        {
            (new DataController(Data)).Fill(Value);
        }
    }
}
