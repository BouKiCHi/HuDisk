using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Disk {
    class HuBasicDiskImage {

        Context Context { get; }
        Log Log { get; }
        HuBasicDiskEntry DiskEntry { get; }

        Setting Setting;

        public bool IplEntry;

        public string IplModeText => IplEntry ? "IPL" : "";

        public HuBasicDiskImage(Context context) {
            this.Context = context;
            Setting = context.Setting;

            IplEntry = context.Setting.IplMode;
            Log = Context.Log;
            DiskEntry = new HuBasicDiskEntry(Context);
        }



        /// <summary>
        /// ファイル追加
        /// </summary>
        /// <param name="FilePathData">追加するファイルパス</param>
        /// <param name="EntryName">追加するエントリ名</param>
        /// <returns></returns>

        public bool AddFile(IEnumerable<string> FilePathData, string EntryName = null) {

            foreach (var s in FilePathData) {
                if (!AddFile(s, EntryName)) return false;
            }

            WriteImage();
            return true;
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
        public List<HuFileEntry> GetEntries() => DiskEntry.GetEntries();

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
        public void Extract(string Pattern) => ExtractPattern(Pattern);

        /// <summary>
        /// ファイル削除
        /// </summary>
        /// <param name="Pattern"></param>
        public void Delete(string Pattern) => DeletePattern(Pattern);

        /// <summary>
        /// ファイル削除。イメージにも書き込む。
        /// </summary>
        /// <param name="Pattern"></param>
        public void DeleteFile(IEnumerable<string> Files) {
            foreach (var Filename in Files) {
                Delete(Filename);
            }
            WriteImage();
        }

        /// <summary>
        /// ファイル削除。イメージにも書き込む。
        /// </summary>
        /// <param name="Pattern"></param>
        public void Delete(IEnumerable<HuFileEntry> EntryData) {
            foreach (var Entry in EntryData) {
                DiskEntry.Delete(Entry);
            }
            WriteImage();
        }


        /// <summary>
        /// ファイルをすべて削除。イメージにも書き込む。
        /// </summary>
        public void DeleteAll() {
            Delete("*");
            WriteImage();
        }


        /// <summary>
        /// イメージの書き出し
        /// </summary>
        public void WriteImage() {
            DiskEntry.WriteImage();
        }

        /// <summary>
        /// ファイル展開
        /// </summary>
        /// <param name="OutputFilename"></param>
        /// <param name="fe"></param>
        private void ExtractFile(string OutputFilename, HuFileEntry fe) {
            // 出力ストリーム選択
            using (Stream fs = SelectOutputStream(OutputFilename)) {
                DiskEntry.ExtractFile(fs, fe);

                //Extract(fs, fe);
                fs.Close();
            }
        }

        /// <summary>
        /// ファイルをディレクトリに展開する
        /// </summary>
        /// <param name="Directory"></param>
        /// <param name="Files"></param>
        public void ExtractToDirectory(string Directory, IEnumerable<HuFileEntry> Files) {
            // 展開
            foreach (var fe in Files) {
                var OutputName = Path.Combine(Directory, fe.GetFilename());
                ExtractFile(OutputName, fe);
            }
        }

        /// <summary>
        /// ファイルの追加
        /// </summary>
        /// <param name="FilePath">追加するファイルのパス</param>
        /// <param name="EntryName">エントリ名(設定したい場合)</param>
        /// <returns></returns>

        public bool AddFile(string FilePath, string EntryName = null) {
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
            fe.IsIplEntry = IplEntry;

            DiskEntry.WriteFileEntry(fe);

            // ファイルをIPL設定する
            if (IplEntry) {
                Log.Info($"IPL Name:{Setting.IplName}");
                IplEntry = false;
            }

            return fe;
        }

        private HuFileEntry MakeFileEntry(string EntryFilename, int Size, DateTime FileDate) {
            var fe = DiskEntry.GetWritableEntry(EntryFilename);
            fe.Set(EntryFilename, Size, FileDate, Setting.ExecuteAddress, Setting.LoadAddress);
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


        private void ExtractPattern(string Name) {
            string EntryName = Setting.EntryName;
            string EntryPattern = !string.IsNullOrEmpty(EntryName) ? EntryName : Name;

            HuFileEntry[] MatchedFiles = GetMatchedFiles(EntryPattern);

            // 展開
            foreach (var fe in MatchedFiles) {
                Log.Info(fe.Description());
                var OutputName = !string.IsNullOrEmpty(EntryName) ? Name : fe.GetFilename();
                ExtractFile(OutputName, fe);
            }
        }






        private void DeletePattern(string Name) {
            HuFileEntry[] MatchedFiles = GetMatchedFiles(Name);

            foreach (var fe in MatchedFiles) {
                Log.Info(fe.Description());
                DiskEntry.Delete(fe);
            }
        }

        /// <summary>
        /// パターンに一致したファイルエントリを取得する
        /// </summary>
        /// <param name="EntryPattern">パターン(グロブ)</param>
        /// <returns></returns>

        public HuFileEntry[] GetMatchedFiles(string EntryPattern) {

            var r = new Regex(PatternToRegex(EntryPattern), RegexOptions.IgnoreCase);
            var Files = DiskEntry.GetEntries();

            var MatchedFiles = Files.Where(x => r.IsMatch(x.GetFilename())).ToArray();
            return MatchedFiles;
        }

        private string PatternToRegex(string Pattern) {
            return "^" + Regex.Escape(Pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
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
