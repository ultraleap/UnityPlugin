using UnityEngine;

public abstract class LeapGuiRadialSpace : LeapGuiSpace {
  public const string RADIUS_PROPERTY = LeapGui.PROPERTY_PREFIX + "RadialSpace_Radius";

  [SerializeField]
  public float radius = 1;

  public interface IRadialTransformer {
    Vector4 GetVectorRepresentation(LeapGuiElement element);
  }
}
