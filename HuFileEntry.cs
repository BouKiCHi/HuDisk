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
                "File:{0,-16} Size:{1,-5} Load:{2,-5} Exec:{3,-5} Start:{4,5}",
                GetFilename(),
                Size,
                LoadAddress.ToString("X4"),
                ExecuteAddress.ToString("X4"),
                StartCluster
            );
        }

        private byte ConvertToBCD(int num) {
            byte r = (byte)(num % 10);
            r |= (byte)(((num/10)%10) << 4);
            return r;
        }

        public void SetTime(DateTime date)
        {
            DateTimeData[0] = ConvertToBCD(date.Year);
            DateTimeData[1] = (byte)(((date.Month)%10) << 4);
            DateTimeData[1] |= (byte)((int)date.DayOfWeek & 0x0f);
            DateTimeData[2] = ConvertToBCD(date.Day);

            DateTimeData[3] = ConvertToBCD(date.Hour);
            DateTimeData[4] = ConvertToBCD(date.Minute);
            DateTimeData[5] = ConvertToBCD(date.Second);
        }
    }
}
