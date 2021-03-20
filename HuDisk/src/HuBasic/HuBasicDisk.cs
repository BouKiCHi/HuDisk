using System;
using System.Collections.Generic;

namespace Disk {

    class HuBasicDisk {

        readonly Context Context;

        public Log Log { get; }
        public HuBasicDiskImage Image { get; }

        public HuBasicDisk(Context Context) {

            this.Context = Context;
            Log = this.Context.Log;
            Image = new HuBasicDiskImage(Context);
        }



        /// <summary>
        /// 編集
        /// </summary>
        /// <returns></returns>
        /// 
        public bool Edit() {
            // イメージ内のディレクトリを設定する
            if (!SetEntryDirectory(Context.Setting.EntryDirectory)) {
                Log.Error("Directory open error!");
                return false;
            }

            var Files = Context.Files;
            var EntryName = Context.Setting.EntryName;

            switch (Context.RunMode) {
                case RunModeTypeEnum.Add:
                    Log.Info("Add files:");
                    if (Files.Count == 1) {
                        Log.Info("No files to add.");
                    } else {
                        if (!AddFile(Files, EntryName)) return false;
                    }
                    EditEnd();

                    break;
                case RunModeTypeEnum.List:
                    Log.Info("List files:");
                    ListFiles();
                    DisplayFreeSpace();
                    break;
                case RunModeTypeEnum.Extract:
                    Log.Info("Extract files:");
                    Extract();
                    break;

                case RunModeTypeEnum.Delete:
                    Log.Info("Delete files:");
                    Delete();
                    EditEnd();
                    break;
            }

            return true;
        }

        /// <summary>
        /// 編集終了
        /// </summary>
        private void EditEnd() {
            DisplayFreeSpace();
            Image.EditEnd();
        }


        /// <summary>
        /// ファイル展開
        /// </summary>
        /// <param name="Filename"></param>
        public void ExtractFile(string Filename) {
            Image.ExtractFiles(Filename);
        }

        /// <summary>
        /// イメージ内ファイル削除
        /// </summary>
        /// <param name="Filename"></param>
        public void DeleteFile(string Filename) {
            Image.DeleteFiles(Filename);
        }



        private void Extract() {
            var Files = Context.Files;
            if (Files.Count == 1) return;

            for (var i = 1; i < Files.Count; i++) {
                var Filename = Files[i];
                ExtractFile(Filename);
            }
        }


        private void Delete() {
            var Files = Context.Files;

            // ファイル未指定ではすべて削除
            if (Files.Count == 1) {
                Image.DeleteFiles("*");
                return;
            }

            for (var i = 1; i < Files.Count; i++) {
                var Filename = Files[i];
                DeleteFile(Filename);
            }
        }

        public bool AddFile(List<string> Files, string EntryName) {


            for (var i = 1; i < Files.Count; i++) {
                string s = Files[i];
                if (!AddFile(s, EntryName)) return false;
            }
            return true;
        }

        /// <summary>
        /// ファイルの追加
        /// </summary>
        /// <param name="FilePath">追加するファイルのパス</param>
        /// <param name="EntryName">エントリ名(設定したい場合)</param>
        /// <returns></returns>
        public bool AddFile(string FilePath, string EntryName = null) {
            return Image.AddFile(FilePath, EntryName);
        }

        /// <summary>
        /// ファイルエントリの取得
        /// </summary>
        /// <returns></returns>
        public List<HuFileEntry> GetFileEntry() {
            return Image.Entries;
        }


        /// <summary>
        /// イメージ内のエントリを設定
        /// </summary>
        public bool SetEntryDirectory(string EntryDirectory) {
            if (EntryDirectory.Length == 0) return true;
            Log.Info("EntryDirectory:" + EntryDirectory);
            EntryDirectory = EntryDirectory.Replace('\\', '/');
            foreach (string Name in EntryDirectory.Split('/')) {
                if (Name.Length == 0) continue;
                var Result = Image.OpenEntryDirectory(Name);
                if (!Image.IsOk(Result)) return false;
            }
            return true;
        }

        /// <summary>
        /// ファイル一覧の表示
        /// </summary>

        public void ListFiles() {
            List<HuFileEntry> Files = GetFileEntry();
            foreach (var f in Files) {
                f.Description();
            }
        }



        public void DisplayFreeSpace() {
            var fc = Image.CountFreeClusters();
            var fb = Image.GetFreeBytes(fc);
            Log.Info($"Free:{fb} byte(s) / {fc} cluster(s)");
        }
    }
}
