using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;

namespace SharpMMDX.Utils
{
    /// <summary>
    /// TAGファイルからテクスチャを作成する
    /// </summary>
    public static class TargeLoader
    {
        public static Texture2D LoadFromFile(Device device, string filename)
        {
            Paloma.TargaImage tga = new Paloma.TargaImage(filename);
            System.IO.MemoryStream ds = new System.IO.MemoryStream();
            tga.Image.Save(ds,System.Drawing.Imaging.ImageFormat.Bmp);
            ds.Position = 0;
            var tex2d = Texture2D.FromStream<Texture2D>(device, ds, (int)ds.Length);
            ds.Dispose();
            return tex2d;
        }
    }
}
