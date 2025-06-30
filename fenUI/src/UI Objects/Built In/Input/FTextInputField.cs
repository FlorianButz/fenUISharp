using FenUISharp.Behavior;
using FenUISharp.Components.Text.Layout;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Model;
using FenUISharp.WinFeatures;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FenUISharp.Objects
{
    public class FTextInputField : Button
    {
        private FText label;
        private StringBuilder text = new();

        public string PlaceholderText = "Type in text...";

        public Vector2 TextPadding = new(10f, 5f);

        private const char ARROW_LEFT = (char)37;
        private const char ARROW_RIGHT = (char)39;
        private const char BACKSPACE = (char)8;
        private const char CONTROL_BACKSPACE = (char)127;
        private const char NEWLINE1 = '\r';
        private const char NEWLINE2 = '\n';
        private const char ESCAPE = '\u001B';
        private const char PASTE = '\u0016'; // Ctrl + V

        private int _caretIndex = 0;
        private int _selectionIndex = 0;

        public FTextInputField(FText label)
        {
            this.label = label;
            label.LayoutModel = new WrapLayout(label) { AllowLinebreakChar = false, AllowLinebreakOnOverflow = false, AllowEllipsis = false };
            label.Model = TextModelFactory.CreateBasic(text.ToString());
            label.SetParent(this);

            label.Padding.SetStaticState(0);
            label.Layout.StretchVertical.SetStaticState(true);
            label.Layout.AbsoluteMarginVertical.SetStaticState(TextPadding.y);
            label.Layout.AbsoluteMarginHorizontal.SetStaticState(TextPadding.x);

            new CursorComponent(this, Cursor.IBEAM);

            Transform.Size.SetResponsiveState(() => new Vector2(300 + TextPadding.x * 2, 25 + TextPadding.y * 2));

            InteractiveSurface.EnableMouseActions.SetStaticState(true);
            InteractiveSurface.OnMouseAction += OnMouseAction;
            InteractiveSurface.OnMouseEnter += () => { };
            // InteractiveSurface.OnMouseExit += () => { isSelected = false;  };

            FContext.GetCurrentWindow().Char += OnKeyTyped;
            WindowFeatures.GlobalHooks.OnKeyPressed += OnKeyPressed;
        }

        private void OnKeyPressed(int obj)
        {
            switch (obj)
            {
                case ARROW_LEFT:
                    _caretIndex--;
                    _selectionIndex = 0;
                    _caretIndex = RMath.Clamp(_caretIndex, 0, text.Length);
                    break;
                case ARROW_RIGHT:
                    _caretIndex++;
                    _caretIndex = RMath.Clamp(_caretIndex, 0, text.Length);
                    break;
                default:
                    break;
            }
        }

        public override void LateBegin()
        {
            base.LateBegin();
            UpdateText();
        }

        protected override void Update()
        {
            base.Update();
            Invalidate(Invalidation.SurfaceDirty);
        }

        private void OnMouseAction(MouseInputCode code)
        {
            if (code.button == 0 && code.state == 0) // Left mouse down
            {
                // isSelected = true;

                Vector2 localMousePos = Transform.GlobalToDrawLocal(FContext.GetCurrentWindow().ClientMousePosition) - TextPadding;
                List<Glyph> glyphs = label.LayoutModel.ProcessModel(label.Model, Shape.LocalBounds);

                bool changedCaretPos = false;

                for (int i = 0; i < glyphs.Count; i++)
                {
                    var caretCheckBound = glyphs[i].Bounds;

                    // Making sure that the caret moves to the left side of a char when the mouse is more on the left side and vice versa
                    caretCheckBound.Offset(-caretCheckBound.Width / 2, 0);

                    if (RMath.ContainsPoint(caretCheckBound, localMousePos))
                    {
                        // Set caret position
                        _caretIndex = i; // Use i instead of i + 1 because of the offset
                        changedCaretPos = true;
                    }
                }

                if (!changedCaretPos) _caretIndex = text.Length;
                Invalidate(Invalidation.SurfaceDirty);
            }
        }

        private void OnKeyTyped(char c)
        {
            // if (char.IsControl(c) && (c != BACKSPACE && c != PASTE)) return;

            int oldCaretPos = _caretIndex;

            if (c == PASTE)
            {
                TryGetClipboardText(out string clipboardText);
                text.Insert(_caretIndex, clipboardText);

                _caretIndex += clipboardText.Length;
            }
            else if (c == BACKSPACE)
            {
                if (text.Length == 0) return;

                text.Remove(RMath.Clamp(_caretIndex - 1, 0, text.Length - 1), 1);
                _caretIndex--;

            }
            else if (c == CONTROL_BACKSPACE)
            {
                // Make sure we don't go out of bounds
                if (_caretIndex == 0 || text.Length == 0)
                    return;

                _caretIndex--;
                while (_caretIndex > 0 && !char.IsLetterOrDigit(text[_caretIndex]))
                    _caretIndex--;

                while (_caretIndex > 0 && char.IsLetterOrDigit(text[_caretIndex - 1]))
                    _caretIndex--;

                int lengthToRemove = (oldCaretPos - _caretIndex);
                text.Remove(_caretIndex, lengthToRemove);
            }
            else
            {
                text.Insert(_caretIndex, c);
                _caretIndex++;
            }

            UpdateText();
        }

        private Vector2 GetCaretPos()
        {
            if (_caretIndex == 0)
                return Transform.GlobalToDrawLocal(label.Transform.DrawLocalToGlobal(new Vector2(0, label.Shape.LocalBounds.MidY)));

            var textToCaret = text.ToString().Substring(0, _caretIndex);

            var model = CreateTextModel(textToCaret);
            var bound = label.LayoutModel.GetBoundingRect(model, SKRect.Create(0, 0, 99999, label.Shape.LocalBounds.Height));
            float xPos = bound.Width;

            if ((_caretIndex - 1) >= 0)
            {
                var textToCaretS = text.ToString().Substring(_caretIndex - 1, 1);
                var modelS = CreateTextModel(textToCaretS);
                xPos -= label.LayoutModel.GetBoundingRect(modelS, SKRect.Create(0, 0, 99999, label.Shape.LocalBounds.Height)).Width / 2;
            }

            xPos += 2.5f;

            return Transform.GlobalToDrawLocal(label.Transform.DrawLocalToGlobal(new Vector2(xPos, label.Shape.LocalBounds.MidY)));
        }

        private void UpdateText()
        {
            if (string.IsNullOrWhiteSpace(text.ToString()))
                label.Model = CreateTextModel(PlaceholderText);
            else
                label.Model = CreateTextModel();

            var bound = label.LayoutModel.GetBoundingRect(label.Model, SKRect.Create(0, 0, 99999, label.Shape.LocalBounds.Height));
            label.Layout.Alignment.SetStaticState(new(1f, 0.5f));
            label.Layout.AlignmentAnchor.SetStaticState(new(1f, 0.5f));

            label.Transform.Size.SetStaticState(new(MathF.Max(bound.Width + TextPadding.x * 2, Shape.LocalBounds.Width), bound.Height));
        }

        TextModel CreateTextModel(string? overrideText = null)
        {
            TextStyle style = new()
            {
                FontSize = 14,
                Color = (overrideText == null) ? () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnBackground : () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface.WithAlpha(75),
                Weight = SKFontStyleWeight.Normal,
                Slant = SKFontStyleSlant.Upright,
                Underlined = false
            };
            TextStyle selectedStyle = new(style) { BackgroundColor = () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Primary.WithAlpha(150) };
            TextAlign algn = new() { HorizontalAlign = Components.Text.Layout.TextAlign.AlignType.Start, VerticalAlign = Components.Text.Layout.TextAlign.AlignType.Middle };

            var returnList = new List<TextSpan>() { };
            if (overrideText == null)
            {
                int selStart = (int)MathF.Min(_caretIndex, _selectionIndex);
                int selEnd = (int)MathF.Max(_caretIndex, _selectionIndex);
                int selLength = selEnd - selStart;

                returnList.Add(new TextSpan(text.ToString().Substring(0, selStart), style));
                returnList.Add(new TextSpan(text.ToString().Substring(selStart, selLength), selectedStyle));
                returnList.Add(new TextSpan(text.ToString().Substring(selEnd, text.Length - selEnd), style));
            }
            else
                returnList.Add(new TextSpan(overrideText, style));

            return new(returnList, algn, FTypeface.Default);
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            int caretHeight = 17;
            var caretPos = GetCaretPos();
            var caretRect = SKRect.Create(caretPos.x - 1, caretPos.y - caretHeight / 2, 2, caretHeight);
            using var roundRect = new SKRoundRect(caretRect, 5);

            canvas.DrawRoundRect(roundRect, new SKPaint() { Color = SKColors.Yellow.WithAlpha((byte)RMath.Clamp((MathF.Sin(FContext.Time * 10) > 0 ? 1 : 0) * 255, 0, 255)) });
        }


        [DllImport("user32.dll")]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        static extern bool GlobalUnlock(IntPtr hMem);

        const uint CF_TEXT = 1;

        static bool TryGetClipboardText(out string result)
        {
            result = null;
            if (!IsClipboardFormatAvailable(CF_TEXT))
                return false;

            if (!OpenClipboard(IntPtr.Zero))
                return false;

            IntPtr handle = GetClipboardData(CF_TEXT);
            if (handle == IntPtr.Zero)
            {
                CloseClipboard();
                return false;
            }

            IntPtr ptr = GlobalLock(handle);
            if (ptr == IntPtr.Zero)
            {
                CloseClipboard();
                return false;
            }

            result = Marshal.PtrToStringAnsi(ptr);
            GlobalUnlock(handle);
            CloseClipboard();
            return result != null;
        }
    }
}
