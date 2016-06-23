#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes {

  public class DisableIf : CombinablePropertyAttribute, IPropertyDisabler {
    public readonly string propertyName;
    public readonly bool equalTo;

    public DisableIf(string propertyName, bool equalTo = true) {
      this.propertyName = propertyName;
      this.equalTo = equalTo;
    }

#if UNITY_EDITOR
    public bool ShouldDisable(SerializedProperty property) {
      SerializedProperty prop = property.serializedObject.FindProperty(propertyName);
      return prop.boolValue == equalTo;
    }
#endif
  }
}
