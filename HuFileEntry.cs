using System;

namespace Disk
{
    class HuFileEntry
    {
        public int Mode;
        public string Name;
        public string Extension;
        public int Size;
        public int LoadAddress;
        public int ExecuteAddress;
        public int StartCluster;
        public int EntryPosition;
        public int EntrySector;

        public byte[] DateTimeData = new byte[6];

        public string GetFilename()
        {
            if (Extension.Length == 0) return Name;
            return Name + "." + Extension;
        }

        public void Description()
        {
            Console.WriteLine(
                "File:{0,-16} Date:{1} Size:{2,-5} Load:{3,-5} Exec:{4,-5} Start:{5,5}",
                GetFilename(),
                GetFileDate(),
                Size,
                LoadAddress.ToString("X4"),
                ExecuteAddress.ToString("X4"),
                StartCluster
            );
        }

        private string GetFileDate()
        {
            int Year = ConvertFromBCD(DateTimeData[0]);
            Year = Year < 80 ? 2000 + Year : 1900 + Year;
            int Month = (DateTimeData[1]>>4);
            int Day = ConvertFromBCD(DateTimeData[2]);

            int Hour = ConvertFromBCD(DateTimeData[3]);
            int Min = ConvertFromBCD(DateTimeData[4]);
            int Sec = ConvertFromBCD(DateTimeData[5]);

            var r = string.Format("{0}-{1:00}-{2:00}",Year,Month,Day);
            r += string.Format(" {0:00}:{1:00}:{2:00}",Hour,Min,Sec);
            return r;
        }

        private int ConvertFromBCD(byte val) {
            int r = ((val&0xf0)>>4)*10;
            r += (val & 0x0f);
            return r;
        }


        private byte ConvertToBCD(int num) {
            byte r = (byte)(num % 10);
            r |= (byte)(((num/10)%10) << 4);
            return r;
        }

        public void SetTime(DateTime date)
        {
            DateTimeData[0] = ConvertToBCD(date.Year);
            DateTimeData[1] = (byte)(((date.Month)) << 4);
            DateTimeData[1] |= (byte)((int)date.DayOfWeek & 0x0f);
            DateTimeData[2] = ConvertToBCD(date.Day);

            DateTimeData[3] = ConvertToBCD(date.Hour);
            DateTimeData[4] = ConvertToBCD(date.Minute);
            DateTimeData[5] = ConvertToBCD(date.Second);
        }
    }
}
