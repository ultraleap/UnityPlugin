using System.Collections.Generic;
using Leap.Unity;

public interface ISupportsAddRemove {
  void OnAddElements(ReadonlyList<ElementIndexPair> toAdd);
  void OnRemoveElements(ReadonlyList<ElementIndexPair> toRemove);
}

public struct ElementIndexPair {
  public LeapGuiElement element;
  public int index;
}
