using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SharpDX;

namespace Sample
{
    // Use these namespaces here to override SharpDX.Direct3D11
    using SharpDX.Toolkit;
    using SharpDX.Toolkit.Graphics;
    using SharpDX.Toolkit.Input;

    /// <summary>
    /// Simple XFileDemo game using SharpDX.Toolkit.
    /// </summary>
    public class Sample : Game
    {
        private GraphicsDeviceManager graphicsDeviceManager;

        private Buffer<uint> indexBuffer;
        private Buffer<VertexPositionNormalTexture> vertexBuffer;
        private BasicEffect[] effects;
        private int[] indexCounts;

        /// <summary>
        /// Initializes a new instance of the <see cref="XFileDemo" /> class.
        /// </summary>
        public Sample()
        {
            // Creates a graphics manager. This is mandatory.
            graphicsDeviceManager = new GraphicsDeviceManager(this) { PreferredGraphicsProfile = new[] { SharpDX.Direct3D.FeatureLevel.Level_11_0 } };

            // Setup the relative directory to the executable directory
            // for loading contents with the ContentManager
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            // Modify the title of the window
            Window.Title = "XFileDemo";

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Xファイルからメッシュの読み込み
            string filename = "../../../ゲキド街v3.0/ゲキド街v3.0.x";
            filename = Path.GetFullPath(filename);
            string dirpath = Path.GetDirectoryName(filename);
            byte[] buffer;
            using (BinaryReader br = new BinaryReader(File.OpenRead(filename)))
            {
                buffer = br.ReadBytes((int)br.BaseStream.Length);
            }
            SharpXFileParser.XFileParser xfp = new SharpXFileParser.XFileParser(buffer);
            SharpXFileParser.Scene scene = xfp.GetImportedData();
            SharpXFileParser.Mesh mesh;
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

            // VertexBuffer
            List<VertexPositionNormalTexture> verts = new List<VertexPositionNormalTexture>();
            List<uint> materaiIndexs = new List<uint>();
            for (int i = 0; i < mesh.PosFaces.Count; i++)
            {
                if (mesh.PosFaces[i].Indices.Count == 3)
                {
                    // Triangle
                    for (int j = 0; j < 3; j++)
                    {
                        Vector3 pos = mesh.Positions[(int)mesh.PosFaces[i].Indices[j]];
                        Vector3 noral = mesh.Positions[(int)mesh.NormalFaces[i].Indices[j]];
                        Vector2 tex = mesh.NumTextures > 0 ? mesh.TexCoords[0][(int)mesh.PosFaces[i].Indices[j]] : Vector2.Zero;
                        verts.Add(new VertexPositionNormalTexture(pos, noral, tex));
                    }
                    materaiIndexs.Add(mesh.FaceMaterials[i]);
                }
                else if (mesh.PosFaces[i].Indices.Count == 4)
                {
                    // Quadrilateral
                    int[] indexLine = new int[] { 0, 1, 2, 0, 2, 3 };
                    foreach (int j in indexLine)
                    {
                        Vector3 pos = mesh.Positions[(int)mesh.PosFaces[i].Indices[j]];
                        Vector3 noral = mesh.Positions[(int)mesh.NormalFaces[i].Indices[j]];
                        Vector2 tex = mesh.NumTextures > 0 ? mesh.TexCoords[0][(int)mesh.PosFaces[i].Indices[j]] : Vector2.Zero;
                        verts.Add(new VertexPositionNormalTexture(pos, noral, tex));
                    }
                    materaiIndexs.Add(mesh.FaceMaterials[i]);
                    materaiIndexs.Add(mesh.FaceMaterials[i]);
                }
                else
                {
                    Console.Error.WriteLine("Polygon is neither triangle nor quadrilateral.");
                }
            }
            vertexBuffer = Buffer.New<VertexPositionNormalTexture>(GraphicsDevice, verts.ToArray(), BufferFlags.VertexBuffer);

            // IndexBuffer
            List<uint>[] indicesPerMaterial = new List<uint>[mesh.Materials.Count];
            for (int i = 0; i < indicesPerMaterial.Length; i++)
            {
                indicesPerMaterial[i] = new List<uint>();
            }
            for (uint i = 0; i < materaiIndexs.Count; i++)
            {
                indicesPerMaterial[(int)materaiIndexs[(int)i]].Add(i);
            }
            indexCounts = new int[indicesPerMaterial.Length];
            uint[] indexBufferSource = new uint[materaiIndexs.Count * 3];
            int indexBufferSourceIndex = 0;
            for (int i = 0; i < indicesPerMaterial.Length; i++)
            {
                for (int j = 0; j < indicesPerMaterial[i].Count; j++)
                {
                    indexBufferSource[indexBufferSourceIndex * 3 + 0] = indicesPerMaterial[i][j] * 3 + 0;
                    indexBufferSource[indexBufferSourceIndex * 3 + 1] = indicesPerMaterial[i][j] * 3 + 1;
                    indexBufferSource[indexBufferSourceIndex * 3 + 2] = indicesPerMaterial[i][j] * 3 + 2;
                    indexBufferSourceIndex++;
                }
                indexCounts[i] = (int)indicesPerMaterial[i].Count;
            }
            indexBuffer = Buffer.New<uint>(GraphicsDevice, indexBufferSource, BufferFlags.IndexBuffer);
            // Material
            var materials = new BasicEffect[mesh.Materials.Count];
            for (int i = 0; i < mesh.Materials.Count; i++)
            {
                materials[i] = new BasicEffect(GraphicsDevice);
                materials[i].DiffuseColor = mesh.Materials[i].Diffuse;
                materials[i].SpecularColor = mesh.Materials[i].Specular;
                materials[i].SpecularPower = mesh.Materials[i].SpecularExponent;
                materials[i].TextureEnabled = false;
                materials[i].VertexColorEnabled = false;
                if (mesh.Materials[i].Textures.Count > 0)
                {
                    if (Path.GetExtension(mesh.Materials[i].Textures[0].Name).ToLower() == ".tga")
                    {
                        Console.WriteLine("TAG Format is not supported in DirectX11 - " + mesh.Materials[i].Textures[0].Name);
                    }
                    if (string.IsNullOrEmpty(mesh.Materials[i].Textures[0].Name))
                    {
                        continue;
                    }
                    string comPath = Path.Combine(dirpath, mesh.Materials[i].Textures[0].Name);
                    try
                    {
                        Texture2D tex = Texture2D.Load(GraphicsDevice, comPath);
                        materials[i].Texture = tex;
                        materials[i].TextureEnabled = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Textuer fiel erro. - " + mesh.Materials[i].Textures[0].Name);
                        continue;
                    }
                }
            }
            effects = materials;

            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

        }

        protected override void Draw(GameTime gameTime)
        {
            // Use time in seconds directly
            var time = (float)gameTime.TotalGameTime.TotalSeconds;

            // Clears the screen with the Color.CornflowerBlue
            GraphicsDevice.Clear(Color.CornflowerBlue);

            var context = ((SharpDX.Direct3D11.Device)GraphicsDevice.MainDevice).ImmediateContext;

            // Prepare matrices// Prepare matrices
            var view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, this.GraphicsDevice.BackBuffer.Width / (float)this.GraphicsDevice.BackBuffer.Height, 0.1f, 100.0f);
            var viewProj = Matrix.Multiply(view, proj);
            var worldViewProj = Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f) * viewProj;

            //context.Draw(39, 0);

            //xmesh.World = Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f);
            //xmesh.View = view;
            //xmesh.Projection = proj;
            //xmesh.Draw(((SharpDX.Direct3D11.Device)GraphicsDevice).ImmediateContext);

            // モデルの描画
            var world = Matrix.Scaling(0.05f) * Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f);
            effects[0].World = world;
            effects[0].View = view;
            effects[0].Projection = proj;
            effects[0].CurrentTechnique.Passes[0].Apply();
            effects[0].EnableDefaultLighting();
            GraphicsDevice.SetVertexInputLayout(VertexInputLayout.FromBuffer(0, vertexBuffer));
            GraphicsDevice.SetVertexBuffer<VertexPositionNormalTexture>(vertexBuffer);
            GraphicsDevice.SetIndexBuffer(indexBuffer, true);
            //GraphicsDevice.DrawIndexed(PrimitiveType.TriangleList, indexBuffer.ElementCount);

            // サブメッシュごとに描画
            int startindex = 0;
            for (int i = 0; i < indexCounts.Length; i++)
            {
                effects[i].World = world;
                effects[i].View = view;
                effects[i].Projection = proj;
                effects[i].CurrentTechnique.Passes[0].Apply();
                effects[i].EnableDefaultLighting();
                GraphicsDevice.DrawIndexed(PrimitiveType.TriangleList, indexCounts[i] * 3, startindex);
                startindex += indexCounts[i] * 3;
            }

            base.Draw(gameTime);
        }
    }
}
