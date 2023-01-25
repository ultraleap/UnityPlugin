/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity
{
    /// <summary>
    /// The base class for all hand models, both graphics and physics.
    ///
    /// This class serves as the interface between the LeapProvider
    /// and the concrete hand object containing the graphics and physics of a hand.
    ///
    /// The UpdateHand()
    /// function is called in the Unity Update() phase for graphics HandModel instances;
    /// and in the Unity FixedUpdate() phase for physics objects. InitHand() is called once,
    /// when the hand is created and is followed by a call to UpdateHand().
    /// </summary>
    public abstract class HandModel : HandModelBase
    {

        [SerializeField]
        private Chirality handedness;
        /// <summary>
        /// The chirality or handedness of this hand (left or right).
        /// </summary>
        public override Chirality Handedness
        {
            get { return handedness; }
            set { handedness = value; }
        }

        private ModelType handModelType;
        /// <summary>
        /// The type of the Hand model (graphics or physics).
        /// </summary>
        public override abstract ModelType HandModelType
        {
            get;
        }

        /// <summary> 
        /// The number of fingers on a hand.
        /// </summary>
        public const int NUM_FINGERS = 5;

        /// <summary>
        /// The model width of the hand in meters. This value is used with the measured value
        /// of the user's hand to scale the model proportionally.
        /// </summary>
        public float handModelPalmWidth = 0.085f;
        /// <summary> 
        /// The array of finger objects for this hand. The array is ordered from thumb (element 0) to pinky (element 4).
        /// </summary>
        public FingerModel[] fingers = new FingerModel[NUM_FINGERS];

        // Unity references
        /// <summary> 
        /// Transform object for the palm object of this hand. 
        /// </summary>
        public Transform palm;
        /// <summary> 
        /// Transform object for the forearm object of this hand. 
        /// </summary>
        public Transform forearm;
        /// <summary> 
        /// Transform object for the wrist joint of this hand. 
        /// </summary>
        public Transform wristJoint;
        /// <summary> 
        /// Transform object for the elbow joint of this hand.
        /// </summary>
        public Transform elbowJoint;

        // Leap references
        /// <summary> 
        /// The Leap Hand object this hand model represents. 
        /// </summary>
        protected Hand hand_;


        /// <summary>
        /// Calculates the position of the palm in global coordinates.
        /// </summary>
        /// <returns> A Vector3 containing the Unity coordinates of the palm position. </returns>
        public Vector3 GetPalmPosition()
        {
            return hand_.PalmPosition;
        }

        /// <summary> 
        /// Calculates the rotation of the hand in global coordinates.
        /// </summary>
        /// <returns> A Quaternion representing the rotation of the hand. </returns>
        public Quaternion GetPalmRotation()
        {
            if (hand_ != null)
            {
                return hand_.Basis.rotation;
            }
            if (palm)
            {
                return palm.rotation;
            }
            return Quaternion.identity;
        }

        /// <summary> 
        /// Calculates the direction vector of the hand in global coordinates.
        /// </summary>
        /// <returns> A Vector3 representing the direction of the hand. </returns>
        public Vector3 GetPalmDirection()
        {
            if (hand_ != null)
            {
                return hand_.Direction;
            }
            if (palm)
            {
                return palm.forward;
            }
            return Vector3.forward;
        }

        /// <summary> 
        /// Calculates the normal vector projecting from the hand in global coordinates.
        /// </summary>
        /// <returns> A Vector3 representing the vector perpendicular to the palm. </returns>
        public Vector3 GetPalmNormal()
        {
            if (hand_ != null)
            {
                return hand_.PalmNormal;
            }
            if (palm)
            {
                return -palm.up;
            }
            return -Vector3.up;
        }

        /// <summary> 
        /// Calculates the direction vector of the forearm in global coordinates.
        /// </summary>
        /// <returns> A Vector3 representing the direction of the forearm (pointing from elbow to wrist). </returns>
        public Vector3 GetArmDirection()
        {
            if (hand_ != null)
            {
                return hand_.Arm.Direction;
            }
            if (forearm)
            {
                return forearm.forward;
            }
            return Vector3.forward;
        }

        /// <summary> 
        /// Calculates the center of the forearm in global coordinates.
        /// </summary>
        /// <returns> A Vector3 containing the Unity coordinates of the center of the forearm. </returns>
        public Vector3 GetArmCenter()
        {
            if (hand_ != null)
            {
                Vector3 leap_center = 0.5f * (hand_.Arm.WristPosition + hand_.Arm.ElbowPosition);
                return leap_center;
            }
            if (forearm)
            {
                return forearm.position;
            }
            return Vector3.zero;
        }

        /// <summary> 
        /// Returns the measured length of the forearm in meters.
        /// </summary>
        public float GetArmLength()
        {
            return (hand_.Arm.WristPosition - hand_.Arm.ElbowPosition).magnitude;
        }

        /// <summary> 
        /// Returns the measured width of the forearm in meters.
        /// </summary>
        public float GetArmWidth()
        {
            return hand_.Arm.Width;
        }

        /// <summary> 
        /// Calculates the position of the elbow in global coordinates.
        /// </summary>
        /// <returns> A Vector3 containing the Unity coordinates of the elbow. </returns>
        public Vector3 GetElbowPosition()
        {
            if (hand_ != null)
            {
                Vector3 local_position = hand_.Arm.ElbowPosition;
                return local_position;
            }
            if (elbowJoint)
            {
                return elbowJoint.position;
            }
            return Vector3.zero;
        }

        /// <summary> 
        /// Calculates the position of the wrist in global coordinates.
        /// </summary>
        /// <returns> A Vector3 containing the Unity coordinates of the wrist. </returns>
        public Vector3 GetWristPosition()
        {
            if (hand_ != null)
            {
                Vector3 local_position = hand_.Arm.WristPosition;
                return local_position;
            }
            if (wristJoint)
            {
                return wristJoint.position;
            }
            return Vector3.zero;
        }

        /// <summary> 
        /// Calculates the rotation of the forearm in global coordinates.
        /// </summary>
        /// <returns> A Quaternion representing the rotation of the arm. </returns>
        public Quaternion GetArmRotation()
        {
            if (hand_ != null)
            {
                Quaternion local_rotation = hand_.Arm.Rotation;
                return local_rotation;
            }
            if (forearm)
            {
                return forearm.rotation;
            }
            return Quaternion.identity;
        }

        /// <summary>
        /// Returns the Leap Hand object represented by this HandModel.
        /// Note that any physical quantities and directions obtained from the
        /// Leap Hand object are relative to the Leap Motion coordinate system,
        /// which uses a right-handed axes and units of millimeters.
        /// </summary>
        public override Hand GetLeapHand()
        {
            return hand_;
        }

        /// <summary>
        /// Assigns a Leap Hand object to this hand model.
        /// </summary>
        public override void SetLeapHand(Hand hand)
        {
            hand_ = hand;
            for (int i = 0; i < fingers.Length; ++i)
            {
                if (fingers[i] != null)
                {
                    fingers[i].SetLeapHand(hand_);
                }
            }
        }

        /// <summary>
        /// Implement this function to initialise this hand after it is created.
        /// This function is called when a new hand is detected by the Leap Motion device.
        /// </summary>
        public override void InitHand()
        {
            for (int f = 0; f < fingers.Length; ++f)
            {
                if (fingers[f] != null)
                {
                    fingers[f].fingerType = (Finger.FingerType)f;
                    fingers[f].InitFinger();
                }
            }
        }

        /// <summary>
        /// Returns the ID associated with the hand in the Leap API.
        /// This ID is guaranteed to be unique among all hands in a frame,
        /// and is invariant for the lifetime of the hand model.
        /// </summary>
        public int LeapID()
        {
            if (hand_ != null)
            {
                return hand_.Id;
            }
            return -1;
        }

        /// <summary>
        /// Implement this function to update this hand once every game loop.
        /// Called once per frame when the LeapProvider calls the event 
        /// OnUpdateFrame (graphics hand) or OnFixedFrame (physics hand)
        /// </summary>
        public override abstract void UpdateHand();
    }
}