using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace BasicSample
{
    /// <summary>
    /// TAGファイルからテクスチャを作成する
    /// </summary>
    public static class TextureUtil
    {
        public static Texture2D LoadFromTgaFile(Device device, string filename)
        {
            var tga = new Paloma.TargaImage(filename);
            return Load(device, tga.Image);
        }

        public static Texture2D LoadFromWicFile(Device device, string filename)
        {

            var image = new System.Drawing.Bitmap(filename);
            return Load(device, image);
        }

        static Texture2D Load(Device device, Bitmap image)
        {
            var boundsRect = new System.Drawing.Rectangle(0, 0, image.Width, image.Height);
            var bitmap = image.Clone(boundsRect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var mapSrc = bitmap.LockBits(boundsRect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var databox = new[] { new DataBox(mapSrc.Scan0, bitmap.Width * 4, bitmap.Height) };
            var textureDesc = new Texture2DDescription()
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Height = bitmap.Height,
                Width = bitmap.Width,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };
            var texture = new Texture2D(device, textureDesc, databox);
            bitmap.UnlockBits(mapSrc);
            return texture;
        }
    }
}
