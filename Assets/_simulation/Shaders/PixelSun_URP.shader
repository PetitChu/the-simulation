Shader "Unlit/PixelSun_URP_WorldPlane_Flat_Simple"
{
    Properties
    {
        _CenterOffsetXY ("Center Offset (world units on quad plane)", Vector) = (0,0,0,0)

        _PixelSizeWorld ("Pixel Size (world units)", Range(0.0005, 1.0)) = 0.02
        _RadiusWorld ("Radius (world units)", Range(0.01, 10.0)) = 0.25
        _EdgeSoftnessWorld ("Edge Softness (world units)", Range(0.0, 0.1)) = 0.002
        _BodyAlpha ("Body Alpha", Range(0,1)) = 1

        _Brightness   ("Brightness", Range(0, 3)) = 1.0
        _RimStrength  ("Rim Darken Strength", Range(0, 1)) = 0.08
        _RimStart     ("Rim Start (0-1)", Range(0,1)) = 0.82

        _LowColor        ("Low Color", Color) = (0.60, 0.05, 0.03, 1)
        _HighColor       ("High Color", Color) = (1.00, 0.35, 0.10, 1)
        _ColorFromNoise  ("Color From Noise", Range(0,1)) = 0.85
        _PaletteSteps    ("Palette Steps", Range(2, 32)) = 6

        _TimeScale     ("Time Scale", Range(0, 4)) = 1
        _RotationRPS   ("Rotation Speed (turns/sec)", Range(-0.5, 0.5)) = 0.06
        _SpinAxis      ("Spin Axis (X=right,Y=up,Z=forward)", Vector) = (0,1,0,0)

        _SurfaceScale     ("Surface Detail (relative)", Range(1, 512)) = 80
        _SurfaceStrength  ("Surface Strength", Range(0, 1)) = 0.75
        _SurfaceContrast  ("Surface Contrast", Range(0.1, 8)) = 2.0

        _EvolveStrength ("Churn Strength", Range(0, 2)) = 0.25
        _EvolveX        ("Churn X", Range(-2, 2)) = 0.05
        _EvolveY        ("Churn Y", Range(-2, 2)) = 0.02

        _SpotsEnabled       ("Spots Enabled", Range(0,1)) = 1
        _SpotScale          ("Spot Group Scale (relative)", Range(1, 512)) = 22
        _SpotDetailScaleMul ("Spot Detail Scale Mult", Range(1, 16)) = 5
        _SpotDetailStrength ("Spot Detail Strength", Range(0, 1)) = 0.35

        _SpotThreshold      ("Spot Threshold", Range(0,1)) = 0.55
        _SpotSoftness       ("Spot Softness", Range(0.0, 0.5)) = 0.18
        _SpotIntensity      ("Spot Intensity", Range(0,1)) = 0.45
        _SpotTint           ("Spot Tint", Color) = (0.20, 0.02, 0.02, 1)
        _SpotTintStrength   ("Spot Tint Strength", Range(0,1)) = 0.35

        _GlowEnabled   ("Glow Enabled", Range(0,1)) = 1
        _GlowWidthWorld ("Glow Width (world units)", Range(0.0, 10.0)) = 0.10
        _GlowPower     ("Glow Power", Range(0.1, 8)) = 2.0
        _GlowFalloff   ("Glow Falloff", Range(0.1, 8)) = 2.5
        _GlowIntensity ("Glow Intensity", Range(0, 4)) = 0.7
        _GlowAlpha     ("Glow Alpha", Range(0, 1)) = 0.5
        _GlowEdgeSoftWorld ("Glow Edge Soft (world units)", Range(0.0, 1.0)) = 0.01
        _GlowColor     ("Glow Color", Color) = (0.18, 0.03, 0.03, 1)

        // NEW: how strongly _Brightness drives the glow
        _GlowBrightnessInfluence ("Glow Brightness Influence", Range(0, 2)) = 0.75

        // Debug dropdown:
        [Enum(Final,0, DiscMask,1, SurfaceNoise,2, Spots,3, Glow,4, SpinPin,5)]
        _DebugMode ("Debug Mode", Float) = 0

        _DebugPinWidth ("Debug Pin Width", Range(0.001, 0.05)) = 0.010
        _DebugPinExtend ("Debug Pin Extend (beyond surface)", Range(0.0, 1.0)) = 0.30
        _DebugPinEndRadius ("Debug Pin End Cap Radius", Range(0.002, 0.12)) = 0.03
        _DebugPinInsideStrength ("Debug Pin Inside Strength", Range(0.0, 1.0)) = 0.25
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Name "PixelSunWorldPlaneFlatSimple"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define TWO_PI 6.28318530718
            #define TIME_WRAP_SECONDS 2048.0

            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _CenterOffsetXY;
                float  _PixelSizeWorld;
                float  _RadiusWorld;
                float  _EdgeSoftnessWorld;
                float  _BodyAlpha;

                float  _Brightness;
                float  _RimStrength;
                float  _RimStart;

                float4 _LowColor;
                float4 _HighColor;
                float  _ColorFromNoise;
                float  _PaletteSteps;

                float  _TimeScale;
                float  _RotationRPS;
                float4 _SpinAxis;

                float  _SurfaceScale;
                float  _SurfaceStrength;
                float  _SurfaceContrast;

                float  _EvolveStrength;
                float  _EvolveX;
                float  _EvolveY;

                float  _SpotsEnabled;
                float  _SpotScale;
                float  _SpotDetailScaleMul;
                float  _SpotDetailStrength;

                float  _SpotThreshold;
                float  _SpotSoftness;
                float  _SpotIntensity;
                float4 _SpotTint;
                float  _SpotTintStrength;

                float  _GlowEnabled;
                float  _GlowWidthWorld;
                float  _GlowPower;
                float  _GlowFalloff;
                float  _GlowIntensity;
                float  _GlowAlpha;
                float  _GlowEdgeSoftWorld;
                float4 _GlowColor;
                float  _GlowBrightnessInfluence;

                float  _DebugMode;
                float  _DebugPinWidth;
                float  _DebugPinExtend;
                float  _DebugPinEndRadius;
                float  _DebugPinInsideStrength;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float chunkyNoise2D(float2 x)
            {
                float n0 = hash21(floor(x));
                float n1 = hash21(floor(x * 0.5 + 17.0));
                return lerp(n0, n1, 0.35);
            }

            float valueNoise2D(float2 x)
            {
                float2 i = floor(x);
                float2 f = frac(x);

                float a = hash21(i);
                float b = hash21(i + float2(1,0));
                float c = hash21(i + float2(0,1));
                float d = hash21(i + float2(1,1));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float triplanarChunkyNoise(float3 p, float scale, float2 timeOffset)
            {
                float3 w = abs(p);
                w = max(w, 1e-5);
                w /= (w.x + w.y + w.z);

                float nXY = chunkyNoise2D(p.xy * scale + timeOffset);
                float nYZ = chunkyNoise2D(p.yz * scale + timeOffset);
                float nZX = chunkyNoise2D(p.zx * scale + timeOffset);

                return nXY * w.z + nYZ * w.x + nZX * w.y;
            }

            float triplanarValueNoise(float3 p, float scale, float2 timeOffset)
            {
                float3 w = abs(p);
                w = max(w, 1e-5);
                w /= (w.x + w.y + w.z);

                float nXY = valueNoise2D(p.xy * scale + timeOffset);
                float nYZ = valueNoise2D(p.yz * scale + timeOffset);
                float nZX = valueNoise2D(p.zx * scale + timeOffset);

                return nXY * w.z + nYZ * w.x + nZX * w.y;
            }

            float3 rotateAroundAxis(float3 v, float3 axis, float angleRad)
            {
                float s = sin(angleRad);
                float c = cos(angleRad);
                return v * c + cross(axis, v) * s + axis * dot(axis, v) * (1.0 - c);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Plane coords (world units) using object local X/Y axes in world
                float3 originWS = TransformObjectToWorld(float3(0,0,0));
                float3 rightWS  = normalize(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));
                float3 upWS     = normalize(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21));

                float3 d = IN.positionWS - originWS;
                float2 plane = float2(dot(d, rightWS), dot(d, upWS));
                plane -= _CenterOffsetXY.xy;

                float radius = max(_RadiusWorld, 1e-5);

                // Full-res coords for glow + debug overlays
                float2 pF = plane;
                float rF = length(pF);

                // Pixel-quantized coords for body
                float px = max(_PixelSizeWorld, 1e-6);
                float2 pQ = floor(plane / px) * px + px * 0.5;
                float r = length(pQ);

                float disc = 1.0 - smoothstep(radius, radius + _EdgeSoftnessWorld, r);
                float nd = saturate(r / radius);

                // Disc-space coords [-1..1] (full-res, used for debug shapes)
                float2 uvF = pF / radius;
                float rrF = dot(uvF, uvF);

                // For shading, hemisphere mapping uses quantized uv (pixel stability)
                float2 uv = pQ / radius;
                float rr = dot(uv, uv);
                float z  = sqrt(max(0.0, 1.0 - rr));
                float3 n = normalize(float3(uv.x, uv.y, z));

                // Spin axis (star space)
                float3 axis = _SpinAxis.xyz;
                float axLen = length(axis);
                axis = (axLen > 1e-4) ? (axis / axLen) : float3(0,1,0);

                // Time bounded for precision
                float timeSec = _Time.y * _TimeScale;
                timeSec = timeSec - floor(timeSec / TIME_WRAP_SECONDS) * TIME_WRAP_SECONDS;

                // Uniform rotation
                float omega = _RotationRPS * TWO_PI;
                float angle = omega * timeSec;
                float3 nR = rotateAroundAxis(n, axis, angle);

                // Noise sampling offsets (churn)
                float2 timeOff = float2(timeSec * _EvolveX, timeSec * _EvolveY) * _EvolveStrength;

                // Surface stays chunky (pixel texture)
                float nSurf = triplanarChunkyNoise(nR, _SurfaceScale, timeOff);
                float surf  = pow(max(nSurf, 1e-5), _SurfaceContrast);
                float surfMix = lerp(1.0, surf, _SurfaceStrength);

                // Flat lighting + subtle rim darken
                float rim = smoothstep(_RimStart, 1.0, nd);
                float light = _Brightness * (1.0 - rim * _RimStrength);
                light *= surfMix;

                // Spots: grouped blotches (smooth macro + micro detail)
                float spotsMask = 0.0;
                if (_SpotsEnabled > 0.5)
                {
                    float3 spotP = nR + float3(0.17, -0.31, 0.09);

                    float macro = triplanarValueNoise(spotP, _SpotScale, timeOff * 0.35);
                    float micro = triplanarValueNoise(
                        spotP + float3(0.41, 0.23, -0.19),
                        _SpotScale * max(_SpotDetailScaleMul, 1.0),
                        timeOff * 0.15
                    );

                    float sN = saturate(macro + (micro - 0.5) * _SpotDetailStrength);

                    float m = smoothstep(_SpotThreshold, _SpotThreshold + _SpotSoftness, sN);
                    spotsMask = (1.0 - m) * disc;

                    light *= (1.0 - spotsMask * _SpotIntensity);
                }

                // Color from noise (flat feel)
                float heatFromNoise = saturate(lerp(0.35, 1.0, surf));
                float heat = lerp(0.5, heatFromNoise, _ColorFromNoise);

                float3 color = lerp(_LowColor.rgb, _HighColor.rgb, heat);
                color *= light;
                color *= disc;

                if (_SpotsEnabled > 0.5 && _SpotTintStrength > 0.001)
                    color = lerp(color, color * _SpotTint.rgb, spotsMask * _SpotTintStrength);

                // Posterize
                float steps = max(2.0, _PaletteSteps);
                color = floor(color * steps) / steps;

                // Glow (now influenced by _Brightness)
                float glow = 0.0;
                float3 glowRGB = 0;
                if (_GlowEnabled > 0.5)
                {
                    float discFull = 1.0 - smoothstep(radius, radius + _GlowEdgeSoftWorld, rF);

                    float outside = saturate((rF - radius) / max(_GlowWidthWorld, 1e-5));
                    float halo = saturate(1.0 - outside);
                    halo = pow(halo, _GlowPower);
                    halo *= exp(-outside * _GlowFalloff);

                    // Brightness influence: 1 + influence*(Brightness-1)
                    float bBoost = 1.0 + _GlowBrightnessInfluence * (_Brightness - 1.0);
                    bBoost = max(0.0, bBoost);

                    glow = halo * _GlowIntensity * bBoost;

                    glowRGB = _GlowColor.rgb * glow;
                    glowRGB += _GlowColor.rgb * discFull * (0.08 * _GlowIntensity) * bBoost;
                }

                // Debug dropdown modes
                int dm = (int)round(_DebugMode);
                if (dm == 1) return float4(disc.xxx, 1);
                if (dm == 2) return float4(nSurf.xxx, 1);
                if (dm == 3) return float4(spotsMask.xxx, 1);
                if (dm == 4) return float4(glow.xxx, 1);

                if (dm == 5)
                {
                    // Spin Pin Debug (Z-aware)
                    float aa = max(px / radius, 0.0015);

                    float insideDisc = 1.0 - step(1.0, sqrt(rrF));
                    float3 bg = insideDisc * 0.06;

                    float2 a2 = axis.xy;
                    float a2Len = length(a2);

                    if (a2Len < 1e-4)
                    {
                        float endR0 = max(_DebugPinEndRadius, 1e-5);
                        float centerDot = 1.0 - smoothstep(endR0, endR0 + aa, length(uvF));
                        float3 dbg0 = bg + centerDot * float3(0.35, 0.85, 1.0);
                        return float4(dbg0, 1);
                    }

                    float2 dir = a2 / a2Len;

                    float poleLen = a2Len;
                    float2 poleP = dir * poleLen;
                    float2 poleN = -dir * poleLen;

                    float extend = max(_DebugPinExtend, 0.0);
                    float outerLen = poleLen * (1.0 + extend);
                    float2 endP = dir * outerLen;
                    float2 endN = -dir * outerLen;

                    float distLine = abs(dir.x * uvF.y - dir.y * uvF.x);
                    float along = dot(uvF, dir);

                    float width = max(_DebugPinWidth, 1e-5);
                    float stroke = 1.0 - smoothstep(width, width + aa, distLine);

                    float segOuter = 1.0 - smoothstep(outerLen, outerLen + aa, abs(along));
                    float segInner = 1.0 - smoothstep(poleLen,  poleLen  + aa, abs(along));

                    float insideMask  = segInner * stroke * _DebugPinInsideStrength;
                    float outsideMask = saturate(segOuter - segInner) * stroke;

                    float endR = max(_DebugPinEndRadius, 1e-5);
                    float capP = 1.0 - smoothstep(endR, endR + aa, length(uvF - endP));
                    float capN = 1.0 - smoothstep(endR, endR + aa, length(uvF - endN));

                    float poleR = endR * 0.75;
                    float poleDotP = 1.0 - smoothstep(poleR, poleR + aa, length(uvF - poleP));
                    float poleDotN = 1.0 - smoothstep(poleR, poleR + aa, length(uvF - poleN));

                    float nearPlus = step(0.0, axis.z);

                    float3 colNear = float3(0.35, 0.85, 1.0);
                    float3 colFar  = float3(1.0, 1.0, 1.0);

                    float3 capColP = lerp(colFar, colNear, nearPlus);
                    float3 capColN = lerp(colNear, colFar, nearPlus);

                    float3 lineOutsideCol = float3(0.35, 0.85, 1.0);
                    float3 lineInsideCol  = float3(1.0, 1.0, 1.0);

                    float3 dbg = bg
                               + outsideMask * lineOutsideCol
                               + insideMask  * lineInsideCol
                               + capP * capColP
                               + capN * capColN
                               + (poleDotP + poleDotN) * float3(1.0, 1.0, 1.0) * 0.6;

                    return float4(dbg, 1);
                }

                // Final
                float3 finalRGB = color + glowRGB;
                float alphaBody = disc * _BodyAlpha;
                float alphaGlow = saturate(glow * _GlowAlpha);
                float finalA = saturate(alphaBody + alphaGlow);

                return float4(finalRGB, finalA);
            }
            ENDHLSL
        }
    }
}
