/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Recording {

  [RecordingFriendly]
  public class RecordedData : MonoBehaviour {

    public List<EditorCurveBindingData> data = new List<EditorCurveBindingData>();

    [Serializable]
    public class EditorCurveBindingData {
      public string path;
      public string propertyName;
      public string typeName;
      public AnimationCurve curve;
    }
  }

}
