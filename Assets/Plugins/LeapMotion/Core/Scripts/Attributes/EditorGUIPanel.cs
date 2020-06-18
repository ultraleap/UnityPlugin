/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Linq;
using System.Reflection;
using UnityEngine;
using Leap.Unity.Query;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Attributes {

  public class EditorGUIPanelAttribute : CombinablePropertyAttribute, 
    ITopPanelDrawer
  {

    public const float LINE_HEIGHT = 20f;

    public int heightInLines;
    public readonly string editorMethodName;

    /// <summary> Pass the name of a **static** method in your MonoBehaviour
    /// that accepts a Rect and Object[] targets, which reflects the current
    /// editor selection (might be multiple of your MonoBehaviours in a
    /// multi-select case). The method is called in an OnGUI inspector context,
    /// so you can make EditorGUI or GUI calls. See the example below for 
    /// example usage. </summary>
    /// <example>
    /// ```
    /// [EditorGUIPanel("DrawPanel")]
    /// public SomeType inspectorObj;
    /// private static void DrawPanel(Rect panel, Object[] targets) {
    ///   #if UNITY_EDITOR
    ///   if (GUI.Button(panel, "Do Thing")) { 
    ///     targets.ForEach≤MyBehaviour≥(r => r.DoThing());
    ///   }
    ///   #endif
    /// }
    /// ```
    /// </example>
    public EditorGUIPanelAttribute(string editorMethodName,
      int heightInLines = 1)
    {
      this.heightInLines = heightInLines;
      this.editorMethodName = editorMethodName;
    }

#if UNITY_EDITOR
    private Action<Rect, UnityEngine.Object[]> _cachedDelegate;
    
    public float GetHeight() {
      return LINE_HEIGHT * heightInLines;
    }

    public void Draw(Rect panelRect, SerializedProperty property) {
      if (_cachedDelegate == null) {
        Type type = targets[0].GetType();

        MethodInfo method = type.GetMethod(editorMethodName,
          BindingFlags.Public |
          BindingFlags.NonPublic |
          BindingFlags.Static |
          BindingFlags.Instance |
          BindingFlags.FlattenHierarchy
        );

        if (method == null) {
          Debug.LogWarning("Could not find method of the name " +
            editorMethodName + " " + "to invoke for the TopButtonPanel " +
            "attribute.");
          return;
        }

        int paramCount = method.GetParameters().Length;
        if (paramCount == 0) {
          Debug.LogWarning("Method " + editorMethodName + "needs to accept a " +
            "Rect arg and Object[] arg to know the size of the panel to draw and " +
            "which components are currently selected.");
        }
        // else if (paramCount == 1) {
        //   Debug.LogWarning("Method " + editorMethodName + "needs to accept a " +
        //     "Rect arg and Object[] arg to know the size of the panel to draw and " +
        //     "which components are currently selected.");
        // }
        else if (paramCount == 1 || paramCount == 2) {
          _cachedDelegate = (rect, targets) => {
            if (!method.IsStatic) {
              // Non-static drawing is only valid for single-object selection.
              if (targets.Length == 1) {
                object[] argArray = new object[1];
                argArray[0] = rect;
                method.Invoke(targets[0], argArray);
              }
            }
            else { // method.IsStatic
              object[] argArray = new object[2];
              argArray[0] = rect;
              argArray[1] = targets;
              method.Invoke(null, argArray);
            }
          };
        } else {
          Debug.LogWarning("Could not invoke the method " + editorMethodName +
            " from TopButtonPanel because the method had more than 1 argument.");
        }
      }

      _cachedDelegate.Invoke(panelRect, targets);

      return;
    }

#endif
  }

}
