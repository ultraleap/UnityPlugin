using Leap.PhysicalHands;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Leap.PhysicalHands.ContactBone;

namespace Leap.PhysicalHands
{
    public static class PhysicalHandUtils
    {
        /// <summary>
        /// Calculates the shortest distance between a list of hand objects and a specified game object.
        /// </summary>
        /// <param name="handsToCompare">The list of hand objects to compare distances from.</param>
        /// <param name="objectToCheckDistanceFrom">The game object to check distances from.</param>
        /// <returns>The shortest distance between the hands and the specified game object.</returns>
        public static float ClosestHandDistance(List<ContactHand> handsToCompare, GameObject objectToCheckDistanceFrom)
        {
            float shortestDistance = float.PositiveInfinity;
            // Loop through each hand in the list
            foreach (var hand in handsToCompare)
            {
                // Calculate the distance between the hand and the object
                float handDistance = Vector3.Distance(
                    hand.palmBone.transform.position, objectToCheckDistanceFrom.transform.position);

                // Update shortestDistance if the current hand is closer
                if (handDistance < shortestDistance)
                {
                    shortestDistance = handDistance;
                }
            }
            return shortestDistance;
        }

        /// <summary>
        /// Calculates the shortest distance between a list of hand objects and a specified game object.
        /// </summary>
        /// <param name="handsToCompare">The list of hand objects to compare distances from.</param>
        /// <param name="objectToCheckDistanceFrom">The game object to check distances from.</param>
        /// <returns>The shortest distance between the hands and the specified game object.</returns>
        public static float ClosestHandBoneDistance(List<ContactHand> handsToCompare, Rigidbody objectToCheckDistanceFrom)
        {
            float shortestDistance = float.PositiveInfinity;

            Dictionary<Collider, ClosestColliderDirection> result;

            if (objectToCheckDistanceFrom == null)
                return shortestDistance;

            // Loop through each hand in the list
            foreach (var hand in handsToCompare)
            {
                foreach (var bone in hand.bones)
                {
                    // Only check for distal joints
                    if (bone.Joint == 2 && bone.NearbyObjects.TryGetValue(objectToCheckDistanceFrom, out result))
                    {
                        foreach (var value in result.Values)
                        {
                            if (value.distance < shortestDistance)
                            {
                                shortestDistance = value.distance;
                            }
                        }
                    }
                }
            }

            return shortestDistance;
        }

        /// <summary>
        /// Finds the closest Contact Hand from a list of contact hands to a given object.
        /// </summary>
        /// <param name="contactHands">A list of ContactHand objects representing hands to compare distances from.</param>
        /// <param name="objectToCheckDistanceFrom">The GameObject from which distances are measured.</param>
        /// <returns>The closest ContactHand to the given object, or null if the list is empty.</returns>
        public static ContactHand ClosestHand(List<ContactHand> contactHands, GameObject objectToCheckDistanceFrom)
        {
            ContactHand closestHand = null;
            float closestDist = float.PositiveInfinity;
            // Check if the list of contact hands is not empty
            if (contactHands.Count > 0)
            {
                // Loop through each contact hand in the list
                foreach (var hand in contactHands)
                {
                    // Calculate the distance between the hand and the object
                    float distanceFromHand = Vector3.Distance(
                        hand.palmBone.transform.position, objectToCheckDistanceFrom.transform.position);

                    // Update closestHand if the current hand is closer
                    if (closestHand == null || distanceFromHand < closestDist)
                    {
                        closestHand = hand;
                        closestDist = distanceFromHand;
                    }
                }
                return closestHand; // Return the closest hand found
            }
            else
            {
                return null; // Return null if the list is empty
            }
        }

        /// <summary>
        /// Finds the closest hand from a list of hands to a given object.
        /// </summary>
        /// <param name="contactHands">A list of Hand objects representing hands to compare distances from.</param>
        /// <param name="objectToCheckDistanceFrom">The GameObject from which distances are measured.</param>
        /// <returns>The closest Hand to the given object, or null if the list is empty.</returns>
        public static Hand ClosestHand(List<Hand> contactHands, GameObject objectToCheckDistanceFrom)
        {
            Hand closestHand = null;
            float closestDist = float.PositiveInfinity;
            // Check if the list of hands is not empty
            if (contactHands.Count > 0)
            {
                // Loop through each hand in the list
                foreach (var hand in contactHands)
                {
                    // Calculate the distance between the hand and the object
                    float distanceFromHand = Vector3.Distance(
                        hand.PalmPosition, objectToCheckDistanceFrom.transform.position);

                    // Update closestHand if the current hand is closer
                    if (closestHand == null || distanceFromHand < closestDist)
                    {
                        closestHand = hand;
                        closestDist = distanceFromHand;
                    }
                }
                return closestHand; // Return the closest hand found
            }
            else
            {
                return null; // Return null if the list is empty
            }
        }
    }
}