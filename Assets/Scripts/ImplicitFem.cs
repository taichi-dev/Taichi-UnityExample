using System.Collections.Generic;
using System.IO;
using System.Linq;
using Taichi;
using UnityEngine;
using UnityEngine.Rendering;

public class ImplicitFem : MonoBehaviour {

    private MeshFilter _MeshFilter;
    private Mesh _Mesh;

    public AotModuleAsset ImplicitFemModule;
    private Kernel _Kernel_GetForce;
    private Kernel _Kernel_Init;
    private Kernel _Kernel_FloorBound;
    private Kernel _Kernel_GetMatrix;
    private Kernel _Kernel_MatMulEdge;
    private Kernel _Kernel_Add;
    private Kernel _Kernel_AddScalarNdArray;
    private Kernel _Kernel_Dot2Scalar;
    private Kernel _Kernel_GetB;
    private Kernel _Kernel_NdArrayToNdArray;
    private Kernel _Kernel_FillNdArray;
    private Kernel _Kernel_ClearField;
    private Kernel _Kernel_InitR2;
    private Kernel _Kernel_UpdateAlpha;
    private Kernel _Kernel_UpdateBetaR2;

    private ComputeGraph _Compute_Graph_g_init;
    private ComputeGraph _Compute_Graph_g_substep;

    public NdArray<float> hes_edge;
    public NdArray<float> hes_vert;
    public NdArray<float> x;
    public NdArray<float> v;
    public NdArray<float> f;
    public NdArray<float> mul_ans;
    public NdArray<int> c2e;
    public NdArray<float> b;
    public NdArray<float> r0;
    public NdArray<float> p0;
    public NdArray<int> indices;
    public NdArray<int> vertices;
    public NdArray<int> edges;
    public NdArray<float> ox;
    public NdArray<float> alpha_scalar;
    public NdArray<float> beta_scalar;
    public NdArray<float> m;
    public NdArray<float> B;
    public NdArray<float> W;
    public NdArray<float> dot_ans;
    public NdArray<float> r_2_scalar;

    List<float> LoadVector3Array(string fname) {
        List<float> rv = new List<float>();
        var asset = Resources.Load<TextAsset>($"Data/ImplicitFem/{fname}");
        using (var stream = new MemoryStream(asset.bytes))
        using (var br = new BinaryReader(stream)) {
            while (br.BaseStream.Position != br.BaseStream.Length) {
                rv.Add(br.ReadSingle());
            }
        }
        return rv;
    }
    List<int> LoadIntArray(string fname) {
        List<int> rv = new List<int>();
        var asset = Resources.Load<TextAsset>($"Data/ImplicitFem/{fname}");
        using (var stream = new MemoryStream(asset.bytes))
        using (var br = new BinaryReader(stream)) {
            while (br.BaseStream.Position != br.BaseStream.Length) {
                rv.Add(br.ReadInt32());
            }
        }
        return rv;
    }



    void Start() {
        var kernels = ImplicitFemModule.GetAllKernels().ToDictionary(x => x.Name);
        if (kernels.Count > 0) {
            _Kernel_GetForce = kernels["get_force"];
            _Kernel_Init = kernels["init"];
            _Kernel_FloorBound = kernels["floor_bound"];
            _Kernel_GetMatrix = kernels["get_matrix"];
            _Kernel_MatMulEdge = kernels["matmul_edge"];
            _Kernel_Add = kernels["add"];
            _Kernel_AddScalarNdArray = kernels["add_scalar_ndarray"];
            _Kernel_Dot2Scalar = kernels["dot2scalar"];
            _Kernel_GetB = kernels["get_b"];
            _Kernel_NdArrayToNdArray = kernels["ndarray_to_ndarray"];
            _Kernel_FillNdArray = kernels["fill_ndarray"];
            _Kernel_ClearField = kernels["clear_field"];
            _Kernel_InitR2 = kernels["init_r_2"];
            _Kernel_UpdateAlpha = kernels["update_alpha"];
            _Kernel_UpdateBetaR2 = kernels["update_beta_r_2"];
        }
        var cgraphs = ImplicitFemModule.GetAllComputeGrpahs().ToDictionary(x => x.Name);
        if (cgraphs.Count > 0) {
            _Compute_Graph_g_init = cgraphs["init"];
            _Compute_Graph_g_substep = cgraphs["substep"];
        }

        var c2eData = LoadIntArray("c2e").ToArray();
        var edgesData = LoadIntArray("edges").ToArray();
        var indicesData = LoadIntArray("indices").ToArray();
        var oxData = LoadVector3Array("ox").ToArray();
        var verticesData = LoadIntArray("vertices").ToArray();

        int vertexCount = oxData.Length / 3;
        int edgeCount = edgesData.Length / 2;
        int faceCount = indicesData.Length / 3;
        int cellCount = c2eData.Length / 6;

        // Taichi memory allocations.
        hes_edge = new NdArray<float>(edgeCount);
        hes_vert = new NdArray<float>(cellCount);
        x = new NdArrayBuilder<float>().Shape(vertexCount).ElemShape(3).Build();
        v = new NdArrayBuilder<float>().Shape(vertexCount).ElemShape(3).Build();
        f = new NdArrayBuilder<float>().Shape(vertexCount).ElemShape(3).Build();
        mul_ans = new NdArrayBuilder<float>().Shape(vertexCount).ElemShape(3).Build();
        c2e = new NdArrayBuilder<int>().Shape(cellCount).ElemShape(6).HostWrite().Build();
        b = new NdArrayBuilder<float>().Shape(vertexCount).ElemShape(3).Build();
        r0 = new NdArrayBuilder<float>().Shape(vertexCount).ElemShape(3).Build();
        p0 = new NdArrayBuilder<float>().Shape(vertexCount).ElemShape(3).Build();
        indices = new NdArrayBuilder<int>().Shape(faceCount).ElemShape(3).HostWrite().Build();
        vertices = new NdArrayBuilder<int>().Shape(cellCount).ElemShape(4).HostWrite().Build();
        edges = new NdArrayBuilder<int>().Shape(edgeCount).ElemShape(2).HostWrite().Build();
        ox = new NdArrayBuilder<float>().Shape(vertexCount).ElemShape(3).HostWrite().Build();
        alpha_scalar = new NdArray<float>();
        beta_scalar = new NdArray<float>();
        m = new NdArray<float>(vertexCount);
        B = new NdArrayBuilder<float>().Shape(cellCount).ElemShape(3, 3).Build();
        W = new NdArray<float>(cellCount);
        dot_ans = new NdArray<float>();
        r_2_scalar = new NdArray<float>();

        // Copy the input data in.
        c2e.CopyFromArray(c2eData);
        edges.CopyFromArray(edgesData);
        indices.CopyFromArray(indicesData);
        ox.CopyFromArray(oxData);
        vertices.CopyFromArray(verticesData);

        Bounds bounds = new Bounds();
        for (int i = 0; i < vertexCount; ++i) {
            bounds.Expand(oxData[i]);
        }

        // Prepare armadillo mesh in rest pose.
        _MeshFilter = GetComponent<MeshFilter>();
        _Mesh = new Mesh();
        var layout = new VertexAttributeDescriptor[] {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, stream: 0),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, stream: 1),
        };
        _Mesh.SetVertexBufferParams(oxData.Length, layout);
        _Mesh.SetVertexBufferData(oxData, 0, 0, oxData.Length, stream: 0);
        _Mesh.SetIndexBufferParams(indicesData.Length, IndexFormat.UInt32);
        _Mesh.SetIndexBufferData(indicesData, 0, 0, indicesData.Length);
        _Mesh.subMeshCount = 1;
        _Mesh.SetSubMesh(0, new SubMeshDescriptor {
            baseVertex = 0,
            bounds = bounds,
            firstVertex = 0,
            indexCount = indicesData.Length,
            indexStart = 0,
            topology = MeshTopology.Triangles,
            vertexCount = oxData.Length,
        });
        _Mesh.RecalculateNormals();
        _Mesh.MarkModified();
        _Mesh.UploadMeshData(false);
        _MeshFilter.mesh = _Mesh;

        if (_Compute_Graph_g_init != null) {
            _Compute_Graph_g_init.LaunchAsync(new Dictionary<string, object> {
                { "hes_edge", hes_edge },
                { "hes_vert", hes_vert },
                { "x", x },
                { "v", v },
                { "f", f },
                { "ox", ox },
                { "vertices", vertices },
                { "m", m },
                { "B", B },
                { "W", W },
                { "c2e", c2e },
            });
        } else {
            // Run the initialization kernels.
            _Kernel_ClearField.LaunchAsync();
            _Kernel_Init.LaunchAsync(x, v, f, ox, vertices);
            _Kernel_GetMatrix.LaunchAsync(c2e, vertices);
        }

        if (SystemInfo.supportsGyroscope) {
            Input.gyro.enabled = true;
        }
    }

    void Update() {
        float g_x, g_y, g_z;
        if (SystemInfo.supportsGyroscope) {
            g_x = Vector3.Dot(Input.gyro.gravity, Vector3.left) * 9.8f;
            g_y = Vector3.Dot(Input.gyro.gravity, Vector3.up) * 9.8f;
            g_z = 0.0f;
        } else {
            g_x = -(Input.mousePosition.x / Screen.width * 2.0f - 1.0f) * 9.8f;
            g_y = (Input.mousePosition.y / Screen.height * 2.0f - 1.0f) * 9.8f;
            g_z = 0.0f;
        }

        const float DT = 7.5e-3f;

        if (_Compute_Graph_g_substep != null) {
            _Compute_Graph_g_substep.LaunchAsync(new Dictionary<string, object> {
                { "x", x },
                { "f", f },
                { "vertices", vertices },
                { "gravity_x", g_x },
                { "gravity_y", g_y },
                { "gravity_z", g_z },
                { "m", m },
                { "B", B },
                { "W", W },
                { "b", b },
                { "v", v },
                { "mul_ans", mul_ans },
                { "edges", edges },
                { "hes_edge", hes_edge },
                { "hes_vert", hes_vert },
                { "r0", r0 },
                { "p0", p0 },
                { "dot_ans", dot_ans },
                { "r_2_scalar", r_2_scalar },
                { "alpha_scalar", alpha_scalar },
                { "beta_scalar", beta_scalar },
                { "k0", 0.0f },
                { "k1", 1.0f },
                { "k2", -1.0f },
                { "dt", DT },
            });

        } else {
            const int NUM_SUBSTEPS = 2;
            const int CG_ITERS = 8;

            for (int i = 0; i < NUM_SUBSTEPS; ++i) {
                _Kernel_GetForce.LaunchAsync(x, f, vertices, g_x, g_y, g_z);
                _Kernel_GetB.LaunchAsync(v, b, f);
                _Kernel_MatMulEdge.LaunchAsync(mul_ans, v, edges);
                _Kernel_Add.LaunchAsync(r0, b, -1.0f, mul_ans);
                _Kernel_NdArrayToNdArray.LaunchAsync(p0, r0);
                _Kernel_Dot2Scalar.LaunchAsync(r0, r0);
                _Kernel_InitR2.LaunchAsync();
                for (int j = 0; j < CG_ITERS; ++j) {
                    _Kernel_MatMulEdge.LaunchAsync(mul_ans, p0, edges);
                    _Kernel_Dot2Scalar.LaunchAsync(p0, mul_ans);
                    _Kernel_UpdateAlpha.LaunchAsync(alpha_scalar);
                    _Kernel_AddScalarNdArray.LaunchAsync(v, v, 1.0f, alpha_scalar, p0);
                    _Kernel_AddScalarNdArray.LaunchAsync(r0, r0, -1.0f, alpha_scalar, mul_ans);
                    _Kernel_Dot2Scalar.LaunchAsync(r0, r0);
                    _Kernel_UpdateBetaR2.LaunchAsync(beta_scalar);
                    _Kernel_AddScalarNdArray.LaunchAsync(p0, r0, 1.0f, beta_scalar, p0);
                }
                _Kernel_FillNdArray.LaunchAsync(f, 0.0f);
                _Kernel_Add.LaunchAsync(x, x, DT, v);
            }
            _Kernel_FloorBound.LaunchAsync(x, v);

        }

        // Copy transformed mesh to vertex buffer.
        x.CopyToNativeBufferAsync(_Mesh.GetNativeVertexBufferPtr(0));
        Runtime.Submit();
    }
}
