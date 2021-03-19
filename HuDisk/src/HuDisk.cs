namespace Disk {

    public enum RunModeTypeEnum {
        None,
        Add,
        Extract,
        List,
        Delete
    };

    public class HuDisk {
        public bool Run(string[] args) {
            return new DiskManager().Parse(args);
        }
    }
}