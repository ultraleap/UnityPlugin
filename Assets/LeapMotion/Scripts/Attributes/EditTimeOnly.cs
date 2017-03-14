#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes {

  public class EditTimeOnly : CombinablePropertyAttribute, IPropertyDisabler {

#if UNITY_EDITOR
    public bool ShouldDisable(SerializedProperty property) {
      return EditorApplication.isPlaying;
    }
#endif
  }
}
