using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Disk.Tests {
    public class TestHelper {

        public static string ComputeHash(string Filename) {
            var Data =File.ReadAllBytes(Filename);
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            var Result = md5.ComputeHash(Data);
            
            return string.Join("",Result.Select(x => x.ToString("x2")));
        }

        public static string ComputeHashSha1(string Filename) {
            var Data = File.ReadAllBytes(Filename);
            SHA1CryptoServiceProvider csp = new SHA1CryptoServiceProvider();
            var Result = csp.ComputeHash(Data);
            return string.Join("", Result.Select(x => x.ToString("x2")));
        }


        public static void RunPowerShell(string Option) {
            Process cmd = new Process();
            cmd.StartInfo.FileName = "PowerShell.exe";
            cmd.StartInfo.Arguments = Option;
            cmd.Start();
        }
    }
}
