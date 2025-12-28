Shader "Unlit/StarPixel_M1"
{
    Properties
    {
        // Core Star Properties
        _PixelSize("Pixel Size", Float) = 6.0
        _Radius("Star Radius", Range(0.1, 1.0)) = 0.65
        _EdgeSoftness("Edge Softness", Range(0.0, 0.05)) = 0.0
        _CenterOffset("Center Offset", Vector) = (0, 0, 0, 0)

        // Lighting
        _Brightness("Brightness", Range(0.0, 2.0)) = 1.0
        _LimbDarkeningPower("Limb Darkening Power", Range(0.5, 5.0)) = 2.0
        _CorePower("Core Power", Range(1.0, 10.0)) = 6.0
        _CoreIntensity("Core Intensity", Range(0.0, 2.0)) = 0.4

        // Surface Texture
        _SurfaceScale("Surface Scale", Float) = 64.0
        _SurfaceContrast("Surface Contrast", Range(0.5, 5.0)) = 2.0
        _SurfaceStrength("Surface Strength", Range(0.0, 1.0)) = 0.7

        // Equatorial Band
        [Toggle] _BandEnabled("Band Enabled", Float) = 1.0
        _BandOffset("Band Offset", Range(-1.0, 1.0)) = 0.0
        _BandWidth("Band Width", Range(0.0, 1.0)) = 0.25
        _BandSoftness("Band Softness", Range(0.0, 0.2)) = 0.05
        _BandNoiseScale("Band Noise Scale", Float) = 48.0
        _BandJaggedness("Band Jaggedness", Range(0.0, 2.0)) = 0.6
        _BandIntensity("Band Intensity", Range(0.0, 2.0)) = 0.6
        _BandColor("Band Color", Color) = (1, 0.8, 0.4, 1)
        _BandColorStrength("Band Color Strength", Range(0.0, 1.0)) = 0.0

        // Dark Spots
        [Toggle] _SpotEnabled("Spots Enabled", Float) = 1.0
        _SpotScale("Spot Scale", Float) = 48.0
        _SpotThreshold("Spot Threshold", Range(0.0, 1.0)) = 0.55
        _SpotSoftness("Spot Softness", Range(0.0, 0.3)) = 0.08
        _SpotIntensity("Spot Intensity", Range(0.0, 1.0)) = 0.5
        _SpotColor("Spot Color", Color) = (0.3, 0.1, 0.05, 1)
        _SpotColorStrength("Spot Color Strength", Range(0.0, 1.0)) = 0.4

        // Color Ramp
        _BaseColor("Base Color", Color) = (0.8, 0.2, 0.1, 1)
        _HotColor("Hot Color", Color) = (1, 0.95, 0.8, 1)
        _PaletteSteps("Palette Steps", Range(2, 32)) = 6

        // Alpha
        _AlphaInside("Alpha Inside", Range(0.0, 1.0)) = 1.0

        // Debug
        [KeywordEnum(Final, DiscMask, Light, SurfaceNoise, BandMask, SpotsMask)] _DebugMode("Debug Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "StarPixel"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _DEBUGMODE_FINAL _DEBUGMODE_DISCMASK _DEBUGMODE_LIGHT _DEBUGMODE_SURFACENOISE _DEBUGMODE_BANDMASK _DEBUGMODE_SPOTSMASK

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            // Properties
            CBUFFER_START(UnityPerMaterial)
                float _PixelSize;
                float _Radius;
                float _EdgeSoftness;
                float2 _CenterOffset;

                float _Brightness;
                float _LimbDarkeningPower;
                float _CorePower;
                float _CoreIntensity;

                float _SurfaceScale;
                float _SurfaceContrast;
                float _SurfaceStrength;

                float _BandEnabled;
                float _BandOffset;
                float _BandWidth;
                float _BandSoftness;
                float _BandNoiseScale;
                float _BandJaggedness;
                float _BandIntensity;
                float4 _BandColor;
                float _BandColorStrength;

                float _SpotEnabled;
                float _SpotScale;
                float _SpotThreshold;
                float _SpotSoftness;
                float _SpotIntensity;
                float4 _SpotColor;
                float _SpotColorStrength;

                float4 _BaseColor;
                float4 _HotColor;
                float _PaletteSteps;

                float _AlphaInside;
            CBUFFER_END

            // Simple 2D hash function for deterministic noise
            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // === SCREEN-SPACE PIXEL QUANTIZATION ===
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float2 stepUV = (_PixelSize / _ScreenParams.xy);
                float2 screenUVq = floor(screenUV / stepUV) * stepUV + stepUV * 0.5;
                float2 delta = screenUVq - screenUV;
                float2 uvQ = IN.uv + _CenterOffset.xy + delta;

                // === CENTERED STAR COORDINATES ===
                // Convert quantized UVs to centered coordinates (-1 to 1)
                // Star will be circular in UV space (circular on the quad itself)
                float2 p = (uvQ * 2.0 - 1.0);

                // === DISC MASK ===
                float r = length(p);
                float discMask = 1.0 - smoothstep(_Radius, _Radius + _EdgeSoftness, r);

                // Early out if outside disc
                if (discMask <= 0.0)
                {
                    return float4(0, 0, 0, 0);
                }

                // === BASE LIGHTING (LIMB DARKENING + CORE) ===
                float nd = saturate(r / _Radius);
                float limb = pow(1.0 - nd, _LimbDarkeningPower);
                float core = pow(1.0 - nd, _CorePower) * _CoreIntensity;
                float baseLight = saturate((limb + core) * _Brightness);

                // === CHUNKY PIXEL SURFACE TEXTURE ===
                float2 cellCoord = uvQ * _SurfaceScale;
                float2 cellId = floor(cellCoord);
                float n0 = hash21(cellId);

                // Second octave for more variation
                float2 cellId1 = floor(uvQ * (_SurfaceScale * 0.5));
                float n1 = hash21(cellId1);
                float n = lerp(n0, n1, 0.35);

                float surf = pow(n, _SurfaceContrast);
                float surfMix = lerp(1.0, surf, _SurfaceStrength);
                float light = baseLight * surfMix;

                // === EQUATORIAL BAND LAYER ===
                float lat = p.y / _Radius;
                float bandCore = 1.0 - smoothstep(_BandWidth, _BandWidth + _BandSoftness, abs(lat - _BandOffset));

                // Band jaggedness noise
                float2 bandCellId = floor(uvQ * _BandNoiseScale);
                float bN = hash21(bandCellId);
                float band = saturate(bandCore + (bN - 0.5) * _BandJaggedness);
                band *= _BandEnabled;

                // Apply band as brightness boost
                float lightBand = light + band * _BandIntensity;

                // === DARK SPOTS LAYER ===
                float2 spotCellId = floor(uvQ * _SpotScale);
                float sN = hash21(spotCellId);
                float spots = smoothstep(_SpotThreshold, _SpotThreshold + _SpotSoftness, sN);
                spots = 1.0 - spots; // Invert so threshold gives dark blobs
                spots *= _SpotEnabled;

                // Apply spots as brightness reduction
                float lightSpots = lightBand * (1.0 - spots * _SpotIntensity);

                // === COLOR RAMP + PALETTE QUANTIZATION ===
                float3 color = lerp(_BaseColor.rgb, _HotColor.rgb, saturate(lightSpots));

                // Optional band color push
                color = lerp(color, _BandColor.rgb, band * _BandColorStrength);

                // Optional spot color tint
                color = lerp(color, _SpotColor.rgb, spots * _SpotColorStrength);

                // Palette posterization
                float steps = max(2.0, _PaletteSteps);
                color = floor(color * steps) / steps;

                // === ALPHA ===
                float alpha = discMask * _AlphaInside;

                // === DEBUG MODES ===
                #if defined(_DEBUGMODE_DISCMASK)
                    return float4(discMask, discMask, discMask, 1.0);
                #elif defined(_DEBUGMODE_LIGHT)
                    return float4(lightSpots, lightSpots, lightSpots, 1.0);
                #elif defined(_DEBUGMODE_SURFACENOISE)
                    return float4(surf, surf, surf, 1.0);
                #elif defined(_DEBUGMODE_BANDMASK)
                    return float4(band, band, band, 1.0);
                #elif defined(_DEBUGMODE_SPOTSMASK)
                    return float4(spots, spots, spots, 1.0);
                #endif

                // === FINAL OUTPUT ===
                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
