/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/
using UnityEngine;

namespace Leap
{
    using System;

    /// <summary>
    /// The Arm class represents the forearm.
    /// </summary>
    [Serializable]
    public class Arm : Bone, IEquatable<Arm>
    {

        /// <summary>
        /// Constructs a default Arm object.
        /// Get valid Arm objects from a Hand object.
        /// 
        /// @since 2.0.3
        /// </summary>
        public Arm() : base() { }

        /// <summary>
        /// Constructs a new Arm object. 
        /// </summary>
        public Arm(Vector3 elbow,
                   Vector3 wrist,
                   Vector3 center,
                   Vector3 direction,
                   float length,
                   float width,
                   Quaternion rotation)
          : base(elbow,
                 wrist,
                 center,
                 direction,
                 length,
                 width,
                 BoneType.TYPE_METACARPAL, //ignored for arms
                 rotation)
        { }

        /// <summary>
        /// Compare Arm object equality.
        /// Two Arm objects are equal if and only if both Arm objects represent the
        /// exact same physical arm in the same frame and both Arm objects are valid.
        /// @since 2.0.3
        /// </summary>
        public bool Equals(Arm other)
        {
            return Equals(other as Bone);
        }

        /// <summary>
        /// A string containing a brief, human readable description of the Arm object.
        /// @since 2.0.3
        /// </summary>
        public override string ToString()
        {
            return "Arm";
        }

        /// <summary>
        /// The position of the elbow.
        /// If not in view, the elbow position is estimated based on typical human
        /// anatomical proportions.
        /// 
        /// @since 2.0.3
        /// </summary>
        public Vector3 ElbowPosition
        {
            get
            {
                return base.PrevJoint;
            }
        }

        /// <summary>
        /// The position of the wrist.
        /// 
        /// Note that the wrist position is not collocated with the end of any bone in
        /// the hand. There is a gap of a few centimeters since the carpal bones are
        /// not included in the skeleton model.
        /// 
        /// @since 2.0.3
        /// </summary>
        public Vector3 WristPosition
        {
            get
            {
                return base.NextJoint;
            }
        }
    }
}