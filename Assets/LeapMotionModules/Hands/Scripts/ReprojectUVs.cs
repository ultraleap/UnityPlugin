using UnityEngine;
using System.Collections;

public class ReprojectUVs : MonoBehaviour
{
    public Camera ProjectionPerspective;
    public float left = -0.2F;
    public float right = 0.2F;
    public float top = 0.2F;
    public float bottom = -0.2F;
    Mesh mesh;
    Vector3[] vertices;
    Vector3[] normals;
    Vector2[] uvs;
    Vector3 ScreenPosition;
    SkinnedMeshRenderer skin;
    Vector2 ScreenDimensions;
    public Transform root;

    public bool bake = true;
    public bool apply = true;
    void Start()
    {
        //ProjectionPerspective.enabled = false;
        mesh = new Mesh();
        mesh.MarkDynamic();
        //Matrix4x4 p = PerspectiveOffCenter(left, right, bottom, top, ProjectionPerspective.nearClipPlane, ProjectionPerspective.farClipPlane);
        //ProjectionPerspective.projectionMatrix = p;
        skin = GetComponent<SkinnedMeshRenderer>();
        skin.BakeMesh(mesh);
        vertices = mesh.vertices;
        uvs = new Vector2[vertices.Length];
        ScreenDimensions = new Vector2(ProjectionPerspective.pixelWidth, ProjectionPerspective.pixelHeight);
    }

    void Update()
    {
        skin.BakeMesh(mesh);
        vertices = mesh.vertices;
        for (int i = 0; i < uvs.Length; i++) {
            ScreenPosition = ProjectionPerspective.WorldToScreenPoint(root.TransformPoint(vertices[i]));
            uvs[i].Set(ScreenPosition.x / ScreenDimensions.x, ScreenPosition.y / ScreenDimensions.y);
        }
        skin.sharedMesh.uv = uvs;
    }
    /*
    void OnDrawGizmos(){
        for (int i = 0; i < uvs.Length; i+=10) {
            Gizmos.DrawSphere(root.TransformPoint(vertices[i]), 0.01f);
        }
    }
    */
    static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        float x = 2.0F * near / (right - left);
        float y = 2.0F * near / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0F * far * near) / (far - near);
        float e = -1.0F;
        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x;
        m[0, 1] = 0;
        m[0, 2] = a;
        m[0, 3] = 0;
        m[1, 0] = 0;
        m[1, 1] = y;
        m[1, 2] = b;
        m[1, 3] = 0;
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = c;
        m[2, 3] = d;
        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = e;
        m[3, 3] = 0;
        return m;
    }

}