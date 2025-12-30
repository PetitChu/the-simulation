using UnityEngine;

namespace BrainlessLabs.Simulation
{
    /// <summary>
    /// Runtime controller that drives a ProceduralStar from a StarState.
    /// Works in both Edit mode and Play mode.
    /// All stars start identical via StarState.Default.
    /// </summary>
    [ExecuteAlways]
    public class ProceduralStarController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ProceduralStar star;

        [Header("Star State")]
        [SerializeField] private StarState state = StarState.Default;

        [Header("Edit Mode")]
        [Tooltip("Apply state changes immediately in Edit mode (requires ExecuteAlways).")]
        [SerializeField] private bool applyInEditMode = true;

        // Private config instance
        private ProceduralStarVisualConfig _cfg;

        private void Awake()
        {
            EnsureReferences();
            EnsureConfig();
            RebuildAndApply();
        }

        private void OnEnable()
        {
            EnsureReferences();
            EnsureConfig();
            RebuildAndApply();
        }

        private void OnValidate()
        {
            // In edit mode, only apply if the flag is set
            if (!Application.isPlaying && !applyInEditMode)
                return;

            EnsureReferences();
            EnsureConfig();
            RebuildAndApply();
        }

        [ContextMenu("Apply Now")]
        public void RebuildAndApply()
        {
            if (star == null)
            {
                Debug.LogWarning("ProceduralStarController: star is null. Cannot apply.", this);
                return;
            }

            EnsureConfig();

            // Clamp state and update it (keeps inspector values sane)
            var clamped = state.Clamped();
            state = clamped;

            // Map state to visual config
            StarVisualMapper.Build(state, _cfg);

            // Apply to star
            star.ApplyVisualConfig(_cfg);
        }

        private void EnsureReferences()
        {
            // Auto-wire ProceduralStar if not assigned
            if (star == null)
            {
                star = GetComponent<ProceduralStar>();
                if (star == null)
                {
                    Debug.LogWarning("ProceduralStarController: No ProceduralStar component found. Please assign one.", this);
                }
            }
        }

        private void EnsureConfig()
        {
            if (_cfg == null)
            {
                _cfg = new ProceduralStarVisualConfig();
            }
        }
    }
}
