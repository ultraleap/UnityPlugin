using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Leap.Unity.Recording {

  public enum SerializedArgumentType {
    None,
    Int,
    Float,
    Vector2,
    Vector3,
    String,
    Color
  }

  public class EventPlayableAsset : PlayableAsset, ITimelineClipAsset {

    public string recipientPath = "relative/path/to/recipient";

    public string message = "MyMethod";

    public SerializedArgumentType argumentType;
    public int intArg = 0;
    public float floatArg = 0F;
    public Vector2 vector2Arg = Vector2.zero;
    public Vector3 vector3Arg = Vector3.zero;
    public string stringArg = "";
    public Color colorArg = Color.magenta;

    public ClipCaps clipCaps {
      get { return ClipCaps.None; }
    }

    public override double duration {
      get { return 0.01F; }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
      var playable = ScriptPlayable<EventPlayable>.Create(graph, inputCount: 0);
      var behaviour = playable.GetBehaviour();
      behaviour.rootObject = owner;
      behaviour.recipientPath = recipientPath;
      behaviour.message = message;
      behaviour.argument = getArgument();

      return playable;
    }

    private object getArgument() {
      switch (argumentType) {
        case SerializedArgumentType.Int:
          return intArg;
        case SerializedArgumentType.Float:
          return floatArg;
        case SerializedArgumentType.Vector2:
          return vector2Arg;
        case SerializedArgumentType.Vector3:
          return vector3Arg;
        case SerializedArgumentType.String:
          return stringArg;
        case SerializedArgumentType.Color:
          return colorArg;
        default:
          return null;
      }
    }

  }

}
