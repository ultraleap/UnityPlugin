

namespace Leap.Unity {

  public interface IStreamReceiver<T> {

    void Open();

    void Receive(T data);

    void Close();

  }

}
