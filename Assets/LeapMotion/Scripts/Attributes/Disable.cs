#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes {

  public class DisableAttribute : CombinablePropertyAttribute, IPropertyDisabler {

#if UNITY_EDITOR
    public bool ShouldDisable(SerializedProperty property) {
      return true;
    }
#endif
  }
}
