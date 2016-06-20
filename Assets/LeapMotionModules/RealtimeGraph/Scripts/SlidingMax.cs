
namespace Leap.Unity.RealtimeGraph {

  public class SlidingMax {

    private struct IndexValuePair {
      public int index;
      public float value;

      public IndexValuePair(int index, float value) {
        this.index = index;
        this.value = value;
      }
    }

    private int _history;
    private int _count;
    private Dequeue<IndexValuePair> _dequeue = new Dequeue<IndexValuePair>();

    public SlidingMax(int history) {
      _history = history;
      _count = 0;
    }

    public void AddValue(float value) {
      while (_dequeue.Count != 0 && _dequeue.Front.value <= value) {
        _dequeue.PopFront();
      }

      _dequeue.PushFront(new IndexValuePair(_count, value));
      _count++;

      while (_dequeue.Back.index < (_count - _history)) {
        _dequeue.PopBack();
      }
    }

    public float Max {
      get {
        return _dequeue.Back.value;
      }
    }
  }
}
