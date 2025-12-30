using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace BrainlessLabs.Simulation
{
    public class ProceduralStar : MonoBehaviour
    {
        [Header("Target")] [SerializeField] private Material targetMaterial;
        [SerializeField] private ProceduralStarPreset preset;

        [Header("Atlas Layout")]
        [Tooltip("Width of each ramp row (more = smoother). 128–256 is plenty.")]
        [SerializeField, Range(16, 1024)]
        private int width = 256;

        [Tooltip("Enable HDR ramps (recommended if your shader brightness/glow can exceed 1.0).")] [SerializeField]
        private bool hdr = true;

        [Tooltip("Bake colors as Linear data. Keep true for Linear projects and predictable results.")] [SerializeField]
        private bool bakeLinear = true;

        [Header("Ramp Rows (bottom->top)")] public Gradient bodyLow; // row 0
        public Gradient bodyHigh; // row 1
        public Gradient glow; // row 2
        public Gradient flare; // row 3
        public Gradient spot; // row 4 (optional)

        [Header("Shader Property Names")]
        [SerializeField] private string rampTexProperty = "_RampAtlas";
        [SerializeField] private string rampRowsProperty = "_RampRows";

        // Internals
        private Texture2D _atlas;
        private const int ROWS = 5;

        private void OnEnable()
        {
            BakeAndApply();
        }

        private void OnValidate() => BakeAndApply();
#if UNITY_EDITOR
        private void Update()
        {
            // Optional: if you animate gradients in editor via scripts, uncomment.
            // BakeAndApply();
        }
#endif

        private void OnDisable()
        {
            if (_atlas != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(_atlas);
#else
            Destroy(_atlas);
#endif
                _atlas = null;
            }
        }

        // Unity calls this when the component is added or when you choose "Reset" in the Inspector
        private void Reset()
        {
            // Intentionally do not read default values from targetMaterial here.
            // A previous implementation copied settings from the material during Reset,
            // which could overwrite user configuration when the material was newly created
            // or not yet fully set up, effectively resetting it back to shader defaults.
            // Reset should only initialize this component's fields and must never modify
            // or depend on the current state of the material asset.
            preset = null;
            width = 256;
            hdr = true;
            bakeLinear = true;
            rampTexProperty = "_RampAtlas";
            rampRowsProperty = "_RampRows";

            // Recreate gradients to their defaults
            bodyLow = MakeFlatGradient(new Color(0.45f, 0.03f, 0.02f, 1f));
            bodyHigh = MakeFlatGradient(new Color(1.00f, 0.35f, 0.10f, 1f));
            glow = MakeFlatGradient(new Color(0.18f, 0.03f, 0.03f, 1f));
            flare = MakeFlatGradient(new Color(0.95f, 0.35f, 0.15f, 1f));
            spot = MakeFlatGradient(new Color(0.20f, 0.02f, 0.02f, 1f));

            // Cleanup any generated atlas
            if (_atlas != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(_atlas);
#else
                Destroy(_atlas);
#endif
                _atlas = null;
            }

            // Optionally bake right away if a material is already assigned
            BakeAndApply();
        }

        [ContextMenu("Bake Ramp Atlas Now")]
        public void BakeAndApply()
        {
            if (targetMaterial == null)
            {
                Debug.LogWarning("ProceduralStar.BakeAndApply was called, but 'targetMaterial' is not assigned. Changes will not be applied.", this);
                return;
            }

            if (preset != null)
            {
                ApplyPreset(preset);
            }

            EnsureDefaults();

            // Create/resize atlas if needed
            var desiredFormat = hdr ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R8G8B8A8_UNorm;
            bool needsCreate = _atlas == null || _atlas.width != width || _atlas.height != ROWS;

            if (needsCreate)
            {
                if (_atlas != null)
                {
                    if (Application.isPlaying) Destroy(_atlas);
                    else DestroyImmediate(_atlas);
                }

                _atlas = new Texture2D(width, ROWS, desiredFormat, TextureCreationFlags.None);
                _atlas.name = "StarRampAtlas (Runtime)";
                _atlas.wrapMode = TextureWrapMode.Clamp;
                _atlas.filterMode = FilterMode.Point; // pixel art crisp
                _atlas.anisoLevel = 0;
                _atlas.hideFlags = HideFlags.DontSave;
            }

            // Fill pixels: row-major (y = 0..ROWS-1), each row is a gradient from x=0..width-1
            var pixels = new Color[width * ROWS];

            bool usePreset = preset != null;

            FillRow(pixels, 0, usePreset ? preset.bodyLow : bodyLow);
            FillRow(pixels, 1, usePreset ? preset.bodyHigh : bodyHigh);
            FillRow(pixels, 2, usePreset ? preset.glow : glow);
            FillRow(pixels, 3, usePreset ? preset.flare : flare);
            FillRow(pixels, 4, usePreset ? preset.spot : spot);
            _atlas.SetPixels(pixels);
            _atlas.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            targetMaterial.SetTexture(rampTexProperty, _atlas);
            targetMaterial.SetFloat(rampRowsProperty, ROWS);
        }

        private void ApplyPreset(ProceduralStarPreset p)
        {
            targetMaterial.SetFloat("_EdgeSoftnessWorld", p.edgeSoftnessWorld);
            targetMaterial.SetFloat("_BodyAlpha", p.bodyAlpha);
            targetMaterial.SetFloat("_Brightness", p.brightness);
            targetMaterial.SetFloat("_RimStrength", p.rimStrength);
            targetMaterial.SetFloat("_RimStart", p.rimStart);

            targetMaterial.SetFloat("_ColorFromNoise", p.colorFromNoise);
            targetMaterial.SetFloat("_PaletteSteps", p.paletteSteps);

            targetMaterial.SetFloat("_TimeScale", p.timeScale);
            targetMaterial.SetFloat("_RotationRPS", p.rotationRPS);

            targetMaterial.SetFloat("_SurfaceScale", p.surfaceScale);
            targetMaterial.SetFloat("_SurfaceStrength", p.surfaceStrength);
            targetMaterial.SetFloat("_SurfaceContrast", p.surfaceContrast);
            targetMaterial.SetFloat("_EvolveStrength", p.evolveStrength);
            targetMaterial.SetFloat("_EvolveX", p.evolveX);
            targetMaterial.SetFloat("_EvolveY", p.evolveY);

            targetMaterial.SetFloat("_SpotsEnabled", p.spotsEnabled);
            targetMaterial.SetFloat("_SpotScale", p.spotScale);
            targetMaterial.SetFloat("_SpotDetailScaleMul", p.spotDetailScaleMul);
            targetMaterial.SetFloat("_SpotDetailStrength", p.spotDetailStrength);
            targetMaterial.SetFloat("_SpotVortexStrength", p.spotVortexStrength);
            targetMaterial.SetFloat("_SpotVortexScale", p.spotVortexScale);
            targetMaterial.SetFloat("_SpotVortexSpeed", p.spotVortexSpeed);
            targetMaterial.SetFloat("_SpotThreshold", p.spotThreshold);
            targetMaterial.SetFloat("_SpotSoftness", p.spotSoftness);
            targetMaterial.SetFloat("_SpotIntensity", p.spotIntensity);
            targetMaterial.SetFloat("_SpotTintStrength", p.spotTintStrength);

            targetMaterial.SetFloat("_GlowEnabled", p.glowEnabled);
            targetMaterial.SetFloat("_GlowWidthWorld", p.glowWidthWorld);
            targetMaterial.SetFloat("_GlowPower", p.glowPower);
            targetMaterial.SetFloat("_GlowFalloff", p.glowFalloff);
            targetMaterial.SetFloat("_GlowIntensity", p.glowIntensity);
            targetMaterial.SetFloat("_GlowAlpha", p.glowAlpha);
            targetMaterial.SetFloat("_GlowEdgeSoftWorld", p.glowEdgeSoftWorld);
            targetMaterial.SetFloat("_GlowBrightnessInfluence", p.glowBrightnessInfluence);

            targetMaterial.SetFloat("_FlaresEnabled", p.flaresEnabled);
            targetMaterial.SetFloat("_FlareRingCount", p.flareRingCount);
            targetMaterial.SetFloat("_FlareRingSeed", p.flareRingSeed);
            targetMaterial.SetFloat("_FlareRingOrbitRPS", p.flareRingOrbitRPS);
            targetMaterial.SetFloat("_FlareRingOffsetWorld", p.flareRingOffsetWorld);
            targetMaterial.SetFloat("_FlareRingMajorWorld", p.flareRingMajorWorld);
            targetMaterial.SetFloat("_FlareRingMinorWorld", p.flareRingMinorWorld);
            targetMaterial.SetFloat("_FlareRingWidthWorld", p.flareRingWidthWorld);
            targetMaterial.SetFloat("_FlareRingTilt", p.flareRingTilt);
            targetMaterial.SetFloat("_FlareRingNear", p.flareRingNear);
            targetMaterial.SetFloat("_FlareRingFar", p.flareRingFar);
            targetMaterial.SetFloat("_FlareRingRimOverlapWorld", p.flareRingRimOverlapWorld);
            targetMaterial.SetFloat("_FlareRingBreakup", p.flareRingBreakup);
            targetMaterial.SetFloat("_FlareRingBreakupScale", p.flareRingBreakupScale);
            targetMaterial.SetFloat("_FlareRingFlickerSpeed", p.flareRingFlickerSpeed);
            targetMaterial.SetFloat("_FlareRingFlickerAmt", p.flareRingFlickerAmt);

            targetMaterial.SetFloat("_FlareLifeEnabled", p.flareLifeEnabled);
            targetMaterial.SetFloat("_FlareLifePeriod", p.flareLifePeriod);
            targetMaterial.SetFloat("_FlareLifeDuty", p.flareLifeDuty);
            targetMaterial.SetFloat("_FlareLifeFadeFrac", p.flareLifeFadeFrac);
            targetMaterial.SetFloat("_FlareLifeJitter", p.flareLifeJitter);
            targetMaterial.SetFloat("_FlareLifeDutyJitter", p.flareLifeDutyJitter);
            targetMaterial.SetFloat("_FlareIntensity", p.flareIntensity);
            targetMaterial.SetFloat("_FlareAlpha", p.flareAlpha);
            targetMaterial.SetFloat("_FlarePosterizeSteps", p.flarePosterizeSteps);
        }

        private void FillRow(Color[] pixels, int row, Gradient g)
        {
            int y = row;
            int rowStart = y * width;

            for (int x = 0; x < width; x++)
            {
                float t = (width <= 1) ? 0f : (float)x / (width - 1);
                Color c = g.Evaluate(t);

                // Optional: clamp negatives if you do weird HDR keying
                // c.r = Mathf.Max(0, c.r); c.g = Mathf.Max(0, c.g); c.b = Mathf.Max(0, c.b);

                if (bakeLinear)
                    c = c.linear; // consistent for Linear workflows

                pixels[rowStart + x] = c;
            }
        }

        private void EnsureDefaults()
        {
            // Create basic defaults if user left them empty
            bodyLow ??= MakeFlatGradient(new Color(0.45f, 0.03f, 0.02f, 1f));
            bodyHigh ??= MakeFlatGradient(new Color(1.00f, 0.35f, 0.10f, 1f));
            glow ??= MakeFlatGradient(new Color(0.18f, 0.03f, 0.03f, 1f));
            flare ??= MakeFlatGradient(new Color(0.95f, 0.35f, 0.15f, 1f));
            spot ??= MakeFlatGradient(new Color(0.20f, 0.02f, 0.02f, 1f));
        }

        private Gradient MakeFlatGradient(Color c)
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