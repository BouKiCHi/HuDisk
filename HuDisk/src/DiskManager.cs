using System;

namespace Disk {
    public class DiskManager {
        readonly Context Context = new Context();

        public DiskManager() {}
        

        public bool Parse(string[] args) {
            
            if (!Context.Parse(args)) {
                return false;
            }
            var d = new HuBasicDiskImage(Context);

            if (Context.FormatImage) d.FormatDisk(); else d.ReadOrFormat();

            d.EditDisk();
            return true;
        }
    }
}