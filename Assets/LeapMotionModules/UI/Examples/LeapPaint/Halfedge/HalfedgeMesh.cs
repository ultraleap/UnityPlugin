using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Halfedge {

  public enum PrimitiveType {
    Tetrahedron
  }

  public class HalfedgeMesh {

    private List<Halfedge> _independentHalfedges;
    private List<IMeshChangeSubscriber> _subscribers;

    /// <summary> Creates a new empty HalfedgeMesh object. </summary>
    public HalfedgeMesh() {
      _independentHalfedges = new List<Halfedge>();
      _subscribers = new List<IMeshChangeSubscriber>();
    }

    public void Subscribe(IMeshChangeSubscriber subscriber) {
      _subscribers.Add(subscriber);
    }

    public static void AddPrimitive(HalfedgeMesh mesh, PrimitiveType primitiveType) {
      switch (primitiveType) {
        case PrimitiveType.Tetrahedron:
          Primitives.AddTetrahedron(mesh);
          break;
      }
    }

    public void AddHalfedgeStructure(Halfedge fullyConnectedHalfedgeStructure) {
      _independentHalfedges.Add(fullyConnectedHalfedgeStructure);

      foreach (var subscriber in _subscribers) {
        subscriber.OnHalfedgeStructureAdded(fullyConnectedHalfedgeStructure);
      }
    }

  }

}