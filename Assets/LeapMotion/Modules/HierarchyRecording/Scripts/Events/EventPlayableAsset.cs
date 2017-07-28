using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Leap.Unity.Recording {

  public class EventPlayableAsset : PlayableAsset, ITimelineClipAsset {

    public ExposedReference<GameObject> recipient;
    public string message = "MyMethod";

    public SerializedArgumentType argumentType;
    public Color colorArg = Color.magenta;
    public float floatArg = 0F;
    public int intArg = 0;
    public Quaternion quaternionArg = Quaternion.identity;
    public string stringArg = "";
    public Vector2 vector2Arg = Vector2.zero;
    public Vector3 vector3Arg = Vector3.zero;
    public Vector4 vector4Arg = Vector4.zero;

    public ClipCaps clipCaps {
      get { return ClipCaps.None; }
    }

    public override double duration {
      get { return 0.01F; }
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
      var playable = ScriptPlayable<EventPlayableBehaviour>.Create(graph, inputCount: 0);
      var behaviour = playable.GetBehaviour();
      behaviour.recipient = recipient.Resolve(graph.GetResolver());
      behaviour.message = message;
      behaviour.argument = getArgument();

      return playable;
    }

    private object getArgument() {
      switch (argumentType) {
        case SerializedArgumentType.Color:
          return colorArg;
        case SerializedArgumentType.Float:
          return floatArg;
        case SerializedArgumentType.Int:
          return intArg;
        case SerializedArgumentType.Quaternion:
          return quaternionArg;
        case SerializedArgumentType.String:
          return stringArg;
        case SerializedArgumentType.Vector2:
          return vector2Arg;
        case SerializedArgumentType.Vector3:
          return vector3Arg;
        case SerializedArgumentType.Vector4:
          return vector4Arg;
        default:
          return null;
      }
    }

    public enum SerializedArgumentType {
      None,
      Color,
      Float,
      Int,
      Quaternion,
      String,
      Vector2,
      Vector3,
      Vector4
    }

  }

}
