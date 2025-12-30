using UnityEngine;

namespace BrainlessLabs.Simulation
{
    [CreateAssetMenu(fileName = "ProceduralStarPreset", menuName = "Simulation/Procedural Star Preset")]
    public class ProceduralStarPreset : ScriptableObject
    {
        [Header("Ramp Rows (bottom->top)")]
        public Gradient bodyLow;
        public Gradient bodyHigh;
        public Gradient glow;
        public Gradient flare;
        public Gradient spot;

        [Header("General")]
        [Range(0.0f, 0.1f)] public float edgeSoftnessWorld = 0.002f;
        [Range(0, 1)] public float bodyAlpha = 1f;
        [Range(0, 3)] public float brightness = 1.0f;
        [Range(0, 1)] public float rimStrength = 0.08f;
        [Range(0, 1)] public float rimStart = 0.82f;

        [Header("Color/Palette")]
        [Range(0, 1)] public float colorFromNoise = 0.85f;
        [Range(2, 32)] public float paletteSteps = 6f;

        [Header("Animation")]
        [Range(0, 4)] public float timeScale = 1f;
        [Range(-0.5f, 0.5f)] public float rotationRPS = 0.06f;

        [Header("Surface")]
        [Range(1, 512)] public float surfaceScale = 80f;
        [Range(0, 1)] public float surfaceStrength = 0.75f;
        [Range(0.1f, 8)] public float surfaceContrast = 2.0f;
        [Range(0, 2)] public float evolveStrength = 0.25f;
        [Range(-2, 2)] public float evolveX = 0.05f;
        [Range(-2, 2)] public float evolveY = 0.02f;

        [Header("Spots")]
        [Range(0, 1)] public float spotsEnabled = 1f;
        [Range(1, 512)] public float spotScale = 22f;
        [Range(1, 16)] public float spotDetailScaleMul = 5f;
        [Range(0, 1)] public float spotDetailStrength = 0.35f;
        [Range(0, 2)] public float spotVortexStrength = 0.65f;
        [Range(1, 256)] public float spotVortexScale = 42f;
        [Range(0, 4)] public float spotVortexSpeed = 1.25f;
        [Range(0, 1)] public float spotThreshold = 0.55f;
        [Range(0, 0.5f)] public float spotSoftness = 0.18f;
        [Range(0, 1)] public float spotIntensity = 0.45f;
        [Range(0, 1)] public float spotTintStrength = 0.35f;

        [Header("Glow")]
        [Range(0, 1)] public float glowEnabled = 1f;
        [Range(0, 10)] public float glowWidthWorld = 0.10f;
        [Range(0.1f, 8)] public float glowPower = 2.0f;
        [Range(0.1f, 8)] public float glowFalloff = 2.5f;
        [Range(0, 4)] public float glowIntensity = 0.7f;
        [Range(0, 1)] public float glowAlpha = 0.5f;
        [Range(0, 1)] public float glowEdgeSoftWorld = 0.01f;
        [Range(0, 2)] public float glowBrightnessInfluence = 0.75f;

        [Header("Flares")]
        [Range(0, 1)] public float flaresEnabled = 1f;
        [Range(0, 6)] public float flareRingCount = 1f;
        [Range(0, 1000)] public float flareRingSeed = 13f;
        [Range(-0.5f, 0.5f)] public float flareRingOrbitRPS = 0.04f;
        [Range(0, 10)] public float flareRingMajorWorld = 0.16f;
        [Range(0, 10)] public float flareRingMinorWorld = 0.06f;
        [Range(0, 2)] public float flareRingWidthWorld = 0.02f;
        [Range(0, 1)] public float flareRingTilt = 0.65f;
        [Range(0, 2)] public float flareRingNear = 1.0f;
        [Range(0, 2)] public float flareRingFar = 0.35f;
        [Range(0, 1)] public float flareRingRimOverlapWorld = 0.01f;
        [Range(0, 1)] public float flareRingBreakup = 0.45f;
        [Range(1, 64)] public float flareRingBreakupScale = 18f;
        [Range(0, 8)] public float flareRingFlickerSpeed = 2.0f;
        [Range(0, 1)] public float flareRingFlickerAmt = 0.35f;

        [Header("Flare Lifetime")]
        [Range(0, 1)] public float flareLifeEnabled = 1f;
        [Range(0.25f, 30)] public float flareLifePeriod = 6.0f;
        [Range(0.05f, 0.95f)] public float flareLifeDuty = 0.55f;
        [Range(0, 0.45f)] public float flareLifeFadeFrac = 0.12f;
        [Range(0, 0.8f)] public float flareLifeJitter = 0.25f;
        [Range(0, 0.6f)] public float flareLifeDutyJitter = 0.20f;
        [Range(0, 6)] public float flareIntensity = 1.2f;
        [Range(0, 1)] public float flareAlpha = 0.55f;
        [Range(0, 32)] public float flarePosterizeSteps = 6f;

        private void Reset()
        {
            ResetToDefaults();
        }

        public void ResetToDefaults()
        {
            bodyLow = MakeFlatGradient(new Color(0.45f, 0.03f, 0.02f, 1f));
            bodyHigh = MakeFlatGradient(new Color(1.00f, 0.35f, 0.10f, 1f));
            glow = MakeFlatGradient(new Color(0.18f, 0.03f, 0.03f, 1f));
            flare = MakeFlatGradient(new Color(0.95f, 0.35f, 0.15f, 1f));
            spot = MakeFlatGradient(new Color(0.20f, 0.02f, 0.02f, 1f));
        }

        private static Gradient MakeFlatGradient(Color c)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) }
            );
            return g;
        }
    }
}
