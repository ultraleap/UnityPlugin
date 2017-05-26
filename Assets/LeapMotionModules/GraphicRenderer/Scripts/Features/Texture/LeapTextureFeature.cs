/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Attributes;

namespace Leap.Unity.GraphicalRenderer {

  [LeapGraphicTag("Texture", 10)]
  [Serializable]
  public class LeapTextureFeature : LeapGraphicFeature<LeapTextureData> {

    [Delayed]
    [EditTimeOnly]
    public string propertyName = "_MainTex";
    
    [EditTimeOnly]
    public UVChannelFlags channel = UVChannelFlags.UV0;
  }
}
