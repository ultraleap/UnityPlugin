/******************************************************************************\
* Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
* Leap Motion proprietary and confidential. Not for distribution.              *
* Use subject to the terms of the Leap Motion SDK Agreement available at       *
* https://developer.leapmotion.com/sdk_agreement, or another agreement         *
* between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
namespace Leap
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    /**
   * The Hand class reports the physical characteristics of a detected hand.
   *
   * Hand tracking data includes a palm position and velocity; vectors for
   * the palm normal and direction to the fingers; properties of a sphere fit
   * to the hand; and lists of the attached fingers.
   *
   * Get Hand objects from a Frame object:
   *
   * \include Hand_Get_First.txt
   *
   * Note that Hand objects can be invalid, which means that they do not contain
   * valid tracking data and do not correspond to a physical entity. Invalid Hand
   * objects can be the result of asking for a Hand object using an ID from an
   * earlier frame when no Hand objects with that ID exist in the current frame.
   * A Hand object created from the Hand constructor is also invalid.
   * Test for validity with the Hand::isValid() function.
   * @since 1.0
   */

    public class Hand {
      int _frameId;
        int _id = 0;
        float _confidence = 0;
        float _grabStrength = 0;
        float _grabAngle = 0;
        float _pinchStrength = 0;
        float _pinchDistance = 0;
        float _palmWidth = 0;
        float _sphereRadius = 0;
        bool _isValid = false;
        bool _isLeft = false;
        bool _isRight = false;
        float _timeVisible = 0;
        Arm _arm;
        FingerList _fingers;
        Vector _palmPosition;
        Vector _stabilizedPalmPosition;
        Vector _palmVelocity;
        Vector _palmNormal;
        Vector _direction;
        Vector _wristPosition;
        Matrix _basis;
        bool _needToCalculateBasis = true;
        Vector _sphereCenter;

        float _minSphereRadius = 42; //mm
        float _maxSphereradius = 160; //mm

        /**
     * Constructs a Hand object.
     *
     * An uninitialized hand is considered invalid.
     * Get valid Hand objects from a Frame object.
     *
     * \include Hand_Hand.txt
     *
     * @since 1.0
     */
        public Hand ()
        {
             _palmPosition = Vector.Zero;
             _stabilizedPalmPosition = Vector.Zero;
             _palmVelocity = Vector.Zero;
             _palmNormal = Vector.Zero;
             _direction = Vector.Zero;
             _wristPosition = Vector.Zero;
             _basis = Matrix.Identity;
             _needToCalculateBasis = false;
             _sphereCenter = Vector.Zero;
        }

        public Hand(int frameID,
                    int id,
                    float confidence,
                    float grabStrength,
                    float grabAngle,
                    float pinchStrength,
                    float pinchDistance,
                    float palmWidth,
                    bool isLeft,
                    float timeVisible,
                    Arm arm,
                    FingerList fingers,
                    Vector palmPosition,
                    Vector stabilizedPalmPosition,
                    Vector palmVelocity,
                    Vector palmNormal,
                    Vector direction,
                    Vector wristPosition)
        {
            _frameId = frameID;
            _id = id;
            _confidence = confidence;
            _grabStrength = grabStrength;
            _grabAngle = grabAngle;
            _pinchStrength = pinchStrength;
            _pinchDistance = pinchDistance;
            _palmWidth = palmWidth;
            _isLeft = isLeft;
            _isRight = !isLeft;
            _timeVisible = timeVisible;
            _arm = arm;
            _fingers = fingers;
            _palmPosition = palmPosition;
            _stabilizedPalmPosition = stabilizedPalmPosition;
            _palmVelocity = palmVelocity;
            _palmNormal = palmNormal;
            _direction = direction;
            _wristPosition = wristPosition;
        }

        public Hand TransformedCopy(Matrix trs){
            FingerList transformedFingers = new FingerList(5);
            for(int f = 0; f < this.Fingers.Count; f++)
                transformedFingers.Add(Fingers[f].TransformedCopy(trs));

            float hScale = trs.xBasis.Magnitude;
            return new Hand(_frameId,
                _id,
                _confidence,
                _grabStrength,
                _grabAngle,
                _pinchStrength,
                _pinchDistance,
                _palmWidth * hScale,
                _isLeft,
                _timeVisible,
                _arm.TransformedCopy(trs),
                transformedFingers,
                trs.TransformPoint(_palmPosition),
                trs.TransformPoint(_stabilizedPalmPosition),
                trs.TransformPoint(_palmVelocity),
                trs.TransformDirection(_palmNormal).Normalized,
                trs.TransformDirection(_direction).Normalized,
                trs.TransformPoint(_wristPosition)
            );
        }

        /**
     * The Finger object with the specified ID attached to this hand.
     *
     * Use the Hand::finger() function to retrieve a Finger object attached to
     * this hand using an ID value obtained from a previous frame.
     * This function always returns a Finger object, but if no finger
     * with the specified ID is present, an invalid Finger object is returned.
     *
     * \include Hand_finger.txt
     *
     * Note that ID values persist across frames, but only until tracking of a
     * particular object is lost. If tracking of a finger is lost and subsequently
     * regained, the new Finger object representing that finger may have a
     * different ID than that representing the finger in an earlier frame.
     *
     * @param id The ID value of a Finger object from a previous frame.
     * @returns The Finger object with the matching ID if one exists for this
     * hand in this frame; otherwise, an invalid Finger object is returned.
     * @since 1.0
     */
        public Finger Finger (int id)
        {
            return this.Fingers.Find (delegate(Finger item) {
                return item.Id == id;
            });
        }

        /**

        /**
     * The change of position of this hand between the current frame and
     * the specified frame.
     *
     * The returned translation vector provides the magnitude and direction of
     * the movement in millimeters.
     *
     * \include Hand_translation.txt
     *
     * If a corresponding Hand object is not found in sinceFrame, or if either
     * this frame or sinceFrame are invalid Frame objects, then this method
     * returns a zero vector.
     *
     * @param sinceFrame The starting frame for computing the translation.
     * @returns A Vector representing the heuristically determined change in
     * hand position between the current frame and that specified in the
     * sinceFrame parameter.
     * @since 1.0
     */
        public Vector Translation (Frame sinceFrame)
        {
            Hand sinceHand = sinceFrame.Hand(this.Id);

            if(!sinceHand.IsValid)
                return Vector.Zero; 

            return this.PalmPosition - sinceHand.PalmPosition;
        }

        /**
     * The estimated probability that the hand motion between the current
     * frame and the specified frame is intended to be a translating motion.
     *
     * \include Hand_translationProbability.txt
     *
     * If a corresponding Hand object is not found in sinceFrame, or if either
     * this frame or sinceFrame are invalid Frame objects, then this method
     * returns zero.
     *
     * @param sinceFrame The starting frame for computing the translation.
     * @returns A value between 0 and 1 representing the estimated probability
     * that the hand motion between the current frame and the specified frame
     * is intended to be a translating motion.
     * @since 1.0
     */
        public float TranslationProbability (Frame sinceFrame)
        {
            //TODO probabilities based on comparison of percentage of "likely" max change (scale:.5, trans, 100mm, rotation 45 degrees) -- time normalize?
            return 0; //not implemented
        }

        /**
     * The axis of rotation derived from the change in orientation of this
     * hand, and any associated fingers, between the current frame
     * and the specified frame.
     *
     * \include Hand_rotationAxis.txt
     *
     * The returned direction vector is normalized.
     *
     * If a corresponding Hand object is not found in sinceFrame, or if either
     * this frame or sinceFrame are invalid Frame objects, then this method
     * returns a zero vector.
     *
     * @param sinceFrame The starting frame for computing the relative rotation.
     * @returns A normalized direction Vector representing the heuristically
     * determined axis of rotational change of the hand between the current
     * frame and that specified in the sinceFrame parameter.
     * @since 1.0
     */
        public Vector RotationAxis (Frame sinceFrame)
        {
            return Vector.YAxis; //not implemented
        }

        /**
     * The angle of rotation around the rotation axis derived from the change
     * in orientation of this hand, and any associated fingers,
     * between the current frame and the specified frame.
     *
     * \include Hand_rotationAngle.txt
     *
     * The returned angle is expressed in radians measured clockwise around the
     * rotation axis (using the right-hand rule) between the start and end frames.
     * The value is always between 0 and pi radians (0 and 180 degrees).
     *
     * If a corresponding Hand object is not found in sinceFrame, or if either
     * this frame or sinceFrame are invalid Frame objects, then the angle of
     * rotation is zero.
     *
     * @param sinceFrame The starting frame for computing the relative rotation.
     * @returns A positive value representing the heuristically determined
     * rotational change of the hand between the current frame and that
     * specified in the sinceFrame parameter.
     * @since 1.0
     */
        public float RotationAngle (Frame sinceFrame)
        {
            return 0; //not implemented
        }

        /**
     * The angle of rotation around the specified axis derived from the change
     * in orientation of this hand, and any associated fingers,
     * between the current frame and the specified frame.
     *
     * \include Hand_rotationAngle_axis.txt
     *
     * The returned angle is expressed in radians measured clockwise around the
     * rotation axis (using the right-hand rule) between the start and end frames.
     * The value is always between -pi and pi radians (-180 and 180 degrees).
     *
     * If a corresponding Hand object is not found in sinceFrame, or if either
     * this frame or sinceFrame are invalid Frame objects, then the angle of
     * rotation is zero.
     *
     * @param sinceFrame The starting frame for computing the relative rotation.
     * @param axis The axis to measure rotation around.
     * @returns A value representing the heuristically determined rotational
     * change of the hand between the current frame and that specified in the
     * sinceFrame parameter around the specified axis.
     * @since 1.0
     */
        public float RotationAngle (Frame sinceFrame, Vector axis)
        {
            return 0; //this.RotationMatrix(sinceFrame); //not implemented
        }

        /**
     * The transform matrix expressing the rotation derived from the change
     * in orientation of this hand, and any associated fingers,
     * between the current frame and the specified frame.
     *
     * \include Hand_rotationMatrix.txt
     *
     * If a corresponding Hand object is not found in sinceFrame, or if either
     * this frame or sinceFrame are invalid Frame objects, then this method
     * returns an identity matrix.
     *
     * @param sinceFrame The starting frame for computing the relative rotation.
     * @returns A transformation Matrix representing the heuristically determined
     * rotational change of the hand between the current frame and that specified
     * in the sinceFrame parameter.
     * @since 1.0
     */
        public Matrix RotationMatrix (Frame sinceFrame)
        {
            Hand sinceHand = sinceFrame.Hand(this.Id);
            
            if(!sinceHand.IsValid)
                return Matrix.Identity; 

            return this.Basis * sinceHand.Basis.RigidInverse();
        }

        /**
     * The estimated probability that the hand motion between the current
     * frame and the specified frame is intended to be a rotating motion.
     *
     * \include Hand_rotationProbability.txt
     *
     * If a corresponding Hand object is not found in sinceFrame, or if either
     * this frame or sinceFrame are invalid Frame objects, then this method
     * returns zero.
     *
     * @param sinceFrame The starting frame for computing the relative rotation.
     * @returns A value between 0 and 1 representing the estimated probability
     * that the hand motion between the current frame and the specified frame
     * is intended to be a rotating motion.
     * @since 1.0
     */
        public float RotationProbability (Frame sinceFrame)
        {
            return 0; //not implemented
        }

        /**
     * The scale factor derived from this hand's motion between the current frame
     * and the specified frame.
     *
     * The scale factor is always positive. A value of 1.0 indicates no
     * scaling took place. Values between 0.0 and 1.0 indicate contraction
     * and values greater than 1.0 indicate expansion.
     *
     * \include Hand_scaleFactor.txt
     *
     * The Leap Motion software derives scaling from the relative inward or outward motion of
     * a hand and its associated fingers (independent of translation
     * and rotation).
     *
     * If a corresponding Hand object is not found in sinceFrame, or if either
     * this frame or sinceFrame are invalid Frame objects, then this method
     * returns 1.0.
     *
     * @param sinceFrame The starting frame for computing the relative scaling.
     * @returns A positive value representing the heuristically determined
     * scaling change ratio of the hand between the current frame and that
     * specified in the sinceFrame parameter.
     * @since 1.0
     */
        public float ScaleFactor (Frame sinceFrame)
        {
            Hand sinceHand = sinceFrame.Hand(this.Id);
            
            if(!sinceHand.IsValid)
                return 1.0f; 

            float thisFactor = 1 - Math.Max(this.PinchStrength, this.GrabStrength);
            float sinceFactor = 1 - Math.Max(sinceHand.PinchStrength, sinceHand.GrabStrength);

            if (thisFactor < Leap.Constants.EPSILON && sinceFactor < Leap.Constants.EPSILON)
                return 1.0f;

            //Contraction
            if(thisFactor > sinceFactor && thisFactor > Leap.Constants.EPSILON)
                return (thisFactor - sinceFactor)/thisFactor;

            //Expansion
            if(sinceFactor > thisFactor && sinceFactor > Leap.Constants.EPSILON)
                return (sinceFactor - thisFactor)/sinceFactor;

            return 1.0f;
        }

        /**
     * The estimated probability that the hand motion between the current
     * frame and the specified frame is intended to be a scaling motion.
     *
     * \include Hand_scaleProbability.txt
     *
     * If a corresponding Hand object is not found in sinceFrame, or if either
     * this frame or sinceFrame are invalid Frame objects, then this method
     * returns zero.
     *
     * @param sinceFrame The starting frame for computing the relative scaling.
     * @returns A value between 0 and 1 representing the estimated probability
     * that the hand motion between the current frame and the specified frame
     * is intended to be a scaling motion.
     * @since 1.0
     */
        public float ScaleProbability (Frame sinceFrame)
        {
            return 0; //not implemented
        }

     
        /**
     * Compare Hand object equality.
     *
     * \include Hand_operator_equals.txt
     *
     * Two Hand objects are equal if and only if both Hand objects represent the
     * exact same physical hand in the same frame and both Hand objects are valid.
     * @since 1.0
     */
        public bool Equals (Hand other)
        {
          return this.IsValid &&
              other.IsValid &&
              (this.Id == other.Id) &&
              (this._frameId == other._frameId);
        }

        /**
     * A string containing a brief, human readable description of the Hand object.
     *
     * @returns A description of the Hand as a string.
     * @since 1.0
     */
        public override string ToString ()
        {
            if(this.IsValid)
                return "Hand " + this.Id + (this.IsLeft ? " left." : " right.");

            return "Invalid hand";
        }

/**
     * A unique ID assigned to this Hand object, whose value remains the same
     * across consecutive frames while the tracked hand remains visible. If
     * tracking is lost (for example, when a hand is occluded by another hand
     * or when it is withdrawn from or reaches the edge of the Leap Motion Controller field of view),
     * the Leap Motion software may assign a new ID when it detects the hand in a future frame.
     *
     * Use the ID value with the Frame::hand() function to find this Hand object
     * in future frames:
     *
     * \include Hand_Get_ID.txt
     *
     * @returns The ID of this hand.
     * @since 1.0
     */
        public int Id {
            get {
                return _id;
            } 
        }

        public long FrameId {
          get {
            return _frameId;
          }
        }

        /**
     * The list of Finger objects detected in this frame that are attached to
     * this hand, given in order from thumb to pinky.  The list cannot be empty.
     *
     * Use PointableList::extended() to remove non-extended fingers from the list.
     *
     * \include Hand_Get_Fingers.txt
     *
     * @returns The FingerList containing all Finger objects attached to this hand.
     * @since 1.0
     */
        public FingerList Fingers {
                    get {
                if(_fingers == null)
                    _fingers = new FingerList(5);
                return _fingers;
//                return (FingerList)this.Frame.Fingers.FindAll (delegate(Finger item) {
//                            return item.HandId == this.Id;
//                    });
            }
        }


/**
     * The center position of the palm in millimeters from the Leap Motion Controller origin.
     *
     * \include Hand_palmPosition.txt
     *
     * @returns The Vector representing the coordinates of the palm position.
     * @since 1.0
     */
        public Vector PalmPosition {
            get {
                return _palmPosition;
            } 
        }

/**
     * The rate of change of the palm position in millimeters/second.
     *
     * \include Hand_palmVelocity.txt
     *
     * @returns The Vector representing the coordinates of the palm velocity.
     * @since 1.0
     */
        public Vector PalmVelocity {
            get {
                return _palmVelocity;
            } 
        }

/**
     * The normal vector to the palm. If your hand is flat, this vector will
     * point downward, or "out" of the front surface of your palm.
     *
     * \image html images/Leap_Palm_Vectors.png
     *
     * The direction is expressed as a unit vector pointing in the same
     * direction as the palm normal (that is, a vector orthogonal to the palm).
     *
     * You can use the palm normal vector to compute the roll angle of the palm with
     * respect to the horizontal plane:
     *
     * \include Hand_Get_Angles.txt
     *
     * @returns The Vector normal to the plane formed by the palm.
     * @since 1.0
     */
        public Vector PalmNormal {
            get {
                return _palmNormal;
            } 
        }

/**
     * The direction from the palm position toward the fingers.
     *
     * The direction is expressed as a unit vector pointing in the same
     * direction as the directed line from the palm position to the fingers.
     *
     * You can use the palm direction vector to compute the pitch and yaw angles of the palm with
     * respect to the horizontal plane:
     *
     * \include Hand_Get_Angles.txt
     *
     * @returns The Vector pointing from the palm position toward the fingers.
     * @since 1.0
     */
        public Vector Direction {
            get {
                return _direction;
            } 
        }

/**
     * The orientation of the hand as a basis matrix.
     *
     * The basis is defined as follows:
     *
     * **xAxis** Positive in the direction of the pinky
     *
     * **yAxis** Positive above the hand
     *
     * **zAxis** Positive in the direction of the wrist
     *
     * Note: Since the left hand is a mirror of the right hand, the
     * basis matrix will be left-handed for left hands.
     *
     * \include Hand_basis.txt
     *
     * @returns The basis of the hand as a matrix.
     * @since 2.0
     */
        public Matrix Basis {
            get {
                if(_needToCalculateBasis){
                    //TODO verify this calculation for both hands
                    _basis.zBasis = -Direction;
                    _basis.yBasis = -PalmNormal;
                    _basis.xBasis = _basis.zBasis.Cross(_basis.yBasis);
                    _basis.xBasis = _basis.xBasis.Normalized;
                    _needToCalculateBasis = false;
                }
                return _basis;
            } 
        }

/**
     * Reports whether this is a valid Hand object.
     *
     * \include Hand_isValid.txt
     *
     * @returns True, if this Hand object contains valid tracking data.
     * @since 1.0
     */
        public bool IsValid {
            get {
                return _isValid;
            } 
        }

/**
     * The center of a sphere fit to the curvature of this hand.
     *
     * \include Hand_sphereCenter.txt
     *
     * This sphere is placed roughly as if the hand were holding a ball.
     *
     * \image html images/Leap_Hand_Ball.png
     *
     * @returns The Vector representing the center position of the sphere.
     * @since 1.0
     */
        public Vector SphereCenter {
            get {
                calculateSphere();
                return _sphereCenter;
            } 
        }

/**
     * The radius of a sphere fit to the curvature of this hand.
     *
     * This sphere is placed roughly as if the hand were holding a ball. Thus the
     * size of the sphere decreases as the fingers are curled into a fist.
     *
     * \include Hand_sphereRadius.txt
     *
     * @returns The radius of the sphere in millimeters.
     * @since 1.0
     */
        public float SphereRadius {
            get {
                calculateSphere();
                return _sphereRadius;
            } 
        }

        private void calculateSphere(){
            float curvatureProxy = (float)Math.Max (GrabStrength, PinchStrength);
            _sphereRadius = _minSphereRadius + (_maxSphereradius - _minSphereRadius) * curvatureProxy;
            _sphereCenter = PalmPosition + PalmNormal * _sphereRadius * 2;
        }

/**
     * The strength of a grab hand pose.
     *
     * The strength is zero for an open hand, and blends to 1.0 when a grabbing hand
     * pose is recognized.
     *
     * \include Hand_grabStrength.txt
     *
     * @returns A float value in the [0..1] range representing the holding strength
     * of the pose.
     * @since 2.0
     */
        public float GrabStrength {
            get {
                return _grabStrength;
            } 
        }

        /**
     * The angle between the fingers and the hand of a grab hand pose.
     *
     * The angle is computed by looking at the angle between the direction of the
     * 4 fingers and the direction of the hand. Thumb is not considered when
     * computing the angle.
     * The angle is 0 radian for an open hand, and reaches pi radians when the pose
     * is a tight fist.
     *
     * @returns The angle of a grab hand pose between 0 and pi radians (0 and 180 degrees).
     * @since 3.0
     */
        public float GrabAngle {
            get {
                return _grabAngle;
            } 
        }

     /**
     * The holding strength of a pinch hand pose.
     *
     * The strength is zero for an open hand, and blends to 1.0 when a pinching
     * hand pose is recognized. Pinching can be done between the thumb
     * and any other finger of the same hand.
     *
     * \include Hand_pinchStrength.txt
     *
     * @returns A float value in the [0..1] range representing the holding strength
     * of the pinch pose.
     * @since 2.0
     */
        public float PinchStrength {
            get {
                return _pinchStrength;
            } 
        }

        /**
     * The distance between the thumb and index finger of a pinch hand pose.
     *
     * The distance is computed by looking at the shortest distance between
     * the last 2 phalanges of the thumb and those of the index finger.
     * This pinch measurement only takes thumb and index finger into account.
     *
     * \include Hand_pinchDistance.txt
     *
     * @returns The distance between the thumb and index finger of a pinch hand
     * pose in millimeters.
     * @since 3.0
     */
    public float PinchDistance {
        get {
            return _pinchDistance;
        } 
    }

    /**
     * The estimated width of the palm when the hand is in a flat position.
     *
     * \include Hand_palmWidth.txt
     *
     * @returns The width of the palm in millimeters
     * @since 2.0
     */
        public float PalmWidth {
            get {
                return _palmWidth;
            } 
        }

/**
     * The stabilized palm position of this Hand.
     *
     * Smoothing and stabilization is performed in order to make
     * this value more suitable for interaction with 2D content. The stabilized
     * position lags behind the palm position by a variable amount, depending
     * primarily on the speed of movement.
     *
     * \include Hand_stabilizedPalmPosition.txt
     *
     * @returns A modified palm position of this Hand object
     * with some additional smoothing and stabilization applied.
     * @since 1.0
     */
        public Vector StabilizedPalmPosition {
            get {
                return _stabilizedPalmPosition;
            } 
        }

/**
     * The position of the wrist of this hand.
     *
     * @returns A vector containing the coordinates of the wrist position in millimeters.
     * @since 2.0.3
     */
        public Vector WristPosition {
            get {
                return _wristPosition;
            } 
        }

/**
     * The duration of time this Hand has been visible to the Leap Motion Controller.
     *
     * \include Hand_timeVisible.txt
     *
     * @returns The duration (in seconds) that this Hand has been tracked.
     * @since 1.0
     */
        public float TimeVisible {
            get {
                return _timeVisible;
            } 
        }

/**
     * How confident we are with a given hand pose.
     *
     * The confidence level ranges between 0.0 and 1.0 inclusive.
     *
     * \include Hand_confidence.txt
     *
     * @since 2.0
     */
        public float Confidence {
            get {
                return _confidence;
            } 
        }

/**
     * Identifies whether this Hand is a left hand.
     *
     * \include Hand_isLeft.txt
     *
     * @returns True if the hand is identified as a left hand.
     * @since 2.0
     */
        public bool IsLeft {
            get {
                return _isLeft;
            } 
        }

/**
     * Identifies whether this Hand is a right hand.
     *
     * \include Hand_isRight.txt
     *
     * @returns True if the hand is identified as a right hand.
     * @since 2.0
     */
        public bool IsRight {
            get {
                return _isRight;
            } 
        }

/**
     * The arm to which this hand is attached.
     *
     * If the arm is not completely in view, Arm attributes are estimated based on
     * the attributes of entities that are in view combined with typical human anatomy.
     *
     * \include Arm_get.txt
     *
     * @returns The Arm object for this hand.
     * @since 2.0.3
     */
        public Arm Arm {
            get {
                return _arm;
            } 
        }

/**
     * Returns an invalid Hand object.
     *
     * \include Hand_invalid.txt
     *
     * You can use the instance returned by this function in comparisons testing
     * whether a given Hand instance is valid or invalid. (You can also use the
     * Hand::isValid() function.)
     *
     * @returns The invalid Hand instance.
     * @since 1.0
     */
        public static Hand Invalid {
            get {
                return new Hand ();
            } 
        }

    }

}
