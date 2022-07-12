/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Preview.FarFieldInteractions
{
    /// <summary>
    /// A Euler angle deadzone. Used to filter a value and stop it from changing until its delta has passed a threshold
    /// This deadzone takes into account the circular relationship between 0 and 360 for Euler Angles
    /// </summary>
    public class EulerAngleDeadzone : MonoBehaviour
    {
        private struct TimestampDeadzoneCentre
        {
            public float Timestamp;
            public float DeadzoneCentre;

            public TimestampDeadzoneCentre(float timestamp, float deadzoneCentre) : this()
            {
                Timestamp = timestamp;
                DeadzoneCentre = deadzoneCentre;
            }
        }

        /// <summary>
        /// How much the value needs to change before the deadzone begins to move
        /// </summary>
        [Tooltip("How much the value needs to change before the deadzone begins to move")]
        public float DeadzoneSize = 25;
        [Space]

        /// <summary>
        /// If true, when still for RecentreMovementSeconds, the deadzone centre will move to the current position
        /// </summary>
        [Tooltip("If true, when still for RecentreMovementSeconds amount of time, the deadzone centre will move to the current position")]
        public bool UseRecentring = true;

        /// <summary>
        /// If recentring is enabled, the deadzone will recentre after this amount of time
        /// </summary>
        [Tooltip("If recentring is enabled, the deadzone will recentre after this amount of time")]
        [Range(0, 2)] public float RecentreMovementSeconds = 1.5f;

        /// <summary>
        /// Used to check for a 'still' value
        /// </summary>
        [Tooltip("Used to check for a 'still' value")]
        [Range(0, 360)] public float RecentreMovementThreshold = 10;

        /// <summary>
        /// How quickly the Recentre delta returns to 0 whilst the value is changing
        /// </summary>
        [Tooltip("How quickly the Recentre delta returns to 0 whilst the value is changing")]
        [Range(0.01f, 30)] public float RecentreLerpSpeed = 1;

        /// <summary>
        /// The centre of the deadzone
        /// </summary>
        public float DeadzoneCentre { get; private set; }

        private float recentredPositionDelta;
        private bool recentredWhilstStill;
        private Queue<TimestampDeadzoneCentre> deadzoneCentreHistory;
        private bool initialised = false;

        /// <summary>
        /// Update the deadzone based on the latest angle
        /// </summary
        public void UpdateDeadzone(float currentAngle)
        {
            //Calculate Deadzone
            if (!initialised)
            {
                DeadzoneCentre = currentAngle;
                recentredPositionDelta = 0;
                deadzoneCentreHistory = new Queue<TimestampDeadzoneCentre>();
                initialised = true;
                recentredWhilstStill = false;
                return;
            }

            float delta = currentAngle + recentredPositionDelta - DeadzoneCentre;
            delta = SimplifyAngle(delta);
            float signDelta = Mathf.Sign(delta);
            float absDelta = Mathf.Abs(delta);

            if (absDelta > DeadzoneSize)
            {
                if (recentredWhilstStill)
                {
                    deadzoneCentreHistory.Clear();
                    recentredWhilstStill = false;
                }
                DeadzoneCentre += (absDelta - DeadzoneSize) * signDelta;
                DeadzoneCentre %= 360;

                //Reduce recentring offset towards 0
                if (absDelta > DeadzoneSize + 0.1f)
                {
                    recentredPositionDelta = Mathf.Lerp(recentredPositionDelta, 0, Time.deltaTime * RecentreLerpSpeed);
                }
            }

            //Calculate if need to recentre
            if (UseRecentring)
            {
                RecentreDeadzone(currentAngle);
            }
        }

        /// <summary>
        /// Move the deadzoneCentre to the current angle, if the value has changed less than RecentreMovementThreshold in the past RecentreMovementSeconds
        /// </summary>
        private void RecentreDeadzone(float currentAngle)
        {
            deadzoneCentreHistory.Enqueue(new TimestampDeadzoneCentre(Time.time, DeadzoneCentre));

            //If the earliest time in the queue was less than RecentreMovementSeconds ago, return
            if (Time.time - deadzoneCentreHistory.Peek().Timestamp < RecentreMovementSeconds)
            {
                return;
            }

            //Dequeue all times later than RecentreMovementSeconds ago
            while (Time.time - deadzoneCentreHistory.Peek().Timestamp > RecentreMovementSeconds)
            {
                deadzoneCentreHistory.Dequeue();
            }

            //If the user hasn't been relatively still for the last RecentreMovementSeconds, return
            for (int i = 0; i < deadzoneCentreHistory.Count; i++)
            {
                float recentreDelta = deadzoneCentreHistory.Peek().DeadzoneCentre - DeadzoneCentre;
                recentreDelta = SimplifyAngle(recentreDelta);
                recentreDelta = Mathf.Abs(recentreDelta);
                if (recentreDelta >= RecentreMovementThreshold)
                {
                    return;
                }
            }

            recentredWhilstStill = true;
            recentredPositionDelta = DeadzoneCentre - currentAngle;
            recentredPositionDelta = SimplifyAngle(recentredPositionDelta);
            deadzoneCentreHistory.Clear();
        }

        /// <summary>
        /// Translates our angle from the range 0-360 to -180 to +180
        /// </summary>
        /// <param name="angle">Euler angle in the range 0-360</param>
        /// <returns>Euler angle in the range -180 - 180</returns>
        private float SimplifyAngle(float angle)
        {
            angle = angle % 360;

            if (angle > 180)
            {
                angle -= 360;
            }
            else if (angle < -180)
            {
                angle += 360;
            }
            return angle;
        }
    }
}