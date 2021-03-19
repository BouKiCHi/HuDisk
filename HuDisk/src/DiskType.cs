using System;
using System.IO;

namespace Disk {

    public enum DiskTypeEnum {
        Disk2D = 0x00,
        Disk2DD = 0x10,
        Disk2HD = 0x20,
        Unknown = 0xFF
    }

    public class DiskType {
        bool ForceType;
        public bool PlainFormat;
        private DiskTypeEnum imageType;

        public DiskTypeEnum ImageType {
            get => imageType; set {
                imageType = value;
                CurrentTrackFormat = new TrackFormat(value);
                DiskParameter = new DiskParameter(value);
            }
        }

        public TrackFormat CurrentTrackFormat;
        public DiskParameter DiskParameter;

        public DiskType() {
            ImageType = DiskTypeEnum.Disk2D;
        }

        // 
        public bool IsNot2D => ImageType == DiskTypeEnum.Disk2DD || ImageType == DiskTypeEnum.Disk2HD;

        public int ImageTypeByte {
            get {
                switch (ImageType) {
                    case DiskTypeEnum.Disk2D:
                        return 0x00;
                    case DiskTypeEnum.Disk2DD:
                        return 0x10;
                    case DiskTypeEnum.Disk2HD:
                        return 0x20;
                }
                return 0x0;
            }
        }


        public void SetDiskTypeFromOption(string value) {
            ForceType = true;
            value = value.ToUpper();
            switch (value) {
                case "2D":
                    ImageType = DiskTypeEnum.Disk2D;
                    break;
                case "2DD":
                    ImageType = DiskTypeEnum.Disk2DD;
                    break;
                case "2HD":
                    ImageType = DiskTypeEnum.Disk2HD;
                    break;
                default:
                    Console.WriteLine("Unknown DiskType!!");
                    break;
            }
        }

        public string GetTypeName() {
            switch (ImageType) {
                case DiskTypeEnum.Disk2D:
                    return "2D";
                case DiskTypeEnum.Disk2DD:
                    return "2DD";
                case DiskTypeEnum.Disk2HD:
                    return "2HD";
                default:
                    return "Unknown";
            }
        }

        public void SetTypeFromExtension(string ext) {
            if (IsNotPlainExtension(ext)) return;
            PlainFormat = true;
            var TypeFromExtenstion = ext == "2D" ? DiskTypeEnum.Disk2D : DiskTypeEnum.Disk2HD;
            if (!ForceType) ImageType = TypeFromExtenstion;
        }

        private static bool IsNotPlainExtension(string ext) => ext != "2D" && ext != "2HD";

        public int GetTrackPerSector() {            
            return CurrentTrackFormat.TrackPerSector;
        }

        public void SetImageTypeFromHeader(byte t) {
            switch(t) {
                case 0x20: // 2HD
                    ImageType = DiskTypeEnum.Disk2HD;
                    break;
                case 0x10: // 2DD
                    ImageType = DiskTypeEnum.Disk2DD;
                    break;
                case 0x00: // 2D
                default:
                    ImageType = DiskTypeEnum.Disk2D;
                    break;
            }
        }
    }
}