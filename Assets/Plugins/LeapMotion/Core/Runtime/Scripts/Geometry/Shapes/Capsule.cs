/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Geometry {

  [System.Serializable]
  public struct Capsule {

    public Vector3 posA;
    public Vector3 posB;
    public float radius;
    public Transform transform;

    // TODO: overrideMatrix not yet needed for Capsules.
    //public Matrix4x4? overrideMatrix;

    #region Constructors

    public Capsule(Vector3 posA, Vector3 posB, float radius)
             : this(posA, posB, radius, null) { }

    public Capsule(Sphere sphere, Vector3 otherCenter) 
             : this(sphere.center, otherCenter, sphere.radius, sphere.transform) { }

    public Capsule(Vector3 posA, Vector3 posB, float radius, Transform transform) {
      this.posA = posA;
      this.posB = posB;
      this.radius = radius;
      this.transform = transform;

      //overrideMatrix = null;
    }

    #endregion



  }

  public static class CapsuleExtensions {

    /// <summary>
    /// Returns a Capsule representing this Sphere swept along a line to a new Center.
    /// </summary>
    public static Capsule Sweep(this Sphere sphere, Vector3 newCenter) {
      var capsule = new Capsule(sphere.center, newCenter, sphere.radius, sphere.transform);

      // TODO: Add this once Sphere supports overrideMatrix
      //capsule.overrideMatrix = sphere.overrideMatrix;

      return capsule;
    }

  }

}
