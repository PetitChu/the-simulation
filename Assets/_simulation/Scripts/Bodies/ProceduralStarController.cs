using System.Collections.Generic;
using System.Linq;
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
        /// <summary>
        /// Records a single card application for history/replay
        /// </summary>
        [System.Serializable]
        private struct StarCardHistoryEntry
        {
            public StarCard card;
            public StarState before;
            public StarState after;
            public float time;
        }
        [Header("References")]
        [SerializeField] private ProceduralStar star;

        [Header("Star State")]
        [SerializeField] private StarState state = StarState.Default;

        [Header("Edit Mode")]
        [Tooltip("Apply state changes immediately in Edit mode (requires ExecuteAlways).")]
        [SerializeField] private bool applyInEditMode = true;

        [Header("Cards / Debug")]
        [SerializeField] private bool recordHistory = true;
        [SerializeField] private List<StarCardHistoryEntry> history = new();

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

        /// <summary>
        /// Applies a StarCard to the current state, mutating it via deltas, clamping, and rebuilding/applying the result.
        /// Records the mutation in history if recordHistory is enabled.
        /// </summary>
        public void ApplyCard(StarCard card)
        {
            if (card == null)
            {
                Debug.LogWarning("ProceduralStarController: Cannot apply null card.", this);
                return;
            }

            EnsureReferences();
            EnsureConfig();

            var before = state;

            // Apply deltas
            state = ApplyDeltas(state, card);

            // Clamp to valid ranges
            state = state.Clamped();

            // Record history
            if (recordHistory)
            {
                var entry = new StarCardHistoryEntry
                {
                    card = card,
                    before = before,
                    after = state,
                    time = Application.isPlaying ? Time.time : 0f
                };
                history.Add(entry);
            }

            // Rebuild and apply
            RebuildAndApply();
        }

        /// <summary>
        /// Applies all deltas from a card to the given state and returns the mutated state.
        /// Does NOT clamp - caller must clamp.
        /// </summary>
        private static StarState ApplyDeltas(StarState s, StarCard card)
        {
            if (card.deltas == null)
                return s;

            foreach (var delta in card.deltas)
            {
                switch (delta.stat)
                {
                    case StarStatId.Mass:
                        s.mass = ApplyOp(s.mass, delta.op, delta.value);
                        break;
                    case StarStatId.RadiusMul:
                        s.radiusMul = ApplyOp(s.radiusMul, delta.op, delta.value);
                        break;
                    case StarStatId.Temperature:
                        s.temperature = ApplyOp(s.temperature, delta.op, delta.value);
                        break;
                    case StarStatId.RotationRps:
                        s.rotationRps = ApplyOp(s.rotationRps, delta.op, delta.value);
                        break;
                    case StarStatId.MagneticActivity:
                        s.magneticActivity = ApplyOp(s.magneticActivity, delta.op, delta.value);
                        break;
                    case StarStatId.Metallicity:
                        s.metallicity = ApplyOp(s.metallicity, delta.op, delta.value);
                        break;
                    case StarStatId.Chroma:
                        s.chroma = ApplyOp(s.chroma, delta.op, delta.value);
                        break;
                    case StarStatId.Exotic:
                        s.exotic = ApplyOp(s.exotic, delta.op, delta.value);
                        break;
                    case StarStatId.Chaos:
                        s.chaos = ApplyOp(s.chaos, delta.op, delta.value);
                        break;
                    case StarStatId.Phase:
                        s.phase = ApplyOp(s.phase, delta.op, delta.value);
                        break;
                }
            }

            return s;
        }

        /// <summary>
        /// Applies a single operation to a value
        /// </summary>
        private static float ApplyOp(float current, StatOp op, float value)
        {
            return op switch
            {
                StatOp.Add => current + value,
                StatOp.Multiply => current * value,
                _ => current
            };
        }

        /// <summary>
        /// Resets the star state to default and clears history
        /// </summary>
        [ContextMenu("Cards/Reset State")]
        public void ResetState()
        {
            state = StarState.Default;
            history.Clear();
            RebuildAndApply();
        }

        /// <summary>
        /// Replays all cards in history from default state.
        /// Rebuilds the history with reapplied cards to maintain history consistency.
        /// </summary>
        [ContextMenu("Cards/Replay History")]
        public void ReplayHistory()
        {
            if (history.Count == 0)
            {
                Debug.Log("ProceduralStarController: History is empty, nothing to replay.", this);
                return;
            }

            // Copy cards from history (we'll be clearing it)
            var cardsToReplay = new StarCard[history.Count];
            for (int i = 0; i < history.Count; i++)
            {
                cardsToReplay[i] = history[i].card;
            }

            // Reset to default
            state = StarState.Default;
            history.Clear();

            // Reapply all cards with history recording enabled
            var validCards = cardsToReplay.Where(c => c != null).ToArray();
            foreach (var card in validCards)
            {
                ApplyCard(card);
            }

            Debug.Log($"ProceduralStarController: Replayed {validCards.Length} cards from history.", this);
        }

        /// <summary>
        /// Clears the card application history without changing state
        /// </summary>
        [ContextMenu("Cards/Clear History")]
        public void ClearHistory()
        {
            history.Clear();
            Debug.Log("ProceduralStarController: History cleared.", this);
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
