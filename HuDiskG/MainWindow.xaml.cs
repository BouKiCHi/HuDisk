using Disk;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HuDiskG {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {

        public string ImagePath;
        public string TitleText { get; }

        public MainWindow() {
            InitializeComponent();

            TitleText = Title;
            var ImagePath = Properties.Settings.Default.ImagePath;
            if (!string.IsNullOrEmpty(ImagePath) && File.Exists(ImagePath)) {
                OpenImage(ImagePath);
            }
            ShowExtractDirectoryMenuItem.IsChecked = Properties.Settings.Default.ShowExtractDirectory;
            ForceAsciiMenuItem.IsChecked = Properties.Settings.Default.ForceAscii;
            ForceBinaryMenuItem.IsChecked = Properties.Settings.Default.ForceBinary;
        }

        private void Window_Closed(object sender, System.EventArgs e) {
            Properties.Settings.Default.ShowExtractDirectory = ShowExtractDirectoryMenuItem.IsChecked;
            Properties.Settings.Default.ForceAscii = ForceAsciiMenuItem.IsChecked;
            Properties.Settings.Default.ForceBinary = ForceBinaryMenuItem.IsChecked;
            Properties.Settings.Default.Save();
        }


        private void NewFileMenuItem_Click(object sender, RoutedEventArgs e) {
            var d = new SaveFileDialog {
                FilterIndex = 1,
                Filter = "D88(.d88)|*.d88|2D(*.2d)|*.2d|2DD(*.2dd)|*.2dd|2HD(*.2hd)|*.2hd|All Files (*.*)|*.*"
            };
            var result = d.ShowDialog();
            if (result == null || result == false) return;
            var ImagePath = d.FileName;
            if (File.Exists(ImagePath)) {
                var r = MessageBox.Show("ファイルが存在します。\n上書きしますか？", "確認", MessageBoxButton.YesNoCancel);
                if (r != MessageBoxResult.Yes) return;
                File.Delete(ImagePath);
            }

            OpenImage(ImagePath, true);
        }



        private void OpenFileMenuItem_Click(object sender, RoutedEventArgs e) {
            var d = new OpenFileDialog {
                FilterIndex = 1,
                Filter = "D88(.d88)|*.d88|2D(*.2d)|*.2d|2DD(*.2dd)|*.2dd|2HD(*.2hd)|*.2hd|All Files (*.*)|*.*"
            };
            var result = d.ShowDialog();
            if (result == null || result == false) return;
            var ImagePath = d.FileName;

            OpenImage(ImagePath);
        }

        private void OpenImage(string ImageFilename, bool Write = false) {
            Properties.Settings.Default.ImagePath = ImageFilename;
            SetFilename(ImageFilename);
            HuBasicDiskImage DiskImage = GetDiskImage(ImageFilename);
            if (Write) DiskImage.WriteImage();
            UpdateData(DiskImage);
        }


        private void SetFilename(string ImagePath) {
            this.ImagePath = ImagePath;
            var Filename = Path.GetFileName(ImagePath);
            Title = $"{TitleText} - {Filename}";
        }

        private HuBasicDiskImage GetDiskImage(string Filename) {
            var ctx = new Context();
            ctx.SetImageFilename(Filename);
            var DiskImage = new HuBasicDiskImage(ctx);
            return DiskImage;
        }

        private HuBasicDiskImage GetDiskImage() {
            var Filename = ImagePath;
            var ctx = new Context();
            ctx.SetImageFilename(Filename);
            ctx.Setting.ForceAsciiMode = ForceAsciiMenuItem.IsChecked;
            ctx.Setting.ForceBinaryMode= ForceBinaryMenuItem.IsChecked;
            var DiskImage = new HuBasicDiskImage(ctx);
            return DiskImage;
        }

        public class ListItem {
            public HuFileEntry Entry;

            public ListItem(HuFileEntry Entry) {
                this.Entry = Entry;
            }

            public string Name { get => Entry.GetFilename(); }

            public string Type { get => Entry.GetTypeText(); }

            public string DateTime { get => Entry.GetDateText(); }

            public int Size { get => Entry.Size; }

            public string LoadAddress { get => Entry.LoadAddress.ToString("X4"); }
            public string ExecuteAddress { get => Entry.ExecuteAddress.ToString("X4"); }

            public int StartCluster { get => Entry.StartCluster; }
        }

        // 表示更新
        private void UpdateData(HuBasicDiskImage DiskImage) {
            var EntryData = DiskImage.GetEntries();
            var FreeCluster = DiskImage.CountFreeClusters();
            var FreeBytes = DiskImage.GetFreeBytes(FreeCluster);
            LabeInfo.Text = $"Free: {FreeCluster}clusters / {FreeBytes}bytes";
            EntryListView.IsEnabled = true;
            var ListData = EntryData.Select(x => new ListItem(x));
            EntryListView.DataContext = ListData;
        }


        // 展開
        private void ExtractMenuItem_Click(object sender, RoutedEventArgs e) {
            Extract();
        }

        private void Extract() {
            var d = new SaveFileDialog {
                Title = "展開先のフォルダの選択",
                FileName = "出力先"
            };
            var Result = d.ShowDialog();
            if (Result == null || Result == false) return;
            var ExtractPath = Path.GetDirectoryName(d.FileName);

            var DiskImage = GetDiskImage();
            var Items = EntryListView.SelectedItems;
            if (Items == null) return;

            var EntryData = new List<HuFileEntry>();
            foreach (ListItem o in Items) {
                EntryData.Add(o.Entry);

            }
            DiskImage.ExtractToDirectory(ExtractPath, EntryData);

            if (ShowExtractDirectoryMenuItem.IsChecked) System.Diagnostics.Process.Start(ExtractPath);

        }

        // 追加
        private void AddMenuItem_Click(object sender, RoutedEventArgs e) {
            var d = new OpenFileDialog {
                Title = "追加するファイルの選択",
                Multiselect = true,
                FilterIndex = 1,
                Filter = "All Files (*.*)|*.*"
            };
            var Result = d.ShowDialog();
            if (Result == null || Result == false) return;

            var FilenameData = d.FileNames;

            AddFile(FilenameData);
        }

        // 削除
        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e) {
            var DiskImage = GetDiskImage();
            var Items = EntryListView.SelectedItems;
            if (Items == null) return;

            var EntryData = new List<HuFileEntry>();
            foreach (ListItem o in Items) {
                EntryData.Add(o.Entry);

            }

            DiskImage.Delete(EntryData);
            UpdateData(DiskImage);
        }

        private void AddFile(string[] FilenameData) {
            var DiskImage = GetDiskImage();
            DiskImage.AddFile(FilenameData);
            UpdateData(DiskImage);
        }


        private void ExitMenuItem_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void EntryListView_PreviewDragOver(object s, DragEventArgs e) {
            var Effect = DragDropEffects.None;
            if (EntryListView.IsEnabled && e.Data.GetDataPresent(DataFormats.FileDrop)) Effect = DragDropEffects.Copy;
            e.Effects = Effect;
            e.Handled = true;
        }

        private void EntryListView_Drop(object sender, DragEventArgs e) {
            if (!EntryListView.IsEnabled || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var FilenameData = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddFile(FilenameData);
        }

        private void ForceAsciiMenuItem_Click(object sender, RoutedEventArgs e) {
            SetChech(sender);
        }

        private void ForceBinaryMenuItem_Click(object sender, RoutedEventArgs e) {
            SetChech(sender);
        }

        private void ShowExtractDirectoryMenuItem_Click(object sender, RoutedEventArgs e) {
            SetChech(sender);
        }

        private static void SetChech(object sender) {
            var mi = sender as MenuItem;
            if (mi == null) return;
            mi.IsChecked = !mi.IsChecked;
        }

        private void EntryListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            Extract();
        }
    }
}
