using System;

namespace Disk {

    public class Program {
        public static int Main(string[] args) {
            return HuDisk.Run(args) ? 0 : 1;
        }

    }

    public class HuDisk {
        public static bool Run(string[] args) {

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