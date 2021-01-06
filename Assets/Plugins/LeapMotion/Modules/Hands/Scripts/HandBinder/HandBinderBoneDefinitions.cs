using UnityEngine;

namespace Leap.Unity.HandsModule {

    [CreateAssetMenu(fileName = "HandBinderBoneDefinitions", menuName = "Ultraleap/HandBinderBoneDefinitions", order = 1)]
    public class HandBinderBoneDefinitions : ScriptableObject {
        public BoneDefinitions BoneDefinitions = new BoneDefinitions();
    }

    /// <summary>
    /// Used to define what bones names are valid for each finger
    /// </summary>
    [System.Serializable]
    public class BoneDefinitions {
        public string[] DefinitionThumb = { "thumb" };
        public string[] DefinitionIndex = { "index" };
        public string[] DefinitionMiddle = { "middle" };
        public string[] DefinitionRing = { "ring" };
        public string[] DefinitionPinky = { "pinky", "little" };
        public string[] DefinitionWrist = { "wrist", "hand", "palm" };
        public string[] DefinitionElbow = { "elbow", "upperArm" };
    }
}