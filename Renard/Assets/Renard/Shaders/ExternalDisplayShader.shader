Shader "Renard/ExternalDisplayShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Rotation ("Rotation (degrees)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Rotation;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
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

            fixed4 frag (v2f i) : SV_Target
            {
                float2 pivot = float2(0.5, 0.5); // Center of rotation
                float rad = radians(_Rotation);
                float2 rotatedUV = RotateUV(i.uv, pivot, rad);
                return tex2D(_MainTex, rotatedUV);
            }
            ENDCG
        }
    }
}
