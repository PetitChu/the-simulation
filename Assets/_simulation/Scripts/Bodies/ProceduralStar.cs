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

        // M2: Atlas rebake optimization - cache hash to avoid unnecessary rebakes
        private int _lastAtlasHash;
        private bool _hasAtlasHash;
        private bool _atlasDirty = false;

        private void OnEnable()
        {
            BakeAndApply();
        }

        private void OnValidate()
        {
            // Mark atlas dirty when inspector values change
            // This ensures gradients and bake settings are re-evaluated
            MarkAtlasDirty();
            BakeAndApply();
        }
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

            // Apply preset floats if preset is set
            if (preset != null)
            {
                ApplyPreset(preset);
            }

            // Ensure component gradients have defaults
            EnsureDefaults();

            // Choose which gradients to bake
            bool usePreset = preset != null;
            Gradient row0 = usePreset ? preset.bodyLow : bodyLow;
            Gradient row1 = usePreset ? preset.bodyHigh : bodyHigh;
            Gradient row2 = usePreset ? preset.glow : glow;
            Gradient row3 = usePreset ? preset.flare : flare;
            Gradient row4 = usePreset ? preset.spot : spot;

            // Bake atlas only if needed (hash-based caching)
            BakeRampAtlasIfDirty(row0, row1, row2, row3, row4);
        }

        /// <summary>
        /// Applies only the float parameters from the visual config to the material.
        /// Does NOT touch gradients or rebake the atlas.
        /// </summary>
        public void ApplyFloatsOnly(ProceduralStarVisualConfig cfg)
        {
            if (targetMaterial == null)
            {
                Debug.LogWarning("ProceduralStar.ApplyFloatsOnly: targetMaterial is null. Cannot apply config.", this);
                return;
            }

            // Set all float material properties
            targetMaterial.SetFloat("_RadiusWorld", cfg.radiusWorld);
            targetMaterial.SetFloat("_EdgeSoftnessWorld", cfg.edgeSoftnessWorld);
            targetMaterial.SetFloat("_BodyAlpha", cfg.bodyAlpha);
            targetMaterial.SetFloat("_Brightness", cfg.brightness);

            targetMaterial.SetFloat("_TimeScale", cfg.timeScale);
            targetMaterial.SetFloat("_RotationRPS", cfg.rotationRPS);

            targetMaterial.SetFloat("_SurfaceContrast", cfg.surfaceContrast);

            targetMaterial.SetFloat("_SpotsEnabled", cfg.spotsEnabled);
            targetMaterial.SetFloat("_SpotThreshold", cfg.spotThreshold);
            targetMaterial.SetFloat("_SpotIntensity", cfg.spotIntensity);
            targetMaterial.SetFloat("_SpotTintStrength", cfg.spotTintStrength);
            targetMaterial.SetFloat("_SpotVortexStrength", cfg.spotVortexStrength);
            targetMaterial.SetFloat("_SpotVortexScale", cfg.spotVortexScale);
            targetMaterial.SetFloat("_SpotVortexSpeed", cfg.spotVortexSpeed);

            targetMaterial.SetFloat("_GlowEnabled", cfg.glowEnabled);
            targetMaterial.SetFloat("_GlowWidthWorld", cfg.glowWidthWorld);
            targetMaterial.SetFloat("_GlowIntensity", cfg.glowIntensity);
            targetMaterial.SetFloat("_GlowAlpha", cfg.glowAlpha);

            targetMaterial.SetFloat("_FlaresEnabled", cfg.flaresEnabled);
            targetMaterial.SetFloat("_FlareRingCount", cfg.flareRingCount);
            targetMaterial.SetFloat("_FlareRingSeed", cfg.flareRingSeed);
            targetMaterial.SetFloat("_FlareRingMajorWorld", cfg.flareRingMajorWorld);
            targetMaterial.SetFloat("_FlareRingMinorWorld", cfg.flareRingMinorWorld);
            targetMaterial.SetFloat("_FlareRingWidthWorld", cfg.flareRingWidthWorld);
            targetMaterial.SetFloat("_FlareRingBreakup", cfg.flareRingBreakup);
            targetMaterial.SetFloat("_FlareLifeJitter", cfg.flareLifeJitter);
            targetMaterial.SetFloat("_FlareLifeDutyJitter", cfg.flareLifeDutyJitter);
            targetMaterial.SetFloat("_FlareIntensity", cfg.flareIntensity);
        }

        /// <summary>
        /// Applies gradients from the config and rebakes the atlas only if they changed.
        /// </summary>
        public void ApplyGradientsAndRebakeIfNeeded(ProceduralStarVisualConfig cfg, bool force = false)
        {
            if (force)
            {
                // Force path: always apply gradients and force a rebake regardless of hash equality.
                bodyLow = cfg.bodyLow;
                bodyHigh = cfg.bodyHigh;
                glow = cfg.glow;
                flare = cfg.flare;
                spot = cfg.spot;

                // Ensure atlas is treated as needing a full rebake.
                MarkAtlasDirty();
                _hasAtlasHash = false;
            }
            else
            {
                // Check if gradients changed by computing hash
                int newHash = ComputeAtlasHash(cfg.bodyLow, cfg.bodyHigh, cfg.glow, cfg.flare, cfg.spot);

                if (!_hasAtlasHash || newHash != _lastAtlasHash)
                {
                    // Gradients changed - copy them into component fields
                    bodyLow = cfg.bodyLow;
                    bodyHigh = cfg.bodyHigh;
                    glow = cfg.glow;
                    flare = cfg.flare;
                    spot = cfg.spot;

                    MarkAtlasDirty();
                }
            }
            // Bake atlas if needed (will check dirty flag + hash)
            BakeRampAtlasIfDirty(bodyLow, bodyHigh, glow, flare, spot);
        }

        /// <summary>
        /// Applies a runtime visual configuration to the star.
        /// M2 optimization: separates float updates from gradient rebakes.
        /// Float-only changes don't trigger atlas rebakes.
        /// </summary>
        public void ApplyVisualConfig(ProceduralStarVisualConfig cfg)
        {
            if (targetMaterial == null)
            {
                Debug.LogWarning("ProceduralStar.ApplyVisualConfig: targetMaterial is null. Cannot apply config.", this);
                return;
            }

            // Force preset to null since runtime config is the source of truth
            preset = null;

            // Apply float parameters (no atlas rebake)
            ApplyFloatsOnly(cfg);

            // Apply gradients and rebake atlas only if gradients changed (hash-based caching)
            ApplyGradientsAndRebakeIfNeeded(cfg);
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

        /// <summary>
        /// Core atlas baking logic: creates/resizes atlas and fills pixels from 5 gradients.
        /// Applies only the ramp atlas texture and its row-count property, not the other float shader parameters (those are set by ApplyFloatsOnly).
        /// </summary>
        private void BakeRampAtlas(Gradient row0, Gradient row1, Gradient row2, Gradient row3, Gradient row4)
        {
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

            FillRow(pixels, 0, row0);
            FillRow(pixels, 1, row1);
            FillRow(pixels, 2, row2);
            FillRow(pixels, 3, row3);
            FillRow(pixels, 4, row4);

            _atlas.SetPixels(pixels);
            _atlas.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            targetMaterial.SetTexture(rampTexProperty, _atlas);
            targetMaterial.SetFloat(rampRowsProperty, ROWS);
        }

        /// <summary>
        /// Bakes the ramp atlas only if gradients or bake settings changed.
        /// Uses hash caching to avoid redundant rebakes.
        /// </summary>
        private void BakeRampAtlasIfDirty(Gradient row0, Gradient row1, Gradient row2, Gradient row3, Gradient row4)
        {
            int newHash = ComputeAtlasHash(row0, row1, row2, row3, row4);
            var desiredFormat = hdr ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R8G8B8A8_UNorm;

            bool needsRebake = _atlasDirty
                            || !_hasAtlasHash
                            || newHash != _lastAtlasHash
                            || _atlas == null
                            || _atlas.width != width
                            || _atlas.height != ROWS
                            || _atlas.graphicsFormat != desiredFormat
                            || targetMaterial.GetTexture(rampTexProperty) != _atlas;

            if (needsRebake)
            {
                _atlasDirty = false;
                _hasAtlasHash = true;
                _lastAtlasHash = newHash;

                BakeRampAtlas(row0, row1, row2, row3, row4);
            }
            else
            {
                // No rebake needed, but ensure material binding is correct without redundant updates
                if (targetMaterial.GetTexture(rampTexProperty) != _atlas)
                {
                    targetMaterial.SetTexture(rampTexProperty, _atlas);
                }

                if (!Mathf.Approximately(targetMaterial.GetFloat(rampRowsProperty), ROWS))
                {
                    targetMaterial.SetFloat(rampRowsProperty, ROWS);
                }
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

        /// <summary>
        /// Mark the atlas as dirty so it will rebake on next apply.
        /// </summary>
        private void MarkAtlasDirty() => _atlasDirty = true;

        /// <summary>
        /// Hash a single gradient to detect changes.
        /// </summary>
        private static int HashGradient(Gradient g)
        {
            if (g == null) return 0;

            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (int)g.mode;

                // Hash color keys
                foreach (var ck in g.colorKeys)
                {
                    hash = hash * 31 + ck.time.GetHashCode();
                    hash = hash * 31 + ck.color.r.GetHashCode();
                    hash = hash * 31 + ck.color.g.GetHashCode();
                    hash = hash * 31 + ck.color.b.GetHashCode();
                }

                // Hash alpha keys
                foreach (var ak in g.alphaKeys)
                {
                    hash = hash * 31 + ak.time.GetHashCode();
                    hash = hash * 31 + ak.alpha.GetHashCode();
                }

                return hash;
            }
        }

        /// <summary>
        /// Compute combined hash of all atlas bake-affecting settings.
        /// </summary>
        private int ComputeAtlasHash(Gradient row0, Gradient row1, Gradient row2, Gradient row3, Gradient row4)
        {
            unchecked
            {
                int hash = 17;
                // Include bake settings
                hash = hash * 31 + width;
                hash = hash * 31 + (hdr ? 1 : 0);
                hash = hash * 31 + (bakeLinear ? 1 : 0);

                // Include all gradient content
                hash = hash * 31 + HashGradient(row0);
                hash = hash * 31 + HashGradient(row1);
                hash = hash * 31 + HashGradient(row2);
                hash = hash * 31 + HashGradient(row3);
                hash = hash * 31 + HashGradient(row4);

                return hash;
            }
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