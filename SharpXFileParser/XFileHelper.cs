using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color4D = SharpDX.Color4;
using Color3D = SharpDX.Color3;
using Vector2 = SharpDX.Vector2;
using Vector3 = SharpDX.Vector3;
using Matrix4x4 = SharpDX.Matrix;
using VectorKey = System.Collections.Generic.KeyValuePair<double,SharpDX.Vector3>;
using QuatKey = System.Collections.Generic.KeyValuePair<double,SharpDX.Quaternion>;
using MatrixKey = System.Collections.Generic.KeyValuePair<double, SharpDX.Matrix>;

namespace SharpXFileParser
{
    public struct Face
    {
        public List<uint> Indices;
    }

    public struct TexEntry
    {
        public string Name;
        public bool IsNormalMap;

        public TexEntry(string name, bool isNormalMap = false)
        {
            Name = name;
            IsNormalMap = isNormalMap;
        }
    }
    public struct Material
    {
        public string Name;
        public bool IsReference;
        public Color4D Diffuse;
        public float SpecularExponent;
        public Color3D Specular;
        public Color3D Emissive;
        public List<TexEntry> Textures;
    }
    public struct BoneWeight
    {
        public uint Vertex;
        public float Weight;
    }
    public struct Bone
    {
        public string Name;
        public List<BoneWeight> Weights;
        public Matrix4x4 OffsetMatrix;
    }

    public class Mesh
    {
        public List<Vector3> Positions;
        public List<Face> PosFaces;
        public List<Vector3> Normals;
        public List<Face> NormalFaces;
        public uint NumTextures;
        public List<Vector2>[] TexCoords;
        public uint NumColorSets;
        public List<Color4D>[] Colors;
        public List<uint> FaceMaterials;
        public List<Material> Materials;
        public List<Bone> Bones;
        public Mesh()
        {
            uint AI_MAX_NUMBER_OF_TEXTURECOORDS = 4;
            uint AI_MAX_NUMBER_OF_COLOR_SETS = 4;
            Positions = new List<Vector3>();
            PosFaces = new List<Face>();
            Normals = new List<Vector3>();
            NormalFaces = new List<Face>();
            TexCoords = new List<Vector2>[AI_MAX_NUMBER_OF_TEXTURECOORDS];
            Colors = new List<Color4D>[AI_MAX_NUMBER_OF_COLOR_SETS];
            FaceMaterials = new List<uint>();
            Materials = new List<Material>();
            Bones = new List<Bone>();
            NumTextures = 0;
            NumColorSets = 0;
        }
    }

    public class Node
    {
        public string Name;
        public Matrix4x4 TrafoMatrix;
        public Node Parent;
        public List<Node> Children = new List<Node>();
        public List<Mesh> Meshes = new List<Mesh>();
        public Node(Node parent = null)
        {
            this.Parent = parent;
        }
    }

    public struct AnimBone
    {
        public string BoneName;
        public List<VectorKey> PosKeys;
        public List<QuatKey> RotKeys;
        public List<VectorKey> ScaleKeys;
        public List<MatrixKey> TrafoKeys;
    }

    public struct Animation
    {
        public string Name;
        public List<AnimBone> Anims;
    }

    public class Scene
    {
        public Node RootNode;
        public List<Mesh> GlobalMeshes = new List<Mesh>();
        public List<Material> GlobalMaterial = new List<Material>();
        public List<Animation> Anims;
        public uint AnimTicksPerSecond;
    }
}
