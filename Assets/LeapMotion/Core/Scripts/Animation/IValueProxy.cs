
/// <summary>
/// A simple interface that allows an object to act as a 'proxy'
/// interface to another object.  The proxy can store a serialized
/// representation of a value on another object.  The value of
/// the proxy can either be updated from the object (pull), or
/// be pushed out to the object (push).
/// </summary>
public interface IValueProxy {

  /// <summary>
  /// Called when this proxy should push its serialized representation
  /// out to the target object.
  /// </summary>
  void OnPushValue();

  /// <summary>
  /// Called when this proxy should pull from the target object into
  /// its serialized representation.
  /// </summary>
  void OnPullValue();
}
