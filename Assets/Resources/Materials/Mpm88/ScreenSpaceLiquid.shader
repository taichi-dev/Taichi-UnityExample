Shader "Unlit/ScreenSpaceLiquid"
{
    Properties
    {
        _SceneColor ("Texture", 2D) = "white" {}
        _SceneColorInvSize ("Scene Color Size Inverse", Vector) = (1,1,1,1)
        _SceneNormalDepth ("Texture", 2D) = "white" {}
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

            sampler2D _SceneColor;
            float2 _SceneColorInvSize;
            sampler2D _SceneNormalDepth;

            const float DEPTH_RANGE = 1000.0f - 0.3f;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 uv2pos(float2 uv) {
                float depth = tex2D(_SceneNormalDepth, uv).x;
                return float3(uv * 2.0f - 1.0f, depth);
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 sceneColor = tex2D(_SceneColor, i.uv);


                float alpha = tex2D(_SceneNormalDepth, i.uv).a;


                float du = _SceneColorInvSize.x;
                float dv = _SceneColorInvSize.y;

                float2 uv_o = i.uv;
                float2 uv_x = i.uv + float2(du, 0.0f);
                float2 uv_y = i.uv + float2(0.0f, dv);

                float3 pos_o = uv2pos(uv_o);
                float3 pos_x = uv2pos(uv_x);
                float3 pos_y = uv2pos(uv_y);

                float3 normal = normalize(cross(pos_x - pos_o, pos_y - pos_o));

                float3 color = alpha < 0.5f ? sceneColor.xyz : normal.xyz;
                return float4(color, 1.0f);
            }
            ENDCG
        }
    }
}
