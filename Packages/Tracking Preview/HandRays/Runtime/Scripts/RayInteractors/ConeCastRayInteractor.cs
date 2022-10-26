using UnityEngine;

namespace Leap.Unity.Preview.HandRays
{
    public class ConeCastRayInteractor : HandRayInteractor
    {
        public float maxRayDistance = 50f;
        public float conecastMaxRadius = 1f;
        public float conecastAngle = 30f;

        public SingleLayer floorLayer;

        protected override int UpdateRayInteractorLogic(HandRayDirection handRayDirection, out RaycastHit[] results)
        {
            int hits = ConeCastExtension.ConeCastAll(handRayDirection.RayOrigin, conecastMaxRadius, handRayDirection.Direction, maxRayDistance, conecastAngle, layerMask, out results);
            Vector3 endPos = Vector3.zero;
            

            if(hits > 0)
            {
                for(int i = 0; i< hits; i++)
                {
                    //Prioritise other colliders over the floor collider
                    if(results[i].collider.gameObject.layer != floorLayer || endPos == Vector3.zero)
                    {
                        endPos = results[i].collider.transform.position;
                    }

                }
            }
            
            linePoints = new Vector3[] { handRayDirection.VisualAimPosition, endPos };
            return hits;
        }
    }
}