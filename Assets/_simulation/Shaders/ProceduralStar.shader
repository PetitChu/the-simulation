Shader "Unlit/ProceduralStar"
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

        // Ramp Atlas (256 x ROWS). X=bottom->top position. Y=row selector.
        _RampAtlas ("Ramp Atlas", 2D) = "white" {}
        _RampRows  ("Ramp Rows", Float) = 5

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

        _SpotVortexStrength ("Spot Vortex Strength", Range(0, 2)) = 0.65
        _SpotVortexScale    ("Spot Vortex Scale", Range(1, 256)) = 42
        _SpotVortexSpeed    ("Spot Vortex Speed", Range(0, 4)) = 1.25

        _SpotThreshold      ("Spot Threshold", Range(0,1)) = 0.55
        _SpotSoftness       ("Spot Softness", Range(0.0, 0.5)) = 0.18
        _SpotIntensity      ("Spot Intensity", Range(0,1)) = 0.45
        _SpotTintStrength   ("Spot Tint Strength", Range(0,1)) = 0.35

        _GlowEnabled   ("Glow Enabled", Range(0,1)) = 1
        _GlowWidthWorld ("Glow Width (world units)", Range(0.0, 10.0)) = 0.10
        _GlowPower     ("Glow Power", Range(0.1, 8)) = 2.0
        _GlowFalloff   ("Glow Falloff", Range(0.1, 8)) = 2.5
        _GlowIntensity ("Glow Intensity", Range(0, 4)) = 0.7
        _GlowAlpha     ("Glow Alpha", Range(0, 1)) = 0.5
        _GlowEdgeSoftWorld ("Glow Edge Soft (world units)", Range(0.0, 1.0)) = 0.01
        _GlowBrightnessInfluence ("Glow Brightness Influence", Range(0, 2)) = 0.75

        // --------------------------------------------
        // FLARES (10 essential parameters)
        // --------------------------------------------
        _FlaresEnabled ("Flares Enabled", Range(0,1)) = 1
        _FlareRingCount ("Ring Count", Range(0, 6)) = 1
        _FlareRingSeed ("Ring Seed", Range(0, 1000)) = 13
        _FlareRingMajorWorld ("Ring Major Radius (world units)", Range(0.0, 1.0)) = 0.08
        _FlareRingMinorWorld ("Ring Minor Radius (world units)", Range(0.0, 1.0)) = 0.035
        _FlareRingWidthWorld ("Ring Stroke Width (world units)", Range(0.0, 0.2)) = 0.012
        _FlareRingBreakup ("Ring Breakup", Range(0,1)) = 0.45
        _FlareLifeJitter  ("Flare Period Jitter", Range(0.0, 0.8)) = 0.25
        _FlareLifeDutyJitter ("Flare Duty Jitter", Range(0.0, 0.6)) = 0.20
        _FlareIntensity ("Flare Intensity", Range(0, 6)) = 1.2

        // Debug dropdown:
        [Enum(Final,0, DiscMask,1, SurfaceNoise,2, Spots,3, Glow,4, SpinPin,5, Flares,6)]
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
            Name "PixelSunWorldPlaneFlatRampAtlas"
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

            TEXTURE2D(_RampAtlas);
            SAMPLER(sampler_RampAtlas);

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

                float  _RampRows;

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

                float  _SpotVortexStrength;
                float  _SpotVortexScale;
                float  _SpotVortexSpeed;

                float  _SpotThreshold;
                float  _SpotSoftness;
                float  _SpotIntensity;
                float  _SpotTintStrength;

                float  _GlowEnabled;
                float  _GlowWidthWorld;
                float  _GlowPower;
                float  _GlowFalloff;
                float  _GlowIntensity;
                float  _GlowAlpha;
                float  _GlowEdgeSoftWorld;
                float  _GlowBrightnessInfluence;

                float  _FlaresEnabled;
                float  _FlareRingCount;
                float  _FlareRingSeed;
                float  _FlareRingMajorWorld;
                float  _FlareRingMinorWorld;
                float  _FlareRingWidthWorld;
                float  _FlareRingBreakup;
                float  _FlareLifeJitter;
                float  _FlareLifeDutyJitter;
                float  _FlareIntensity;

                float  _DebugMode;
                float  _DebugPinWidth;
                float  _DebugPinExtend;
                float  _DebugPinEndRadius;
                float  _DebugPinInsideStrength;
            CBUFFER_END

            // Safely normalize a vector, returning a default up-vector if magnitude is too small.
            // This prevents NaN/infinity from rsqrt when the input is near-zero.
            float3 safeNormalize(float3 v)
            {
                float lenSq = dot(v, v);
                if (lenSq < 1e-8)
                    return float3(0, 1, 0);
                return v * rsqrt(lenSq);
            }

            // Hash function: float2 -> float
            // Magic constants (123.34, 456.21, 34.345) provide good distribution for procedural noise.
            // These values are chosen empirically to reduce visible patterns in the output.
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            // Hash function: float -> float
            // Magic constants (0.1031, 33.33) provide pseudo-random distribution.
            // These values are standard in procedural noise generation for good spatial distribution.
            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
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

            float2 rot2(float2 p, float a)
            {
                float s = sin(a);
                float c = cos(a);
                return float2(c*p.x - s*p.y, s*p.x + c*p.y);
            }

            float ellipseStroke(float2 p, float a, float b, float halfWidth, float aa)
            {
                a = max(a, 1e-6);
                b = max(b, 1e-6);
                float2 q = float2(p.x / a, p.y / b);
                float rr = length(q);
                float d = abs(rr - 1.0) * min(a, b);
                return 1.0 - smoothstep(halfWidth, halfWidth + aa, d);
            }

            // Ramp atlas sampling:
            // rows: 0=BodyLow, 1=BodyHigh, 2=Glow, 3=Flare, 4=SpotTint
            float3 SampleRamp(float t01, float row)
            {
                float rows = max(1.0, _RampRows);
                float v = (row + 0.5) / rows;
                return SAMPLE_TEXTURE2D(_RampAtlas, sampler_RampAtlas, float2(saturate(t01), v)).rgb;
            }

            // LIFETIME ENVELOPE
            float pulseEnvelope01(float t, float period, float duty, float fadeFrac, float phase01)
            {
                period = max(period, 1e-4);
                duty = clamp(duty, 0.01, 0.99);
                float u = frac(t / period + phase01);     // 0..1

                if (u >= duty) return 0.0;

                float f = clamp(fadeFrac, 0.0, 0.49);
                f = min(f, duty * 0.49);

                float a = smoothstep(0.0, f, u);
                float b = 1.0 - smoothstep(duty - f, duty, u);
                return a * b;
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
                float3 originWS = TransformObjectToWorld(float3(0,0,0));
                float3 rightWS  = safeNormalize(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));
                float3 upWS     = safeNormalize(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21));

                float3 d = IN.positionWS - originWS;
                float2 plane = float2(dot(d, rightWS), dot(d, upWS));
                plane -= _CenterOffsetXY.xy;

                float radius = max(_RadiusWorld, 1e-5);

                float2 pF = plane;
                float rF  = length(pF);

                float px  = max(_PixelSizeWorld, 1e-6);
                float2 pQ = floor(plane / px) * px + px * 0.5;
                float r   = length(pQ);

                float disc = 1.0 - smoothstep(radius, radius + _EdgeSoftnessWorld, r);
                float nd   = saturate(r / radius);

                float2 uvF = pF / radius;
                float2 uv  = pQ / radius;

                // pole->center factor (symmetric, inverted)
                float gradT_body   = saturate(1.0 - abs(uv.y));

                float rr = dot(uv, uv);
                float z  = sqrt(max(0.0, 1.0 - rr));
                float3 n = safeNormalize(float3(uv.x, uv.y, z));

                float3 axis = _SpinAxis.xyz;
                float axisLenSq = dot(axis, axis);
                if (axisLenSq > 1e-8)
                {
                    axis = axis / sqrt(axisLenSq);
                }
                else
                {
                    axis = float3(0, 1, 0);
                }

                float timeSec = _Time.y * _TimeScale;
                timeSec = timeSec - floor(timeSec / TIME_WRAP_SECONDS) * TIME_WRAP_SECONDS;

                float omega = _RotationRPS * TWO_PI;
                float angle = omega * timeSec;
                float3 nR = rotateAroundAxis(n, axis, angle);

                float2 timeOff = float2(timeSec * _EvolveX, timeSec * _EvolveY) * _EvolveStrength;

                float surfaceNoise = triplanarChunkyNoise(nR, _SurfaceScale, timeOff);
                float surf  = pow(max(surfaceNoise, 1e-5), _SurfaceContrast);
                float surfMix = lerp(1.0, surf, _SurfaceStrength);

                float rim   = smoothstep(_RimStart, 1.0, nd);
                float light = _Brightness * (1.0 - rim * _RimStrength);
                light *= surfMix;

                float spotsMask = 0.0;
                if (_SpotsEnabled > 0.5)
                {
                    float3 spotP0 = nR + float3(0.17, -0.31, 0.09);

                    float vScale = max(_SpotVortexScale, 1.0);
                    float vStr   = _SpotVortexStrength;
                    float eps    = 0.85 / vScale;

                    float2 vTimeOff = timeOff * (0.35 * _SpotVortexSpeed);

                    float f0 = triplanarValueNoise(spotP0, vScale, vTimeOff);
                    float fx = triplanarValueNoise(spotP0 + float3(eps,0,0), vScale, vTimeOff);
                    float fy = triplanarValueNoise(spotP0 + float3(0,eps,0), vScale, vTimeOff);
                    float fz = triplanarValueNoise(spotP0 + float3(0,0,eps), vScale, vTimeOff);
                    float3 grad = float3(fx - f0, fy - f0, fz - f0) / max(eps, 1e-5);

                    float3 t1 = safeNormalize(cross(nR, grad));
                    float3 t2 = cross(nR, t1);

                    float t = timeSec * _SpotVortexSpeed * TWO_PI;
                    float3 flow = t1 * cos(t) + t2 * sin(t);

                    float warpAmp = vStr * (0.05 + 0.20 * f0);
                    float3 spotP  = spotP0 + flow * warpAmp;

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

                float heatFromNoise = saturate(lerp(0.35, 1.0, surf));
                float heat = lerp(0.5, heatFromNoise, _ColorFromNoise);

                // Multi-stop vertical ramps from atlas
                float3 lowCol  = SampleRamp(gradT_body, 0.0);
                float3 highCol = SampleRamp(gradT_body, 1.0);

                float3 color = lerp(lowCol, highCol, heat);
                color *= light;
                color *= disc;

                if (_SpotsEnabled > 0.5 && _SpotTintStrength > 0.001)
                {
                    float3 spotTint = SampleRamp(gradT_body, 4.0);
                    color = lerp(color, color * spotTint, spotsMask * _SpotTintStrength);
                }

                float steps = max(2.0, _PaletteSteps);
                color = floor(color * steps) / steps;

                // Glow
                float glow = 0.0;
                float3 glowRGB = 0;

                float bBoost = 1.0 + _GlowBrightnessInfluence * (_Brightness - 1.0);
                bBoost = max(0.0, bBoost);

                if (_GlowEnabled > 0.5)
                {
                    float discFull = 1.0 - smoothstep(radius, radius + _GlowEdgeSoftWorld, rF);

                    float outside = saturate((rF - radius) / max(_GlowWidthWorld, 1e-5));
                    float halo = saturate(1.0 - outside);
                    halo = pow(halo, _GlowPower);
                    halo *= exp(-outside * _GlowFalloff);

                    glow = halo * _GlowIntensity * bBoost;

                    float3 glowCol = SampleRamp(gradT_body, 2.0);
                    glowRGB = glowCol * glow;
                    glowRGB += glowCol * discFull * (0.08 * _GlowIntensity) * bBoost;
                }

                // Flares
                float flareMask = 0.0;
                float3 flareRGB = 0;

                if (_FlaresEnabled > 0.5 && _FlareRingCount >= 1.0)
                {
                    const int MAX_RINGS = 6;
                    int count = (int)round(clamp(_FlareRingCount, 0.0, (float)MAX_RINGS));

                    float aBase = max(_FlareRingMajorWorld / radius, 0.0);
                    float bBase = max(_FlareRingMinorWorld / radius, 0.0);
                    float wBase = max(_FlareRingWidthWorld / radius, 0.0);
                    const float rimOverlap = 0.01 / radius; // Hardcoded rim overlap

                    float aaN = max(px / radius, 0.0015);

                    // Flares should be visible at and near the star edge
                    // No longer need outsideMask since flares are positioned at surface
                    float outsideMask = 1.0;

                    float flareBrightBoost = max(0.0, 1.0 + 0.35 * (_Brightness - 1.0));
                    const float tilt = 0.65; // Hardcoded near/far tilt

                    // Star rotation angle for flares (negate to match surface rotation direction)
                    float rotAngle = -angle;

                    // brightness-noise agency for flares
                    float2 dir2 = uv;
                    float dirLen = length(dir2);
                    dir2 = (dirLen > 1e-6) ? (dir2 / dirLen) : float2(1, 0);

                    float3 rimN = safeNormalize(float3(dir2.x, dir2.y, 0.0));
                    float3 rimNR = rotateAroundAxis(rimN, axis, angle);
                    float nFl = triplanarChunkyNoise(rimNR, _SurfaceScale, timeOff);
                    float flSurf = pow(max(nFl, 1e-5), _SurfaceContrast);
                    float flSurfMix = lerp(1.0, flSurf, _SurfaceStrength);

                    float flSteps = max(2.0, _PaletteSteps);
                    flSurfMix = floor(flSurfMix * flSteps) / flSteps;
                    float flareNoiseMul = lerp(0.60, 1.40, saturate(flSurfMix));

                    float3 flareCol = SampleRamp(gradT_body, 3.0);

                    [unroll]
                    for (int i = 0; i < MAX_RINGS; i++)
                    {
                        if (i >= count) break;
                        float fi = (float)i;
                        float seed = _FlareRingSeed + fi * 37.17;

                        float r0 = hash11(seed + 1.23);
                        float r1 = hash11(seed + 9.87);
                        float r2 = hash11(seed + 21.4);
                        float r3 = hash11(seed + 44.0);

                        // Fixed angular position on star surface (anchor point)
                        float baseAng = (fi / max(1.0, (float)count)) * TWO_PI;
                        baseAng += (r0 - 0.5) * 0.75;

                        // Calculate 3D anchor position on sphere surface
                        float2 anchorUV = float2(cos(baseAng), sin(baseAng));
                        float anchorZ = sqrt(max(0.0, 1.0 - dot(anchorUV, anchorUV)));
                        float3 anchorPos = float3(anchorUV.x, anchorUV.y, anchorZ);

                        // Rotate anchor with the star
                        float3 anchorRotated = rotateAroundAxis(anchorPos, axis, rotAngle);

                        // Visibility: skip flares on far side
                        float visibility = smoothstep(-0.1, 0.2, anchorRotated.z);
                        if (visibility < 0.01) continue;

                        // Calculate lifecycle
                        const float basePeriod = 6.0;
                        const float baseDuty = 0.55;
                        const float fadeFrac = 0.12;

                        float pJ = lerp(1.0 - _FlareLifeJitter, 1.0 + _FlareLifeJitter, hash11(seed + 101.1));
                        float dJ = lerp(1.0 - _FlareLifeDutyJitter, 1.0 + _FlareLifeDutyJitter, hash11(seed + 202.2));

                        float period = max(0.25, basePeriod * pJ);
                        float duty   = clamp(baseDuty * dJ, 0.05, 0.95);

                        float phase01 = hash11(seed + 303.3);
                        float life = pulseEnvelope01(timeSec, period, duty, fadeFrac, phase01);

                        // Skip if not alive
                        if (life < 0.01) continue;

                        // Scale loop dimensions with lifecycle
                        float sizeScale = life;
                        float loopHeight = aBase * lerp(0.8, 1.3, r2) * sizeScale;  // Peak height of the loop
                        float loopWidth = bBase * lerp(0.8, 1.2, r3) * sizeScale;   // Angular width of the loop
                        float strokeWidth = wBase * sizeScale;

                        // Create loop/arc in 3D:
                        // The loop extends radially outward from the anchor, reaches a peak height,
                        // then curves back down to the surface
                        // We'll sample points along the loop and find the closest distance to pixel

                        float minDist = 999.0;
                        float closestT = 0.0;
                        const int LOOP_SAMPLES = 16;

                        [unroll]
                        for (int s = 0; s < LOOP_SAMPLES; s++)
                        {
                            float t = (float)s / (float)(LOOP_SAMPLES - 1); // 0 to 1 along the loop

                            // Loop shape: parabolic arc
                            // t=0: at anchor point (surface)
                            // t=0.5: at peak height
                            // t=1: back at surface
                            float height = loopHeight * (1.0 - 4.0 * (t - 0.5) * (t - 0.5)); // Parabola

                            // Angular offset along the surface (creates the "loop" effect)
                            float angOffset = (t - 0.5) * loopWidth;

                            // Calculate 3D position on loop (before rotation)
                            // Start from anchor, extend outward + rotate around anchor
                            float localAng = baseAng + angOffset;
                            float3 loopDir = normalize(float3(cos(localAng), sin(localAng), 0));
                            float3 loopPos = loopDir * (1.0 + height);

                            // Rotate the loop point with the star
                            float3 loopRotated = rotateAroundAxis(loopPos, axis, rotAngle);

                            // Project to 2D
                            float2 loopProj = loopRotated.xy;

                            // Distance from current pixel to this loop point
                            float dist = length(uv - loopProj);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                closestT = t;
                            }
                        }

                        // Render the loop as a stroke
                        float stroke = 1.0 - smoothstep(strokeWidth * 0.5, strokeWidth * 0.5 + aaN, minDist);

                        // Breakup noise along the loop
                        if (_FlareRingBreakup > 0.001)
                        {
                            const float breakupScale = 18.0;
                            float nB = valueNoise2D(float2(closestT * breakupScale, timeSec * 0.55 + seed));
                            float breakup = lerp(1.0, lerp(0.55, 1.25, nB), _FlareRingBreakup);
                            stroke *= breakup;
                        }

                        // Brightness variation along the loop (brighter at peak)
                        float loopBrightness = lerp(0.7, 1.0, 1.0 - abs(closestT - 0.5) * 2.0);

                        // Flicker
                        const float flickerSpeed = 2.0;
                        const float flickerAmt = 0.35;
                        float flick = 1.0;
                        if (flickerAmt > 0.001)
                        {
                            float tF = timeSec * flickerSpeed;
                            float a0 = hash11(seed * 3.1 + floor(tF));
                            float a1 = hash11(seed * 3.1 + floor(tF) + 1.0);
                            float ft = frac(tF);
                            ft = ft * ft * (3.0 - 2.0 * ft);
                            float f = lerp(a0, a1, ft);
                            flick = lerp(1.0, f, flickerAmt);
                        }

                        float m = stroke * life * visibility;
                        flareMask = saturate(flareMask + m);

                        float flareVal = m * _FlareIntensity * loopBrightness * flick * flareBrightBoost * bBoost;
                        flareVal *= flareNoiseMul;

                        // Posterization
                        const float posterizeSteps = 6.0;
                        if (posterizeSteps >= 2.0)
                            flareVal = floor(flareVal * posterizeSteps) / posterizeSteps;

                        flareRGB += flareCol * flareVal;
                    }

                    flareMask = saturate(flareMask);
                }

                int dm = (int)round(_DebugMode);
                if (dm == 1) return float4(disc.xxx, 1);
                if (dm == 2) return float4(surfaceNoise.xxx, 1);
                if (dm == 3) return float4(spotsMask.xxx, 1);
                if (dm == 4) return float4(glow.xxx, 1);
                if (dm == 6) return float4(flareMask.xxx, 1);

                // SpinPin debug visualization (dm == 5)
                if (dm == 5)
                {
                    float3 ax = axis;
                    float2 a2 = ax.xy;
                    float a2Len = length(a2);
                    float aa = max(px / radius, 0.0015);

                    float rrF = dot(uvF, uvF);
                    float insideDisc = 1.0 - step(1.0, sqrt(rrF));
                    float3 bg = insideDisc * 0.06;

                    if (a2Len < 1e-4)
                    {
                        float endR0 = max(_DebugPinEndRadius, 1e-5);
                        float centerDot = 1.0 - smoothstep(endR0, endR0 + aa, length(uvF));
                        float3 dbg0 = bg + centerDot * float3(0.35, 0.85, 1.0);
                        return float4(dbg0, 1);
                    }

                    float2 dir = a2 / a2Len;
                    float poleLen = a2Len;
                    float extend = max(_DebugPinExtend, 0.0);
                    float outerLen = poleLen * (1.0 + extend);

                    float distLine = abs(dir.x * uvF.y - dir.y * uvF.x);
                    float along = dot(uvF, dir);

                    float width = max(_DebugPinWidth, 1e-5);
                    float stroke = 1.0 - smoothstep(width, width + aa, distLine);

                    float segOuter = 1.0 - smoothstep(outerLen, outerLen + aa, abs(along));
                    float segInner = 1.0 - smoothstep(poleLen,  poleLen  + aa, abs(along));

                    float insideMask  = segInner * stroke * _DebugPinInsideStrength;
                    float outsideMask2 = saturate(segOuter - segInner) * stroke;

                    float3 dbg = bg
                               + outsideMask2 * float3(0.35, 0.85, 1.0)
                               + insideMask  * float3(1.0, 1.0, 1.0);

                    return float4(dbg, 1);
                }

                float3 finalRGB = color + glowRGB + flareRGB;

                float alphaBody  = disc * _BodyAlpha;
                float alphaGlow  = saturate(glow * _GlowAlpha);
                float alphaFlare = flareMask * 0.55; // Hardcoded flare alpha

                float finalA = saturate(alphaBody + alphaGlow + alphaFlare);
                return float4(finalRGB, finalA);
            }
            ENDHLSL
        }
    }
}
