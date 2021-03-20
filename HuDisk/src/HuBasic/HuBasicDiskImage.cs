using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Disk {
    class HuBasicDiskImage {

        Context Context { get; }
        Log Log { get; }
        HuBasicDiskEntry DiskEntry { get; }

        Setting Setting;

        public bool SetIplEntry;

        public string IplModeText => SetIplEntry ? "IPL" : "";

        public HuBasicDiskImage(Context context) {
            this.Context = context;
            Setting = context.Setting;

            SetIplEntry = context.Setting.IplMode;
            Log = Context.Log;
            DiskEntry = new HuBasicDiskEntry(Context);
        }


        /// <summary>
        /// ファイルの追加
        /// </summary>
        /// <returns>成功でtrue</returns>
        public bool AddFile(string FilePath, string EntryFilename, int Size, DateTime FileDate) {
            var fe = AddEntry(EntryFilename, Size, FileDate);
            if (fe == null) return false;

            return WriteFileToImage(FilePath, Size, fe.StartCluster);
        }

        /// <summary>
        /// 編集終了
        /// </summary>
        public void EditEnd() {
            DiskEntry.WriteDisk();
        }

        /// <summary>
        /// 空きバイト数
        /// </summary>
        public int GetFreeBytes(int FreeCluster) => DiskEntry.GetFreeBytes(FreeCluster);

        /// <summary>
        /// 空きクラスタ数
        /// </summary>
        public int CountFreeClusters() => DiskEntry.CountFreeClusters();

        /// <summary>
        /// ファイルエントリを取得
        /// </summary>
        public List<HuFileEntry> Entries => DiskEntry.GetEntries();

        /// <summary>
        /// イメージのディレクトリを開く
        /// </summary>
        public HuBasicDiskEntry.OpenEntryResult OpenEntryDirectory(string name) => DiskEntry.OpenEntryDirectory(name);

        /// <summary>
        /// ステータスの確認
        /// </summary>
        /// <param name="Result"></param>
        /// <returns></returns>
        public bool IsOk(HuBasicDiskEntry.OpenEntryResult Result) => DiskEntry.IsOk(Result);

        /// <summary>
        /// ファイル展開(パターン)
        /// </summary>
        /// <param name="Pattern"></param>
        public void ExtractFiles(string Pattern) => PatternFiles(Pattern, true, false);

        /// <summary>
        /// ファイル削除
        /// </summary>
        /// <param name="Pattern"></param>
        public void DeleteFiles(string Pattern) => PatternFiles(Pattern, false, true);


        /// <summary>
        /// ファイル展開
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="StartCluster">開始クラスタ</param>
        /// <param name="FileSize"></param>
        public void Extract(Stream fs, int StartCluster, int FileSize) {
            DiskEntry.ExtractFile(fs, StartCluster, FileSize);
        }


        public bool AddFile(string FilePath, string EntryName) {
            if (!File.Exists(FilePath)) return false;
            var fi = new FileInfo(FilePath);

            // EntryFilename = エントリ上のファイル名
            var EntryFilename = !string.IsNullOrEmpty(EntryName) ? EntryName : Path.GetFileName(FilePath);
            var Size = (int)fi.Length;

            Log.Info($"Add:{EntryFilename} Size:{Size} {IplModeText}");


            var FileDate = File.GetLastWriteTime(FilePath);
            return AddFile(FilePath, EntryFilename, Size, FileDate);
        }




        private HuFileEntry AddEntry(string EntryFilename, int Size, DateTime FileDate) {
            HuFileEntry fe = MakeFileEntry(EntryFilename, Size, FileDate);
            if (fe == null) {
                Log.Error("ERROR:No entry space!");
                return null;
            }

            var fc = DiskEntry.GetFreeCluster(fe);
            if (fc < 0) {
                Log.Error("ERROR:No free cluster!");
                return null;
            }
            fe.StartCluster = fc;
            fe.IsIplEntry = SetIplEntry;

            DiskEntry.WriteFileEntry(fe);

            // ファイルをIPL設定する
            if (SetIplEntry) {
                Log.Info($"IPL Name:{Setting.IplName}");
                SetIplEntry = false;
            }

            return fe;
        }

        private HuFileEntry MakeFileEntry(string EntryFilename, int Size, DateTime FileDate) {
            var fe = DiskEntry.GetWritableEntry(EntryFilename);
            fe.SetFileInfo(EntryFilename, Size, FileDate, Setting.ExecuteAddress, Setting.LoadAddress);
            return fe;
        }

        private bool WriteFileToImage(string FilePath, int Size, int StartCluster) {
            bool Result;
            using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                Result = DiskEntry.WriteStream(fs, StartCluster, Size);
                fs.Close();
            }

            return Result;
        }


        private void PatternFiles(string Name, bool Extract, bool Delete) {
            string EntryName = Setting.EntryName;
            string EntryPattern = !string.IsNullOrEmpty(EntryName) ? EntryName : Name;

            var r = new Regex(PatternToRegex(EntryPattern), RegexOptions.IgnoreCase);
            var Files = DiskEntry.GetEntries();

            foreach (var fe in Files) {
                if (!r.IsMatch(fe.GetFilename())) continue;
                fe.Description();
                var OutputName = !string.IsNullOrEmpty(EntryName) ? Name : fe.GetFilename();

                // 展開
                if (Extract) {
                    ExtractFileFromCluster(OutputName, fe.StartCluster, fe.Size);
                }

                // 削除
                if (Delete) {
                    DiskEntry.DeleteFile(fe);
                }
            }
        }

        private string PatternToRegex(string Pattern) {
            return "^" + Regex.Escape(Pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
        }

        private void ExtractFileFromCluster(string OutputFile, int StartCluster, int FileSize) {
            // 出力ストリーム選択
            Stream fs = SelectOutputStream(OutputFile);

            Extract(fs, StartCluster, FileSize);
            fs.Close();
        }


        private static Stream SelectOutputStream(string OutputFile) {
            Stream fs;
            if (OutputFile == "-") {
                fs = Console.OpenStandardOutput();
            } else {
                fs = new FileStream(OutputFile,
                FileMode.Create,
                FileAccess.Write,
                FileShare.ReadWrite);
            }

            return fs;
        }
    }
}
