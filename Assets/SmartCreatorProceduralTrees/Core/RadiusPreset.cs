using UnityEngine;

namespace SmartCreator.ProceduralTrees.Core
{
    [CreateAssetMenu(menuName = "Procedural Trees/Radius-by-Depth Preset")]
    public class RadiusPreset : ScriptableObject
    {
        public AnimationCurve curve = AnimationCurve.Linear(0, 1, 1, 0);
    }
}
