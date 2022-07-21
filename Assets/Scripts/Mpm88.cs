using System.Collections.Generic;
using System.Linq;
using Taichi;
using UnityEngine;

public class Mpm88 : MonoBehaviour {
    const int NPARTICLE = 1022;
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

    private GameObject _Prim;
    private Mesh _PrimMesh;
    private Matrix4x4[] _InstanceTransforms;

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

        _Prim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _PrimMesh = _Prim.GetComponent<MeshFilter>().mesh;
        DestroyImmediate(_Prim);
        _InstanceTransforms = new Matrix4x4[NPARTICLE];

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

        var verts = x.ToArray();
        for (int i = 0; i < NPARTICLE; ++i) {
            Vector3 offset = new Vector3(verts[i * 2 + 0], verts[i * 2 + 1], 0.0f) * 10;
            _InstanceTransforms[i] = Matrix4x4.Translate(offset) * Matrix4x4.Scale(new Vector3(0.5f, 0.5f, 0.5f));
        }

        Graphics.DrawMeshInstanced(_PrimMesh, 0, ParticleMaterial, _InstanceTransforms);

        Taichi.Runtime.Submit();
    }
}
