using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  /// <summary>
  /// Implements IIndexable over GameObjects by indexing a simple property list
  /// configured in the inspector.
  /// </summary>
  public class IndexableGameObjects : MonoBehaviour,
                                      IIndexable<GameObject> {

    [SerializeField]
    private List<GameObject> _gameObjects;
    public List<GameObject> gameObjects {
      get {
        if (_gameObjects == null) _gameObjects = new List<GameObject>();
        return _gameObjects;
      }
    }

    public GameObject this[int idx] {
      get {
        return gameObjects[idx];
      }
    }

    public int Count {
      get {
        return gameObjects.Count;
      }
    }

  }

}
