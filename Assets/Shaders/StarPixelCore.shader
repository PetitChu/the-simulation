Shader "Custom/StarPixelCore"
{
    Properties
    {
        _PixelSize ("Pixel Size (px)", Range(1,16)) = 4
        _CenterOffset ("Center Offset", Vector) = (0,0,0,0)
        _Radius ("Radius", Range(0,1)) = 0.45
        _EdgeSoftness ("Edge Softness", Range(0,0.1)) = 0
        _LimbDarkeningPower ("Limb Darkening Power", Range(0,8)) = 2
        _CoreIntensity ("Core Intensity", Range(0,5)) = 1
        _BaseColor ("Base Color", Color) = (1,0.7,0.25,1)
        _HotColor ("Hot Color", Color) = (1,0.95,0.7,1)
        _AlphaInside ("Alpha Inside", Range(0,1)) = 1
        _EllipseScale ("Ellipse Scale (X,Y)", Vector) = (1,1,0,0)
        _RampTex ("Ramp Tex", 2D) = "white" {}
        [Toggle(_USE_RAMPTEX)] _UseRampTex ("Use Ramp Tex", Float) = 0
        _DebugMode ("Debug Mode", Int) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardUnlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _USE_RAMPTEX
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PixelSize;
                float4 _CenterOffset;
                float _Radius;
                float _EdgeSoftness;
                float _LimbDarkeningPower;
                float _CoreIntensity;
                float4 _BaseColor;
                float4 _HotColor;
                float _AlphaInside;
                float4 _EllipseScale;
                int _DebugMode;
            CBUFFER_END

            TEXTURE2D(_RampTex); SAMPLER(sampler_RampTex);

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

            float2 SnapUVToPixels(float2 uv)
            {
                float2 uvPerPixel = 1.0 / _ScreenParams.xy;
                float2 step = uvPerPixel * _PixelSize;
                float2 snapped = floor(uv / step) * step + step * 0.5;
                return snapped;
            }

            float2 GetCenteredUV(float2 uv)
            {
                float2 centered = (uv + _CenterOffset.xy - 0.5) * 2.0;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                centered.x *= aspect * _EllipseScale.x;
                centered.y *= _EllipseScale.y;
                return centered;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float3 EvaluateRamp(float t)
            {
                #ifdef _USE_RAMPTEX
                float3 rampSample = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(t, 0.5)).rgb;
                return rampSample;
                #else
                return lerp(_BaseColor.rgb, _HotColor.rgb, t);
                #endif
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 snappedUV = SnapUVToPixels(IN.uv);
                float2 p = GetCenteredUV(snappedUV);
                float r = length(p);

                float edgeSoft = max(_EdgeSoftness, 1e-5);
                float mask = 1.0 - smoothstep(_Radius - edgeSoft, _Radius + edgeSoft, r);

                float nd = saturate(r / max(_Radius, 1e-5));
                float limb = pow(saturate(1.0 - nd), _LimbDarkeningPower);
                float coreFactor = saturate(limb * _CoreIntensity);

                float3 rgb = EvaluateRamp(coreFactor);
                float alpha = mask * _AlphaInside;

                if (_DebugMode == 1)
                {
                    return half4(mask.xxx, alpha);
                }
                else if (_DebugMode == 2)
                {
                    return half4(coreFactor.xxx, alpha);
                }

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
