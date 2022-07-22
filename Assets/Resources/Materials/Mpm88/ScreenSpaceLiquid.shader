Shader "Unlit/ScreenSpaceLiquid"
{
    Properties
    {
        _SceneColor ("Texture", 2D) = "white" {}
        _SceneColorInvSize ("Scene Color Size Inverse", Vector) = (1,1,1,1)
        _SceneNormal ("Texture", 2D) = "white" {}
        _SceneNormalInvSize("Scene Color Size Inverse", Vector) = (1,1,1,1)
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
            sampler2D _SceneNormal;
            float2 _SceneNormalInvSize;

            float3 _LiquidColor;
            float4 _LiquidSpecularColor;

            const float DEPTH_RANGE = 1000.0f - 0.3f;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 uv2pos(float2 uv) {
                float depth = tex2D(_SceneNormal, uv).x;
                return float3(uv * 2.0f - 1.0f, depth);
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 lightDir = normalize(float3(0,1,1));

                float4 sceneColor = tex2D(_SceneColor, i.uv);

                float4 normal_mask = tex2D(_SceneNormal, i.uv);
                float3 normal = normalize(normal_mask.rgb + float3(0, 0, 1e-5f));


                float liquidSpaceMask = saturate(normal_mask.a);
                float LoN = dot(lightDir, normal);
                float3 liquidSpecular = _LiquidSpecularColor.rgb * pow(LoN, exp(1 + 10 * _LiquidSpecularColor.a));


                //float3 color = sceneColor * lerp(1, _LiquidColor, liquidSpaceMask) + liquidSpecular * liquidSpaceMask;
                float3 color = lerp(sceneColor, _LiquidColor, liquidSpaceMask) + liquidSpecular * liquidSpaceMask;
                return float4(color, 1.0f);
            }
            ENDCG
        }
    }
}
