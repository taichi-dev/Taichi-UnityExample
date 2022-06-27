using UnityEngine;
using Taichi.Unity;

public partial class Fractal : MonoBehaviour {
    const int WIDTH = 640;
    const int HEIGHT = 320;

    private Module_fractal _Fractal;
    private NdArray<float> _CanvasImported;

    private MeshRenderer _MeshRenderer;
    private ComputeBuffer _CanvasComputeBuffer;
    private Texture2D _Texture;
    private float[] _FractalData;
    private Color[] _FractalDataColor;

    public bool UseComputeGraph = false;

    // Start is called before the first frame update
    void Start() {
        if (UseComputeGraph) {
            _Fractal = new Module_fractal_cgraph(Application.dataPath + "/TaichiModules/fractal.cgraph");
        } else {
            _Fractal = new Module_fractal_kernel(Application.dataPath + "/TaichiModules/fractal");
        }

        _CanvasComputeBuffer = new ComputeBuffer(WIDTH * HEIGHT, sizeof(float));
        _CanvasImported = new NdArray<float>(_CanvasComputeBuffer, WIDTH, HEIGHT);

        _Texture = new Texture2D(WIDTH, HEIGHT);

        _FractalData = new float[WIDTH * HEIGHT];
        _FractalDataColor = new Color[WIDTH * HEIGHT];

        _MeshRenderer = GetComponent<MeshRenderer>();
        _MeshRenderer.material.mainTexture = _Texture;
    }

    Color GetColor() {
        var iframe = Time.frameCount;

        float pos = iframe % 100;
        float alpha = (pos < 50) ? (pos / 50.0f) : (2.0f - pos / 50.0f);

        var color = new Color();
        color.r = alpha * 0.75f;
        color.g = 0.75f;
        color.b = (1.0f - alpha) * 0.75f;
        color.a = 1.0f;
        return color;
    }

    // Update is called once per frame
    void Update() {
        // Note that we are reading data from last frame here.
        _CanvasComputeBuffer.GetData(_FractalData);
        for (int i = 0; i < _FractalData.Length; ++i) {
            var v = _FractalData[i];
            _FractalDataColor[i] = new Color(v, v, v);
        }
        _Texture.SetPixels(_FractalDataColor);
        _Texture.Apply();
        _MeshRenderer.material.color = GetColor();

        // Now launch kernels and compute graphs, but it won't be immediately
        // executed on graphics device.
        _Fractal.Apply(Time.frameCount * 0.03f, _CanvasImported);

        // Everything settled. Submit launched kernels and compute graphs to
        // the device for execution. Note that we can only submit ONCE in a
        // frame and we CANNOT wait on the device, because `Update is called
        // on the GAME THREAD yet everything for rendering is created in the
        // RENDER THREAD. `TaichiRuntime.Submit` will submit the commands in
        // the RENDER THREAD.
        TaichiRuntime.Singleton.Submit();
    }
}
