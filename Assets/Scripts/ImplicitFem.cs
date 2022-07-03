using System.Collections.Generic;
using System.IO;
using Taichi;
using UnityEngine;
using UnityEngine.Rendering;

public class ImplicitFem : MonoBehaviour {
    private MeshFilter _MeshFilter;
    private Mesh _Mesh;

    public AotModuleAsset ImplicitFemModule;
    private Module_implicit_fem _Module;
    private Module_implicit_fem.Data _Data;

    List<float> LoadVector3Array(string fname) {
        List<float> rv = new List<float>();
        using (var br = new BinaryReader(File.OpenRead(Application.dataPath + $"/Data/ImplicitFem/{fname}.bin"))) {
            while (br.BaseStream.Position != br.BaseStream.Length) {
                rv.Add(br.ReadSingle());
            }
        }
        return rv;
    }
    List<int> LoadIntArray(string fname) {
        List<int> rv = new List<int>();
        using (var br = new BinaryReader(File.OpenRead(Application.dataPath + $"/Data/ImplicitFem/{fname}.bin"))) {
            while (br.BaseStream.Position != br.BaseStream.Length) {
                rv.Add(br.ReadInt32());
            }
        }
        return rv;
    }

    void Start() {
        _Module = new Module_implicit_fem(ImplicitFemModule);

        var c2e = LoadIntArray("c2e").ToArray();
        var edges = LoadIntArray("edges").ToArray();
        var indices = LoadIntArray("indices").ToArray();
        var ox = LoadVector3Array("ox").ToArray();
        var vertices = LoadIntArray("vertices").ToArray();

        int vertexCount = ox.Length / 3;
        int edgeCount = edges.Length / 2;
        int faceCount = indices.Length / 3;
        int cellCount = c2e.Length / 6;

        _Data = new Module_implicit_fem.Data(vertexCount, faceCount, edgeCount, cellCount);
        _Data.c2e.CopyFromArray(c2e);
        _Data.edges.CopyFromArray(edges);
        _Data.indices.CopyFromArray(indices);
        _Data.ox.CopyFromArray(ox);
        _Data.vertices.CopyFromArray(vertices);

        Bounds bounds = new Bounds();
        for (int i = 0; i < vertexCount; ++i) {
            bounds.Expand(ox[i]);
        }

        _MeshFilter = GetComponent<MeshFilter>();
        _Mesh = new Mesh();
        var layout = new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, stream: 1),
        };
        _Mesh.SetVertexBufferParams(ox.Length, layout);
        _Mesh.SetVertexBufferData(ox, 0, 0, ox.Length, stream: 0);
        _Mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
        _Mesh.SetIndexBufferData(indices, 0, 0, indices.Length);
        _Mesh.subMeshCount = 1;
        _Mesh.SetSubMesh(0, new SubMeshDescriptor {
            baseVertex = 0,
            bounds = bounds,
            firstVertex = 0,
            indexCount = indices.Length,
            indexStart = 0,
            topology = MeshTopology.Triangles,
            vertexCount = ox.Length,
        });
        _Mesh.RecalculateNormals();
        _Mesh.MarkModified();
        _Mesh.UploadMeshData(true);
        _MeshFilter.mesh = _Mesh;

        _Module.Initialize(_Data);
    }

    void Update() {
        _Module.Apply(_Data);
        _Data.x.CopyToNativeBufferAsync(_Mesh.GetNativeVertexBufferPtr(0));
        Runtime.Submit();
    }
}
