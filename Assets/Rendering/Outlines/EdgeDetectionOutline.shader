Shader "Hidden/InLovingMemory/EdgeDetectionOutline"
{
    Properties
    {
        [HideInInspector] _BlitTexture("Blit Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Overlay"
        }

        Pass
        {
            Name "Edge Detection Outline"

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZTest Always
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Thickness;
            float _DepthMinThreshold;
            float _DepthMaxThreshold;
            float _NormalMinThreshold;
            float _NormalMaxThreshold;
            float _LuminanceMinThreshold;
            float _LuminanceMaxThreshold;
            float4 _OutlineColor;

            float SampleDepth(float2 uv, float2 offset)
            {
                return LoadSceneDepth(uv + offset * _BlitTexture_TexelSize.xy);
            }

            float3 SampleNormal(float2 uv, float2 offset)
            {
                return SampleSceneNormals(uv + offset * _BlitTexture_TexelSize.xy) * 0.5 + 0.5;
            }

            float SampleLuminance(float2 uv, float2 offset)
            {
                float3 color = SampleSceneColor(uv + offset * _BlitTexture_TexelSize.xy);
                return dot(color, float3(0.3, 0.59, 0.11));
            }

            float DepthSobel(float2 uv)
            {
                float bottomLeft = SampleDepth(uv, float2(-1, -1) * _Thickness);
                float topRight = SampleDepth(uv, float2(1, 1) * _Thickness);
                float topLeft = SampleDepth(uv, float2(-1, 1) * _Thickness);
                float bottomRight = SampleDepth(uv, float2(1, -1) * _Thickness);

                float gradientX = bottomLeft - topRight;
                float gradientY = topLeft - bottomRight;
                return sqrt(gradientX * gradientX + gradientY * gradientY);
            }

            float NormalSobel(float2 uv)
            {
                float3 bottomLeft = SampleNormal(uv, float2(-1, -1) * _Thickness);
                float3 topRight = SampleNormal(uv, float2(1, 1) * _Thickness);
                float3 topLeft = SampleNormal(uv, float2(-1, 1) * _Thickness);
                float3 bottomRight = SampleNormal(uv, float2(1, -1) * _Thickness);

                float3 gradientX = bottomLeft - topRight;
                float3 gradientY = topLeft - bottomRight;
                return sqrt(dot(gradientX, gradientX) + dot(gradientY, gradientY));
            }

            float LuminanceSobel(float2 uv)
            {
                float bottomLeft = SampleLuminance(uv, float2(-1, -1) * _Thickness);
                float topRight = SampleLuminance(uv, float2(1, 1) * _Thickness);
                float topLeft = SampleLuminance(uv, float2(-1, 1) * _Thickness);
                float bottomRight = SampleLuminance(uv, float2(1, -1) * _Thickness);

                float gradientX = bottomLeft - topRight;
                float gradientY = topLeft - bottomRight;
                return sqrt(gradientX * gradientX + gradientY * gradientY);
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                float depthEdge = smoothstep(
                    _DepthMinThreshold,
                    _DepthMaxThreshold,
                    DepthSobel(uv));

                float normalEdge = smoothstep(
                    _NormalMinThreshold,
                    _NormalMaxThreshold,
                    NormalSobel(uv));

                float luminanceEdge = smoothstep(
                    _LuminanceMinThreshold,
                    _LuminanceMaxThreshold,
                    LuminanceSobel(uv));

                float edge = max(depthEdge, max(normalEdge, luminanceEdge));
                return float4(_OutlineColor.rgb, edge * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
}
