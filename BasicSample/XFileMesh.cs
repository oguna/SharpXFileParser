using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using SharpXFileParser;
using System.Runtime.InteropServices;
using System.IO;

namespace BasicSample
{
    class XFileMesh : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        struct VertexPositionNormalTexture
        {
            public Vector4 Position;
            public Vector4 Normal;
            public Vector2 UV;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CBuffer
        {
            public Matrix WorldViewProj;
            public Vector4 Ambient;
            public Vector4 LocalLightDirection;
            public Vector4 Diffuse;
        }

        struct Submesh
        {
            public Color4 Diffuse;
            public float SpecularExponent;
            public Color3 Specular;
            public Color3 Emissive;
            public Texture2D Texture;
            public ShaderResourceView TextureView;
        }
        string dirpath;
        VertexShader vertexShader;
        PixelShader pixelShader;
        PixelShader texPixelShader;
        SamplerState sampler;
        InputLayout layout;
        Buffer vertexBuffer;
        Buffer indexBuffer;
        Buffer contantBuffer;
        Device device;
        Submesh[] submeshes;
        int[] indexCounts;
        int triangleCount;

        // シーン
        public Matrix World;
        public Matrix View;
        public Matrix Projection;

        public XFileMesh(Device device)
        {
            this.device = device;
        }

        public void Load(string filename)
        {
            // Xファイルからメッシュの読み込み
            filename = Path.GetFullPath(filename);
            dirpath = Path.GetDirectoryName(filename);
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
            triangleCount = 0;
            for (int i = 0; i < mesh.PosFaces.Count; i++)
            {
                if (mesh.PosFaces[i].Indices.Count == 3)
                {
                    // Triangle
                    for (int j = 0; j < 3; j++)
                    {
                        Vector3 pos = mesh.Positions[(int)mesh.PosFaces[i].Indices[j]];
                        Vector3 noral = mesh.Normals[(int)mesh.NormalFaces[i].Indices[j]];
                        noral.Normalize();
                        Vector2 tex = mesh.NumTextures > 0 ? mesh.TexCoords[0][(int)mesh.PosFaces[i].Indices[j]] : Vector2.Zero;
                        var vert = new VertexPositionNormalTexture()
                        {
                            Position = new Vector4(pos,1),
                            Normal = new Vector4(noral, 1),
                            UV = tex
                        };
                        verts.Add(vert);
                    }
                    materaiIndexs.Add(mesh.FaceMaterials[i]);
                    triangleCount++;
                }
                else if (mesh.PosFaces[i].Indices.Count == 4)
                {
                    // Quadrilateral
                    int[] indexLine = new int[] { 0, 1, 2, 0, 2, 3 };
                    foreach (int j in indexLine)
                    {
                        Vector3 pos = mesh.Positions[(int)mesh.PosFaces[i].Indices[j]];
                        Vector3 noral = mesh.Normals[(int)mesh.NormalFaces[i].Indices[j]];
                        Vector2 tex = mesh.NumTextures > 0 ? mesh.TexCoords[0][(int)mesh.PosFaces[i].Indices[j]] : Vector2.Zero;
                        var vert = new VertexPositionNormalTexture()
                        {
                            Position = new Vector4(pos, 1),
                            Normal = new Vector4(noral, 1),
                            UV = tex
                        };
                        verts.Add(vert);
                    }
                    materaiIndexs.Add(mesh.FaceMaterials[i]);
                    materaiIndexs.Add(mesh.FaceMaterials[i]);
                    triangleCount += 2;
                }
                else
                {
                    Console.Error.WriteLine("Polygon is neither triangle nor quadrilateral.");
                }
            }
            vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, verts.ToArray());

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
            indexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, indexBufferSource.ToArray());
        
            // Materials
            submeshes = new Submesh[mesh.Materials.Count];
            for(int i=0; i<mesh.Materials.Count; i++)
            {
                Submesh submesh = new Submesh()
                {
                    Diffuse = mesh.Materials[i].Diffuse,
                    SpecularExponent = mesh.Materials[i].SpecularExponent,
                    Specular = mesh.Materials[i].Specular,
                    Emissive = mesh.Materials[i].Emissive
                };
                // Tex
                if (mesh.Materials[i].Textures.Count > 0)
                {
                    if (!String.IsNullOrWhiteSpace (mesh.Materials[i].Textures[0].Name))
                    {
                        string comPath = Path.Combine(dirpath, mesh.Materials[i].Textures[0].Name);
                        if (File.Exists(comPath))
                        {
                            try
                            {
                                Texture2D tex = Texture2D.FromFile<Texture2D>(device, comPath);
                                submesh.Texture = tex;
                                submesh.TextureView = new ShaderResourceView(device, tex);
                            }
                            catch (Exception e)
                            {
                                if (Path.GetExtension(comPath).ToUpper() == ".TGA")
                                {
                                    submesh.Texture = TargeLoader.LoadFromFile(device, comPath);
                                    submesh.TextureView = new ShaderResourceView(device, submesh.Texture);
                                }
                            }
                        }
                    }
                }
                submeshes[i] = submesh;
            }

            // Create Sampler
            sampler = new SamplerState(device, new SamplerStateDescription()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                BorderColor = Color.Black,
                ComparisonFunction = Comparison.Never,
                MaximumAnisotropy = 16,
                MipLodBias = 0,
                MinimumLod = 0,
                MaximumLod = 16,
            });

            // Constant Buffer
            var size = Utilities.SizeOf<CBuffer>();
            contantBuffer = new Buffer(device, size, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            // Compile Vertex and Pixel shaders
            var vertexShaderByteCode = ShaderBytecode.CompileFromFile("Lambert.fx", "VS", "vs_4_0");
            vertexShader = new VertexShader(device, vertexShaderByteCode);

            var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Lambert.fx", "PS", "ps_4_0");
            pixelShader = new PixelShader(device, pixelShaderByteCode);

            var texPixelShaderByteCode = ShaderBytecode.CompileFromFile("Lambert.fx", "PSTex", "ps_4_0");
            texPixelShader = new PixelShader(device, texPixelShaderByteCode);

            var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
            // Layout from VertexShader input signature
            layout = new InputLayout(device, signature, new[]
                    {
                        new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0),
                        new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0),
                        new InputElement("TEXCOORD", 0, Format.R32G32_Float, 32, 0)
                    });
        }

        public void Draw(DeviceContext context)
        {
            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<VertexPositionNormalTexture>(), 0));
            context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
            context.VertexShader.Set(vertexShader);
            context.VertexShader.SetConstantBuffer(0, contantBuffer);
            context.PixelShader.Set(pixelShader);
            context.PixelShader.SetConstantBuffer(0, contantBuffer);

            // Update WorldViewProj Matrix
            var worldViewProj = World * View * Projection;
            worldViewProj.Transpose();

            int startindex = 0;
            for(int i=0; i<indexCounts.Length; i++)
            {
                CBuffer cbuffer = new CBuffer()
                {
                    WorldViewProj = worldViewProj,
                    Ambient = new Color(0.2f, 0.2f, 0.2f, 1f).ToVector4(),
                    LocalLightDirection =  -new Vector4(Vector3.Normalize( new Vector3(1,2,3)),0),
                    Diffuse = submeshes[i].Diffuse.ToVector4(),
                };
                context.UpdateSubresource(ref cbuffer, contantBuffer);
                if (submeshes[i].TextureView == null)
                {
                    context.PixelShader.Set(pixelShader);
                }
                else
                {
                    context.PixelShader.Set(texPixelShader);
                    context.PixelShader.SetSampler(0, sampler);
                    context.PixelShader.SetShaderResource(0, submeshes[i].TextureView);
                }

                context.DrawIndexed(indexCounts[i] * 3, startindex, 0);
                startindex += indexCounts[i] * 3;
            }
        }

        public void Dispose()
        {
            Utilities.Dispose(ref vertexShader);
            Utilities.Dispose(ref pixelShader);
            Utilities.Dispose(ref texPixelShader);
            Utilities.Dispose(ref sampler);
            Utilities.Dispose(ref layout);
            Utilities.Dispose(ref vertexBuffer);
            Utilities.Dispose(ref indexBuffer);
            Utilities.Dispose(ref contantBuffer);
        }
    }
}
