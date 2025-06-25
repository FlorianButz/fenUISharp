namespace FenUISharp.States
{
    public interface IStateListener
    {
        public void OnInternalStateChanged<T>(T value);
    }
}