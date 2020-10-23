/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEditor;

namespace Leap.Unity.Recording {

  [CustomEditor(typeof(EventClip))]
  public class EventClipEditor : CustomEditorBase<EventClip> {

    protected override void OnEnable() {
      base.OnEnable();

      specifyConditionalDrawing(() => target.argumentType == SerializedArgumentType.Color,
                                "colorArg");
      specifyConditionalDrawing(() => target.argumentType == SerializedArgumentType.Float,
                                "floatArg");
      specifyConditionalDrawing(() => target.argumentType == SerializedArgumentType.Int,
                                "intArg");
      specifyConditionalDrawing(() => target.argumentType == SerializedArgumentType.Quaternion,
                                "quaternionArg");
      specifyConditionalDrawing(() => target.argumentType == SerializedArgumentType.String,
                                "stringArg");
      specifyConditionalDrawing(() => target.argumentType == SerializedArgumentType.Vector2,
                                "vector2Arg");
      specifyConditionalDrawing(() => target.argumentType == SerializedArgumentType.Vector3,
                                "vector3Arg");
      specifyConditionalDrawing(() => target.argumentType == SerializedArgumentType.Vector4,
                                "vector4Arg");
    }

  }

}
