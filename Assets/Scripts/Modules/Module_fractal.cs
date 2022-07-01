using Taichi;

abstract class Module_fractal {
    public class Data {
        public float t;
        public NdArray<float> canvas;

        public Data(int width, int height) {
            t = 0.0f;
            canvas = new NdArray<float>(new int[] { width, height }, true, false);
        }
    }

    public abstract void Apply(Data data);
}

class Module_fractal_kernel : Module_fractal {
    AotModule _Module;
    Kernel _Kernel;

    public Module_fractal_kernel(string module_path) {
        _Module = new AotModule(module_path);
        _Kernel = _Module.GetKernel("fractal");
    }

    public override void Apply(Data data) {
        var args = new ArgumentListBuilder()
            .Add(data.t)
            .Add(data.canvas)
            .ToArray();
        _Kernel.LaunchAsync(args);
    }
}

class Module_fractal_cgraph : Module_fractal {
    AotModule _Module;
    ComputeGraph _ComputeGraph;

    public Module_fractal_cgraph(string module_path) {
        _Module = new AotModule(module_path);
        _ComputeGraph = _Module.GetComputeGraph("fractal");
    }

    public override void Apply(Data data) {
        var args = new NamedArgumentListBuilder()
            .Add("t", data.t)
            .Add("canvas", data.canvas)
            .ToArray();
        _ComputeGraph.LaunchAsync(args);
    }

    public void OnDestroy() {
        _Module.Dispose();
    }
}
