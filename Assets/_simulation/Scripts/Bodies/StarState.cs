using UnityEngine;

namespace BrainlessLabs.Simulation
{
    [System.Serializable]
    public struct StarState
    {
        // Core stats
        public float mass;
        public float radiusMul;
        public float temperature;
        public float rotationRps;
        public float magneticActivity;
        public float metallicity;

        // Fantasy knobs (for "any type possible")
        public float chroma;
        public float exotic;
        public float chaos;
        public float phase;

        // Other
        public int seed;

        public static StarState Default => new StarState
        {
            mass = 1.0f,
            radiusMul = 1.0f,
            temperature = 0.55f,
            rotationRps = 0.06f,
            magneticActivity = 0.15f,
            metallicity = 0.25f,
            chroma = 0f,
            exotic = 0f,
            chaos = 0f,
            phase = 0f,
            seed = 13
        };

        public StarState Clamped()
        {
            return new StarState
            {
                mass = Mathf.Clamp(mass, 0.25f, 10f),
                radiusMul = Mathf.Clamp(radiusMul, 0.25f, 5f),
                temperature = Mathf.Clamp01(temperature),
                rotationRps = Mathf.Clamp(rotationRps, -0.5f, 0.5f),
                magneticActivity = Mathf.Clamp01(magneticActivity),
                metallicity = Mathf.Clamp01(metallicity),
                chroma = Mathf.Clamp01(chroma),
                exotic = Mathf.Clamp01(exotic),
                chaos = Mathf.Clamp01(chaos),
                phase = Mathf.Clamp01(phase),
                seed = seed
            };
        }
    }
}
