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
        public void Format2DTest() {
            FormatTest("57a986d0370262ae54ea5e1e3be6a0b6","2d");
        }

        [TestMethod()]
        public void Format2DDTest() {
            FormatTest("463b60094e719bcba6c2405c2e0fcae5", "2dd");
        }

        [TestMethod()]
        public void Format2HDTest() {
            FormatTest("0beccefc479b2b01e33c27dd6a0be8c3", "2hd");
        }

        private void FormatTest(string ExpectedHash, string FormatType) {
            var HuDisk = new HuDisk();

            var ImageFilename = "image.d88";
            if (File.Exists(ImageFilename)) File.Delete(ImageFilename);

            HuDisk.Run(new string[] { ImageFilename, "--format", "--type", FormatType });
            var ImageHash = TestHelper.ComputeHash(ImageFilename);
            Console.WriteLine($"Hash:{ImageHash}");
            Assert.AreEqual(ExpectedHash, ImageHash);
        }

    }
}