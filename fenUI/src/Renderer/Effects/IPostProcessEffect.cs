namespace FenUISharp.RuntimeEffects
{
    public interface IPostProcessEffect
    {
        void OnBeforeRender(PPInfo info);
        void OnAfterRender(PPInfo info);
        void OnLateAfterRender(PPInfo info);
    }
}