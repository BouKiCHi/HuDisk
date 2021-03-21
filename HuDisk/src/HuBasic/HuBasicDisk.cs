using System;
using System.Collections.Generic;
using System.Linq;

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

                case RunModeTypeEnum.Extract:
                    Log.Info("Extract files:");
                    Extract();
                    return true;

                case RunModeTypeEnum.Add:
                    Log.Info("Add files:");
                    if (!AddFile(Files, EntryName)) return false;
                    break;

                case RunModeTypeEnum.List:
                    Log.Info("List files:");
                    ListFiles();
                    break;

                case RunModeTypeEnum.Delete:
                    Log.Info("Delete files:");
                    // ファイル未指定ではすべて削除
                    DeleteFile(Files);

                    break;
            }

            DisplayFreeSpace();
            return true;
        }

        private bool AddFile(List<string> Files, string EntryName) {
            if (Files.Count == 1) {
                Log.Info("No files to add.");
                Image.WriteImage();
                return true;
            }
            return Image.AddFile(Files.Skip(1), EntryName);

        }

        private void DeleteFile(List<string> Files) {
            if (Files.Count == 1) {
                Image.DeleteAll();
            } else {
                Image.DeleteFile(Files.Skip(1).ToArray());
            }
        }

        private void Extract() {
            var Files = Context.Files;
            if (Files.Count == 1) return;

            for (var i = 1; i < Files.Count; i++) {
                var Filename = Files[i];
                Image.Extract(Filename);
            }
        }

        /// <summary>
        /// イメージ内のエントリを設定
        /// </summary>
        public bool SetEntryDirectory(string EntryDirectory) {
            if (EntryDirectory.Length == 0) return true;
            Log.Info("EntryDirectory:" + EntryDirectory);

            // パス区切りは「\」と「/」を使用できる
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
            List<HuFileEntry> Files = Image.GetEntries();
            foreach (var f in Files) {
                Log.Info(f.Description());
            }
        }

        public void DisplayFreeSpace() {
            var fc = Image.CountFreeClusters();
            var fb = Image.GetFreeBytes(fc);
            Log.Info($"Free:{fb} byte(s) / {fc} cluster(s)");
        }
    }
}
