namespace Disk {
    public class TrackFormat {
        public int TrackPerSector;
        public int TrackMax;

        public void SetTrackFormat(int TrackPerSector, int TrackMax) {
            this.TrackPerSector = TrackPerSector;
            this.TrackMax = TrackMax;
        }

        const int TrackPerSector2D = 16;
        const int TrackPerSector2DD = 16;
        const int TrackPerSector2HD = 26;
        const int TrackMax2D = 80;
        const int TrackMax2DD = 160;
        const int TrackMax2HD = 154;

        public TrackFormat(DiskTypeEnum ImageType) {
            switch (ImageType) {
                case DiskTypeEnum.Disk2D:
                    SetTrackFormat(TrackPerSector2D, TrackMax2D);
                    break;
                case DiskTypeEnum.Disk2DD:
                    SetTrackFormat(TrackPerSector2DD, TrackMax2DD);
                    break;
                case DiskTypeEnum.Disk2HD:
                    SetTrackFormat(TrackPerSector2HD, TrackMax2HD);
                    break;
            }
        }

    }
}