using Taichi;

class Module_input_output{
    AotModule _Module;
    Kernel _Kernel_InputOutput;

    public class Data {
        public NdArray<float> input1;
        public NdArray<float> input2;
        public NdArray<float> output1;
        public NdArray<float> output2;

        public Data() {
            input1 = new NdArray<float>(16, 16);
            input2 = new NdArray<float>(16, 16);
            output1 = new NdArray<float>(16, 16);
            output2 = new NdArray<float>(16, 16);
        }
    };

    public Module_input_output(string module_path) {
        _Module = new AotModule(module_path);
        _Kernel_InputOutput = _Module.GetKernel("input_output");
    }

    public void Apply(Data data) {
        {
            var args = new ArgumentListBuilder()
                .Add(data.input1)
                .Add(data.input2)
                .Add(data.output1)
                .Add(data.output2)
                .ToArray();
            _Kernel_InputOutput.LaunchAsync(args);
        }
    }
}
