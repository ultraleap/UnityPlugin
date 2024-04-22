/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;


namespace Leap.Unity.Examples
{

    /// <summary>
    /// Renderer for a PinchStrokeProcessor that renders thick ribbons.
    /// It neeeds a mesh filter and a mesh renderer
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ThickRibbonRenderer : MonoBehaviour
    {
        private const float VERTICAL_THICKNESS_MULTIPLIER = 1 / 40F;

        /// <summary>
        /// The material of the ribbons
        /// </summary>
        public Material _ribbonMaterial;
        /// <summary>
        /// The gameobject where the ribbons should be placed under in the scene
        /// </summary>
        public GameObject _finalizedRibbonParent;

        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private List<Vector3> _verts;
        private List<int> _indices;
        private List<Color> _colors;
        private List<Vector3> _normals;
        private List<StrokePoint> _cachedStrokeRenderered;

        protected void Start()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            _meshRenderer.material = _ribbonMaterial;

            _verts = new List<Vector3>();
            _indices = new List<int>();
            _colors = new List<Color>();
            _normals = new List<Vector3>();

            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// initialize the renderer.
        /// Clears all lists such as vertices and normals of a generated ribbon mesh
        /// </summary>
        public void InitializeRenderer()
        {
            _meshFilter.mesh = _mesh = new Mesh();
            _mesh.MarkDynamic();

            _ribbonSegments.Clear();

            _verts.Clear();
            _indices.Clear();
            _colors.Clear();
            _normals.Clear();
        }

        private List<RibbonSegment> _ribbonSegments = new List<RibbonSegment>();
        /// <summary>
        /// Updates the renderer.
        /// Adds new vertices to the ribbon based on the changed stroke points
        /// </summary>
        /// <param name="stroke">The complete stroke</param>
        /// <param name="maxChangedFromEnd">The number of stroke points that have been changed at the end of the stroke</param>
        public void UpdateRenderer(List<StrokePoint> stroke, int maxChangedFromEnd)
        {
            if (stroke.Count <= 1 || maxChangedFromEnd == 0) return;

            int startIdx = Mathf.Max(0, (stroke.Count - 1) - maxChangedFromEnd - 1);
            int endIdx = stroke.Count - 1;

            // Lop off outdated vertices and indices.

            int expectedNewNumRibbons = stroke.Count - 1;
            int firstChangedRibbonIndex = expectedNewNumRibbons - 1 - maxChangedFromEnd;
            int numRibbonSegmentsOutdated = _ribbonSegments.Count - firstChangedRibbonIndex;
            int numVertsOutdated = 0;
            int numRibbonVertsAccountFor = 0;
            int numIndicesOutdated = 0;
            for (int i = 0; i < numRibbonSegmentsOutdated; i++)
            {
                if (i >= 0 && i < _ribbonSegments.Count)
                {
                    numVertsOutdated += _ribbonSegments[_ribbonSegments.Count - 1 - i].NumVerts();
                    numRibbonVertsAccountFor++;
                    numIndicesOutdated += _ribbonSegments[_ribbonSegments.Count - 1 - i].NumIndices();
                }
            }
            for (int i = 0; i < numVertsOutdated; i++)
            {
                if (_verts.Count > 0)
                {
                    _verts.RemoveAt(_verts.Count - 1);
                    _colors.RemoveAt(_colors.Count - 1);
                    _normals.RemoveAt(_normals.Count - 1);
                }
            }
            for (int i = 0; i < numIndicesOutdated; i++)
            {
                if (_indices.Count > 0)
                {
                    _indices.RemoveAt(_indices.Count - 1);
                }
            }

            // Update Ribbon Segment representation.
            for (int i = startIdx; i < endIdx; i++)
            {

                // Define the current ribbon segment.
                StrokePoint curPoint = stroke[i];
                StrokePoint nextPoint = stroke[i + 1];
                Vector3 curSegmentDirection = (nextPoint.position - curPoint.position).normalized;

                Vector3 prevSegmentDirection = curSegmentDirection;
                bool hasPrevSegment = (i > 0);
                if (hasPrevSegment)
                {
                    prevSegmentDirection = (stroke[i].position - stroke[i - 1].position).normalized;
                }

                Vector3 nextSegmentDirection = curSegmentDirection;
                bool hasNextSegment = (i < endIdx - 1);
                if (hasNextSegment)
                {
                    nextSegmentDirection = (stroke[i + 2].position - stroke[i + 1].position).normalized;
                }

                bool curSegmentHasStartCap = i == 0;
                if (hasPrevSegment)
                {
                    curSegmentHasStartCap = (Vector3.Dot(prevSegmentDirection, curSegmentDirection) < -0.8F);
                }
                Vector3 startFacing, startNormal;
                if (curSegmentHasStartCap)
                {
                    startFacing = -curSegmentDirection;
                    startNormal = curPoint.normal;
                }
                else
                {
                    startFacing = -(prevSegmentDirection + curSegmentDirection).normalized;
                    startNormal = (stroke[i - 1].normal + curPoint.normal).normalized;
                }

                bool curSegmentHasEndCap = i == stroke.Count - 2;
                if (hasNextSegment)
                {
                    curSegmentHasEndCap = (Vector3.Dot(curSegmentDirection, nextSegmentDirection) < -0.8F);
                }
                Vector3 endFacing, endNormal;
                if (curSegmentHasEndCap)
                {
                    endFacing = curSegmentDirection;
                    endNormal = curPoint.normal;
                }
                else
                {
                    endFacing = (curSegmentDirection + nextSegmentDirection).normalized;
                    endNormal = (curPoint.normal + nextPoint.normal).normalized;
                }

                RibbonSegment curRibbonSegment = new RibbonSegment(
                  curPoint.position,
                  startFacing,
                  startNormal,
                  curPoint.thickness,
                  nextPoint.position,
                  endFacing,
                  endNormal,
                  nextPoint.thickness,
                  curSegmentHasStartCap,
                  curSegmentHasEndCap
                  );

                // Add or modify the constructed ribbon segment
                if (i > _ribbonSegments.Count - 1)
                {
                    // Add a new ribbon segment definition.
                    _ribbonSegments.Add(curRibbonSegment);
                }
                else
                {
                    // Modify an existing ribbon segment definition.
                    _ribbonSegments[i] = curRibbonSegment;
                }
            }

            // Construct and add new vertices and indices per RibbonSegment.
            for (int i = startIdx; i < endIdx; i++)
            {
                AddRibbonSegmentIndices(_ribbonSegments[i], _verts.Count, _indices);
                AddRibbonSegmentVerts(_ribbonSegments[i], _verts, _normals);
                for (int j = 0; j < _ribbonSegments[i].NumVerts(); j++)
                { // color vertices
                    _colors.Add(stroke[i].color);
                }
            }

            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.SetTriangles(_indices, 0);
            _mesh.SetNormals(_normals);
            _mesh.SetColors(_colors);
            _mesh.RecalculateBounds();
            _mesh.RecalculateNormals();
            _mesh.UploadMeshData(false);

            _cachedStrokeRenderered = stroke;
        }

        private void AddRibbonSegmentVerts(RibbonSegment ribbonSegment, List<Vector3> verts, List<Vector3> normals)
        {
            // Vertex order:
            // Side up, right, down, left
            // Startcap (if any)
            // Endcap (if any)
            Vector3 startPos = ribbonSegment.startPos;
            Vector3 startFaceLeftDir = Vector3.Cross(ribbonSegment.startFacing, ribbonSegment.startNormal).normalized;
            Vector3 startFaceUpDir = ribbonSegment.startNormal;
            float startXThickness = ribbonSegment.startThickness;
            float startYThickness = ribbonSegment.startThickness * VERTICAL_THICKNESS_MULTIPLIER;
            Vector3 endPos = ribbonSegment.endPos;
            Vector3 endFaceLeftDir = Vector3.Cross(ribbonSegment.endNormal, ribbonSegment.endFacing).normalized; // "left" looking at/through the start face
            Vector3 endFaceUpDir = ribbonSegment.endNormal;
            float endXThickness = ribbonSegment.endThickness;
            float endYThickness = ribbonSegment.endThickness * VERTICAL_THICKNESS_MULTIPLIER;

            Vector3 startLeft = startFaceLeftDir * startXThickness;
            Vector3 startRight = -startLeft;
            Vector3 startUp = startFaceUpDir * startYThickness;
            Vector3 startDown = -startUp;

            Vector3 endLeft = endFaceLeftDir * endXThickness;
            Vector3 endRight = -endLeft;
            Vector3 endUp = endFaceUpDir * endYThickness;
            Vector3 endDown = -endUp;

            // Startcap
            if (ribbonSegment.hasStartCap)
            {
                verts.Add(startPos + startLeft + startDown);
                verts.Add(startPos + startRight + startDown);
                verts.Add(startPos + startRight + startUp);
                verts.Add(startPos + startLeft + startUp);
                normals.Add(ribbonSegment.startFacing);
                normals.Add(ribbonSegment.startFacing);
                normals.Add(ribbonSegment.startFacing);
                normals.Add(ribbonSegment.startFacing);

                // Start side up
                verts.Add(startPos + startLeft + startUp);
                verts.Add(startPos + startRight + startUp);
                normals.Add(startUp);
                normals.Add(startUp);

                // Start side right
                verts.Add(startPos + startRight + startUp);
                verts.Add(startPos + startRight + startDown);
                normals.Add(startRight);
                normals.Add(startRight);

                // Start side down
                verts.Add(startPos + startRight + startDown);
                verts.Add(startPos + startLeft + startDown);
                normals.Add(startDown);
                normals.Add(startDown);

                // Start side left
                verts.Add(startPos + startLeft + startDown);
                verts.Add(startPos + startLeft + startUp);
                normals.Add(startLeft);
                normals.Add(startLeft);
            }

            // End side up
            verts.Add(endPos + endLeft + endUp);
            verts.Add(endPos + endRight + endUp);
            normals.Add(endUp);
            normals.Add(endUp);

            // End side right
            verts.Add(endPos + endRight + endUp);
            verts.Add(endPos + endRight + endDown);
            normals.Add(endRight);
            normals.Add(endRight);

            // End side down
            verts.Add(endPos + endRight + endDown);
            verts.Add(endPos + endLeft + endDown);
            normals.Add(endDown);
            normals.Add(endDown);

            // End side left
            verts.Add(endPos + endLeft + endDown);
            verts.Add(endPos + endLeft + endUp);
            normals.Add(endLeft);
            normals.Add(endLeft);

            // Endcap
            if (ribbonSegment.hasEndCap)
            {
                verts.Add(endPos + endLeft + endUp);
                verts.Add(endPos + endRight + endUp);
                verts.Add(endPos + endRight + endDown);
                verts.Add(endPos + endLeft + endDown);
                normals.Add(ribbonSegment.endFacing);
                normals.Add(ribbonSegment.endFacing);
                normals.Add(ribbonSegment.endFacing);
                normals.Add(ribbonSegment.endFacing);
            }
        }

        private void AddRibbonSegmentIndices(RibbonSegment ribbonSegment, int firstSegmentVertIdx, List<int> indices)
        {
            int v = firstSegmentVertIdx;

            // Startcap
            if (ribbonSegment.hasStartCap)
            {
                indices.Add(v + 0);
                indices.Add(v + 1);
                indices.Add(v + 2);

                indices.Add(v + 0);
                indices.Add(v + 2);
                indices.Add(v + 3);

                v += 4; // having a startcap offsets the expected vertex index for this segment

                // Side up
                indices.Add(v + 0);
                indices.Add(v + 1);
                indices.Add(v + 9);

                indices.Add(v + 0);
                indices.Add(v + 9);
                indices.Add(v + 8);

                // Side right
                indices.Add(v + 2);
                indices.Add(v + 3);
                indices.Add(v + 11);

                indices.Add(v + 2);
                indices.Add(v + 11);
                indices.Add(v + 10);

                // Side down
                indices.Add(v + 4);
                indices.Add(v + 5);
                indices.Add(v + 13);

                indices.Add(v + 4);
                indices.Add(v + 13);
                indices.Add(v + 12);

                // Side left
                indices.Add(v + 6);
                indices.Add(v + 7);
                indices.Add(v + 15);

                indices.Add(v + 6);
                indices.Add(v + 15);
                indices.Add(v + 14);
            }
            else
            {
                // With no startcap, side tris connect to previous segment's verts.

                // Side up
                indices.Add(v + 0);
                indices.Add(v - 8);
                indices.Add(v + 1);

                indices.Add(v + 1);
                indices.Add(v - 8);
                indices.Add(v - 7);

                // Side right
                indices.Add(v + 2);
                indices.Add(v - 6);
                indices.Add(v + 3);

                indices.Add(v + 3);
                indices.Add(v - 6);
                indices.Add(v - 5);

                // Side down
                indices.Add(v + 4);
                indices.Add(v - 4);
                indices.Add(v + 5);

                indices.Add(v + 5);
                indices.Add(v - 4);
                indices.Add(v - 3);

                // Side left
                indices.Add(v + 6);
                indices.Add(v - 2);
                indices.Add(v + 7);

                indices.Add(v + 7);
                indices.Add(v - 2);
                indices.Add(v - 1);
            }

            // Endcap
            if (ribbonSegment.hasEndCap)
            {
                if (ribbonSegment.hasStartCap)
                {
                    v += 16;
                }
                else
                {
                    v += 8;
                }

                indices.Add(v + 0);
                indices.Add(v + 1);
                indices.Add(v + 2);

                indices.Add(v + 0);
                indices.Add(v + 2);
                indices.Add(v + 3);
            }

        }

        /// <summary>
        /// Finalize the renderer.
        /// Creates a gameobject for the finished ribbon and places it under the ribbonParent in the scene
        /// </summary>
        public void FinalizeRenderer()
        {
            GameObject meshObj = new GameObject();
            meshObj.transform.parent = _finalizedRibbonParent.transform;
            MeshRenderer renderer = meshObj.AddComponent<MeshRenderer>();
            renderer.material = _meshRenderer.material;
            MeshFilter filter = meshObj.AddComponent<MeshFilter>();
            filter.mesh = _mesh;


            _cachedStrokeRenderered = new List<StrokePoint>();

            _meshFilter.mesh = _mesh = null;
        }

    }
    /// <summary>
    /// Used by the ThickRibbonRenderer to create the ribbon mesh
    /// </summary>
    public struct RibbonSegment
    {
        public Vector3 startPos;
        public Vector3 startFacing;
        public Vector3 startNormal;
        public float startThickness;

        public Vector3 endPos;
        public Vector3 endFacing;
        public Vector3 endNormal;
        public float endThickness;

        public bool hasStartCap;
        public bool hasEndCap;

        public RibbonSegment(
          Vector3 startPos,
          Vector3 startFacing,
          Vector3 startNormal,
          float startThickness,
          Vector3 endPos,
          Vector3 endFacing,
          Vector3 endNormal,
          float endThickness,
          bool hasStartCap,
          bool hasEndCap
          )
        {
            this.startPos = startPos;
            this.startFacing = startFacing;
            this.startNormal = startNormal;
            this.startThickness = startThickness;
            this.endPos = endPos;
            this.endFacing = endFacing;
            this.endNormal = endNormal;
            this.endThickness = endThickness;
            this.hasStartCap = hasStartCap;
            this.hasEndCap = hasEndCap;
        }

        public int NumVerts()
        {
            return 8 + (hasStartCap ? 4 + 8 : 0) + (hasEndCap ? 4 : 0);
        }

        public int NumIndices()
        {
            return 24 + (hasStartCap ? 6 : 0) + (hasEndCap ? 6 : 0);
        }

    }


}