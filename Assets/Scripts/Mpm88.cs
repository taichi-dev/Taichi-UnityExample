using System.Collections.Generic;
using UnityEngine;
using Taichi;
using UnityEngine.Rendering;
using System.Linq;

public class Mpm88 : MonoBehaviour {
    private Mesh _Mesh;
    private MeshFilter _MeshFilter;

    public AotModuleAsset mpm88Module;
    private Kernel _Kernel_subsetep_reset_grid;
    private Kernel _Kernel_substep_p2g;
    private Kernel _Kernel_substep_update_grid_v;
    private Kernel _Kernel_substep_g2p;
    private Kernel _Kernel_init_particles;

    public NdArray<float> pos;
    public NdArray<float> x;
    public NdArray<float> v;
    public NdArray<float> C;
    public NdArray<float> J;
    public NdArray<float> grid_v;
    public NdArray<float> grid_m;

    private ComputeGraph _Compute_Graph_g_init;
    private ComputeGraph _Compute_Graph_g_update;

    int NParticles = 60000;

    // Start is called before the first frame update
    void Start() {
        var kernels = mpm88Module.GetAllKernels().ToDictionary(x => x.Name);
        if (kernels.Count > 0) {
            _Kernel_subsetep_reset_grid = kernels["substep_reset_grid"];
            _Kernel_substep_p2g = kernels["substep_p2g"];
            _Kernel_substep_update_grid_v = kernels["substep_update_grid_v"];
            _Kernel_substep_g2p = kernels["substep_g2p"];
            _Kernel_init_particles = kernels["init_particles"];
        }
        var cgraphs = mpm88Module.GetAllComputeGrpahs().ToDictionary(x => x.Name);
        if (cgraphs.Count > 0) {
            _Compute_Graph_g_init = cgraphs["init"];
            _Compute_Graph_g_update = cgraphs["update"];
        }
        int n_grid = 128;

        //Taichi Allocate memory,hostwrite are not considered
        pos = new NdArrayBuilder<float>().Shape(NParticles).ElemShape(3).Build();
        x = new NdArrayBuilder<float>().Shape(NParticles).ElemShape(2).Build();
        v = new NdArrayBuilder<float>().Shape(NParticles).ElemShape(2).Build();
        C = new NdArrayBuilder<float>().Shape(NParticles).ElemShape(2, 2).Build();
        J = new NdArray<float>(NParticles);
        grid_v = new NdArrayBuilder<float>().Shape(n_grid, n_grid).ElemShape(2).Build();
        grid_m = new NdArrayBuilder<float>().Shape(n_grid, n_grid).Build();

        if (_Compute_Graph_g_init != null) {
            _Compute_Graph_g_init.LaunchAsync(new Dictionary<string, object>
            {
                { "x", x },
                { "v", v },
                { "J", J },
            });
        } else {
            //kernel initialize
        }

        _MeshFilter = GetComponent<MeshFilter>();
        _Mesh = new Mesh();
        var layout = new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 0),
        };
        int[] indices = new int[NParticles];
        for (int i = 0; i < NParticles; ++i) {
            indices[i] = i;
        }
        Vector3[] vertices = new Vector3[NParticles];

        var index = indices.ToArray();
        _Mesh.vertices = vertices;
        _Mesh.SetIndices(indices, MeshTopology.Points, 0);
        _Mesh.name = "mpm88";
        _Mesh.MarkModified();
        _Mesh.UploadMeshData(false);
        _MeshFilter.mesh = _Mesh;
    }

    // Update is called once per frame
    void Update() {
        if (_Compute_Graph_g_update != null) {
            _Compute_Graph_g_update.LaunchAsync(new Dictionary<string, object>
            {
                {"v", v},
                {"grid_m",grid_m},
                {"x",x},
                {"C",C},
                {"J",J},
                {"grid_v",grid_v},
                {"pos",pos}
            });
        }
        pos.CopyToNativeBufferAsync(_Mesh.GetNativeVertexBufferPtr(0));
        Runtime.Submit();
    }
}