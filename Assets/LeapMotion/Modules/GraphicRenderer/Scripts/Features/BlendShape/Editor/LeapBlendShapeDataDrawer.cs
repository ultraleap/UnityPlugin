/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEditor;

namespace Leap.Unity.GraphicalRenderer {

  [CustomPropertyDrawer(typeof(LeapBlendShapeData))]
  public class LeapBlendShapeDataDrawer : CustomPropertyDrawerBase {

    protected override void init(SerializedProperty property) {
      base.init(property);

      drawProperty("_amount");
      drawProperty("_type");

      var typeProp = property.FindPropertyRelative("_type");

      drawConditionalType("_translation", typeProp, LeapBlendShapeData.BlendShapeType.Translation);
      drawConditionalType("_rotation", typeProp, LeapBlendShapeData.BlendShapeType.Rotation);
      drawConditionalType("_scale", typeProp, LeapBlendShapeData.BlendShapeType.Scale);
      drawConditionalType("_transform", typeProp, LeapBlendShapeData.BlendShapeType.Transform);
    }

    private void drawConditionalType(string name, SerializedProperty typeProp, LeapBlendShapeData.BlendShapeType type) {
      drawPropertyConditionally(name, () => {
        return !typeProp.hasMultipleDifferentValues && typeProp.intValue == (int)type;
      });
    }
  }
}
