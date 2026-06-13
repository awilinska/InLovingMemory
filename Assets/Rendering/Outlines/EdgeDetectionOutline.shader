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
                float2 left = float2(-1, 0) * _Thickness;
                float2 right = float2(1, 0) * _Thickness;
                float2 bottom = float2(0, -1) * _Thickness;
                float2 top = float2(0, 1) * _Thickness;

                float bottomLeft = SampleDepth(uv, left + bottom);
                float bottomCenter = SampleDepth(uv, bottom);
                float bottomRight = SampleDepth(uv, right + bottom);
                float centerLeft = SampleDepth(uv, left);
                float centerRight = SampleDepth(uv, right);
                float topLeft = SampleDepth(uv, left + top);
                float topCenter = SampleDepth(uv, top);
                float topRight = SampleDepth(uv, right + top);

                float gradientX =
                    bottomRight + 2.0 * centerRight + topRight
                    - bottomLeft - 2.0 * centerLeft - topLeft;
                float gradientY =
                    topLeft + 2.0 * topCenter + topRight
                    - bottomLeft - 2.0 * bottomCenter - bottomRight;
                return sqrt(gradientX * gradientX + gradientY * gradientY) * 0.25;
            }

            float NormalSobel(float2 uv)
            {
                float2 left = float2(-1, 0) * _Thickness;
                float2 right = float2(1, 0) * _Thickness;
                float2 bottom = float2(0, -1) * _Thickness;
                float2 top = float2(0, 1) * _Thickness;

                float3 bottomLeft = SampleNormal(uv, left + bottom);
                float3 bottomCenter = SampleNormal(uv, bottom);
                float3 bottomRight = SampleNormal(uv, right + bottom);
                float3 centerLeft = SampleNormal(uv, left);
                float3 centerRight = SampleNormal(uv, right);
                float3 topLeft = SampleNormal(uv, left + top);
                float3 topCenter = SampleNormal(uv, top);
                float3 topRight = SampleNormal(uv, right + top);

                float3 gradientX =
                    bottomRight + 2.0 * centerRight + topRight
                    - bottomLeft - 2.0 * centerLeft - topLeft;
                float3 gradientY =
                    topLeft + 2.0 * topCenter + topRight
                    - bottomLeft - 2.0 * bottomCenter - bottomRight;
                return sqrt(dot(gradientX, gradientX) + dot(gradientY, gradientY)) * 0.25;
            }

            float LuminanceSobel(float2 uv)
            {
                float2 left = float2(-1, 0) * _Thickness;
                float2 right = float2(1, 0) * _Thickness;
                float2 bottom = float2(0, -1) * _Thickness;
                float2 top = float2(0, 1) * _Thickness;

                float bottomLeft = SampleLuminance(uv, left + bottom);
                float bottomCenter = SampleLuminance(uv, bottom);
                float bottomRight = SampleLuminance(uv, right + bottom);
                float centerLeft = SampleLuminance(uv, left);
                float centerRight = SampleLuminance(uv, right);
                float topLeft = SampleLuminance(uv, left + top);
                float topCenter = SampleLuminance(uv, top);
                float topRight = SampleLuminance(uv, right + top);

                float gradientX =
                    bottomRight + 2.0 * centerRight + topRight
                    - bottomLeft - 2.0 * centerLeft - topLeft;
                float gradientY =
                    topLeft + 2.0 * topCenter + topRight
                    - bottomLeft - 2.0 * bottomCenter - bottomRight;
                return sqrt(gradientX * gradientX + gradientY * gradientY) * 0.25;
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
