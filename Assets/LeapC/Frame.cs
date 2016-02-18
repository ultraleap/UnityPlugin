
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
    using System.Runtime.InteropServices;

    /**
   * The Frame class represents a set of hand and finger tracking data detected
   * in a single frame.
   *
   * The Leap Motion software detects hands, fingers and tools within the tracking area, reporting
   * their positions, orientations, gestures, and motions in frames at the Leap Motion frame rate.
   *
   * Access Frame objects through an instance of the Controller class:
   *
   * \include Controller_Frame_1.txt
   *
   * Implement a Listener subclass to receive a callback event when a new Frame is available.
   * @since 1.0
   */

    public class Frame
    {
        FingerList _fingers;
        HandList _hands;
        TrackedQuad _trackedQuad;
        InteractionBox _interactionBox;
        long _id = -1;
        float _fps = 0;
        long _timestamp = -1;
        bool _isValid = false;

        public Frame (long id, long timestamp, float fps, InteractionBox interactionBox)
        {
            _id = id;
            _timestamp = timestamp;
            _fps = fps;
            _isValid = true;
            _fingers = new FingerList (15);
            _hands = new HandList (3);
            _trackedQuad = new TrackedQuad ();
            InteractionBox = interactionBox;
        }

        public Frame TransformedCopy (Matrix trs)
        {
            Frame transformedFrame = new Frame (_id, 
                                         _timestamp, 
                                         _fps, 
                                         new InteractionBox (InteractionBox.Center, InteractionBox.Size));
            //TODO should InteractionBox be transformed, too?

            for (int h = 0; h < this.Hands.Count; h++)
                transformedFrame.AddHand (this.Hands [h].TransformedCopy (trs));
            return transformedFrame;
        }

        public void AddHand (Hand hand)
        {
            if (_hands == null)
                _hands = new HandList (3);

            _hands.Add (hand);
            if (_fingers == null)
                _fingers = new FingerList (15);


            for (int f = 0; f < hand.Fingers.Count; f++) {
                _fingers.Add (hand.Fingers [f]);
            }
        }

        /**
  * Encodes this Frame object as a byte string.
  *
  * \include Frame_serialize.txt
  *
  * @since 2.1.0
  */
        public byte[] Serialize {
            get {
                byte[] ptr = new byte[1];
                ptr [1] = 0;
                return ptr;
            }
        }

        /**
  * Decodes a byte string to replace the properties of this Frame.
  *
  * A Controller object must be instantiated for this function to succeed, but
  * it does not need to be connected. To extract gestures from the deserialized
  * frame, you must enable the appropriate gestures first.
  *
  * Any existing data in the frame is
  * destroyed. If you have references to
  * child objects (hands, fingers, etc.), these are preserved as long as the
  * references remain in scope.
  *
  * \include Frame_deserialize.txt
  *
  * **Note:** The behavior when calling functions which take
  * another Frame object as a parameter is undefined when either frame has
  * been deserialized. For example, calling ``Gestures(sinceFrame)`` on a
  * deserialized frame or with a deserialized frame as parameter (or both)
  * does not necessarily return all gestures that occurred between the two
  * frames. Motion functions, like ``ScaleFactor(startFrame)``, are more
  * likely to return reasonable results, but could return anomalous values
  * in some cases.
  *
  * @param arg A byte array containing the bytes of a serialized frame.
  * @since 2.1.0
  */
        public void Deserialize (byte[] arg)
        {
    
        }

        /**
     * Constructs a Frame object.
     *
     * Frame instances created with this constructor are invalid.
     * Get valid Frame objects by calling the Controller::frame() function.
     *
     * \include Frame_Frame.txt
     *
     * The only time you should use this constructor is before deserializing
     * serialized frame data. Call ``Frame::deserialize(string)`` to recreate
     * a saved Frame.
     *
     * @since 1.0
     */
        public Frame ()
        {
            _fingers = new FingerList (15);
            _hands = new HandList (3);
            _trackedQuad = new TrackedQuad ();
        }

        /**
     * The Hand object with the specified ID in this frame.
     *
     * Use the Frame::hand() function to retrieve the Hand object from
     * this frame using an ID value obtained from a previous frame.
     * This function always returns a Hand object, but if no hand
     * with the specified ID is present, an invalid Hand object is returned.
     *
     * \include Frame_hand.txt
     *
     * Note that ID values persist across frames, but only until tracking of a
     * particular object is lost. If tracking of a hand is lost and subsequently
     * regained, the new Hand object representing that physical hand may have
     * a different ID than that representing the physical hand in an earlier frame.
     *
     * @param id The ID value of a Hand object from a previous frame.
     * @returns The Hand object with the matching ID if one exists in this frame;
     * otherwise, an invalid Hand object is returned.
     * @since 1.0
     */
        public Hand Hand (int id)
        {
            return this.Hands.Find (delegate(Hand item) {
                return item.Id == id;
            });
        }

        /**
     * The Finger object with the specified ID in this frame.
     *
     * Use the Frame::finger() function to retrieve the Finger object from
     * this frame using an ID value obtained from a previous frame.
     * This function always returns a Finger object, but if no finger
     * with the specified ID is present, an invalid Finger object is returned.
     *
     * \include Frame_finger.txt
     *
     * Note that ID values persist across frames, but only until tracking of a
     * particular object is lost. If tracking of a finger is lost and subsequently
     * regained, the new Finger object representing that physical finger may have
     * a different ID than that representing the finger in an earlier frame.
     *
     * @param id The ID value of a Finger object from a previous frame.
     * @returns The Finger object with the matching ID if one exists in this frame;
     * otherwise, an invalid Finger object is returned.
     * @since 1.0
     */
        public Finger Finger (int id)
        {
            return this.Fingers.Find (delegate(Finger item) {
                return item.Id == id;
            });
        }

        /**
     * The change of position derived from the overall linear motion between
     * the current frame and the specified frame.
     *
     * The returned translation vector provides the magnitude and direction of
     * the movement in millimeters.
     *
     * \include Frame_translation.txt
     *
     * The Leap Motion software derives frame translation from the linear motion of
     * all objects detected in the field of view.
     *
     * If either this frame or sinceFrame is an invalid Frame object, then this
     * method returns a zero vector.
     *
     * @param sinceFrame The starting frame for computing the relative translation.
     * @returns A Vector representing the heuristically determined change in
     * position of all objects between the current frame and that specified
     * in the sinceFrame parameter.
     * @since 1.0
     */
        public Vector Translation (Frame sinceFrame)
        {
            return Vector.Zero;
        }

        /**
     * The estimated probability that the overall motion between the current
     * frame and the specified frame is intended to be a translating motion.
     *
     * \include Frame_translationProbability.txt
     *
     * If either this frame or sinceFrame is an invalid Frame object, then this
     * method returns zero.
     *
     * @param sinceFrame The starting frame for computing the translation.
     * @returns A value between 0 and 1 representing the estimated probability
     * that the overall motion between the current frame and the specified frame
     * is intended to be a translating motion.
     * @since 1.0
     */
        public float TranslationProbability (Frame sinceFrame)
        {
            return 0;
        }

        /**
     * The axis of rotation derived from the overall rotational motion between
     * the current frame and the specified frame.
     *
     * The returned direction vector is normalized.
     *
     * \include Frame_rotationAxis.txt
     *
     * The Leap Motion software derives frame rotation from the relative change in position and
     * orientation of all objects detected in the field of view.
     *
     * If either this frame or sinceFrame is an invalid Frame object, or if no
     * rotation is detected between the two frames, a zero vector is returned.
     *
     * @param sinceFrame The starting frame for computing the relative rotation.
     * @returns A normalized direction Vector representing the axis of the
     * heuristically determined rotational change between the current frame
     * and that specified in the sinceFrame parameter.
     * @since 1.0
     */
        public Vector RotationAxis (Frame sinceFrame)
        {
            return Vector.YAxis;
        }

        /**
     * The angle of rotation around the rotation axis derived from the overall
     * rotational motion between the current frame and the specified frame.
     *
     * The returned angle is expressed in radians measured clockwise around the
     * rotation axis (using the right-hand rule) between the start and end frames.
     * The value is always between 0 and pi radians (0 and 180 degrees).
     *
     * \include Frame_rotationAngle.txt
     *
     * The Leap Motion software derives frame rotation from the relative change in position and
     * orientation of all objects detected in the field of view.
     *
     * If either this frame or sinceFrame is an invalid Frame object, then the
     * angle of rotation is zero.
     *
     * @param sinceFrame The starting frame for computing the relative rotation.
     * @returns A positive value containing the heuristically determined
     * rotational change between the current frame and that specified in the
     * sinceFrame parameter.
     * @since 1.0
     */
        public float RotationAngle (Frame sinceFrame)
        {
            return 0;
        }

        /**
     * The angle of rotation around the specified axis derived from the overall
     * rotational motion between the current frame and the specified frame.
     *
     * The returned angle is expressed in radians measured clockwise around the
     * rotation axis (using the right-hand rule) between the start and end frames.
     * The value is always between -pi and pi radians (-180 and 180 degrees).
     *
     * \include Frame_rotationAngle_axis.txt
     *
     * The Leap Motion software derives frame rotation from the relative change in position and
     * orientation of all objects detected in the field of view.
     *
     * If either this frame or sinceFrame is an invalid Frame object, then the
     * angle of rotation is zero.
     *
     * @param sinceFrame The starting frame for computing the relative rotation.
     * @param axis The axis to measure rotation around.
     * @returns A value containing the heuristically determined rotational
     * change between the current frame and that specified in the sinceFrame
     * parameter around the given axis.
     * @since 1.0
     */
        public float RotationAngle (Frame sinceFrame, Vector axis)
        {
            return 0;
        }

        /**
     * The transform matrix expressing the rotation derived from the overall
     * rotational motion between the current frame and the specified frame.
     *
     * \include Frame_rotationMatrix.txt
     *
     * The Leap Motion software derives frame rotation from the relative change in position and
     * orientation of all objects detected in the field of view.
     *
     * If either this frame or sinceFrame is an invalid Frame object, then this
     * method returns an identity matrix.
     *
     * @param sinceFrame The starting frame for computing the relative rotation.
     * @returns A transformation Matrix containing the heuristically determined
     * rotational change between the current frame and that specified in the
     * sinceFrame parameter.
     * @since 1.0
     */
        public Matrix RotationMatrix (Frame sinceFrame)
        {
            return Matrix.Identity;
        }

        /**
     * The estimated probability that the overall motion between the current
     * frame and the specified frame is intended to be a rotating motion.
     *
     * \include Frame_rotationProbability.txt
     *
     * If either this frame or sinceFrame is an invalid Frame object, then this
     * method returns zero.
     *
     * @param sinceFrame The starting frame for computing the relative rotation.
     * @returns A value between 0 and 1 representing the estimated probability
     * that the overall motion between the current frame and the specified frame
     * is intended to be a rotating motion.
     * @since 1.0
     */
        public float RotationProbability (Frame sinceFrame)
        {
            return 0;
        }

        /**
     * The scale factor derived from the overall motion between the current frame
     * and the specified frame.
     *
     * The scale factor is always positive. A value of 1.0 indicates no
     * scaling took place. Values between 0.0 and 1.0 indicate contraction
     * and values greater than 1.0 indicate expansion.
     *
     * \include Frame_scaleFactor.txt
     *
     * The Leap Motion software derives scaling from the relative inward or outward motion of
     * all objects detected in the field of view (independent of translation
     * and rotation).
     *
     * If either this frame or sinceFrame is an invalid Frame object, then this
     * method returns 1.0.
     *
     * @param sinceFrame The starting frame for computing the relative scaling.
     * @returns A positive value representing the heuristically determined
     * scaling change ratio between the current frame and that specified in the
     * sinceFrame parameter.
     * @since 1.0
     */
        public float ScaleFactor (Frame sinceFrame)
        {
            return 1.0f;
        }

        /**
     * The estimated probability that the overall motion between the current
     * frame and the specified frame is intended to be a scaling motion.
     *
     * \include Frame_scaleProbability.txt
     *
     * If either this frame or sinceFrame is an invalid Frame object, then this
     * method returns zero.
     *
     * @param sinceFrame The starting frame for computing the relative scaling.
     * @returns A value between 0 and 1 representing the estimated probability
     * that the overall motion between the current frame and the specified frame
     * is intended to be a scaling motion.
     * @since 1.0
     */
        public float ScaleProbability (Frame sinceFrame)
        {
            return 0;
        }

        /**
     * Compare Frame object equality.
     *
     * \include Frame_operator_equals.txt
     *
     * Two Frame objects are equal if and only if both Frame objects represent
     * the exact same frame of tracking data and both Frame objects are valid.
     * @since 1.0
     */
        public bool Equals (Frame other)
        {
            return this.IsValid && other.IsValid && (this.Id == other.Id) && (this.Timestamp == other.Timestamp);
        }

        /**
     * A string containing a brief, human readable description of the Frame object.
     *
     * @returns A description of the Frame as a string.
     * @since 1.0
     */
        public override string ToString ()
        {
            return "Frame id: " + this.Id + " timestamp: " + this.Timestamp;
        }

        /**
     * A unique ID for this Frame.
     *
     * Consecutive frames processed by the Leap Motion software have consecutive
     * increasing values. You can use the frame ID to avoid processing the same
     * Frame object twice:
     *
     * \include Frame_Duplicate.txt
     *
     * As well as to make sure that your application processes every frame:
     *
     * \include Frame_Skipped.txt
     *
     * @returns The frame ID.
     * @since 1.0
     */
        public long Id {
            get {
                return _id;
            } 
        }

        /**
     * The frame capture time in microseconds elapsed since an arbitrary point in 
     * time in the past.
     *
     * Use Controller::now() to calculate the age of the frame.
     *
     * \include Frame_timestamp.txt
     *
     * @returns The timestamp in microseconds.
     * @since 1.0
     */
        public long Timestamp {
            get {
                return _timestamp;
            } 
        }

        /**
     * The instantaneous framerate.
     *
     * The rate at which the Leap Motion software is providing frames of data
     * (in frames per second). The framerate can fluctuate depending on available computing
     * resources, activity within the device field of view, software tracking settings,
     * and other factors.
     *
     * \include Frame_currentFramesPerSecond.txt
     *
     * @returns An estimate of frames per second of the Leap Motion Controller.
     * @since 1.0
     */
        public float CurrentFramesPerSecond {
            get {
                return _fps;
            } 
        }

        /**
     * The list of Finger objects detected in this frame, given in arbitrary order.
     * The list can be empty if no fingers are detected.
     *
     * Use PointableList::extended() to remove non-extended fingers from the list.
     *
     * \include Frame_fingers.txt
     *
     * @returns The FingerList containing all Finger objects detected in this frame.
     * @since 1.0
     */
        public FingerList Fingers {
            get {
                if (_fingers == null)
                    _fingers = new FingerList (15);

                return _fingers;
            } 
        }

        /**
     * The list of Hand objects detected in this frame, given in arbitrary order.
     * The list can be empty if no hands are detected.
     *
     * \include Frame_hands.txt
     *
     * @returns The HandList containing all Hand objects detected in this frame.
     * @since 1.0
     */
        public HandList Hands {
            get {
                if (_hands == null)
                    _hands = new HandList (3);

                return _hands;
            } 
        }

        /**
     * Reports whether this Frame instance is valid.
     *
     * A valid Frame is one generated by the Leap::Controller object that contains
     * tracking data for all detected entities. An invalid Frame contains no
     * actual tracking data, but you can call its functions without risk of a
     * null pointer exception. The invalid Frame mechanism makes it more
     * convenient to track individual data across the frame history. For example,
     * you can invoke:
     *
     * \include Frame_Valid_Chain.txt
     *
     * for an arbitrary Frame history value, "n", without first checking whether
     * frame(n) returned a null object. (You should still check that the
     * returned Finger instance is valid.)
     *
     * @returns True, if this is a valid Frame object; false otherwise.
     * @since 1.0
     */
        public bool IsValid {
            get {
                return _isValid;
            } 
        }

        /**
     * The current InteractionBox for the frame. See the InteractionBox class
     * documentation for more details on how this class should be used.
     *
     * \include Frame_interactionBox.txt
     *
     * @returns The current InteractionBox object.
     * @since 1.0
     */
        public InteractionBox InteractionBox { get{
                if(_interactionBox == null)
                    _interactionBox = new InteractionBox(new Vector(0, 200, 0), 
                                                         new Vector(200, 200, 200)
                    );

                return _interactionBox;
            } 
            private set{
                _interactionBox = value;
            } 
        }

        public int SerializeLength {
            get {
                return 0;
            } 
        }

        /**
     * Note: This class is an experimental API for internal use only. It may be
     * removed without warning.
     *
     * Returns information about the currently detected quad in the scene.
     *
     * \include Frame_trackedQuad.txt
     * If no quad is being tracked, then an invalid TrackedQuad is returned.
     * @since 2.2.6
     **/
        public TrackedQuad TrackedQuad {
            get {
                if (_trackedQuad == null)
                    _trackedQuad = new TrackedQuad ();

                return _trackedQuad;
            } 
            set {
                _trackedQuad = value;
            }
        }

        /**
     * Returns an invalid Frame object.
     *
     * You can use the instance returned by this function in comparisons testing
     * whether a given Frame instance is valid or invalid. (You can also use the
     * Frame::isValid() function.)
     *
     * \include Frame_Invalid_Demo.txt
     *
     * @returns The invalid Frame instance.
     * @since 1.0
     */
        public static Frame Invalid {
            get {
                return new Frame ();
            } 
        }

    }

}
