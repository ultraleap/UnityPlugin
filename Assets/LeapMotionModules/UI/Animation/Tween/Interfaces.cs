
namespace Leap.Unity.Animation {

  public interface IInterpolator : IPoolable {
    void Interpolate(float percent);
    float length { get; }
    bool isValid { get; }
  }

  public interface IPoolable {
    void OnSpawn(); //called by pool when spawn happens
    void Recycle(); //called by user to have the object return itself
  }
}
