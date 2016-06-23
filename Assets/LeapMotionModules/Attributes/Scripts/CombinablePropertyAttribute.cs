using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes {

  public interface IPropertyConstrainer {
#if UNITY_EDITOR
    void ConstrainValue(SerializedProperty property);
#endif
  }

  public interface IPropertyDisabler {
#if UNITY_EDITOR
    bool ShouldDisable(SerializedProperty property);
#endif
  }

  public interface IFullPropertyDrawer {
#if UNITY_EDITOR
    void DrawProperty(Rect rect, SerializedProperty property, GUIContent label);
#endif
  }

  public interface IAdditiveDrawer {
#if UNITY_EDITOR
    float GetWidth();
    void Draw(Rect rect, SerializedProperty property);
#endif
  }

  public interface IBeforeLabelAdditiveDrawer : IAdditiveDrawer { }
  public interface IAfterLabelAdditiveDrawer : IAdditiveDrawer { }
  public interface IBeforeFieldAdditiveDrawer : IAdditiveDrawer { }
  public interface IAfterFieldAdditiveDrawer : IAdditiveDrawer { }

  public abstract class CombinablePropertyAttribute : PropertyAttribute { }
}
