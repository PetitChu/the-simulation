using UnityEngine;

namespace BrainlessLabs.Simulation
{
    /// <summary>
    /// Static mapper that converts a StarState into a ProceduralStarVisualConfig.
    /// This is where all the "gameplay stat -> visual appearance" logic lives.
    /// </summary>
    public static class StarVisualMapper
    {
        public static void Build(in StarState input, ProceduralStarVisualConfig outCfg)
        {
            // A) Clamp state
            var s = input.Clamped();

            // B) Compute derived basics
            float radiusWorld = Mathf.Clamp(
                0.25f * Mathf.Pow(s.mass, 0.6f) * s.radiusMul,
                0.05f,
                5f
            );

            // Edge softness scales with radius (pixel size left unchanged)
            float edgeSoftnessWorld = Mathf.Clamp(
                radiusWorld * 0.008f,
                0f,
                0.1f
            );

            // C) Brightness & timing
            float brightness = Mathf.Clamp(
                0.6f + Mathf.Pow(s.mass, 1.15f) * 0.2f + s.temperature * 0.2f,
                0.1f,
                1.5f
            );

            float timeScale = 1f + s.chaos * 0.5f;
            float rotationRps = s.rotationRps;

            // D) Spots - driven by magneticActivity + chaos/exotic
            float spotsEnabled = (s.magneticActivity > 0.05f) ? 1f : 0f;
            float spotThreshold = Mathf.Lerp(0.70f, 0.35f, s.magneticActivity);
            float spotIntensity = Mathf.Lerp(0.20f, 0.65f, s.magneticActivity);
            float spotTintStrength = Mathf.Clamp(
                0.15f + 0.6f * s.metallicity + 0.3f * s.magneticActivity,
                0f,
                1f
            );

            // Vortex/breakup stronger with activity and chaos
            float spotVortexStrength = Mathf.Lerp(0.3f, 1.2f, s.magneticActivity * 0.7f + s.chaos * 0.3f);
            float spotVortexScale = 42f;
            float spotVortexSpeed = 1.25f + s.chaos * 0.5f;

            // E) Flares - driven by magneticActivity + exotic
            float flaresEnabled = (s.magneticActivity > 0.10f) ? 1f : 0f;
            float flareRingCount = Mathf.Round(Mathf.Lerp(0f, 6f, s.magneticActivity));
            float flareIntensity = Mathf.Clamp(
                0.4f + 2.0f * s.magneticActivity + 1.2f * s.exotic,
                0f,
                6f
            );

            // Ring sizes scale with radius
            float flareRingOffsetWorld = radiusWorld * 0.9f;
            float flareRingMajorWorld = radiusWorld * 0.65f;
            float flareRingMinorWorld = radiusWorld * 0.25f;
            float flareRingWidthWorld = Mathf.Clamp(radiusWorld * 0.08f, 0f, 2f);

            // Breakup/jitter from chaos
            float flareRingBreakup = Mathf.Lerp(0.2f, 0.85f, s.chaos);
            float flareLifeJitter = Mathf.Lerp(0.05f, 0.6f, s.chaos);
            float flareLifeDutyJitter = Mathf.Lerp(0.05f, 0.4f, s.chaos);

            // Seed
            int flareRingSeed = s.seed;

            // Orbit - locked to star rotation (not independent orbit)
            float flareRingOrbitRPS = rotationRps;

            // F) Glow scales with brightness and radius
            float glowEnabled = 1f;
            float glowWidthWorld = Mathf.Clamp(radiusWorld * 0.40f, 0f, 10f);
            float glowIntensity = Mathf.Clamp(
                0.4f + brightness * 0.35f + s.exotic * 0.4f,
                0f,
                4f
            );
            float glowAlpha = Mathf.Clamp(0.35f + s.magneticActivity * 0.25f, 0f, 1f);

            // G) Color + 5 gradients
            Color baseColor = TemperatureToColor(s.temperature, s.chroma, s.phase, s.exotic);

            // Build gradients
            BuildGradients(baseColor, outCfg);

            // H) Fill outCfg with all computed values
            // Only set gameplay-driven parameters, not art parameters
            outCfg.radiusWorld = radiusWorld;
            outCfg.edgeSoftnessWorld = edgeSoftnessWorld;

            outCfg.bodyAlpha = 1f;
            outCfg.brightness = brightness;

            outCfg.timeScale = timeScale;
            outCfg.rotationRPS = rotationRps;

            outCfg.surfaceContrast = Mathf.Lerp(5.0f, 1.0f, s.chaos);

            outCfg.spotsEnabled = spotsEnabled;
            outCfg.spotVortexStrength = spotVortexStrength;
            outCfg.spotVortexScale = spotVortexScale;
            outCfg.spotVortexSpeed = spotVortexSpeed;
            outCfg.spotThreshold = spotThreshold;
            outCfg.spotIntensity = spotIntensity;
            outCfg.spotTintStrength = spotTintStrength;

            outCfg.glowEnabled = glowEnabled;
            outCfg.glowWidthWorld = glowWidthWorld;
            outCfg.glowIntensity = glowIntensity;
            outCfg.glowAlpha = glowAlpha;

            outCfg.flaresEnabled = flaresEnabled;
            outCfg.flareRingCount = flareRingCount;
            outCfg.flareRingSeed = flareRingSeed;
            outCfg.flareRingOrbitRPS = flareRingOrbitRPS;
            outCfg.flareRingOffsetWorld = flareRingOffsetWorld;
            outCfg.flareRingMajorWorld = flareRingMajorWorld;
            outCfg.flareRingMinorWorld = flareRingMinorWorld;
            outCfg.flareRingWidthWorld = flareRingWidthWorld;
            outCfg.flareRingBreakup = flareRingBreakup;
            outCfg.flareLifeJitter = flareLifeJitter;
            outCfg.flareLifeDutyJitter = flareLifeDutyJitter;
            outCfg.flareIntensity = flareIntensity;
        }

        /// <summary>
        /// Converts temperature (0-1) to a base star color with chroma, phase, and exotic modulation.
        /// </summary>
        private static Color TemperatureToColor(float t01, float chroma, float phase, float exotic)
        {
            // Piecewise lerp between anchor colors
            // 0.0: deep red
            // 0.25: orange
            // 0.5: warm white/yellow
            // 0.75: cool white/blue
            // 1.0: blue

            Color c;
            if (t01 < 0.25f)
            {
                // Deep red -> orange
                float t = t01 / 0.25f;
                Color deepRed = new Color(0.6f, 0.05f, 0.02f);
                Color orange = new Color(0.9f, 0.35f, 0.10f);
                c = Color.Lerp(deepRed, orange, t);
            }
            else if (t01 < 0.5f)
            {
                // Orange -> warm yellow
                float t = (t01 - 0.25f) / 0.25f;
                Color orange = new Color(0.9f, 0.35f, 0.10f);
                Color warmYellow = new Color(0.85f, 0.75f, 0.50f);
                c = Color.Lerp(orange, warmYellow, t);
            }
            else if (t01 < 0.75f)
            {
                // Warm yellow -> cool tint
                float t = (t01 - 0.5f) / 0.25f;
                Color warmYellow = new Color(0.85f, 0.75f, 0.50f);
                Color coolTint = new Color(0.65f, 0.70f, 0.85f);
                c = Color.Lerp(warmYellow, coolTint, t);
            }
            else
            {
                // Cool tint -> blue
                float t = (t01 - 0.75f) / 0.25f;
                Color coolTint = new Color(0.65f, 0.70f, 0.85f);
                Color blue = new Color(0.4f, 0.5f, 0.85f);
                c = Color.Lerp(coolTint, blue, t);
            }

            // Apply chroma (increases saturation)
            if (chroma > 0.001f)
            {
                Color.RGBToHSV(c, out float h, out float s, out float v);
                s = Mathf.Lerp(s, 1f, chroma * 0.5f);
                c = Color.HSVToRGB(h, s, v);
            }

            // Apply phase (hue shift) + exotic (saturation/value boost toward "weird")
            if (phase > 0.001f || exotic > 0.001f)
            {
                Color.RGBToHSV(c, out float h, out float s, out float v);

                // Phase: small hue shift
                h = Mathf.Repeat(h + phase * 0.2f, 1f);

                // Exotic: push toward more saturated and brighter
                s = Mathf.Lerp(s, 1f, exotic * 0.3f);
                v = Mathf.Lerp(v, 1f, exotic * 0.2f);

                c = Color.HSVToRGB(h, s, v);
            }

            return c;
        }

        /// <summary>
        /// Builds the 5 gradients from the base color.
        /// </summary>
        private static void BuildGradients(Color baseColor, ProceduralStarVisualConfig cfg)
        {
            // bodyLow: dark base → base → slightly brighter (avoid pure black)
            cfg.bodyLow.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(baseColor * 0.10f, 0f),
                    new GradientColorKey(baseColor * 0.4f, 0.33f),
                    new GradientColorKey(baseColor, 0.67f),
                    new GradientColorKey(baseColor * 1.1f, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            // bodyHigh: mid → base → moderately bright
            cfg.bodyHigh.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(baseColor * 0.3f, 0f),
                    new GradientColorKey(baseColor, 0.33f),
                    new GradientColorKey(baseColor * 1.2f, 0.67f),
                    new GradientColorKey(baseColor * 1.3f, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            // glow: base tint → brighter (avoid pure black)
            Color glowBase = baseColor * 0.6f;
            cfg.glow.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(glowBase * 0.4f, 0f),
                    new GradientColorKey(glowBase, 0.33f),
                    new GradientColorKey(glowBase * 1.3f, 0.67f),
                    new GradientColorKey(glowBase * 1.4f, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            // flare: base → bright → brighter (no white)
            cfg.flare.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(baseColor, 0f),
                    new GradientColorKey(baseColor * 1.2f, 0.33f),
                    new GradientColorKey(baseColor * 1.4f, 0.67f),
                    new GradientColorKey(baseColor * 1.5f, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            // spot: dark base (slightly tinted) across (avoid pure black)
            Color spotColor = baseColor * 0.25f;
            cfg.spot.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(spotColor, 0f),
                    new GradientColorKey(spotColor, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }
    }
}
