/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
