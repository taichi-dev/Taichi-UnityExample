using System;
using System.Collections.Generic;
using System.IO;
using Taichi.Unity;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class ImplicitFem : MonoBehaviour {
    private MeshFilter _MeshFilter;
    private Mesh _Mesh;

    private Module_implicit_fem _Module;
    private Module_implicit_fem.Data _Data;
    private IntPtr _VertexBufferPtr;

    List<Vector3> LoadVector3Array(string fname) {
        List<Vector3> rv = new List<Vector3>();
        using (var br = new BinaryReader(File.OpenRead(Application.dataPath + $"/Data/ImplicitFem/{fname}.bin"))) {
            while (br.BaseStream.Position != br.BaseStream.Length) {
                rv.Add(new Vector3 {
                    x = br.ReadSingle(),
                    y = br.ReadSingle(),
                    z = br.ReadSingle(),
                });
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
        _Module = new Module_implicit_fem(Application.dataPath + "/TaichiModules/implicit_fem");

        var c2e = LoadIntArray("c2e");
        var edges = LoadIntArray("edges");
        var indices = LoadIntArray("indices");
        var ox = LoadVector3Array("ox");
        var vertices = LoadIntArray("vertices");

        uint vertexCount = (uint)(ox.Count);
        uint edgeCount = (uint)(edges.Count / 2);
        uint faceCount = (uint)(indices.Count / 3);
        uint cellCount = (uint)(c2e.Count / 6);

        _Data = new Module_implicit_fem.Data(vertexCount, faceCount, edgeCount, cellCount);
        _Data.c2e.ComputeBuffer.SetData(c2e);
        _Data.edges.ComputeBuffer.SetData(edges);
        _Data.indices.ComputeBuffer.SetData(indices);
        _Data.ox.ComputeBuffer.SetData(ox);
        _Data.vertices.ComputeBuffer.SetData(vertices);

        Bounds bounds = new Bounds();
        for (int i = 0; i < vertexCount; ++i) {
            bounds.Expand(ox[i]);
        }

        _MeshFilter = GetComponent<MeshFilter>();
        _Mesh = new Mesh();
        _Mesh.MarkDynamic();
        var layout = new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, stream: 1),
        };
        _Mesh.SetVertexBufferParams(ox.Count, layout);
        _Mesh.SetVertexBufferData(ox, 0, 0, ox.Count, stream: 0);
        _Mesh.SetIndexBufferParams(indices.Count, IndexFormat.UInt32);
        _Mesh.SetIndexBufferData(indices, 0, 0, indices.Count);
        _Mesh.subMeshCount = 1;
        _Mesh.SetSubMesh(0, new SubMeshDescriptor {
            baseVertex = 0,
            bounds = bounds,
            firstVertex = 0,
            indexCount = indices.Count,
            indexStart = 0,
            topology = MeshTopology.Triangles,
            vertexCount = ox.Count,
        });
        _Mesh.RecalculateNormals();
        _Mesh.MarkModified();
        _Mesh.UploadMeshData(true);
        _MeshFilter.mesh = _Mesh;

        _VertexBufferPtr = _Mesh.GetNativeVertexBufferPtr(0);
    }

    int _LastFrameNumber = 0;
    bool _Initialized = false;
    void Update() {
        if (_LastFrameNumber >= Time.frameCount) { return; }
        _LastFrameNumber = Time.frameCount;

        var x = _Data.x.ToCpu();
        var c2e = _Data.c2e.ToCpu();
        var edges = _Data.edges.ToCpu();
        var indices = _Data.indices.ToCpu();
        var ox = _Data.ox.ToCpu();
        var vertices = _Data.vertices.ToCpu();
        if (_Initialized) {
            Debug.Log("applied effect in frame " + Time.frameCount.ToString());
            _Module.Apply(_Data);
            TaichiRuntime.Singleton.CopyMemoryToNativeAsync(_Data.x.ToTiMemorySlice(), _VertexBufferPtr, 0, _Data.x.Size);
            TaichiRuntime.Singleton.Submit();
        } else {
            Debug.Log("triggerred initialization in frame " + Time.frameCount.ToString());
            if (_Module.Initialize(_Data)) {
                Debug.Log("initialized in frame " + Time.frameCount.ToString());
                _Initialized = true;
                TaichiRuntime.Singleton.Submit();
            } else {
                TaichiRuntime.Singleton.Submit();
            }
        }
    }
}
