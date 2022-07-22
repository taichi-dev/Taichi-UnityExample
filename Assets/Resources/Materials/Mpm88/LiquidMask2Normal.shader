Shader "Unlit/LiquidMask2Normal"
{
    Properties
    {
        _LiquidMask ("Texture", 2D) = "white" {}
        _LiquidMaskInvSize ("Scene Color Size Inverse", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _LiquidMask;
            float2 _LiquidMaskInvSize;

            const float DEPTH_RANGE = 1000.0f - 0.3f;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float SampleLiquidMask(float2 uv, float2 delta) {
              float2 duv = delta * _LiquidMaskInvSize;
              float rv = tex2D(_LiquidMask, uv + duv).x;
              return saturate(rv * 10.0f > 0.5f ? 1.0f : 0.0f);
            }

            float4 frag(v2f i) : SV_Target
            {
                float o = SampleLiquidMask(i.uv, float2(0, 0));
                float e = SampleLiquidMask(i.uv, float2(2, 0));
                float n = SampleLiquidMask(i.uv, float2(0, 2));

                float deo = o - e;
                float dno = max(o - n, 0.0f);

                float x = deo;
                float y = dno;
                float2 xy = float2(x, y);
                float xyl = length(xy) + 1e-5f;
                xy = xy / (xyl);
                float z = sqrt(1.0f - dot(xy, xy));

                float3 normal = normalize(float3(x, y, z) + float3(0, 0, 1e-5f));
                return float4(normal, saturate(round(o)));
            }
            ENDCG
        }
    }
}
