using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity.Interaction {

  [CustomEditor(typeof(ContactBoneParent))]
  public class ContactBoneParentEditor : CustomEditorBase<ContactBoneParent> {

    protected override void OnEnable() {
      base.OnEnable();
    }

    public override void OnInspectorGUI() {
      EditorGUILayout.HelpBox("Contact bone parents must have no parent and must not have their "
                              + "transforms translated; otherwise, child Colliders will not have "
                              + "their rigidbodies' velocities set correctly.",
                              MessageType.Info);

      base.OnInspectorGUI();
    }

  }

}