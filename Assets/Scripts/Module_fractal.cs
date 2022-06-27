using System.Collections.Generic;
using Taichi.Unity;

abstract class Module_fractal {
    public abstract void Apply(float t, NdArray<float> canvas);
}

class Module_fractal_kernel : Module_fractal {
    TaichiAotModule _Module;
    TaichiKernel _Kernel;

    public Module_fractal_kernel(string module_path) {
        _Module = TaichiRuntime.Singleton.LoadAotModule(module_path);
        _Kernel = _Module.GetKernel("fractal");
    }

    public override void Apply(float t, NdArray<float> canvas) {
        ArgumentListBuilder builder = new ArgumentListBuilder();
        _Kernel.Launch(builder.Add(t).Add(canvas).ToArray());
    }
}

class Module_fractal_cgraph : Module_fractal {
    TaichiAotModule _Module;
    TaichiComputeGraph _ComputeGraph;

    public Module_fractal_cgraph(string module_path) {
        _Module = TaichiRuntime.Singleton.LoadAotModule(module_path);
        _ComputeGraph = _Module.GetComputeGraph("fractal");
    }

    public override void Apply(float t, NdArray<float> canvas) {
        NamedArgumentListBuilder builder = new NamedArgumentListBuilder();
        _ComputeGraph.Launch(builder.Add("t", t).Add("canvas", canvas).ToArray());
    }

    public void OnDestroy() {
        _Module.Dispose();
    }
}
