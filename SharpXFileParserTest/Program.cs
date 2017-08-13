using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SharpXFileParser;

namespace SharpXFileParserTest
{
    [TestFixture]
    public class ExecutionTest
    {
        [TestCase("../../../ゲキド街v3.0/ゲキド街v3.0.x")]
        [TestCase("../../../X/anim_test.x")]
        [TestCase("../../../X/BCN_Epileptic.X")]
        [TestCase("../../../X/fromtruespace_bin32.X")]
        [TestCase("../../../X/kwxport_test_cubewithvcolors.X")]
        [TestCase("../../../X/test.x")]
        [TestCase("../../../X/test_cube_binary.x")]
        [TestCase("../../../X/test_cube_compressed.x")]
        [TestCase("../../../X/test_cube_text.x")]
        [TestCase("../../../X/Testwuson.x")]
        public void TryParse(string path)
        {
            var filename = Path.GetFullPath(path);
            var dirpath = Path.GetDirectoryName(path);
            byte[] buffer;
            using (BinaryReader br = new BinaryReader(File.OpenRead(filename)))
            {
                buffer = br.ReadBytes((int)br.BaseStream.Length);
            }
            XFileParser xfp = new XFileParser(buffer);
            Scene scene = xfp.GetImportedData();
            Mesh mesh;
            if (scene.GlobalMeshes.Count > 0)
            {
                mesh = scene.GlobalMeshes[0];
            }
            else if (scene.RootNode.Meshes.Count > 0)
            {
                mesh = scene.RootNode.Meshes[0];
            }
            else
            {
                mesh = scene.RootNode.Children[0].Meshes[0];
            }
            Assert.NotNull(mesh);
        }
        public static void Main()
        {
            ExecutionTest test = new ExecutionTest();
            test.TryParse("../../../X/test_cube_compressed.x");
        }
    }
}
