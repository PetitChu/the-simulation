using UnityEngine;

namespace BrainlessLabs.Simulation
{
    /// <summary>
    /// Runtime visual configuration for a procedural star.
    /// Contains all shader-facing values that the mapper outputs.
    /// </summary>
    public sealed class ProceduralStarVisualConfig
    {
        // Core sizing
        public float radiusWorld;
        public float pixelSizeWorld;
        public float edgeSoftnessWorld;

        // General appearance
        public float bodyAlpha;
        public float brightness;
        public float rimStrength;
        public float rimStart;

        // Color/Palette
        public float colorFromNoise;
        public float paletteSteps;

        // Animation
        public float timeScale;
        public float rotationRPS;

        // Surface detail
        public float surfaceScale;
        public float surfaceStrength;
        public float surfaceContrast;
        public float evolveStrength;
        public float evolveX;
        public float evolveY;

        // Spots
        public float spotsEnabled;
        public float spotScale;
        public float spotDetailScaleMul;
        public float spotDetailStrength;
        public float spotVortexStrength;
        public float spotVortexScale;
        public float spotVortexSpeed;
        public float spotThreshold;
        public float spotSoftness;
        public float spotIntensity;
        public float spotTintStrength;

        // Glow
        public float glowEnabled;
        public float glowWidthWorld;
        public float glowPower;
        public float glowFalloff;
        public float glowIntensity;
        public float glowAlpha;
        public float glowEdgeSoftWorld;
        public float glowBrightnessInfluence;

        // Flares
        public float flaresEnabled;
        public float flareRingCount;
        public float flareRingSeed;
        public float flareRingOrbitRPS;
        public float flareRingOffsetWorld;
        public float flareRingMajorWorld;
        public float flareRingMinorWorld;
        public float flareRingWidthWorld;
        public float flareRingTilt;
        public float flareRingNear;
        public float flareRingFar;
        public float flareRingRimOverlapWorld;
        public float flareRingBreakup;
        public float flareRingBreakupScale;
        public float flareRingFlickerSpeed;
        public float flareRingFlickerAmt;

        // Flare lifetime
        public float flareLifeEnabled;
        public float flareLifePeriod;
        public float flareLifeDuty;
        public float flareLifeFadeFrac;
        public float flareLifeJitter;
        public float flareLifeDutyJitter;
        public float flareIntensity;
        public float flareAlpha;
        public float flarePosterizeSteps;

        // Gradients (5 ramp rows)
        public Gradient bodyLow;
        public Gradient bodyHigh;
        public Gradient glow;
        public Gradient flare;
        public Gradient spot;

        public ProceduralStarVisualConfig()
        {
            // Ensure gradients are never null
            bodyLow = new Gradient();
            bodyHigh = new Gradient();
            glow = new Gradient();
            flare = new Gradient();
            spot = new Gradient();
        }
    }
}
