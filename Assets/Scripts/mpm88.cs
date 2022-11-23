using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Taichi;
using UnityEngine.Rendering;
using System.Linq;
using UnityEngine.UIElements;

public class mpm88 : MonoBehaviour
{
    const int WIDTH = 240;
    const int HEIGHT = 240;

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

    private NdArray<float> _Canvas;
    private MeshRenderer _MeshRenderer;
    private Texture2D _Texture;
    private Texture2D _Clear;
    private Color[] _mpm88Color;
    private float r = 1.0f;
    private Color ParticleColor=new Color(1.0f,0.0f,0.0f);

    // Start is called before the first frame update
    void Start()
    {
        var kernels = mpm88Module.GetAllKernels().ToDictionary(x => x.Name);
        if(kernels.Count>0)
        {
            _Kernel_subsetep_reset_grid = kernels["substep_reset_grid"];
            _Kernel_substep_p2g = kernels["substep_p2g"];
            _Kernel_substep_update_grid_v = kernels["substep_update_grid_v"];
            _Kernel_substep_g2p = kernels["substep_g2p"];
            _Kernel_init_particles = kernels["init_particles"];
        }
        var cgraphs = mpm88Module.GetAllComputeGrpahs().ToDictionary(x => x.Name);
        if(cgraphs.Count>0)
        {
            _Compute_Graph_g_init = cgraphs["init"];
            _Compute_Graph_g_update = cgraphs["update"];
        }

        _Canvas = new NdArray<float>(new int[] { WIDTH, HEIGHT }, true, false);
        _Texture = new Texture2D(WIDTH, HEIGHT);
        _Clear = new Texture2D(WIDTH, HEIGHT);
        _mpm88Color = new Color[WIDTH* HEIGHT];
        _MeshRenderer = GetComponent<MeshRenderer>();
        _MeshRenderer.material.mainTexture = _Texture;


        // int n_particles = 8192 * 5;
        int n_particles =  50;
        int n_grid = 128;
        double dt = 2e-4;
        int p_rho = 1;
        double gravity = 9.8;
        int bound = 3;
        int E = 400;

        //Taichi Allocate memory,hostwrite are not considered
        pos = new NdArrayBuilder<float>().Shape(n_particles).ElemShape(3).HostRead().Build();
        x = new NdArrayBuilder<float>().Shape(n_particles).ElemShape(2).Build();
        v = new NdArrayBuilder<float>().Shape(n_particles).ElemShape(2).Build();
        C = new NdArrayBuilder<float>().Shape(n_particles).ElemShape(2, 2).Build();
        J = new NdArray<float>(n_particles);
        grid_v = new NdArrayBuilder<float>().Shape(n_grid, n_grid).ElemShape(2).Build();
        grid_m = new NdArrayBuilder<float>().Shape(n_grid, n_grid).Build();


        if(_Compute_Graph_g_init !=null)
        {
            _Compute_Graph_g_init.LaunchAsync(new Dictionary<string, object>
            {
                { "x", x },
                { "v", v },
                { "J", J },
            });
        }
        else
        {
            //kernel initialize
        }
    }
 
    void test(ref Color[] mpm88_color,ref Vector3 temp)
    {
        for (int i = 0; i < mpm88_color.Length; ++i)
        {
                Vector2 tt = new Vector2(temp.x * WIDTH, temp.y * HEIGHT);
                Vector2 pixel = new Vector2(i%WIDTH, i/HEIGHT);
                Vector2 distance_vector = tt - pixel;
                if (distance_vector.magnitude <= r)
                {
                //Debug.Log("hello" + distance_vector.magnitude);
                    mpm88_color[i] = ParticleColor;

                }
                else
                {
                    mpm88_color[i] = new Color(1.0f,1.0f,1.0f);
                }
        }
    }
    void in_circle_or_not(ref Color[] mpm88_color,ref float[] pos)
    {
        for(int i = 0;i<mpm88_color.Length;++i)
        {
           for(int j = 0;j<pos.Length;j+=3)
           {
                Vector2 tt = new Vector2(pos[j ] * WIDTH, pos[j+1]*HEIGHT);
                Vector2 pixel = new Vector2(i % WIDTH, i / HEIGHT);
                Vector2 distance_vecor = tt- pixel; 
                if(distance_vecor.magnitude<=r)
                {
                    mpm88_color[i] = ParticleColor;
                    break;
                }
           }
            if (mpm88_color[i] == ParticleColor) continue;
            else mpm88_color[i] = Color.white;       
        }
    }

    // Update is called once per frame
    void Update()
    {
        //_Texture = _Clear;
        
        //Debug.Log(pos.Count);
        float[] temp2 = new float[pos.Count];
        pos.CopyToArray(temp2);
        
        //test(ref _mpm88Color, ref temp);
        in_circle_or_not(ref _mpm88Color, ref temp2);
        _Texture.SetPixels(_mpm88Color);
        _Texture.Apply();

        if (_Compute_Graph_g_update!=null)
        {
            _Compute_Graph_g_update.LaunchAsync(new Dictionary<string, object>
            {
                {"v", v},//have
                {"grid_m",grid_m},
                {"x",x},//have
                {"C",C},
                {"J",J},//have
                {"grid_v",grid_v},
                {"pos",pos}
            });
        }
        Runtime.Submit();
    }
}
