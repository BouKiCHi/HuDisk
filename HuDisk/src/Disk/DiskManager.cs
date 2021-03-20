using System;

namespace Disk {
    public class DiskManager {

        public static bool Parse(string[] args) {

            var Context = new Context();

            if (!Context.Parse(args)) {
                return false;
            }

            Console.WriteLine("ImageFile:{0}", Context.Setting.ImageFile);

            var d = new HuBasicDisk(Context);
            return d.Edit();
        }

    }
}