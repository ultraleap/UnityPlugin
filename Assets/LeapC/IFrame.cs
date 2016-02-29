using System;
using System.Collections.Generic;

namespace Leap
{
    /**
    * The IFrame class represents a set of hand and finger tracking data detected
    * in a single frame.
    *
    * The Leap Motion software detects hands, fingers and tools within the tracking area, reporting
    * their positions, orientations, gestures, and motions in frames at the Leap Motion frame rate.
    *
    * Access IFrame objects through an instance of the Controller class:
    *
    * \include Controller_Frame_1.txt
    *
    * Implement a Listener subclass to receive a callback event when a new Frame is available.
    */
    public interface IFrame
    {
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
        long Id { get; }

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
        long Timestamp { get; }

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
        float CurrentFramesPerSecond { get; }

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
        IHand Hand(int id);

        /**
        * The list of Hand objects detected in this frame, given in arbitrary order.
        * The list can be empty if no hands are detected.
        *
        * \include Frame_hands.txt
        *
        * @returns The List<Hand> containing all Hand objects detected in this frame.
        * @since 1.0
        */
        List<IHand> Hands { get; }

        /**
        * The current InteractionBox for the frame. See the InteractionBox class
        * documentation for more details on how this class should be used.
        *
        * \include Frame_interactionBox.txt
        *
        * @returns The current InteractionBox object.
        * @since 1.0
        */
        InteractionBox InteractionBox { get; }

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
        TrackedQuad TrackedQuad { get; set; }

        int SerializeLength { get; }

        /**
        * Encodes this Frame object as a byte string.
        *
        * \include Frame_serialize.txt
        *
        * @since 2.1.0
        */
        byte[] Serialize { get; }

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
        void Deserialize(byte[] arg);

        /**
        * Creates a shallow copy of this Frame, transforming all hands, fingers, and bones by 
        * the specified transform on demand.
        *
        * @param trs A Matrix containing the desired translation, rotation, and scale
        * of the copied Frame.
        * @returns a new Frame object with the transform applied.
        */
        IFrame TransformedShallowCopy(ref Matrix trs);

        /**
        * Creates a copy of this Frame, transforming all hands, fingers, and bones by the specified transform.
        *
        * @param trs A Matrix containing the desired translation, rotation, and scale
        * of the copied Frame.
        * @returns a new Frame object with the transform applied.
        * @since 3.0
        */
        IFrame TransformedCopy(ref Matrix trs);
        
        /**
        * A string containing a brief, human readable description of the Frame object.
        *
        * @returns A description of the Frame as a string.
        * @since 1.0
        */
        string ToString();

        /**
        * Compare Frame object equality.
        *
        * \include Frame_operator_equals.txt
        *
        * Two Frame objects are equal if and only if both Frame objects represent
        * the exact same frame of tracking data and both Frame objects are valid.
        * @since 1.0
        */
        bool Equals(IFrame other);
    }
}
