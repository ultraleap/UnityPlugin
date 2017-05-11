using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {

  public class Comment : MonoBehaviour {

    [TextArea]
    [SerializeField]
    protected string _comment;

    [SerializeField, HideInInspector]
    protected bool _isEditing = true;

#if UNITY_EDITOR
    [ContextMenu("Edit")]
    private void beginEditing() {
      Undo.RecordObject(this, "Enabled editing");
      EditorUtility.SetDirty(this);
      _isEditing = true;
    }
#endif
  }
}
