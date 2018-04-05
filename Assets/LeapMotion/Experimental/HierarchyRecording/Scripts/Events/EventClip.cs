/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Attributes;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Leap.Unity.Recording {

  public class EventClip : PlayableAsset, ITimelineClipAsset {

    #region Inspector

    public EventPlayableBehaviour behaviour;

    public ExposedReference<GameObject> recipient;
    public string message = "MyMethod";
    
    [Header("Event Argument")]
    [SerializeField, OnEditorChange("argumentType")]
    private SerializedArgumentType _argumentType = SerializedArgumentType.None;
    public SerializedArgumentType argumentType {
      get {
        return _argumentType;
      }
      set {
        _argumentType = value;
        if (!_userSetScrubType) {
          if (value == SerializedArgumentType.None) {
            _eventScrubType = EventScrubType.Trigger;
          }
          else {
            _eventScrubType = EventScrubType.StateChange;
          }
        }
      }
    }
    public Color colorArg = Color.magenta;
    public float floatArg = 0F;
    public int intArg = 0;
    public Quaternion quaternionArg = Quaternion.identity;
    [TextArea]
    public string stringArg = "";
    public Vector2 vector2Arg = Vector2.zero;
    public Vector3 vector3Arg = Vector3.zero;
    public Vector4 vector4Arg = Vector4.zero;

    [Header("Scrubbing Behavior (Play-mode Only, Optional)")]
    [SerializeField, OnEditorChange("eventScrubType")]
    [Tooltip("This field only affects event-firing behavior when moving the playhead "
           + "backwards across an event clip. Trigger events, such as sound effects, will "
           + "only fire when the playhead moves forward past a clip's start time. "
           + "State changes, such as setting an object's Color or a Text value, will fire "
           + "the previous event when the playhead scrubs backward across a given event's "
           + "start time, reflecting the state of the object since the last state change. "
           + "Cumulative State Change events work like State Change events, but fire "
           + "all events from the first event up to and including the previous event.")]
    private EventScrubType _eventScrubType = EventScrubType.Trigger;
    public EventScrubType eventScrubType {
      get {
        return _eventScrubType;
      }
      set {
        _userSetScrubType = true;
        _eventScrubType = value;
      }
    }
    [SerializeField, HideInInspector]
    private bool _userSetScrubType = false;

    #endregion

    #region Clip Behavior

    public ClipCaps clipCaps {
      get { return ClipCaps.None; }
    }

    public override double duration {
      get { return 0.5F; }
    }

    #endregion

    #region PlayableAsset Implementation

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
      var playable = ScriptPlayable<EventPlayableBehaviour>.Create(graph, inputCount: 0);
      this.behaviour = playable.GetBehaviour();
      this.behaviour.recipient = recipient.Resolve(graph.GetResolver());
      this.behaviour.message = message;
      this.behaviour.argument = getArgument();

      return playable;
    }

    private object getArgument() {
      switch (_argumentType) {
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

    #endregion

    #region Public Methods

    public void FireEvent() {
      if (this.behaviour == null) {
        Debug.LogError("Unable to fire event: PlayableBehaviour has not been constructed. "
                     + "Are you attempting to invoke the clip from outside a PlayableGraph "
                     + "context?");
        return;
      }

      this.behaviour.FireEvent();
    }

    #endregion

  }

  #region Enums

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

  public enum EventScrubType {
    Trigger,
    StateChange
  }

  #endregion

}
