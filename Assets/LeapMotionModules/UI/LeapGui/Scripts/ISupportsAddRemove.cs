using System.Collections.Generic;

public interface ISupportsAddRemove {
  void OnAddElements(List<ElementIndexPair> toAdd);
  void OnRemoveElements(List<ElementIndexPair> toRemove);
}

public struct ElementIndexPair {
  public LeapGuiElement element;
  public int index;
}
