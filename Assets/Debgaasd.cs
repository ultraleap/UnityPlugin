using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Leap.Unity;
using Leap.Unity.Query;
using Leap.Unity.Query;


public class Debgaasd : MonoBehaviour {

  private List<int> _data;

  public List<int> result;

  private void Start() {
    _data = new List<int>();
    for (int i = 0; i < 20; i++) {
      _data.Add(i);
    }
  }

  private void Update() {
    using (new ProfilerSample("Query")) {
      result.Clear();
      foreach (var item in _data.SelectMany(i => Enumerable.Repeat(i, i))) {
        result.Add(item);
      }
    }

    //using (new ProfilerSample("Query")) {
    //  result.Clear();
    //  foreach (var item in _data.Query().Select(i => i * 2).Where(i => i % 5 == 0).Select(i => i * 4).Where(i => i % 7 == 0)) {
    //    result.Add(item);
    //  }
    //}
  }


}
