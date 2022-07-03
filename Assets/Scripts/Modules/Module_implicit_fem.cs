using System.Linq;
using Taichi;

class Module_implicit_fem {
    const float DT = 7.5e-3f;
    const int NUM_SUBSTEPS = 2;
    const int CG_ITERS = 8;

    Kernel _Kernel_GetForce;
    Kernel _Kernel_Init;
    Kernel _Kernel_FloorBound;
    Kernel _Kernel_GetMatrix;
    Kernel _Kernel_MatMulEdge;
    Kernel _Kernel_Add;
    Kernel _Kernel_AddScalarNdArray;
    Kernel _Kernel_Dot2Scalar;
    Kernel _Kernel_GetB;
    Kernel _Kernel_NdArrayToNdArray;
    Kernel _Kernel_FillNdArray;
    Kernel _Kernel_ClearField;
    Kernel _Kernel_InitR2;
    Kernel _Kernel_UpdateAlpha;
    Kernel _Kernel_UpdateBetaR2;

    public class Data {
        public float g_x;
        public float g_y;
        public float g_z;
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

        public Data(int vertexCount, int faceCount, int edgeCount, int cellCount) {
            g_x = 0.0f;
            g_y = -9.8f;
            g_z = 0.0f;
            x = new NdArray<float>(vertexCount, 3);
            v = new NdArray<float>(vertexCount, 3);
            f = new NdArray<float>(vertexCount, 3);
            mul_ans = new NdArray<float>(vertexCount, 3);
            c2e = new NdArray<int>(cellCount, 6);
            b = new NdArray<float>(vertexCount, 3);
            r0 = new NdArray<float>(vertexCount, 3);
            p0 = new NdArray<float>(vertexCount, 3);
            indices = new NdArray<int>(faceCount, 3);
            vertices = new NdArray<int>(cellCount, 4);
            edges = new NdArray<int>(edgeCount, 2);
            ox = new NdArray<float>(vertexCount, 3);
            alpha_scalar = new NdArray<float>();
            beta_scalar = new NdArray<float>();
        }
    };

    public Module_implicit_fem(AotModuleAsset module) {
        var kernels = module.GetAllKernels().ToDictionary(x => x.Name);
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

    public bool Initialize(Data data) {
        // Note these are `&` (logical and) and is not `&&` (fusing logical
        // and). So all the initialization states are checked and memory
        // imports are triggered.
        _Kernel_ClearField.LaunchAsync();
        _Kernel_Init.LaunchAsync(data.x, data.v, data.f, data.ox, data.vertices);
        _Kernel_GetMatrix.LaunchAsync(data.c2e, data.vertices);
        return true;
    }

    public void Apply(Data data) {
        for (int i = 0; i < NUM_SUBSTEPS; ++i) {
            _Kernel_GetForce.LaunchAsync(data.x, data.f, data.vertices, data.g_x, data.g_y, data.g_z);
            _Kernel_GetB.LaunchAsync(data.v, data.b, data.f);
            _Kernel_MatMulEdge.LaunchAsync(data.mul_ans, data.v, data.edges);
            _Kernel_Add.LaunchAsync(data.r0, data.b, -1.0f, data.mul_ans);
            _Kernel_NdArrayToNdArray.LaunchAsync(data.p0, data.r0);
            _Kernel_Dot2Scalar.LaunchAsync(data.r0, data.r0);
            _Kernel_InitR2.LaunchAsync();

            for (int j = 0; j < CG_ITERS; ++j) {
                _Kernel_MatMulEdge.LaunchAsync(data.mul_ans, data.p0, data.edges);
                _Kernel_Dot2Scalar.LaunchAsync(data.p0, data.mul_ans);
                _Kernel_UpdateAlpha.LaunchAsync(data.alpha_scalar);
                _Kernel_AddScalarNdArray.LaunchAsync(data.v, data.v, 1.0f, data.alpha_scalar, data.p0);
                _Kernel_AddScalarNdArray.LaunchAsync(data.r0, data.r0, -1.0f, data.alpha_scalar, data.mul_ans);
                _Kernel_Dot2Scalar.LaunchAsync(data.r0, data.r0);
                _Kernel_UpdateBetaR2.LaunchAsync(data.beta_scalar);
                _Kernel_AddScalarNdArray.LaunchAsync(data.p0, data.r0, 1.0f, data.beta_scalar, data.p0);
            }

            _Kernel_FillNdArray.LaunchAsync(data.f, 0.0f);
            _Kernel_Add.LaunchAsync(data.x, data.x, DT, data.v);
        }

        _Kernel_FloorBound.LaunchAsync(data.x, data.v);
    }
}
