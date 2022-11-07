using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    public class ConeCastRayInteractor : HandRayInteractor
    {
        public float maxRayDistance = 50f;
        public float conecastMaxRadius = 1f;
        public float conecastAngle = 30f;

        protected override int UpdateRayInteractorLogic(HandRayDirection handRayDirection, out RaycastHit[] results, out RaycastHit primaryHit)
        {
            int hits = ConeCastExtension.ConeCastAll(handRayDirection.RayOrigin, conecastMaxRadius, handRayDirection.Direction, maxRayDistance, conecastAngle, layerMask, out results);
            Vector3 endPos = Vector3.zero;

            if (hits > 0)
            {
                primaryHit = results[0];
                float currentShortestDistance = Mathf.Infinity;
                Ray ray = new Ray(handRayDirection.AimPosition, handRayDirection.Direction);

                for (int i = 0; i < hits; i++)
                {
                    //Prioritise other colliders over the floor collider
                    if (results[i].collider.gameObject.layer != farFieldLayerManager.FloorLayer || endPos == Vector3.zero)
                    {
                        Vector3 point = results[i].collider.transform.position;
                        float newDistance = Vector3.Cross(ray.direction, point - ray.origin).magnitude;

                        if (newDistance < currentShortestDistance)
                        {
                            primaryHit = results[i];
                            endPos = results[i].collider.transform.position;
                            currentShortestDistance = newDistance;
                        }
                    }
                }
                linePoints = new Vector3[] { handRayDirection.VisualAimPosition, endPos };
            }
            else
            {
                primaryHit = new RaycastHit();
                linePoints = new Vector3[] { handRayDirection.VisualAimPosition, handRayDirection.VisualAimPosition + (handRayDirection.Direction * maxRayDistance) };
            }

            numPoints = linePoints.Length;
            return hits;
        }
    }
}