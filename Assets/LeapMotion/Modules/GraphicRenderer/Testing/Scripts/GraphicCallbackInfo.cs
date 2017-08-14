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

namespace Leap.Unity.GraphicalRenderer.Tests {

  public static class GraphicExtensions {

    public static GraphicCallbackInfo.GraphicInfo OnAwake(this LeapGraphic graphic) {
      return graphic.GetComponent<GraphicCallbackInfo>().awakeInfo;
    }

    public static GraphicCallbackInfo.GraphicInfo OnEnable(this LeapGraphic graphic) {
      return graphic.GetComponent<GraphicCallbackInfo>().enableInfo;
    }

    public static GraphicCallbackInfo.GraphicInfo OnStart(this LeapGraphic graphic) {
      return graphic.GetComponent<GraphicCallbackInfo>().startInfo;
    }
  }

  public class GraphicCallbackInfo : MonoBehaviour {

    public GraphicInfo awakeInfo, enableInfo, startInfo;

    private void Awake() {
      awakeInfo = new GraphicInfo(gameObject);
    }

    private void OnEnable() {
      enableInfo = new GraphicInfo(gameObject);
    }

    private void Start() {
      startInfo = new GraphicInfo(gameObject);
    }

    public struct GraphicInfo {
      public readonly bool hasFired;

      private bool _wasAttached;
      private LeapGraphicGroup _attachedGroup;

      public bool hasNotFired {
        get {
          return !hasFired;
        }
      }

      public bool wasAttached {
        get {
          if (!hasFired) throw new Exception("Event has not fired yet.");
          return _wasAttached;
        }
      }

      public LeapGraphicGroup attachedGroup {
        get {
          if (!hasFired) throw new Exception("Event has not fired yet.");
          return _attachedGroup;
        }
      }

      public GraphicInfo(GameObject obj) {
        var graphic = obj.GetComponent<LeapGraphic>();
        hasFired = true;
        _wasAttached = graphic.isAttachedToGroup;
        _attachedGroup = graphic.attachedGroup;
      }
    }
  }
}
