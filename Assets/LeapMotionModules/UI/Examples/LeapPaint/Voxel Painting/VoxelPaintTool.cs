using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Examples.LeapPaint {

  public class VoxelPaintTool : MonoBehaviour {

    public Voxel voxelPrefab;

    private const int MAX_VOXELS = 4096;
    private const float VOXEL_WIDTH = 0.01F;

    private Voxel[] _voxels = new Voxel[MAX_VOXELS];
    private Dictionary<VoxelPos, Voxel> _voxelMap = new Dictionary<VoxelPos, Voxel>();
    private int _usedVoxelCount = 0;
    private GameObject _usedVoxelParent;
    private GameObject _storedVoxelParent;

    void Start() {
      for (int i = 0; i < MAX_VOXELS; i++) {
        _voxels[i] = PrepareNewVoxel();
      }
    }

    public void AddSingleVoxel(Vector3 worldPos) {
      VoxelPos newVoxelPos = VoxelPosFromWorldPos(worldPos);
      Voxel origVoxel;
      if (_voxelMap.TryGetValue(newVoxelPos, out origVoxel)) {
        // copy desired settings? for now just ignore
      }
      else {
        Voxel voxel = _voxels[_usedVoxelCount++];
        voxel.transform.position = WorldPosFromVoxelPos(newVoxelPos);
        voxel.transform.parent = _usedVoxelParent.transform;
        voxel.gameObject.SetActive(true);
        _voxelMap[newVoxelPos] = voxel;
      }
    }

    private static VoxelPos VoxelPosFromWorldPos(Vector3 worldPos) {
      return new VoxelPos {
        x = (int)(worldPos.x / VOXEL_WIDTH),
        y = (int)(worldPos.y / VOXEL_WIDTH),
        z = (int)(worldPos.z / VOXEL_WIDTH)
      };
    }

    private static Vector3 WorldPosFromVoxelPos(VoxelPos voxelPos) {
      return new Vector3 {
        x = voxelPos.x * VOXEL_WIDTH,
        y = voxelPos.y * VOXEL_WIDTH,
        z = voxelPos.z * VOXEL_WIDTH
      };
    }

    private struct VoxelPos {
      public int x, y, z;
    }

    private Voxel PrepareNewVoxel() {
      if (_usedVoxelParent == null) {
        _usedVoxelParent = new GameObject("Used Voxels");
        _usedVoxelParent.transform.position = Vector3.zero;
      }
      if (_storedVoxelParent == null) {
        _storedVoxelParent = new GameObject("Stored Voxels");
        _storedVoxelParent.transform.position = new Vector3(10000F, -10000F, -10000F);
      }

      Voxel voxel = Instantiate(voxelPrefab);
      voxel.transform.parent = _storedVoxelParent.transform;
      voxel.transform.localPosition = Vector3.zero;
      voxel.transform.localScale = Vector3.one * VOXEL_WIDTH;
      Renderer voxelRenderer = voxel.GetComponent<Renderer>();
      voxelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
      voxel.gameObject.SetActive(false);

      return voxel;
    }

  }

}