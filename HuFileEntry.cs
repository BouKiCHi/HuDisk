using System;

namespace Disk
{
    class HuFileEntry
    {
        public int Mode;
        public string Name;
        public string Extension;
        public int Size;
        public int LoadAddress;
        public int ExecuteAddress;
        public int StartCluster;
        public int EntryPosition;
        public int EntrySector;

        public string GetFilename()
        {
            if (Extension.Length == 0) return Name;
            return Name + "." + Extension;
        }

        public void Description()
        {
            Console.WriteLine(
                "File:{0,-16} Size:{1,-5} Load:{2,-5} Exec:{3,-5} Start:{4,5}",
                GetFilename(),
                Size,
                LoadAddress.ToString("X4"),
                ExecuteAddress.ToString("X4"),
                StartCluster
            );
        }
    }
}
