Shader "Unlit/NormalUpsample"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainTexInvSize ("Texture Size Inverse", Vector) = (1,1,1,1)
        _SameSizeTex ("Same Size Texture", 2D) = "white" {}
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

            sampler2D _MainTex;
            float2 _MainTexInvSize;
            sampler2D _SameSizeTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 SampleSameSizeTex(float2 uv) {
              float4 normal_mask = tex2D(_SameSizeTex, uv);
              float mask = saturate(normal_mask.w);
              float3 normal = normal_mask.xyz;
              return float4(normal, mask);
            }
            float4 SampleMainTex(float2 uv, float alpha) {
              float radian = alpha * 2 * 3.1415926f;
              float du = _MainTexInvSize.x * cos(radian);
              float dv = _MainTexInvSize.y * sin(radian);
              float4 normal_mask = tex2D(_MainTex, uv + 2 * float2(du, dv));
              float mask = normal_mask.w;
              float3 normal = normal_mask.xyz * mask;
              return float4(normal, mask);
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 uc = SampleSameSizeTex(i.uv);
                float4 dc = 0.125f * (
                  SampleMainTex(i.uv, 0 / 8.0f) +
                  SampleMainTex(i.uv, 1 / 8.0f) +
                  SampleMainTex(i.uv, 2 / 8.0f) +
                  SampleMainTex(i.uv, 3 / 8.0f) +
                  SampleMainTex(i.uv, 4 / 8.0f) +
                  SampleMainTex(i.uv, 5 / 8.0f) +
                  SampleMainTex(i.uv, 6 / 8.0f) +
                  SampleMainTex(i.uv, 7 / 8.0f));

                float4 normal_mask = uc * 0.25f + dc * 0.75f;

                float mask = saturate(normal_mask.w) > 0.5f ? 1.0f : 0.0f;
                float3 normal = normal_mask.xyz * mask;
                return float4(normal, round(mask));
            }
            ENDCG
        }
    }
}
