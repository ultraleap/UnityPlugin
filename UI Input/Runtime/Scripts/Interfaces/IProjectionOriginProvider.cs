using UnityEngine;

namespace Leap.Unity.InputModule
{
    /// <summary>
    /// Provides the location of the projection origin for raycasting
    /// </summary>
    interface IProjectionOriginProvider
    {
        /// <summary>
        /// Proxy for the MonoBehaviour Update
        /// </summary>
        void Update();

        /// <summary>
        /// Call when in the MonoBehaviour Process method
        /// </summary>
        void Process();

        /// <summary>
        /// Returns the projection origin for the indicated hand
        /// </summary>
        /// <param name="isLeftHand">True if this is for a left hand</param>
        /// <returns>The projection origin</returns>
        Vector3 ProjectionOriginForHand(bool isLeftHand);

        Quaternion CurrentRotation { get; }
        Vector3 ProjectionOriginLeft { get; }
        Vector3 ProjectionOriginRight { get; }

    }
}
