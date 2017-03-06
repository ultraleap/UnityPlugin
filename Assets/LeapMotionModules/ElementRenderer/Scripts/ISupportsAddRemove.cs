using System.Collections.Generic;
using Leap.Unity;

public interface ISupportsAddRemove {
  void OnAddElements(List<LeapGuiElement> element, List<int> indexes);
  void OnRemoveElements(List<int> toRemove);
}
