/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
