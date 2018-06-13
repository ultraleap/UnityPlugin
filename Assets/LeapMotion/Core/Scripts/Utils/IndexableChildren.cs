using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  /// <summary>
  /// Implements IIndexable over GameObjects by indexing this transform's
  /// children. 
  /// </summary>
  public class IndexableChildren : MonoBehaviour, IIndexable<GameObject> {

    public GameObject this[int idx] {
      get { return this.transform.GetChild(idx).gameObject; }
    }

    public int Count {
      get {
        return this.transform.childCount;
      }
    }

  }

}
