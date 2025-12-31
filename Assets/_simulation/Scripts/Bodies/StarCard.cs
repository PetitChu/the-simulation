using UnityEngine;

namespace BrainlessLabs.Simulation
{
    /// <summary>
    /// Identifies which stat in StarState to mutate
    /// </summary>
    public enum StarStatId
    {
        Mass,
        RadiusMul,
        Temperature,
        RotationRps,
        MagneticActivity,
        Metallicity,
        Chroma,
        Exotic,
        Chaos,
        Phase
    }

    /// <summary>
    /// Operation to apply when mutating a stat
    /// </summary>
    public enum StatOp
    {
        Add,        // current += value
        Multiply    // current *= value (use factors like 1.1, 0.9, 2.0, etc.)
    }

    /// <summary>
    /// A single mutation to apply to a StarState stat
    /// </summary>
    [System.Serializable]
    public struct StatDelta
    {
        public StarStatId stat;
        public StatOp op;
        public float value;
    }

    /// <summary>
    /// Data-driven card that applies a list of StatDelta mutations to StarState
    /// </summary>
    [CreateAssetMenu(menuName = "Simulation/Star/Star Card", fileName = "StarCard_")]
    public sealed class StarCard : ScriptableObject
    {
        [Header("Card Info")]
        public string title;

        [TextArea]
        public string description;

        [Header("Visual (Optional)")]
        public Sprite icon;
        public int sortOrder;

        [Header("Mutations")]
        public StatDelta[] deltas;
    }
}
