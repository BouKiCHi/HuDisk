using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Text;

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

            foreach (var s in SizeData) {
                CheckFilesize(s);
            }
        }

        [TestMethod()]
        public void ExtractImageTest() {
            var ImageFilename = "test.d88";
            var HuDisk = new HuDisk();
            Assert.IsTrue(HuDisk.Run(new string[] { ImageFilename, "-l" }));
            Assert.IsTrue(HuDisk.Run(new string[] { ImageFilename, "-v","--ascii", "-x", "*" }));
        }


        [TestMethod()]
        public void FileUtilityTest() {
            const string ClusterFilename = "cluster.bin";
            const string ClusterMoveFilename = "cluster.bin.org";

            DeleteFile(ClusterFilename);
            DeleteFile(ClusterMoveFilename);


            var FileData = MakeFileData(4096, "CLUSTER HEAD","FOOT");
            File.WriteAllBytes(ClusterFilename, FileData);
            File.Move(ClusterFilename, ClusterMoveFilename);

            File.WriteAllBytes(ClusterFilename, FileData);
            var b = IsEqualFile(ClusterFilename, ClusterMoveFilename);
            Assert.IsTrue(b);


            b = IsEqualFile(ClusterFilename, FileData);
            Assert.IsTrue(b);

            // 違う
            var FileData2 = MakeFileData(4096, "CLUSTER HEAD ABC", "FOOT");
            b = IsEqualFile(ClusterFilename, FileData2);
            Assert.IsFalse(b);
        }

        [TestMethod()]
        public void AddD88ImageTest() {
            const string ImageFilename = "addimage.d88";
            DeleteFile(ImageFilename);

            // 
            AddData(ImageFilename, 78);
        }

        [TestMethod()]
        public void Add2dImageTest() {
            const string ImageFilename = "addimage.2d";
            DeleteFile(ImageFilename);

            // 
            AddData(ImageFilename, 78);
        }

        private void AddData(string ImageFilename, int MaxCluster) {
            var HuDisk = new HuDisk();
            for (var i = 0; i < MaxCluster; i++) {
                AddClusterFile(HuDisk, ImageFilename, i);
            }

            AddClusterFile(HuDisk, ImageFilename, MaxCluster, true);

            for(var i = 0; i < MaxCluster; i++) {
                string ClusterFilename = GetClusterFilename(i);
                var Data = MakeClusterData(i);

                var Result = HuDisk.Run(new string[] { ImageFilename, "-x", ClusterFilename });
                Assert.IsTrue(Result);

                IsEqualFile(ClusterFilename, Data);

                DeleteFile(ClusterFilename);
            }
        }


        private void AddClusterFile(HuDisk huDisk, string ImageFilename, int i, bool Fail = false) {
            string ClusterFilename = GetClusterFilename(i);

            WriteClusterFile(i, ClusterFilename);

            bool Result = huDisk.Run(new string[] { ImageFilename, "-a", ClusterFilename });
            if (Fail) Assert.IsFalse(Result); else Assert.IsTrue(Result);

            DeleteFile(ClusterFilename);

        }

        private static string GetClusterFilename(int i) {
            return $"c{i:D4}.bin";
        }

        private byte[] WriteClusterFile(int i, string ClusterFilename) {
            DeleteFile(ClusterFilename);
            var FileData = MakeClusterData(i);
            File.WriteAllBytes(ClusterFilename, FileData);
            return FileData;
        }

        private byte[] MakeClusterData(int i) {
            return MakeFileData(4096, $"CLUSTER HEAD C{i:D4}", "FOOT");
        }

        private byte[] MakeFileData(int Length, string Head, string Foot) {
            var Data = new byte[Length];
            var HeadBytes = Encoding.UTF8.GetBytes(Head);
            var FootBytes = Encoding.UTF8.GetBytes(Foot);

            Array.Copy(HeadBytes, 0, Data, 0, HeadBytes.Length);
            Array.Copy(FootBytes, 0, Data, Data.Length - FootBytes.Length, FootBytes.Length);

            return Data;
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
            TestEqualFile(SourceFile, Filename);

            DeleteFile(Filename);
        }

        private void TestEqualFile(string SourceFile, string Filename) {
            var t = IsEqualFile(SourceFile, Filename);
            Assert.IsTrue(t);
        }
        private bool IsEqualFile(string SourceFile, string Filename) {
            var s1 = File.ReadAllBytes(SourceFile);
            return IsEqualFile(Filename, s1);
        }

        private static bool IsEqualFile(string Filename, byte[] Data) {
            var s2 = File.ReadAllBytes(Filename);
            if (Data.Length != s2.Length) return false;
            return Data.SequenceEqual(s2);
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