/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Geometry
{
    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    [System.Serializable]
    public struct Point
    {

        [SerializeField]
        public Transform transform;

        [SerializeField]
        private Vector3 _position;
        public Vector3 position
        {
            get
            {
                if (transform == null) return _position;
                else return transform.TransformPoint(_position);
            }
            set
            {
                if (transform == null) _position = value;
                else _position = transform.InverseTransformPoint(value);
            }
        }

        public Point(Component transformSource = null)
          : this(default(Vector3), transformSource) { }

        public Point(Vector3 position = default(Vector3), Component transformSource = null)
        {
            this.transform = transformSource.transform;
            _position = Vector3.zero;
        }

        public static implicit operator Vector3(Point point)
        {
            return point.position;
        }

    }

    [System.Obsolete("This code will be moved to a legacy package in the next major version of the plugin. If you believe that it needs to be kept in tracking, please open a discussion on the GitHub forum (https://github.com/ultraleap/UnityPlugin/discussions)")]
    public static class PointExtensions
    {



    }

}