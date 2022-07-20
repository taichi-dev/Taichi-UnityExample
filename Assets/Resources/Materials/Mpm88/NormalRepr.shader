Shader "Unlit/NormalRepr"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        BlendOp Add
        AlphaToMask On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv: TEXCOORD0;
                float worldDepth: TEXCOORD1;
                float2 worldWidth_worldHeight : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            const float DEPTH_RANGE = 1000.0f - 0.3f;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldDepth = UnityObjectToViewPos(v.vertex).z;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv * 2.0f - 1.0f;

                float u = uv.x;
                float v = uv.y;
                float r2 = dot(uv, uv);
                float r = sqrt(r2);

                float alpha;
                float z;
                if (r < 1.0f) {
                  alpha = 1.0f;
                  //z = (-i.worldDepth);
                  z = (-i.worldDepth - sqrt(1.0f - r));
                } else {
                  alpha = 0.0f;
                  z = 0.0f;
                }

                return float4(z, 0.0f, 0.0f, alpha);
            }
            ENDCG
        }
    }
}
