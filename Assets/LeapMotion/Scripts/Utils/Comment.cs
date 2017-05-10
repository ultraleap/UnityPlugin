using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {

  public class Comment : MonoBehaviour {

    [TextArea]
    [SerializeField]
    private string _comment;

    [SerializeField, HideInInspector]
#pragma warning disable 0414
    private bool _isEditing = true;
#pragma warning restore 0414

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
