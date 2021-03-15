using Microsoft.VisualStudio.TestTools.UnitTesting;
using Disk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Disk.Tests {
    [TestClass()]
    public class HuDiskTests {
        [TestMethod()]
        public void NoArgumentTest() {
            var HuDisk = new HuDisk();
            Assert.IsFalse(HuDisk.Run(new string[] { }));
        }


        [TestMethod()]
        public void ExtractFileTest() {
            int[] SizeData = new[] {
                255, 256, 257, 511, 512, 513, 555, 767, 768, 769, 2048, 4096,
            };

            foreach(var s in SizeData) {
                CheckFilesize(s);
            }
        }

        private void CheckFilesize(int FileSize) {
            var ImageFilename = "sizetest.2d";
            var HuDisk = new HuDisk();
            var Filename = $"{FileSize}.bin";
            var SourceFile = GetFilepath(Filename);
            DeleteFile(ImageFilename);
            DeleteFile(Filename);
            Assert.IsTrue(HuDisk.Run(new string[] { ImageFilename, "-a", SourceFile }));
            Assert.IsTrue(HuDisk.Run(new string[] { ImageFilename, "-x", Filename }));
            var fi = new FileInfo(Filename);
            Assert.AreEqual(FileSize, fi.Length);
            CheckFileEqual(SourceFile, Filename);
        }

        private void CheckFileEqual(string sourceFile, string filename) {
            var s1 = File.ReadAllBytes(sourceFile);
            var s2 = File.ReadAllBytes(filename);
            Assert.AreEqual(s1.Length, s2.Length);
            Assert.IsTrue(s1.SequenceEqual(s2));
        }

        string GetFilepath(string Filename) {
            return "..\\..\\data\\" + Filename;
        }


        [TestMethod()]
        public void Format2DTest() {
            FormatTest("2d.d88", "e9c50ac3da824df3ac04b05bb76a6180266cae2f", "2d");
        }

        [TestMethod()]
        public void Format2DDTest() {
            FormatTest("2dd.d88", "d26abeb9569412422b8aafe2179df97130970547", "2dd");
        }

        [TestMethod()]
        public void Format2HDTest() {
            FormatTest("2hd.d88", "ed6e1a291f2e9d31c4ca9c057d20e24d68eb46b5", "2hd");
        }

        private void FormatTest(string ImageFilename, string ExpectedHash, string FormatType) {
            var HuDisk = new HuDisk();

            DeleteFile(ImageFilename);

            HuDisk.Run(new string[] { ImageFilename, "--format", "--type", FormatType });
            var ImageHash = TestHelper.ComputeHashSha1(ImageFilename);
            Console.WriteLine($"Hash:{ImageHash}");
            Assert.AreEqual(ExpectedHash, ImageHash);
        }

        private static void DeleteFile(string ImageFilename) {
            if (File.Exists(ImageFilename)) File.Delete(ImageFilename);
        }
    }
}