using System;

namespace Disk {
    class Program {

        static void Main(string[] args) {
            try {
                var HuDisk = new HuDisk();
                HuDisk.Run(args);
            }
            catch (System.Exception e) {
                Console.WriteLine("Error:{0}", e.Message);
                Console.WriteLine("Trace:{0}", e.StackTrace);
            }
        }
    }

}
