/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Attachments;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  /// <summary>
  /// This example script allows you to specify a prefab to be spawned at every
  /// attachment point Transform on an AttachmentHand.
  /// </summary>
  [AddComponentMenu("")]
  [ExecuteInEditMode]
  public class SpawnAttachmentPrefabs : MonoBehaviour {

    public AttachmentHand attachmentHand;
    public GameObject prefab;

    private Dictionary<AttachmentPointBehaviour, GameObject> _instances = new Dictionary<AttachmentPointBehaviour, GameObject>();

    void OnValidate() {
      attachmentHand.OnAttachmentPointsModified -= refreshAttachmentPrefabs;
      attachmentHand.OnAttachmentPointsModified += refreshAttachmentPrefabs;
    }

    void Start() {
      attachmentHand.OnAttachmentPointsModified -= refreshAttachmentPrefabs;
      attachmentHand.OnAttachmentPointsModified += refreshAttachmentPrefabs;
    }

    private HashSet<AttachmentPointBehaviour> _pointsLastRefresh = new HashSet<AttachmentPointBehaviour>();
    private List<AttachmentPointBehaviour> _pointsRemovalBuffer = new List<AttachmentPointBehaviour>();

    private void refreshAttachmentPrefabs() {
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

        _pointsRemovalBuffer.Clear();
        foreach (var point in _pointsLastRefresh) {
          if (!_instances.ContainsKey(point)) {
            _pointsRemovalBuffer.Add(point);
          }
        }

        foreach (var point in _pointsRemovalBuffer) {
          _pointsLastRefresh.Remove(point);
        }

        _pointsLastRefresh.Clear();

        foreach (var pointObjPair in _instances) {
          _pointsLastRefresh.Add(pointObjPair.Key);
        }
      }
    }

  }

}
