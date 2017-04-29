using Leap.Unity.Attachments;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  [AddComponentMenu("")]
  [ExecuteInEditMode]
  public class SpawnAttachmentPrefabs : MonoBehaviour {

    public AttachmentHand attachmentHand;
    public GameObject prefab;

    private Dictionary<AttachmentPointBehaviour, GameObject> _instances = new Dictionary<AttachmentPointBehaviour, GameObject>();

    void Start() {
      if (attachmentHand != null && prefab != null) {
        foreach (var point in attachmentHand.points) {
          if (!_instances.ContainsKey(point)) {
            GameObject obj = Instantiate<GameObject>(prefab);
            obj.transform.parent = point.transform;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            _instances[point] = obj;
          }
        }
      }
    }

  }

}
