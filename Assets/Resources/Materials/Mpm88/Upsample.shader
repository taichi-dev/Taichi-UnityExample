Shader "Unlit/Upsample"
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

            float4 frag (v2f i) : SV_Target
            {
                float4 color = tex2D(_SameSizeTex, i.uv) * 0.5f +
                  tex2D(_MainTex, i.uv + float2(0.0f,  _MainTexInvSize.y)) * 0.125f +
                  tex2D(_MainTex, i.uv + float2(0.0f, -_MainTexInvSize.y)) * 0.125f +
                  tex2D(_MainTex, i.uv + float2( _MainTexInvSize.x, 0.0f)) * 0.125f +
                  tex2D(_MainTex, i.uv + float2(-_MainTexInvSize.x, 0.0f)) * 0.125f;
                return color;
            }
            ENDCG
        }
    }
}
