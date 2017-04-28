/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using Leap.Unity.Playback;

namespace Leap.Unity.Interaction.Testing {

  public class InteractionTestRecording : Recording {

    [SerializeField]
    protected List<Vector3> _initialPositions = new List<Vector3>();

    [SerializeField]
    protected List<Quaternion> _initialRotations = new List<Quaternion>();

    [SerializeField]
    protected List<Vector3> _initialScale = new List<Vector3>();

    [SerializeField]
    protected List<float> _sphereObjs = new List<float>();

    [SerializeField]
    protected List<Vector3> _obbObjs = new List<Vector3>();

    public void CaptureCurrentShapes() {
      var behaviours = FindObjectsOfType<IInteractionBehaviour>();

      foreach (var behaviour in behaviours) {
        Collider c = behaviour.GetComponent<Collider>();
        if (c is SphereCollider) {
          _initialPositions.Add(c.transform.localPosition);
          _initialRotations.Add(c.transform.localRotation);
          _initialScale.Add(c.transform.localScale);
          _sphereObjs.Add((c as SphereCollider).radius);
        }
      }

      foreach (var behaviour in behaviours) {
        Collider c = behaviour.GetComponent<Collider>();
        if (c is BoxCollider) {
          _initialPositions.Add(c.transform.localPosition);
          _initialRotations.Add(c.transform.localRotation);
          _initialScale.Add(c.transform.localScale);
          _obbObjs.Add((c as BoxCollider).size);
        }
      }
    }

    public void CreateInitialShapes(Transform root) {
      int index = 0;

      foreach (float radius in _sphereObjs) {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.transform.parent = root;
        obj.transform.localPosition = _initialPositions[index];
        obj.transform.localRotation = _initialRotations[index];
        obj.transform.localScale = _initialScale[index];

        obj.GetComponent<SphereCollider>().radius = radius;

        obj.AddComponent<SadisticInteractionBehaviour>();

        index++;
      }

      foreach (Vector3 size in _obbObjs) {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.transform.parent = root;
        obj.transform.localPosition = _initialPositions[index];
        obj.transform.localRotation = _initialRotations[index];
        obj.transform.localScale = _initialScale[index];

        obj.GetComponent<BoxCollider>().size = size;

        obj.AddComponent<SadisticInteractionBehaviour>();

        index++;
      }
    }
  }
}
