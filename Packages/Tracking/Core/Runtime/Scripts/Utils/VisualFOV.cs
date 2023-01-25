/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity
{
    /// <summary>
    /// Class to generate meshes that can be used for field of view visualization.
    /// Set the FOV variables depending on the device type used, call UpdateFOVs() and 
    /// access the meshes OptimalFOVMesh, NoTrackingFOVMesh and MaxFOVMesh.
    /// </summary>
    public class VisualFOV
    {
        // FOV Variables
        [Range(0, 360)]
        public float HorizontalFOV = 0f;
        [Range(0, 360)]
        public float VerticalFOV = 90f;
        public float MaxDistance = 100f;
        public float OptimalMaxDistance = 50f;
        public float MinDistance = 1f;

        // Quality settings
        public int RayCount = 10;

        public Mesh OptimalFOVMesh;
        public Mesh NoTrackingFOVMesh;
        public Mesh MaxFOVMesh;


        public bool ShowRaycasts = false;
        public bool ShowMaximumField = true;
        public bool ShowNoTrackingField = false;
        public bool ShowOptimalField = true;

        // Ray colors
        public Color intersectedRayColor;
        public Color rayColor;

        public VisualFOV()
        {
            OptimalFOVMesh = new Mesh();
            NoTrackingFOVMesh = new Mesh();
            MaxFOVMesh = new Mesh();

            UpdateFOVS();
        }

        /// <summary>
        /// regenerate the FOV meshes. 
        /// </summary>
        public void UpdateFOVS()
        {
            if (ShowNoTrackingField)
            {
                DrawFOV(NoTrackingFOVMesh, 0, MinDistance);
            }
            var _currentMinDistance = MinDistance;
            if (ShowOptimalField)
            {
                DrawFOV(OptimalFOVMesh, _currentMinDistance, OptimalMaxDistance);
                _currentMinDistance = OptimalMaxDistance;
            }
            if (ShowMaximumField)
            {
                DrawFOV(MaxFOVMesh, _currentMinDistance, MaxDistance);
            }
        }

        private void DrawFOV(Mesh mesh, float minDistance, float maxDistance)
        {
            var origins = new List<Vector3>();
            var verts = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            if (RayCount <= 0 || HorizontalFOV <= 0 || VerticalFOV <= 0)
                return;
            int offset = RayCount * RayCount;

            var ray = new Ray
            {
                origin = Vector3.zero
            };

            RaycastHit hit;
            var i = -HorizontalFOV / 2;
            for (int horizontalFOVCount = 0; horizontalFOVCount < RayCount; horizontalFOVCount++)
            {
                var j = -VerticalFOV / 2;
                for (int verticalFOVCount = 0; verticalFOVCount < RayCount; verticalFOVCount++)
                {
                    ray.direction = Quaternion.Euler(-90, 0, 0) * Quaternion.Euler(j, i, 0) * Vector3.forward;
                    //check for distance
                    if (Physics.Raycast(ray, out hit, maxDistance, LayerMask.GetMask()))
                    {
                        var distance = Vector3.Distance(hit.point, ray.origin);
                        if (distance < minDistance)
                        {
                            if (ShowRaycasts)
                                Debug.DrawRay(ray.origin + ((hit.point - ray.origin).normalized), (hit.point - ray.origin).normalized, intersectedRayColor);
                            origins.Add(ray.origin + ((hit.point - ray.origin).normalized * distance));
                            verts.Add(hit.point - ray.origin);
                        }
                        else
                        {
                            if (ShowRaycasts)
                                Debug.DrawRay(ray.origin + ((hit.point - ray.origin).normalized * minDistance), (hit.point - ray.origin).normalized * Vector3.Distance(hit.point, ray.origin + ((hit.point - ray.origin).normalized * minDistance)), intersectedRayColor);
                            origins.Add(ray.origin + ((hit.point - ray.origin).normalized * minDistance));
                            verts.Add(hit.point - ray.origin);
                        }
                    }
                    else
                    {
                        if (ShowRaycasts)
                            Debug.DrawRay(ray.origin + (ray.direction * minDistance), ray.direction * (maxDistance - minDistance), rayColor);
                        origins.Add(ray.origin + (ray.direction * minDistance));
                        verts.Add(ray.direction * maxDistance);
                    }

                    if (horizontalFOVCount > 0)
                    {

                        if (verticalFOVCount < RayCount - 1)
                        {
                            triangles.Add(verts.Count - RayCount - 1);
                            triangles.Add(verts.Count - RayCount);
                            triangles.Add(verts.Count - 1);

                            triangles.Add(offset + verts.Count - 1);
                            triangles.Add(offset + verts.Count - RayCount);
                            triangles.Add(offset + verts.Count - RayCount - 1);
                        }

                        if (verticalFOVCount > 0 && verticalFOVCount < RayCount)
                        {

                            triangles.Add(verts.Count - RayCount - 1);
                            triangles.Add(verts.Count - 1);
                            triangles.Add(verts.Count - 2);

                            triangles.Add(offset + verts.Count - 2);
                            triangles.Add(offset + verts.Count - 1);
                            triangles.Add(offset + verts.Count - RayCount - 1);
                        }
                    }
                    j += (VerticalFOV / (RayCount - 1));
                }


                if (horizontalFOVCount > 0)
                {
                    //add top
                    triangles.Add(offset + verts.Count - RayCount);
                    triangles.Add(verts.Count - RayCount - RayCount);
                    triangles.Add(verts.Count - RayCount);

                    triangles.Add(verts.Count - RayCount - RayCount);
                    triangles.Add(offset + verts.Count - RayCount);
                    triangles.Add(offset + verts.Count - RayCount - RayCount);
                    //bottom
                    triangles.Add(verts.Count - 1);
                    triangles.Add(verts.Count - RayCount - 1);
                    triangles.Add(offset + verts.Count - 1);

                    triangles.Add(offset + verts.Count - RayCount - 1);
                    triangles.Add(offset + verts.Count - 1);
                    triangles.Add(verts.Count - RayCount - 1);
                }
                i += (HorizontalFOV / (RayCount - 1));
            }

            //Add sides
            for (int k = 1; k < RayCount; k++)
            {
                if (k > 0)
                {
                    triangles.Add(k);
                    triangles.Add(k - 1);
                    triangles.Add(offset + k);

                    triangles.Add(k - 1);
                    triangles.Add(offset + k - 1);
                    triangles.Add(offset + k);
                }

                if (k < RayCount)
                {
                    triangles.Add(verts.Count - k - 1);
                    triangles.Add(verts.Count - k);
                    triangles.Add(offset + verts.Count - k - 1);

                    triangles.Add(verts.Count - k);
                    triangles.Add(offset + verts.Count - k);
                    triangles.Add(offset + verts.Count - k - 1);
                }
            }

            verts.AddRange(origins);

            mesh.vertices = verts.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
        }
    }
}