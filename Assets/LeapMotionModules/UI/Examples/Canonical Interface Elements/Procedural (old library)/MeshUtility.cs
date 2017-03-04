using System.Collections.Generic;

public static class MeshUtility {

  public static void FlipTris(List<int> tris) {
    for (int i = 0; i < tris.Count; i += 3) {
      int temp = tris[i];
      tris[i] = tris[i + 1];
      tris[i + 1] = temp;
    }
  }
}
