namespace Leap.Unity.Halfedge {

  public interface IMeshChangeSubscriber {

    void OnHalfedgeStructureAdded(Halfedge fullyConnectedHalfedgeStructure);

  }

}