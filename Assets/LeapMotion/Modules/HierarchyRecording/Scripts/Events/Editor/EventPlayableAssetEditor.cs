using UnityEditor;

namespace Leap.Unity.Recording {

  [CustomEditor(typeof(EventPlayableAsset))]
  public class EventPlayableAssetEditor : CustomEditorBase<EventPlayableAsset> {

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
