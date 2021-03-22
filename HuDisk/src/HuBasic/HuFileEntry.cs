using System;
using System.IO;

namespace Disk {
    public class HuFileEntry {
        public int FileMode;
        public string Name;
        public string Extension;
        public int Password;
        public int Size;
        public int LoadAddress;
        public int ExecuteAddress;
        public int StartCluster;
        public int EntryPosition;
        public int EntrySector;

        public const int MaxNameLength = 13;
        public const int MaxExtensionLength = 3;

        public byte[] DateTimeData = new byte[6];
        internal bool IsIplEntry;

        /// <summary>
        /// ファイル名.拡張子のファイル名を得る
        /// </summary>
        /// <returns></returns>
        public string GetFilename() {
            if (Extension.Length == 0) return Name;
            return Name + "." + Extension;
        }

        public string Description() {
            string TypeText = GetTypeText();
            string LoadAddressText = LoadAddress.ToString("X4");
            string ExecuteAddressText = ExecuteAddress.ToString("X4");

            string AddressText = $"Load:{LoadAddressText,-5} Exec:{ExecuteAddressText,-5}";
            string BasicInfoText = $"{GetFilename(),-16} Type:{TypeText,4} Date:{GetDateText()} Size:{Size,-5}";

            return $"{BasicInfoText} {AddressText} Start:{StartCluster,5}";
        }

        public string GetTypeText() {
            if (IsDirectory) return "DIR";
            if (IsAscii) return "ASC";
            if (IsBinary) return "BIN";
            return "FILE";
        }

        public string GetDateText() {
            int Year = ConvertFromBCD(DateTimeData[0]);
            Year = Year < 80 ? 2000 + Year : 1900 + Year;
            int Month = (DateTimeData[1] >> 4);
            int Day = ConvertFromBCD(DateTimeData[2]);

            int Hour = ConvertFromBCD(DateTimeData[3]);
            int Min = ConvertFromBCD(DateTimeData[4]);
            int Sec = ConvertFromBCD(DateTimeData[5]);

            var r = string.Format("{0}-{1:00}-{2:00}", Year, Month, Day);
            r += string.Format(" {0:00}:{1:00}:{2:00}", Hour, Min, Sec);
            return r;
        }

        private int ConvertFromBCD(byte val) {
            int r = ((val & 0xf0) >> 4) * 10;
            r += (val & 0x0f);
            return r;
        }


        private byte ConvertToBCD(int num) {
            byte r = (byte)(num % 10);
            r |= (byte)(((num / 10) % 10) << 4);
            return r;
        }

        public void SetTime(DateTime date) {
            DateTimeData[0] = ConvertToBCD(date.Year);
            DateTimeData[1] = (byte)(((date.Month)) << 4);
            DateTimeData[1] |= (byte)((int)date.DayOfWeek & 0x0f);
            DateTimeData[2] = ConvertToBCD(date.Day);

            DateTimeData[3] = ConvertToBCD(date.Hour);
            DateTimeData[4] = ConvertToBCD(date.Minute);
            DateTimeData[5] = ConvertToBCD(date.Second);
        }

        public void SetDelete() {
            FileMode = 0x00;
        }

        public bool IsDelete() => FileMode == 0x00;

        const int DirectoryFileModeByte = 0x80;

        const int BinaryFileModeByte = 0x01;

        const int AsciiFileModeByte = 0x04;


        // ファイルモードの値 bit:機能
        // 7:ディレクトリ 6:読み出しのみ 5:ベリファイ？ 4:隠しファイル 3:不明 2:アスキー 1:BASIC 0:バイナリ
        public bool IsDirectory => (FileMode & 0x80) != 0x00;

        public bool IsAscii => (FileMode & 0x04) != 0x00;
        public bool IsBinary => (FileMode & 0x01) != 0x00;


        const int PasswordNoUseByte = 0x20;


        /// <summary>
        /// ファイルエントリの設定
        /// </summary>
        public void Set(string Filename, int Size, DateTime FileDate,int ExecuteAddress, int LoadAddress,
            bool UseBinaryFileMode = true, bool NoPassword = true) {
            SetTime(FileDate);
            FileMode = UseBinaryFileMode ? BinaryFileModeByte : AsciiFileModeByte;

            // サイズがWORDよりも大きい場合は0にする
            if (Size >= 0x10000) Size = 0x0;
            this.Size = Size;
            this.ExecuteAddress = ExecuteAddress;
            this.LoadAddress = LoadAddress;
            SetFilename(Filename);
            SetNoPassword(NoPassword);
        }



        public void SetNewDirectoryEntry(string Filename) {
            SetTime(DateTime.Now);
            FileMode = DirectoryFileModeByte;
            SetFilename(Filename);
            SetNoPassword(true);
        }

        private void SetNoPassword(bool NoPassword) {
            Password = NoPassword ? PasswordNoUseByte : 0x00;
        }

        private void SetFilename(string Filename) {
            Name = Path.GetFileNameWithoutExtension(Filename);
            Extension = Path.GetExtension(Filename);
        }

        public void SetEntryFromSector(DataController dc, int sector, int pos, string Name, string Extension) {
            this.Name = Name;
            this.Extension = Extension;
            EntrySector = sector;
            EntryPosition = pos;
            FileMode = dc.GetByte(pos);
            Password = dc.GetByte(pos + 0x11);
            Size = dc.GetWord(pos + 0x12);
            LoadAddress = dc.GetWord(pos + 0x14);
            ExecuteAddress = dc.GetWord(pos + 0x16);
            DateTimeData = dc.Copy(pos + 0x18, 6);
            StartCluster = dc.GetByte(pos + 0x1e);
            StartCluster |= dc.GetByte(pos + 0x1f) << 7;
        }
    }
}
