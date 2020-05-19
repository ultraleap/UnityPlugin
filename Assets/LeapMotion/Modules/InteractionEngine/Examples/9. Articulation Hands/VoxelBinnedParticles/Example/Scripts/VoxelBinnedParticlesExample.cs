using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.RuntimeGizmos;

namespace Leap.Unity.Particles {
  
  public class VoxelBinnedParticlesExample : MonoBehaviour, IRuntimeGizmoComponent {
    public const int MAX_PARTICLES = 1024 * 64;
    public const int MAX_CAPSULES = 1024;

    public const int BOX_SIDE = 64;
    public const int BOX_COUNT = BOX_SIDE * BOX_SIDE * BOX_SIDE;

    [Header("Hands")]
    public LeapProvider _provider;

    [SerializeField]
    public float _handCapsuleRadius = 0.025f;

    [Header("Custom Capsule")]
    public Transform _capsuleA;

    public Transform _capsuleB;

    public float _capsuleRadius;

    [Header("Custom Plane")]
    public Transform _plane;

    [Header("Settings")]
    public Mesh _mesh;

    public ComputeShader _shader;

    public Shader _display;

    //[SerializeField]
    //public Material _displayMat;

    [StructLayout(LayoutKind.Sequential)]
    private struct Particle {
      public Vector3 position;
      public Vector3 prevPosition;
      public Vector3 color;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Capsule {
      public Vector3 pointA;
      public Vector3 pointB;
      public float radius;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DebugData {
      public uint tests;
    }

    private int _integrate;
    private int _resolveCollisions;
    private int _accumulate_x;
    private int _accumulate_y;
    private int _accumulate_z;
    private int _copy;
    private int _sort;

    private ComputeBuffer _capsules;
    private Capsule[] _capsuleArray = new Capsule[MAX_CAPSULES];

    private ComputeBuffer _particleFront;
    private ComputeBuffer _particleBack;

    private ComputeBuffer _count;
    private ComputeBuffer _boxStart;
    private ComputeBuffer _boxEnd;

    private ComputeBuffer _debugData;

    private ComputeBuffer _argBuffer;

    private Material _displayMat;

    void OnEnable() {
      _capsules = new ComputeBuffer(MAX_CAPSULES, Marshal.SizeOf(typeof(Capsule)));

      _particleFront = new ComputeBuffer(MAX_PARTICLES, Marshal.SizeOf(typeof(Particle)));
      _particleBack = new ComputeBuffer(MAX_PARTICLES, Marshal.SizeOf(typeof(Particle)));

      _count = new ComputeBuffer(BOX_COUNT, sizeof(uint));
      _boxStart = new ComputeBuffer(BOX_COUNT, sizeof(uint));
      _boxEnd = new ComputeBuffer(BOX_COUNT, sizeof(uint));

      _debugData = new ComputeBuffer(MAX_PARTICLES, Marshal.SizeOf(typeof(DebugData)));

      _argBuffer = new ComputeBuffer(5, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
      uint[] args = new uint[5];
      args[0] = (uint)_mesh.GetIndexCount(0);
      args[1] = MAX_PARTICLES;
      _argBuffer.SetData(args);

      uint[] counts = new uint[BOX_COUNT];
      for (int i = 0; i < BOX_COUNT; i++) {
        counts[i] = 0;
      }
      _count.SetData(counts);

      _integrate = _shader.FindKernel("Integrate");
      _resolveCollisions = _shader.FindKernel("ResolveCollisions");
      _accumulate_x = _shader.FindKernel("Accumulate_X");
      _accumulate_y = _shader.FindKernel("Accumulate_Y");
      _accumulate_z = _shader.FindKernel("Accumulate_Z");
      _copy = _shader.FindKernel("Copy");
      _sort = _shader.FindKernel("Sort");

      Particle[] particles = new Particle[MAX_PARTICLES];
      for (int i = 0; i < MAX_PARTICLES; i++) {
        Vector3 pos = transform.TransformPoint(new Vector3(Random.Range(-0.2f, 0.2f),
                                                           Random.Range(-0.9f, 0f),
                                                           Random.Range(-0.2f, 0.2f)));
        particles[i] = new Particle() {
          position = pos,
          prevPosition = pos,
          color = new Vector3(Random.value, Random.value, Random.value)
        };
      }
      _particleFront.SetData(particles);

      foreach (var index in new int[] { _integrate,
                                      _resolveCollisions,
                                      _accumulate_x, _accumulate_y, _accumulate_z,
                                      _copy, _sort }
      ) {
        _shader.SetBuffer(index, "_Capsules", _capsules);
        _shader.SetBuffer(index, "_ParticleFront", _particleFront);
        _shader.SetBuffer(index, "_ParticleBack", _particleBack);
        _shader.SetBuffer(index, "_BinParticleCount", _count);
        _shader.SetBuffer(index, "_BinStart", _boxStart);
        _shader.SetBuffer(index, "_BinEnd", _boxEnd);
        _shader.SetBuffer(index, "_DebugData", _debugData);
      }

      _displayMat = new Material(_display);
      _displayMat.SetBuffer("_Particles", _particleFront);
    }

    void OnDisable() {
      if (_particleFront != null) _particleFront.Release();
      if (_particleBack != null) _particleBack.Release();

      if (_count != null) _count.Release();
      if (_boxStart != null) _boxStart.Release();
      if (_boxEnd != null) _boxEnd.Release();

      if (_argBuffer != null) _argBuffer.Release();
      if (_capsules != null) _capsules.Release();

      if (_debugData != null) _debugData.Release();
    }

    void Update() {
      int index = 0;
      if (_provider != null) {
        Frame frame = _provider.CurrentFrame;
        foreach (var hand in frame.Hands) {
          foreach (var finger in hand.Fingers) {
            foreach (var bone in finger.bones) {
              _capsuleArray[index++] = new Capsule() {
                pointA = bone.PrevJoint.ToVector3(),
                pointB = bone.NextJoint.ToVector3(),
                radius = _handCapsuleRadius
              };
            }
          }
        }
      }

      _capsules.SetData(_capsuleArray);
      _shader.SetInt("_CapsuleCount", index);

      _shader.SetVector("_PlanePosition", _plane.position);
      _shader.SetVector("_PlaneNormal", _plane.up);

      for (int i = 0; i < 2; i++) {
        _shader.SetVector("_Center", transform.position);

        using (new ProfilerSample("Integrate")) {
          _shader.Dispatch(_integrate, MAX_PARTICLES / 64, 1, 1);
        }

        using (new ProfilerSample("Accumulate")) {
          _shader.Dispatch(_accumulate_x, BOX_SIDE / 4, BOX_SIDE / 4, BOX_SIDE / 4);
          _shader.Dispatch(_accumulate_y, BOX_SIDE / 4, BOX_SIDE / 4, BOX_SIDE / 4);
          _shader.Dispatch(_accumulate_z, BOX_SIDE / 4, BOX_SIDE / 4, BOX_SIDE / 4);
        }

        using (new ProfilerSample("Copy")) {
          _shader.Dispatch(_copy, BOX_COUNT / 64, 1, 1);
        }
        using (new ProfilerSample("Sort")) {
          _shader.Dispatch(_sort, MAX_PARTICLES / 64, 1, 1);
        }

        using (new ProfilerSample("Resolve Collisions")) {
          _shader.Dispatch(_resolveCollisions, MAX_PARTICLES / 64, 1, 1);
        }
      }

      /*
      DebugData[] data = new DebugData[MAX_PARTICLES];
      _debugData.GetData(data);
      Debug.Log("##########");
      Debug.Log(data[1000].tests);
      */
    }

    void LateUpdate() {
      Graphics.DrawMeshInstancedIndirect(_mesh,
                                          0,
                                          _displayMat,
                                          new Bounds(Vector3.zero, Vector3.one * 10000),
                                          _argBuffer);//, layer: 1, castShadows: UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows: false);
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
    //  drawer.DrawWireCapsule(_capsuleA.position, _capsuleB.position, _capsuleRadius);
    }
  }
  
}
