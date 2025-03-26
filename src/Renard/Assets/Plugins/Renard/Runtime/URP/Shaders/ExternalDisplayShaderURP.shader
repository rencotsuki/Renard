Shader "Renard/ExternalDisplayShaderURP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Rotation ("Rotation (degrees)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }
        LOD 100

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _Rotation;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS);
                o.uv = v.uv;
                return o;
            }

            float2 RotateUV(float2 uv, float2 pivot, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                uv -= pivot;
                float2 rotatedUV = float2(
                    uv.x * c - uv.y * s,
                    uv.x * s + uv.y * c
                );
                return rotatedUV + pivot;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 pivot = float2(0.5, 0.5); // Center of rotation
                float rad = radians(_Rotation);
                float2 rotatedUV = RotateUV(i.uv, pivot, rad);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, rotatedUV);
            }
            ENDHLSL
        }
    }
}
