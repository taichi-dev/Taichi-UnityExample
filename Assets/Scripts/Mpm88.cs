using System.Collections.Generic;
using System.Linq;
using Taichi;
using UnityEngine;
using UnityEngine.Rendering;

public class Mpm88 : MonoBehaviour {
    const int NPARTICLE = 8192;
    const int NGRID = 128;

    public AotModuleAsset Mpm88Module;

    public ComputeGraph _Compute_Graph_g_init;
    public ComputeGraph _Compute_Graph_g_update;

    public Material ParticleMaterial;

    private NdArray<float> x;
    private NdArray<float> v;
    private NdArray<float> C;
    private NdArray<float> J;
    private NdArray<float> grid_v;
    private NdArray<float> grid_m;

    private MeshFilter _MeshFilter;
    private Mesh _Mesh;

    void Start() {
        var cgraphs = Mpm88Module.GetAllComputeGrpahs().ToDictionary(x => x.Name);
        _Compute_Graph_g_init = cgraphs["init"];
        _Compute_Graph_g_update = cgraphs["update"];

        x = new NdArrayBuilder<float>().ElemShape(2).Shape(NPARTICLE).HostRead().Build();
        v = new NdArrayBuilder<float>().ElemShape(2).Shape(NPARTICLE).Build();
        C = new NdArrayBuilder<float>().ElemShape(2, 2).Shape(NPARTICLE).Build();
        J = new NdArrayBuilder<float>().ElemShape().Shape(NPARTICLE).Build();
        grid_v = new NdArrayBuilder<float>().ElemShape(2).Shape(NGRID, NGRID).Build();
        grid_m = new NdArrayBuilder<float>().Shape(NGRID, NGRID).Build();

        _MeshFilter = GetComponent<MeshFilter>();
        _Mesh = new Mesh();

        var layout = new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 2, stream: 0),
        };
        _Mesh.SetVertexBufferParams(NPARTICLE, layout);
        _Mesh.SetIndexBufferParams(NPARTICLE, IndexFormat.UInt32);
        var idxs = new uint[NPARTICLE];
        for (int i = 0; i < NPARTICLE; ++i) {
            idxs[i] = (uint)i;
        }
        _Mesh.SetIndexBufferData(idxs, 0, 0, idxs.Length);
        _Mesh.SetSubMesh(0, new SubMeshDescriptor(0, NPARTICLE, MeshTopology.Points), MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds);
        _Mesh.UploadMeshData(false);
        //_Mesh.bounds = new Bounds(new Vector3(0.5f, 0.5f, 0.0f), new Vector3(1.0f, 1.0f, 1e-5f));
        _MeshFilter.mesh = _Mesh;

        _Compute_Graph_g_init.LaunchAsync(new Dictionary<string, object> {
            { "x", x },
            { "v", v },
            { "J", J },
        });
    }

    void Update() {
        _Compute_Graph_g_update.LaunchAsync(new Dictionary<string, object> {
            { "x", x  },
            { "v", v },
            { "C", C },
            { "J", J },
            { "grid_v", grid_v },
            { "grid_m", grid_m },
        });

        x.CopyToNativeBufferAsync(_Mesh.GetNativeVertexBufferPtr(0));
        Runtime.Submit();
    }
}
