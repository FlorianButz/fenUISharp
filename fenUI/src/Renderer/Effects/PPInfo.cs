using FenUISharp.Objects;
using SkiaSharp;

namespace FenUISharp.RuntimeEffects
{
    public struct PPInfo
    {
        public SKSurface source;
        public SKImageInfo sourceInfo;

        public SKSurface target;
        public UIObject owner;
    } 
}