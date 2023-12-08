Shader "Hidden/Oil Painting/Line Integral Convolution"
{
   Tags { "RenderType"="Opaque" }
    LOD 200

    Pass
    {
        HLSLPROGRAM
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float2 uv     : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        Varyings vert(Attributes input)
        {
            Varyings output = (Varyings)0;

            VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
            output.vertex = vertexInput.positionCS;
            output.uv = input.uv;

            return output;
        }

        float3 SampleMain(float2 uv)
        {
            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
        }

        half4 frag(Varyings input) : SV_Target
        {
            return half4(1 - SampleMain(input.uv), 0);
        }

        #pragma vertex vert
        #pragma fragment frag

        ENDHLSL
    }
 }
FallBack "Diffuse"