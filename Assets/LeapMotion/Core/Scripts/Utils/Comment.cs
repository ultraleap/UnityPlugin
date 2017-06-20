/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {

  public class Comment : MonoBehaviour {

    [TextArea]
    [SerializeField]
    protected string _comment;
    public string text {
      get { return _comment; }
      set { _comment = value; }
    }

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
