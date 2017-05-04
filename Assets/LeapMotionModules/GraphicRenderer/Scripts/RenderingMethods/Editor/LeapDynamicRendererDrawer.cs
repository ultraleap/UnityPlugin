using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;  

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(LeapDynamicRenderer))]
  public class LeapDynamicRendererDrawer : LeapMesherBaseDrawer {

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
      base.OnGUI(position, property, label);

      //Nothing to do yet!
    }
  }
}
