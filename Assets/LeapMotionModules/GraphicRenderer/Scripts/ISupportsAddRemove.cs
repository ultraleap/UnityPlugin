
namespace Leap.Unity.GraphicalRenderer {

  public interface ISupportsAddRemove {
    /// <summary>
    /// The graphic to add.  It is always added to the end of
    /// a list.
    /// </summary>
    void OnAddGraphic(LeapGraphic graphic);

    /// <summary>
    /// The graphic to remove, in addition to it's previous index.
    /// </summary>
    void OnRemoveGraphic(LeapGraphic graphic, int index);
  }
}
