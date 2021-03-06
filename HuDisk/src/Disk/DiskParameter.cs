﻿namespace Disk {
    public class DiskParameter {
        public int AllocationTableStart;
        public int EntrySectorStart;
        public int MaxCluster;
        public int ClusterPerSector;

        public void SetDiskParameter(int allocationTableStart, int entrySector, int maxCluster, int clusterPerSector) {
            AllocationTableStart = allocationTableStart;
            EntrySectorStart = entrySector;
            MaxCluster = maxCluster;
            ClusterPerSector = clusterPerSector;
        }

        public DiskParameter(DiskTypeEnum ImageType) {
            switch (ImageType) {
                case DiskTypeEnum.Disk2D:
                    SetDiskParameter(AllocationTable2DSector, EntrySectorStart2D, MaxCluster2D, ClusterPerSector2D);
                    break;
                case DiskTypeEnum.Disk2DD:
                    SetDiskParameter(AllocationTable2DDSector, EntrySectorStart2DD, MaxCluster2DD, ClusterPerSector2DD);
                    break;
                case DiskTypeEnum.Disk2HD:
                    SetDiskParameter(AllocationTable2HDSector, EntrySectorStart2HD, MaxCluster2HD, ClusterPerSector2HD);
                    break;
            }
        }

        const int AllocationTable2DSector = 14;
        const int AllocationTable2DDSector = 14;
        const int AllocationTable2HDSector = 28;

        const int EntrySectorStart2D = 16;
        const int EntrySectorStart2DD = 16;
        const int EntrySectorStart2HD = 32;

        const int MaxCluster2D = 80;
        const int MaxCluster2DD = 160;
        const int MaxCluster2HD = 250;

        const int ClusterPerSector2D = 16;
        const int ClusterPerSector2DD = 16;
        const int ClusterPerSector2HD = 16;


    }
}