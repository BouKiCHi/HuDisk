using System.IO;

namespace Disk {
    public class Setting {
        public string IplName = "";

        public bool IplMode = false;
        public bool X1SMode = false;

        public int ExecuteAddress = 0x00;
        public int LoadAddress = 0x00;

        public bool FormatImage = false;

        public bool ForceAsciiMode;
        public bool ForceBinaryMode;


        public string EntryName = "";
        public string EntryDirectory = "";

        public DiskType DiskType = new DiskType();

        public string ImageFile;
        public string ImageExtension { get; private set; }

        public void SetImageFilename(string Filename) {
            ImageFile = Filename;
            ImageExtension = ExtractExtension(ImageFile);
            DiskType.SetTypeFromExtension(ImageExtension);
        }

        private string ExtractExtension(string Filename) {
            var ext = Path.GetExtension(Filename);
            if (ext.StartsWith(".")) ext = ext.Substring(1);
            ext = ext.ToUpper();
            return ext;
        }

        public bool SetImageType(string Value) {
            return DiskType.SetDiskTypeFromOption(Value);
        }
    }
}