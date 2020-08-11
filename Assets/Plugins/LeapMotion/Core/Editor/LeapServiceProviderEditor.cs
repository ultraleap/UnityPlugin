/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using LeapInternal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Leap.Unity
{

  [CustomEditor(typeof(LeapServiceProvider))]
  public class LeapServiceProviderEditor : CustomEditorBase<LeapServiceProvider> {

    internal const float INTERACTION_VOLUME_MODEL_IMPORT_SCALE_FACTOR = 0.001f;

    protected Quaternion deviceRotation = Quaternion.identity;
    protected bool isVRProvider = false;

    protected Vector3 controllerOffset = Vector3.zero;

    private const float LMC_BOX_RADIUS = 0.45f;
    private const float LMC_BOX_WIDTH = 0.965f;
    private const float LMC_BOX_DEPTH = 0.6671f;

    private const float PEAK_INTERACTION_VOLUME_OPACITY = 0.4f;
    private GenericMesh rigelInteractionZoneMesh;
    private readonly Color BackfaceEdgeColour = new Color(1, 1, 1, 0.02f);

    protected override void OnEnable() {
      ParseRigelInteractionMeshData();

      base.OnEnable();

      specifyCustomDecorator("_frameOptimization", frameOptimizationWarning);

      specifyConditionalDrawing("_frameOptimization",
                                (int)LeapServiceProvider.FrameOptimizationMode.None,
                                "_physicsExtrapolation",
                                "_physicsExtrapolationTime");

      specifyConditionalDrawing("_physicsExtrapolation",
                                (int)LeapServiceProvider.PhysicsExtrapolationMode.Manual,
                                "_physicsExtrapolationTime");

      deferProperty("_workerThreadProfiling");
    }

    private void frameOptimizationWarning(SerializedProperty property) {
      LeapServiceProvider.FrameOptimizationMode mode = (LeapServiceProvider.FrameOptimizationMode)property.intValue;
      string warningText;

      switch (mode) {

        case LeapServiceProvider.FrameOptimizationMode.ReuseUpdateForPhysics:
          warningText = "Reusing update frames for physics introduces a frame of latency "
                      + "for physics interactions.";
          break;
        case LeapServiceProvider.FrameOptimizationMode.ReusePhysicsForUpdate:
          warningText = "This optimization REQUIRES physics framerate to match your "
                      + "target framerate EXACTLY.";
          break;
        default:
          return;
      }

      EditorGUILayout.HelpBox(warningText, MessageType.Warning);
    }

    public override void OnInspectorGUI() {

#if UNITY_2019_3_OR_NEWER
      // Easily tracking VR-enabled-or-not requires an XR package installed, so remove this warning for now.
#else
      if (UnityEditor.PlayerSettings.virtualRealitySupported && !isVRProvider) {
        EditorGUILayout.HelpBox(
          "VR support is enabled. If your Leap is mounted to your headset, you should be "
          + "using LeapXRServiceProvider instead of LeapServiceProvider. (If your Leap "
          + "is not mounted to your headset, you can safely ignore this warning.)",
          MessageType.Warning);
      }
#endif

      base.OnInspectorGUI();
    }

    public virtual void OnSceneGUI() {

      switch (GetSelectedInteractionVolume()) {
        case LeapServiceProvider.InteractionVolumeVisualization.None:
          break;
        case LeapServiceProvider.InteractionVolumeVisualization.Peripheral:
          DrawPeripheralInteractionZone(LMC_BOX_WIDTH, LMC_BOX_DEPTH, LMC_BOX_RADIUS, Color.white);
          break;
        case LeapServiceProvider.InteractionVolumeVisualization.Rigel:
          DrawRigelInteractionZoneMesh();
          break;
        case LeapServiceProvider.InteractionVolumeVisualization.Automatic:
          DetectConnectedDevice();
          break;
        default:
          break;
      }

    }

    private void ParseRigelInteractionMeshData() {

      ObjFileParser rigelMeshDataParser = new ObjFileParser();

      if (rigelInteractionZoneMesh == null)
      {
        try
        {
          rigelInteractionZoneMesh = rigelMeshDataParser.FromObj(Path.Combine(Application.dataPath, "UnityModules", "Assets", "Plugins", "LeapMotion", "Core", "Models", "Rigel-interaction-cone.obj"),
              ObjFileParser.SwapYZ,
              INTERACTION_VOLUME_MODEL_IMPORT_SCALE_FACTOR);

          int edgeCount = rigelInteractionZoneMesh.Edges().Count();
        }
        catch (Exception e)
        {
          Debug.LogException(e);
        }
      }
    }


    private void DetectConnectedDevice() {

      LeapServiceProvider lsp = this.target.GetComponent<LeapServiceProvider>();
      Connection c = Connection.GetConnection(0);
      if (c.Devices.Count == 1)
      {
        if (c.Devices.First().Type == Device.DeviceType.TYPE_RIGEL)
        {
          DrawRigelInteractionZoneMesh();
        }
        else if (c.Devices.First().Type == Device.DeviceType.TYPE_PERIPHERAL)
        {
          DrawPeripheralInteractionZone(LMC_BOX_WIDTH, LMC_BOX_DEPTH, LMC_BOX_RADIUS, Color.white);
        }
      }
    }

    private LeapServiceProvider.InteractionVolumeVisualization GetSelectedInteractionVolume() {
      LeapServiceProvider lsp = this.target.GetComponent<LeapServiceProvider>();

      return lsp.SelectedInteractionVolumeVisualization;
    }


    private void DrawRigelInteractionZoneMesh() {

      if (this.rigelInteractionZoneMesh != null && this.rigelInteractionZoneMesh.Edges() != null)
      {
        foreach (Edge edge in this.rigelInteractionZoneMesh.Edges())
        {
          // Draw edges
          if (edge.CommonNormal != null)
          {
            Vector3 edgeVertex0 = target.transform.TransformPoint(edge.VertexLocations[0].Item2);
            Vector3 edgeVertex1 = target.transform.TransformPoint(edge.VertexLocations[1].Item2);
            Vector3 edgeCommonNormal = target.transform.TransformPoint(edge.CommonNormal);

            float angle;
            if (SceneView.currentDrawingSceneView.camera.orthographic)
            {
              // Iso Camera
              angle = Vector3.Angle(SceneView.currentDrawingSceneView.camera.transform.forward, edgeCommonNormal);
            }
            else
            {
              // Perspective Camera
              angle = Vector3.Angle(edgeVertex0 - SceneView.currentDrawingSceneView.camera.transform.position, edgeCommonNormal);
            }

            // Shade the edge line based on it's angle to the camera, making it more intense as it approaches 90 degrees
            // This gives the interaction volume a cell style appearance
            float shadingFactor = Math.Abs(angle / 90.0f);
            if (shadingFactor > 1)
            {
              shadingFactor = 2 - shadingFactor;
            }

            if (angle >= Math.Abs(90)) // Was <= if YZ flipping is not on 
            {
              Handles.color = new Color(shadingFactor, shadingFactor, shadingFactor, shadingFactor * PEAK_INTERACTION_VOLUME_OPACITY);
              Handles.DrawAAPolyLine(edgeVertex0, edgeVertex1);
            }
            else // Edge is effectively a backface edge
            {
              Handles.color = BackfaceEdgeColour;
              Handles.DrawAAPolyLine(edgeVertex0, edgeVertex1);
            }
          }
        }
      }
    }

    private void DrawPeripheralInteractionZone(float box_width, float box_depth, float box_radius, Color interactionZoneColor) {

      Color previousColor = Handles.color;
      Handles.color = interactionZoneColor;

      Vector3 origin = target.transform.TransformPoint(controllerOffset);
      getLocalGlobalPoint(-1, 1, 1, box_width, box_depth, box_radius, out Vector3 local_top_left, out Vector3 top_left);
      getLocalGlobalPoint(1, 1, 1, box_width, box_depth, box_radius, out Vector3 local_top_right, out Vector3 top_right);
      getLocalGlobalPoint(-1, 1, -1, box_width, box_depth, box_radius, out Vector3 local_bottom_left, out Vector3 bottom_left);
      getLocalGlobalPoint(1, 1, -1, box_width, box_depth, box_radius, out Vector3 local_bottom_right, out Vector3 bottom_right);

      Handles.DrawAAPolyLine(origin, top_left);
      Handles.DrawAAPolyLine(origin, top_right);
      Handles.DrawAAPolyLine(origin, bottom_left);
      Handles.DrawAAPolyLine(origin, bottom_right);

      drawControllerEdge(origin, local_top_left, local_top_right, box_radius);
      drawControllerEdge(origin, local_bottom_left, local_top_left, box_radius);
      drawControllerEdge(origin, local_bottom_left, local_bottom_right, box_radius);
      drawControllerEdge(origin, local_bottom_right, local_top_right, box_radius);

      drawControllerArc(origin, local_top_left, local_bottom_left, local_top_right,
                          local_bottom_right, box_radius);
      drawControllerArc(origin, local_top_left, local_top_right, local_bottom_left,
                          local_bottom_right, box_radius);

      Handles.color = previousColor;
    }

    private void getLocalGlobalPoint(int x, int y, int z, float box_width, float box_depth, float box_radius, out Vector3 local, out Vector3 global) {

      local = deviceRotation * new Vector3(x * box_width, y * box_radius, z * box_depth);
      global = target.transform.TransformPoint(controllerOffset
                                               + box_radius * local.normalized);
    }

    private void drawControllerEdge(Vector3 origin,
                                    Vector3 edge0, Vector3 edge1,
                                    float box_radius) { 
      Vector3 right_normal = target.transform
                                   .TransformDirection(Vector3.Cross(edge0, edge1));
      float right_angle = Vector3.Angle(edge0, edge1);

      Handles.DrawWireArc(origin, right_normal, target.transform.TransformDirection(edge0),
                          right_angle, target.transform.lossyScale.x * box_radius);
    }

    private void drawControllerArc(Vector3 origin,
                                   Vector3 edgeA0, Vector3 edgeA1,
                                   Vector3 edgeB0, Vector3 edgeB1,
                                   float box_radius) {

      Vector3 faceA = target.transform.rotation * Vector3.Lerp(edgeA0, edgeA1, 0.5f);
      Vector3 faceB = target.transform.rotation * Vector3.Lerp(edgeB0, edgeB1, 0.5f);

      float resolutionIncrement = 1f / 50f;
      for (float i = 0f; i < 1f; i += resolutionIncrement)
      {
        Vector3 begin = Vector3.Lerp(faceA, faceB, i).normalized
                        * target.transform.lossyScale.x * box_radius;
        Vector3 end = Vector3.Lerp(faceA, faceB, i + resolutionIncrement).normalized
                      * target.transform.lossyScale.x * box_radius;

        Handles.DrawAAPolyLine(origin + begin, origin + end);
      }
    }

    internal enum enumaxis
    {
      X,
      Y,
      Z
    }

    /// <summary>
    /// Parses an OBJ file into a generic mesh data structure
    /// </summary>
    internal class ObjFileParser {
      private const char delimiter = ' ';
      private GenericMesh mesh;

      private bool swapYZ;
      internal const bool SwapYZ = true;

      internal float _scaleFactor;

      public GenericMesh FromObj(string filePath, bool SwapYZ = false, float scaleFactor = 1) {

        this.swapYZ = SwapYZ;
        this._scaleFactor = scaleFactor;

        if (File.Exists(filePath) && Path.GetExtension(filePath) == ".obj")
        {
          using (StreamReader fs = File.OpenText(filePath))
          {
            mesh = new GenericMesh();

            string line;
            while (fs.EndOfStream == false)
            {
              line = fs.ReadLine();
              ParseLine(line);
            }

            mesh.AddEdges();
          }
        }

        return mesh;
      }


      private void ParseLine(string line) {

        // See https://en.wikipedia.org/wiki/Wavefront_.obj_file for Obj file format

        line = Regex.Replace(line, @"\s+", " "); // Remove unecessary duplicate whitespace

        if (line.StartsWith("vn"))
        {
          ParseVertexNormal(line);
        }
        else if (line.StartsWith("v "))
        {
          ParseVertex(line);
        }
        else if (line.StartsWith("f "))
        {
          ParsePolygonalFaceElement(line);
        }
      }

      private void ParsePolygonalFaceElement(string line) {

        // Expect three vertex elements (following the identifier), vertex elements can take 3 different forms a vertex index reference optionally
        // with a normal vertex index reference, optionally with texture coordinate index reference
        string[] elements = line.Split(delimiter);
        if (elements.Count() == 4)
        {
          try
          {
            this.mesh.AddTriangle(ParseVertexFaceElement(elements[1]), ParseVertexFaceElement(elements[2]), ParseVertexFaceElement(elements[3]));
          }
          catch (Exception e)
          {
            Debug.LogException(e);
          }
        }
      }


      private MeshVertex ParseVertexFaceElement(string vertexString) {

        string[] elements = vertexString.Split('/');

        switch (elements.Count())
        {
          // 1. Vertex indices
          // A valid vertex index matches the corresponding vertex elements of a previously defined vertex list.If an index is positive then it refers to the offset in that vertex list, starting at 1.If an index is negative then it relatively refers to the end of the vertex list, -1 referring to the last element.
          // Each face can contain three or more vertices.
          // f v1 v2 v3 ....
          case 1:
            return new MeshVertex(int.Parse(elements[0]) - 1);

          // 2. Vertex texture coordinate indices
          // Optionally, texture coordinate indices can be used to specify texture coordinates when defining a face. To add a texture coordinate index to a vertex index when defining a face, one must put a slash immediately after the vertex index and then put the texture coordinate index. No spaces are permitted before or after the slash.A valid texture coordinate index starts from 1 and matches the corresponding element in the previously defined list of texture coordinates.Each face can contain three or more elements.
          // f v1/ vt1 v2 / vt2 v3 / vt3...
          case 2:
            return new MeshVertex(int.Parse(elements[0]) - 1, int.Parse(elements[1]) - 1);

          case 3:
            // 3a. Vertex normal indices
            // Optionally, normal indices can be used to specify normal vectors for vertices when defining a face.To add a normal index to a vertex index when defining a face, one must put a second slash after the texture coordinate index and then put the normal index.A valid normal index starts from 1 and matches the corresponding element in the previously defined list of normals.Each face can contain three or more elements.
            // f v1 / vt1 / vn1 v2 / vt2 / vn2 v3 / vt3 / vn3...

            // 3b. Vertex normal indices without texture coordinate indices
            // As texture coordinates are optional, one can define geometry without them, but one must put two slashes after the vertex index before putting the normal index.
            // f v1//vn1 v2//vn2 v3//vn3 ...

            if (elements[1].Length == 0)
            {
              return new MeshVertex(int.Parse(elements[0]) - 1, null, int.Parse(elements[2]) - 1);
            }
            else
            {
              return new MeshVertex(int.Parse(elements[0]) - 1, int.Parse(elements[1]) - 1, int.Parse(elements[2]) - 1);
            }

          default:
            return null;
        }
      }

      private void ParseVertex(string line) {

        // Expect three floats after the descriptor
        string[] elements = line.Split(delimiter);
        if (elements.Count() == 4)
        {
          try
          {
            this.mesh.Vertices.Add(new Vector3() {
              x = float.Parse(elements[1]) * this._scaleFactor,
              y = swapYZ ? float.Parse(elements[3]) * this._scaleFactor : float.Parse(elements[2]) * this._scaleFactor,
              z = swapYZ ? float.Parse(elements[2]) * this._scaleFactor : float.Parse(elements[3]) * this._scaleFactor,
            });
          }
          catch (Exception e)
          {
            Debug.LogException(e);
          }
        }
      }

      private void ParseVertexNormal(string line) {

        // Expect three floats after the descriptor
        string[] elements = line.Split(delimiter);
        if (elements.Count() == 4)
        {
          try
          {
            Vector3 normal = new Vector3() {
              x = float.Parse(elements[1]),
              y = swapYZ ? float.Parse(elements[3]) : float.Parse(elements[2]),
              z = swapYZ ? float.Parse(elements[2]) : float.Parse(elements[3])
            };

            this.mesh.Normals.Add(normal);
          }
          catch (Exception e)
          {
            Debug.LogException(e);
          }
        }
      }
    }

    /// <summary>
    /// Holds information about a vertex - location, UV coordinates and normal
    /// </summary>
    public class MeshVertex {

      public readonly int VertexIndex;
      public readonly int? UVIndex;
      public readonly int? NormalIndex;

      public MeshVertex(int vertexIndex) {

        VertexIndex = vertexIndex;
      }

      public MeshVertex(int vertexIndex, int uvIndex) {

        VertexIndex = vertexIndex;
        UVIndex = uvIndex;
      }

      public MeshVertex(int vertexIndex, int? uvIndex, int? normalIndex) {

        VertexIndex = vertexIndex;
        UVIndex = uvIndex;
        NormalIndex = normalIndex;
      }
    }

    /// <summary>
    /// Generic class for holding mesh data
    /// </summary>
    internal class GenericMesh {

      private readonly string meshName;
      private readonly List<MeshVertex> meshVertices = new List<MeshVertex>();
      private readonly Dictionary<int, Edge> edges = new Dictionary<int, Edge>();

      private readonly List<Vector3> vertices = new List<Vector3>();
      private readonly List<Vector2> uv = new List<Vector2>();
      private readonly List<Vector3> normals = new List<Vector3>();

      public GenericMesh() {

      }

      /// <summary>
      /// Mesh vertices
      /// </summary>
      internal List<Vector3> Vertices => this.vertices;

      /// <summary>
      /// Mesh normals
      /// </summary>
      internal List<Vector3> Normals => this.normals;

      /// <summary>
      /// Mesh texture coordinates
      /// </summary>
      internal List<Vector2> UV => this.uv;

      /// <summary>
      /// Enumerator for triangles in the mesh
      /// </summary>
      /// <returns>The next triangle as an array of three mesh vertex values</returns>
      internal IEnumerable<MeshVertex[]> Triangles() {

        for (int index = 0; index <= this.meshVertices.Count - 3; index += 3)
        {
          yield return this.meshVertices
              .Skip(index)
              .Take(3).ToArray();
        }
      }

      /// <summary>
      /// Enumarator for the edges in the mesh
      /// </summary>
      /// <returns>The next edge</returns>
      internal IEnumerable<Edge> Edges() {

        foreach (KeyValuePair<int, Edge> edge in this.edges)
        {
          yield return edge.Value;
        }
      }

      /// <summary>
      /// Calculates the mesh bounds in a particular axus
      /// </summary>
      /// <param name="axis">Target axis</param>
      /// <param name="min">Min value in the axis</param>
      /// <param name="max">Max value in the axis</param>
      internal void Bounds(enumaxis axis, out float min, out float max) {

        min = 0.0f;
        max = 0.0f;

        if (this.vertices.Count > 0)
        {
          switch (axis)
          {
            case enumaxis.X:
              min = this.vertices.Min(v => v.x);
              max = this.vertices.Max(v => v.x);
              break;

            case enumaxis.Y:
              min = this.vertices.Min(v => v.y);
              max = this.vertices.Max(v => v.y);
              break;

            case enumaxis.Z:
              min = this.vertices.Min(v => v.z);
              max = this.vertices.Max(v => v.z);
              break;

            default:
              break;
          }
        }
      }


      /// <summary>
      /// Adds a triangle to the generic mesh
      /// </summary>
      /// <param name="meshVertex1"></param>
      /// <param name="meshVertex2"></param>
      /// <param name="meshVertex3"></param>
      internal void AddTriangle(MeshVertex meshVertex1, MeshVertex meshVertex2, MeshVertex meshVertex3) {

        this.meshVertices.Add(meshVertex1);
        this.meshVertices.Add(meshVertex2);
        this.meshVertices.Add(meshVertex3);
      }

      /// <summary>
      /// Processes the mesh data to add edges for visualisation. Will attempt to regenerate quad edges if it determines triangles form a quad
      /// </summary>
      internal void AddEdges() {

        foreach (MeshVertex[] triangleA in this.Triangles())
        {
          bool foundQuad = false;

          // Check current triangle against all other triangles to see if this triangle forms a quad with another
          foreach (MeshVertex[] triangleB in this.Triangles())
          {

            // Attempt to remove any diagonal edges of triangles that form a quad. 
            // The assumption here is that quads will be formed from adjacent triangles
            if (NormalsMatch(triangleA, triangleB) && TrianglesShareTwoPoints(triangleA, triangleB, out IEnumerable<MeshVertex> commonPoints))
            {
              // Triangles form a quad, we just need to add the boundary of the quad to the edge list
              AddEdgeIfNotCommon(triangleA[0], triangleA[1], commonPoints);
              AddEdgeIfNotCommon(triangleA[1], triangleA[2], commonPoints);
              AddEdgeIfNotCommon(triangleA[2], triangleA[0], commonPoints);

              AddEdgeIfNotCommon(triangleB[0], triangleB[1], commonPoints);
              AddEdgeIfNotCommon(triangleB[1], triangleB[2], commonPoints);
              AddEdgeIfNotCommon(triangleB[2], triangleB[0], commonPoints);

              foundQuad = true;
              break;
            }
          }

          if (!foundQuad)
          {
            // Add the triangle's edges 
            AddEdge(triangleA[0], triangleA[1]);
            AddEdge(triangleA[1], triangleA[2]);
            AddEdge(triangleA[2], triangleA[0]);
          }
        }

        void AddEdgeIfNotCommon(MeshVertex edgeVertexA, MeshVertex edgeVertexB, IEnumerable<MeshVertex> commonVertices)
        {
          if ((commonVertices.First().VertexIndex == edgeVertexA.VertexIndex && commonVertices.Last().VertexIndex == edgeVertexB.VertexIndex) ||
              (commonVertices.First().VertexIndex == edgeVertexB.VertexIndex && commonVertices.Last().VertexIndex == edgeVertexA.VertexIndex))
          {
            // Skip
          }
          else
          {
            AddEdge(edgeVertexA, edgeVertexB);
          }
        }
      }

      /// <summary>
      /// Adds an edge to the edge array
      /// </summary>
      /// <param name="meshVertex1">First edge vertex</param>
      /// <param name="meshVertex2">Seconh edge vertex</param>
      private void AddEdge(MeshVertex meshVertex1, MeshVertex meshVertex2) {

        int edgeKey = Edge.SzudzikID(meshVertex1.VertexIndex, meshVertex2.VertexIndex);

        if (!this.edges.ContainsKey(edgeKey))
        {
          this.edges.Add(edgeKey, new Edge(
              new Tuple<int, Vector3>[] { new Tuple<int, Vector3>(meshVertex1.VertexIndex, this.Vertices[meshVertex1.VertexIndex]),
                                                 new Tuple<int, Vector3>(meshVertex2.VertexIndex, this.Vertices[meshVertex2.VertexIndex])},
              new Vector3[] { this.Normals[meshVertex1.NormalIndex.Value], this.Normals[meshVertex2.NormalIndex.Value] }
          ));
        }
      }

      private bool NormalsMatch(MeshVertex[] triangleA, MeshVertex[] triangleB) {

        Vector3 nA = Normal(triangleA);
        Vector3 nB = Normal(triangleB);
        nA.Normalize();
        nB.Normalize();

        return Vector3.SqrMagnitude(nA - nB) < 0.00001f;

        Vector3 Normal(MeshVertex[] triangle)
        {
          return Vector3.Cross(this.vertices[triangle[1].VertexIndex] - this.vertices[triangle[0].VertexIndex],
                               this.vertices[triangle[2].VertexIndex] - this.vertices[triangle[1].VertexIndex]);
        }
      }

      private bool TrianglesShareTwoPoints(IEnumerable<MeshVertex> previousTriangle, 
                                           IEnumerable<MeshVertex> triangle, 
                                           out IEnumerable<MeshVertex> commonPoints) {

        commonPoints = previousTriangle.Where(vtx => VertexIsInTriangle(vtx.VertexIndex, previousTriangle));

        return commonPoints.Count() == 2;

        bool VertexIsInTriangle(int vertexID, IEnumerable<MeshVertex> triangleToCheck)
        {
          return triangle.Any(v => v.VertexIndex == vertexID);
        }
      }
    }

    /// <summary>
    /// Holds information about an edge in a mesh. Derived from the edge of the triangles that make up the mesh
    /// </summary>
    internal class Edge {

      public readonly Tuple<int, Vector3>[] VertexLocations;
      public readonly Vector2[] UVCoords;
      public readonly Vector3[] VertexNormals;

      /// <summary>
      /// The normal of the edge, based on the combination of the edge vertex normals
      /// </summary>
      public readonly Vector3 CommonNormal;

      public Edge(Tuple<int, Vector3>[] vertexLocations, Vector2[] uvCoords, Vector3[] vertexNormals) {

        if (vertexLocations.Count() != 2 ||
            (uvCoords.Count() != 2 || uvCoords.Count() != 0) ||
            (vertexNormals.Count() != 2 || vertexNormals.Count() != 0))
        {
          throw (new ArgumentException("Array size is incorrect for edge data"));
        }

        this.VertexLocations = vertexLocations;
        this.UVCoords = uvCoords;
        this.VertexNormals = vertexNormals;
        this.CommonNormal = this.VertexNormals[0] + this.VertexNormals[1];
      }

      /// <summary>
      /// Creates an Edge based on two vertex locations and the respective normals
      /// </summary>
      /// <param name="vertexLocations">Location of the edge start and end point, with index information</param>
      /// <param name="vertexNormals">Normal associated with the edge start and end point</param>
      public Edge(Tuple<int, Vector3>[] vertexLocations, Vector3[] vertexNormals) {

        if (vertexLocations.Count() == 2 ||
            (vertexNormals.Count() == 2 || vertexNormals.Count() == 0))
        {
          this.VertexLocations = vertexLocations;
          this.VertexNormals = vertexNormals;

          this.CommonNormal = this.VertexNormals[0] + this.VertexNormals[1];
          this.CommonNormal.Normalize();
          //this.CommonNormal.Scale(new Vector3(1000f, 1000f, 1000f));
        }
        else
        {
          throw (new ArgumentException("Array size is incorrect for edge data"));
        }
      }

      public override string ToString() {

        return $"({VertexLocations[0].Item2.x},{VertexLocations[0].Item2.y},{VertexLocations[0].Item2.z}) -> " +
               $"({VertexLocations[1].Item2.x},{VertexLocations[1].Item2.y},{VertexLocations[1].Item2.z})";
      }

      /// <summary>
      /// Given a pair of integers, that are the vertex IDs of the edge, compute a compact ID for this pair of values
      /// </summary>
      /// <param name="a">Vertex Index #1</param>
      /// <param name="b">Vertex Index #2</param>
      /// <returns>Unique ID for this vertex index pair</returns>
      internal static int SzudzikID(int a, int b) {

        // From https://stackoverflow.com/questions/919612/mapping-two-integers-to-one-in-a-unique-and-deterministic-way
        return a >= b ? a * a + a + b : a + b * b;
      }
    }
  }
}


