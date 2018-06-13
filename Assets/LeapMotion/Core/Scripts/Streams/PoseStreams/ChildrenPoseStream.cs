using Leap.Unity.Attributes;
using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Streams {
  
  /// <summary>
  /// Every update, this script opens a Pose stream, sends the world-space Pose
  /// of each of its Transform's children, and closes the stream.
  /// </summary>
  [ExecuteInEditMode]
  public class ChildrenPoseStream : MonoBehaviour, IStream<Pose> {

    [Tooltip("Filter Transform pose sources based on the requireNameContains "
      + "string.")]
    public bool useNameRequirement = false;
    [Tooltip("Only include child Transforms in the pose stream that contain "
      + "this string in their name.")]
    [DisableIf("useNameRequirement", isEqualTo: false)]
    public string requireNameContains = "Control Point";

    public enum UpdateMode {
      Update, LateUpdate
    }
    [QuickButton("Send Now", "doUpdate", tooltip: "Open, send, and close the "
      + "pose stream now, whether in play mode or edit mode.")]
    public UpdateMode updateMode = UpdateMode.Update;

    [Tooltip("If true, whenever the update type this stream is configured for "
      + "occurs at edit-time, the stream will be opened, sent, and closed.")]
    [EditTimeOnly]
    public bool periodicEditTimeRefresh = false;

    [Tooltip("Include children-of-children and so on. Poses are sent in "
      + "order from top to bottom in the hierarchy window -- in other words, "
      + "depth-first and then by child index order.")]
    public bool includeRecursiveChildren = true;

    [Tooltip("Preview of child data.")]
    [SerializeField]
    [Disable]
    private List<Transform> _children = new List<Transform>();

    // Stream events.
    public event Action OnOpen = () => { };
    public event Action<Pose> OnSend = (pose) => { };
    public event Action OnClose = () => { };

    private void OnValidate() {
      updateChildren();
    }

    private void Update() {
      if (!Application.isPlaying && !periodicEditTimeRefresh) return;

      if (updateMode != UpdateMode.Update) return;

      doUpdate();
    }

    private void LateUpdate() {
      if (!Application.isPlaying && !periodicEditTimeRefresh) return;

      if (updateMode != UpdateMode.LateUpdate) return;

      doUpdate();
    }

    private void doUpdate() {
      updateChildren();

      updateStream();
    }

    private void updateChildren() {
      _children.Clear();

      if (includeRecursiveChildren) {
        this.transform.GetAllChildren(_children);
      }
      else {
        foreach (var child in this.transform.GetChildren()) {
          _children.Add(child);
        }
      }

      if (useNameRequirement) {
        var filteredList = Pool<List<Transform>>.Spawn(); filteredList.Clear();
        try {
          foreach (var child in _children.Query()
                     .Where(t => t.name.Contains(requireNameContains))) {
            filteredList.Add(child);
          }
        }
        finally {
          Utils.Swap(ref filteredList, ref _children);

          filteredList.Clear();
          Pool<List<Transform>>.Recycle(filteredList);
        }
      }
    }

    private void updateStream() {
      OnOpen();

      foreach (var child in _children) {
        OnSend(child.ToPose());
      }

      OnClose();
    }

  }

}
