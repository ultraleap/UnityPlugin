using System.Collections.Generic;
using UnityEngine;

public static class ConeCastExtension
{
    public static int ConeCastAll(Vector3 origin, float maxRadius, Vector3 direction, float maxDistance, float coneAngle, LayerMask layerMask, out RaycastHit[] results)
    {
        RaycastHit[] sphereCastHits = Physics.SphereCastAll(origin - new Vector3(0, 0, maxRadius), maxRadius, direction, maxDistance, layerMask);
        List<RaycastHit> coneCastHits = new List<RaycastHit>();

        if (sphereCastHits.Length > 0)
        {
            for (int i = 0; i < sphereCastHits.Length; i++)
            {
                Vector3 hitPoint = sphereCastHits[i].point;
                Vector3 directionToHit = hitPoint - origin;
                float angleToHit = Vector3.Angle(direction, directionToHit);

                if (angleToHit < coneAngle)
                {
                    coneCastHits.Add(sphereCastHits[i]);
                }
            }
        }

        results = coneCastHits.ToArray();
        return coneCastHits.Count;
    }
}