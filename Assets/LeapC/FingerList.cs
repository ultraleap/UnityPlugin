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
    using System.Collections.Generic;

    /**
   * The FingerList class represents a list of Finger objects.
   *
   * Get a FingerList object by calling Frame::fingers().
   *
   * \include FingerList_FingerList.txt
   *
   * @since 1.0
   */

    public class FingerList : List<Finger>
    {
        public FingerList():base(){}
        public FingerList(int initialCapacity):base(initialCapacity){}

        /**
     * Returns a new list containing those fingers in the current list that are
     * extended.
     *
     * \include FingerList_extended.txt
     *
     * @returns The list of extended fingers from the current list.
     * @since 2.0
     */
        public FingerList Extended ()
        {
            return (FingerList) this.FindAll (delegate (Finger finger) {
                return finger.IsExtended;
            });
        }


        /**
     * Returns a list containing fingers from the current list of a given finger type by
     * modifying the existing list.
     *
     * \include FingerList_fingerType.txt
     *
    * @returns The list of matching fingers from the current list.
     * @since 2.0
     */
        public FingerList FingerType (Finger.FingerType type)
        {
            return (FingerList) this.FindAll (delegate (Finger finger) {
                return finger.Type == type;
            });
        }



/**
     * Reports whether the list is empty.
     *
     * \include FingerList_isEmpty.txt
     *
     * @returns True, if the list has no members.
     * @since 1.0
     */
        public bool IsEmpty {
            get {
                return this.Count == 0;
            } 
        }

/**
     * The member of the list that is farthest to the left within the standard
     * Leap Motion frame of reference (i.e has the smallest X coordinate).
     *
     * \include FingerList_leftmost.txt
     *
     * @returns The leftmost finger, or invalid if list is empty.
     * @since 1.0
     */
        public Finger Leftmost {
            get {
                Finger mostest = new Finger();
                float position = float.MaxValue;
                foreach(Finger finger in this){
                    if(finger.TipPosition.x < position){
                        mostest = finger;
                        position = finger.TipPosition.x;
                    }
                }
                return mostest;
            } 
        }

/**
     * The member of the list that is farthest to the right within the standard
     * Leap Motion frame of reference (i.e has the largest X coordinate).
     *
     * \include FingerList_rightmost.txt
     *
     * @returns The rightmost finger, or invalid if list is empty.
     * @since 1.0
     */
        public Finger Rightmost {
            get {
                Finger mostest = new Finger();
                float position = float.MaxValue;
                foreach(Finger finger in this){
                    if(finger.TipPosition.x > position){
                        mostest = finger;
                        position = finger.TipPosition.x;
                    }
                }
                return mostest;
            } 
        }

/**
     * The member of the list that is farthest to the front within the standard
     * Leap Motion frame of reference (i.e has the smallest Z coordinate).
     *
     * \include FingerList_frontmost.txt
     *
     * @returns The frontmost finger, or invalid if list is empty.
     * @since 1.0
     */
        public Finger Frontmost {
            get {
                Finger mostest = new Finger();
                float position = float.MaxValue;
                foreach(Finger finger in this){
                    if(finger.TipPosition.z < position){
                        mostest = finger;
                        position = finger.TipPosition.z;
                    }
                }
                return mostest;
            } 
        }

    }

}
