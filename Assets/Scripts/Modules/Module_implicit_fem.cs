using Taichi;

class Module_implicit_fem {
    const float DT = 7.5e-3f;
    const int NUM_SUBSTEPS = 2;
    const int CG_ITERS = 8;

    AotModule _Module;
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

    public Module_implicit_fem(string module_path) {
        _Module = new AotModule(module_path);
        _Kernel_GetForce = _Module.GetKernel("get_force");
        _Kernel_Init = _Module.GetKernel("init");
        _Kernel_FloorBound = _Module.GetKernel("floor_bound");
        _Kernel_GetMatrix = _Module.GetKernel("get_matrix");
        _Kernel_MatMulEdge = _Module.GetKernel("matmul_edge");
        _Kernel_Add = _Module.GetKernel("add");
        _Kernel_AddScalarNdArray = _Module.GetKernel("add_scalar_ndarray");
        _Kernel_Dot2Scalar = _Module.GetKernel("dot2scalar");
        _Kernel_GetB = _Module.GetKernel("get_b");
        _Kernel_NdArrayToNdArray = _Module.GetKernel("ndarray_to_ndarray");
        _Kernel_FillNdArray = _Module.GetKernel("fill_ndarray");
        _Kernel_ClearField = _Module.GetKernel("clear_field");
        _Kernel_InitR2 = _Module.GetKernel("init_r_2");
        _Kernel_UpdateAlpha = _Module.GetKernel("update_alpha");
        _Kernel_UpdateBetaR2 = _Module.GetKernel("update_beta_r_2");
    }

    public bool Initialize(Data data) {
        // Note these are `&` (logical and) and is not `&&` (fusing logical
        // and). So all the initialization states are checked and memory
        // imports are triggered.
        {
            var args = new ArgumentListBuilder()
                .ToArray();
            _Kernel_ClearField.LaunchAsync(args);
        }
        {
            var args = new ArgumentListBuilder()
                .Add(data.x)
                .Add(data.v)
                .Add(data.f)
                .Add(data.ox)
                .Add(data.vertices)
                .ToArray();
            _Kernel_Init.LaunchAsync(args);
        }
        {
            var args = new ArgumentListBuilder()
                .Add(data.c2e)
                .Add(data.vertices)
                .ToArray();
            _Kernel_GetMatrix.LaunchAsync(args);
        }
        return true;
    }

    public void Apply(Data data) {
        for (int i = 0; i < NUM_SUBSTEPS; ++i) {
            {
                var args = new ArgumentListBuilder()
                    .Add(data.x)
                    .Add(data.f)
                    .Add(data.vertices)
                    .Add(data.g_x)
                    .Add(data.g_y)
                    .Add(data.g_z)
                    .ToArray();
                _Kernel_GetForce.LaunchAsync(args);
            }
            {
                var args = new ArgumentListBuilder()
                    .Add(data.v)
                    .Add(data.b)
                    .Add(data.f)
                    .ToArray();
                _Kernel_GetB.LaunchAsync(args);
            }
            {
                var args = new ArgumentListBuilder()
                    .Add(data.mul_ans)
                    .Add(data.v)
                    .Add(data.edges)
                    .ToArray();
                _Kernel_MatMulEdge.LaunchAsync(args);
            }
            {
                var args = new ArgumentListBuilder()
                    .Add(data.r0)
                    .Add(data.b)
                    .Add(-1.0f)
                    .Add(data.mul_ans)
                    .ToArray();
                _Kernel_Add.LaunchAsync(args);
            }
            {
                var args = new ArgumentListBuilder()
                    .Add(data.p0)
                    .Add(data.r0)
                    .ToArray();
                _Kernel_NdArrayToNdArray.LaunchAsync(args);
            }
            {
                var args = new ArgumentListBuilder()
                    .Add(data.r0)
                    .Add(data.r0)
                    .ToArray();
                _Kernel_Dot2Scalar.LaunchAsync(args);
            }
            {
                var args = new ArgumentListBuilder()
                    .ToArray();
                _Kernel_InitR2.LaunchAsync(args);
            }

            for (int j = 0; j < CG_ITERS; ++j) {
                {
                    var args = new ArgumentListBuilder()
                        .Add(data.mul_ans)
                        .Add(data.p0)
                        .Add(data.edges)
                        .ToArray();
                    _Kernel_MatMulEdge.LaunchAsync(args);
                }
                {
                    var args = new ArgumentListBuilder()
                        .Add(data.p0)
                        .Add(data.mul_ans)
                        .ToArray();
                    _Kernel_Dot2Scalar.LaunchAsync(args);
                }
                {
                    var args = new ArgumentListBuilder()
                        .Add(data.alpha_scalar)
                        .ToArray();
                    _Kernel_UpdateAlpha.LaunchAsync(args);
                }
                {
                    var args = new ArgumentListBuilder()
                        .Add(data.v)
                        .Add(data.v)
                        .Add(1.0f)
                        .Add(data.alpha_scalar)
                        .Add(data.p0)
                        .ToArray();
                    _Kernel_AddScalarNdArray.LaunchAsync(args);
                }
                {
                    var args = new ArgumentListBuilder()
                        .Add(data.r0)
                        .Add(data.r0)
                        .Add(-1.0f)
                        .Add(data.alpha_scalar)
                        .Add(data.mul_ans)
                        .ToArray();
                    _Kernel_AddScalarNdArray.LaunchAsync(args);
                }
                {
                    var args = new ArgumentListBuilder()
                        .Add(data.r0)
                        .Add(data.r0)
                        .ToArray();
                    _Kernel_Dot2Scalar.LaunchAsync(args);
                }
                {
                    var args = new ArgumentListBuilder()
                        .Add(data.beta_scalar)
                        .ToArray();
                    _Kernel_UpdateBetaR2.LaunchAsync(args);
                }
                {
                    var args = new ArgumentListBuilder()
                        .Add(data.p0)
                        .Add(data.r0)
                        .Add(1.0f)
                        .Add(data.beta_scalar)
                        .Add(data.p0)
                        .ToArray();
                    _Kernel_AddScalarNdArray.LaunchAsync(args);
                }
            }

            {
                var args = new ArgumentListBuilder()
                    .Add(data.f)
                    .Add(0.0f)
                    .ToArray();
                _Kernel_FillNdArray.LaunchAsync(args);
            }
            {
                var args = new ArgumentListBuilder()
                    .Add(data.x)
                    .Add(data.x)
                    .Add(DT)
                    .Add(data.v)
                    .ToArray();
                _Kernel_Add.LaunchAsync(args);
            }
        }

        {
            var args = new ArgumentListBuilder()
                .Add(data.x)
                .Add(data.v)
                .ToArray();
            _Kernel_FloorBound.LaunchAsync(args);
        }
    }
}
