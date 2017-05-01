using System.Collections.Generic;

namespace Leap.Unity.GraphicalRenderer {

  public interface ISupportsAddRemove {
    void OnAddRemoveGraphics(List<int> dirtyIndexes);
  }
}
