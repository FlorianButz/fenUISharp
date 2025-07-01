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
        private const char ARROW_UP = (char)0x26;
        private const char ARROW_DOWN = (char)0x28;
        private const char BACKSPACE = (char)8;
        private const char CONTROL_BACKSPACE = (char)127;
        private const char NEWLINE1 = '\r';
        private const char NEWLINE2 = '\n';
        private const char ESCAPE = '\u001B';

        private const char PASTE = '\u0016'; // Ctrl + V
        private const char COPY = '\u0003'; // Ctrl + C
        private const char CUT = '\u0018';
        private const char UNDO = '\u001A';

        private const char SELALL = '\u0001'; // Ctrl + A
        private const char TAB = '\u0009'; // Tabulator

        private int _caretIndex = 0;
        public int CaretIndex
        {
            get => _caretIndex; set
            {
                _caretIndex = RMath.Clamp(value, 0, text.Length);
                if (!FContext.GetKeyboardInputManager().IsShiftPressed) _selectionIndex = _caretIndex;
            }
        }

        private int _selectionIndex = 0;
        private float _lastTypedTimer = 0f;
        private float _typedTimerResetLength = 0.6f;

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
            InteractiveSurface.OnDrag += OnDrag;
            InteractiveSurface.OnDoubleMouseAction += OnDoubleMouseAction;
            // InteractiveSurface.OnMouseEnter += () => { };
            // InteractiveSurface.OnMouseExit += () => { isSelected = false;  };

            FContext.GetKeyboardInputManager().OnTextTyped += OnKeyTyped;
            FContext.GetKeyboardInputManager().OnKeyTyped += OnKeyPressed;
        }

        private void OnDoubleMouseAction(MouseInputButton button)
        {
            if (button == MouseInputButton.Left)
            {
                CaretIndex--;
                while (CaretIndex > 0 && !char.IsLetterOrDigit(text[CaretIndex]))
                    CaretIndex--;

                while (CaretIndex > 0 && char.IsLetterOrDigit(text[CaretIndex - 1]))
                    CaretIndex--;

                var selectionIndex = CaretIndex;

                CaretIndex++;
                while (CaretIndex < text.Length && !char.IsLetterOrDigit(text[CaretIndex - 1]))
                    CaretIndex++;

                while (CaretIndex < text.Length && char.IsLetterOrDigit(text[CaretIndex]))
                    CaretIndex++;

                _selectionIndex = selectionIndex;
                UpdateText();
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            FContext.GetKeyboardInputManager().OnTextTyped -= OnKeyTyped;
            FContext.GetKeyboardInputManager().OnKeyTyped -= OnKeyPressed;
        }

        private void OnKeyPressed(char obj)
        {
            switch (obj)
            {
                case ARROW_LEFT:
                    CaretIndex--;

                    if (FContext.GetKeyboardInputManager().IsControlPressed)
                    {
                        while (CaretIndex > 0 && !char.IsLetterOrDigit(text[CaretIndex]))
                            CaretIndex--;

                        while (CaretIndex > 0 && char.IsLetterOrDigit(text[CaretIndex - 1]))
                            CaretIndex--;
                    }

                    UpdateText();
                    break;
                case ARROW_RIGHT:
                    CaretIndex++;

                    if (FContext.GetKeyboardInputManager().IsControlPressed)
                    {
                        while (CaretIndex < text.Length && !char.IsLetterOrDigit(text[CaretIndex - 1]))
                            CaretIndex++;

                        while (CaretIndex < text.Length && char.IsLetterOrDigit(text[CaretIndex]))
                            CaretIndex++;
                    }
                    UpdateText();
                    break;
                case ARROW_UP:
                    CaretIndex = 0;
                    break;
                case ARROW_DOWN:
                    CaretIndex = text.Length;
                    break;
                default:
                    break;
            }

            _lastTypedTimer = _typedTimerResetLength;
        }

        public override void LateBegin()
        {
            base.LateBegin();
            UpdateText();
        }

        protected override void Update()
        {
            base.Update();

            if (_lastTypedTimer > 0)
                _lastTypedTimer -= FContext.DeltaTime;

            // if()
            Invalidate(Invalidation.SurfaceDirty);
        }

        private void OnDrag(Vector2 vector)
        {
            int oldSelectionIndex = _selectionIndex;
            CaretIndex = MousePosToCaretIndex(FContext.GetCurrentWindow().ClientMousePosition);
            _selectionIndex = oldSelectionIndex;

            _lastTypedTimer = _typedTimerResetLength;
            UpdateText();
        }

        private void OnMouseAction(MouseInputCode code)
        {
            if (code.button == 0 && code.state == 0) // Left mouse down
            {
                // isSelected = true;

                CaretIndex = MousePosToCaretIndex(FContext.GetCurrentWindow().ClientMousePosition);
                _lastTypedTimer = _typedTimerResetLength;

                UpdateText();
                Invalidate(Invalidation.SurfaceDirty);
            }
        }

        private int MousePosToCaretIndex(Vector2 mousePos)
        {
            Vector2 localMousePos = label.Transform.GlobalToDrawLocal(mousePos);
            List<Glyph> glyphs = label.LayoutModel.ProcessModel(label.Model, Shape.LocalBounds);

            for (int i = 0; i < glyphs.Count; i++)
            {
                var caretCheckBound = glyphs[i].Bounds;
                caretCheckBound.Inflate(1, 2);

                // Making sure that the caret moves to the left side of a char when the mouse is more on the left side and vice versa
                caretCheckBound.Offset(-caretCheckBound.Width / 2, 0);

                if (RMath.ContainsPoint(caretCheckBound, localMousePos))
                {
                    // Return caret position
                    return i; // Use i instead of i + 1 because of the offset
                }
            }

            // Makes quickly selecting less annoying by defaulting to the 0th index when the mouse is on the left side of the first char
            if (localMousePos.x < Shape.LocalBounds.Left)
                return 0;

            return text.Length;
        }

        private void OnKeyTyped(char c)
        {
            // if (char.IsControl(c) && (c != BACKSPACE && c != PASTE)) return;

            int oldCaretPos = CaretIndex;

            switch (c)
            {
                case PASTE:
                    RemoveSelectedText();

                    TryGetClipboardText(out string clipboardText);
                    text.Insert(CaretIndex, clipboardText);

                    CaretIndex += clipboardText.Length;
                    break;

                case COPY:
                    TrySetClipboardText(GetSelectedText());
                    break;

                case CUT:
                    TrySetClipboardText(GetSelectedText());
                    RemoveSelectedText();
                    break;

                case TAB:
                    string tabInsert = "    ";

                    text.Insert(CaretIndex, tabInsert);
                    CaretIndex += tabInsert.Length;
                    break;

                case BACKSPACE:
                    if (text.Length == 0) return;

                    if (_selectionIndex == CaretIndex)
                    {
                        if (CaretIndex != 0)
                        {
                            text.Remove(RMath.Clamp(CaretIndex - 1, 0, text.Length - 1), 1);
                            CaretIndex--;
                        }
                    }
                    else
                        RemoveSelectedText();
                    break;

                case CONTROL_BACKSPACE:
                    // Make sure we don't go out of bounds
                    if (text.Length == 0)
                        return;

                    if (_selectionIndex == CaretIndex)
                    {
                        CaretIndex--;
                        while (CaretIndex > 0 && !char.IsLetterOrDigit(text[CaretIndex]))
                            CaretIndex--;

                        while (CaretIndex > 0 && char.IsLetterOrDigit(text[CaretIndex - 1]))
                            CaretIndex--;

                        int lengthToRemove = (oldCaretPos - CaretIndex);
                        text.Remove(CaretIndex, lengthToRemove);
                    }
                    else
                        RemoveSelectedText();
                    break;

                case SELALL:
                    CaretIndex = text.Length;
                    _selectionIndex = 0;
                    break;

                default:
                    if (c < 32) break; // Non-printable control key that wasn't handled above

                    RemoveSelectedText();

                    text.Insert(CaretIndex, c);
                    CaretIndex++;
                    _selectionIndex = CaretIndex;
                    break;
            }

            UpdateText();
        }

        private string GetSelectedText()
        {
            if (_selectionIndex == CaretIndex) return ""; // Return empty

            int selStart = (int)MathF.Min(CaretIndex, _selectionIndex);
            int selEnd = (int)MathF.Max(CaretIndex, _selectionIndex);
            int selLength = selEnd - selStart;

            return text.ToString().Substring(selStart, selLength);
        }

        private void RemoveSelectedText()
        {
            if (_selectionIndex == CaretIndex) return; // No need to remove anything

            int selStart = (int)MathF.Min(CaretIndex, _selectionIndex);
            int selEnd = (int)MathF.Max(CaretIndex, _selectionIndex);
            int selLength = selEnd - selStart;

            text = text.Remove(selStart, selLength);
            CaretIndex = (int)MathF.Min(CaretIndex, _selectionIndex); // Use the smaller one. This also makes sure it stays in bounds. Trigger the setter function again

            _selectionIndex = CaretIndex; // Reset selection
        }

        private Vector2 GetCaretPos()
        {
            if (CaretIndex == 0 || text.Length == 0)
                return Transform.GlobalToDrawLocal(label.Transform.DrawLocalToGlobal(new Vector2(0, label.Shape.LocalBounds.MidY)));

            var textToCaret = text.ToString().Substring(0, CaretIndex);

            var model = CreateTextModel(textToCaret);
            var bound = label.LayoutModel.GetBoundingRect(model, SKRect.Create(0, 0, 99999, label.Shape.LocalBounds.Height));
            float xPos = bound.Width;

            if ((CaretIndex - 1) >= 0)
            {
                var textToCaretS = text.ToString().Substring(CaretIndex - 1, 1);
                var modelS = CreateTextModel(textToCaretS);
                xPos -= label.LayoutModel.GetBoundingRect(modelS, SKRect.Create(0, 0, 99999, label.Shape.LocalBounds.Height)).Width / 2;
            }

            xPos += 2.5f;

            return Transform.GlobalToDrawLocal(label.Transform.DrawLocalToGlobal(new Vector2(xPos, label.Shape.LocalBounds.MidY)));
        }

        private float viewPosition = 0f;

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

            // Make sure caret stays in-bounds
            // Calculate text (from right to caret pos); subtract the width of the label and apply that as offset

            var visibleBounds = Shape.LocalBounds;
            visibleBounds.Inflate(-TextPadding.x, -TextPadding.y);

            float textWidthFromRightToCaret;
            if (text.Length == 0 || text.Length - CaretIndex <= 0)
                textWidthFromRightToCaret = visibleBounds.Width;
            else
            {
                var textToCaret = text.ToString().Substring(CaretIndex, text.Length - CaretIndex);

                var model = CreateTextModel(textToCaret);
                var measuredBound = label.LayoutModel.GetBoundingRect(model, SKRect.Create(0, 0, 99999, label.Shape.LocalBounds.Height));
                textWidthFromRightToCaret = measuredBound.Width;
            }

            float textWidthFromLeftToCaret;
            if (text.Length == 0 || CaretIndex == 0)
                textWidthFromLeftToCaret = visibleBounds.Width;
            else
            {
                var textToCaret = text.ToString().Substring(0, CaretIndex);

                var model = CreateTextModel(textToCaret);
                var measuredBound = label.LayoutModel.GetBoundingRect(model, SKRect.Create(0, 0, 99999, label.Shape.LocalBounds.Height));
                textWidthFromLeftToCaret = measuredBound.Width;
            }

            if ((textWidthFromRightToCaret - visibleBounds.Width) > 0)
                viewPosition = RMath.Clamp(MathF.Max(textWidthFromRightToCaret - visibleBounds.Width, viewPosition), 0, float.MaxValue);
            else if ((textWidthFromLeftToCaret - visibleBounds.Width) > 0)
                viewPosition = -RMath.Clamp(MathF.Min(textWidthFromLeftToCaret - visibleBounds.Width, viewPosition), 0, float.MaxValue);

            Console.WriteLine("V" + viewPosition);

            label.Transform.LocalPosition.SetStaticState(new(viewPosition, 0));
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
                int selStart = (int)MathF.Min(CaretIndex, _selectionIndex);
                int selEnd = (int)MathF.Max(CaretIndex, _selectionIndex);
                int selLength = selEnd - selStart;

                returnList.Add(new TextSpan(text.ToString().Substring(0, selStart), style));
                returnList.Add(new TextSpan(text.ToString().Substring(selStart, selLength), selectedStyle));
                returnList.Add(new TextSpan(text.ToString().Substring(selEnd, text.Length - selEnd), style));
            }
            else
                returnList.Add(new TextSpan(overrideText, style));

            return new(returnList, algn, FTypeface.Default);
        }

        public override void DrawChildren(SKCanvas? canvas)
        {
            var localBounds = Shape.LocalBounds;
            localBounds.Inflate(-TextPadding.x, -TextPadding.y);
            canvas?.ClipRect(localBounds, antialias: true);

            base.DrawChildren(canvas);
        }

        public override void AfterRender(SKCanvas canvas)
        {
            base.AfterRender(canvas);

            int caretHeight = 18;
            int caretWidth = 1;
            var caretPos = GetCaretPos();
            var caretRect = SKRect.Create(caretPos.x - caretWidth / 2, caretPos.y - caretHeight / 2, caretWidth, caretHeight);
            // using var roundRect = new SKRoundRect(caretRect, 2);

            canvas.DrawRect(caretRect, new SKPaint() { Color = SKColors.Yellow.WithAlpha((byte)RMath.Clamp((_lastTypedTimer > 0 ? 255 : (MathF.Sin(FContext.Time * 10) + 1) * 255), 0, 255)) });
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

        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        const uint CF_TEXT = 1;
        const uint GMEM_MOVEABLE = 0x0002;

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

        static bool TrySetClipboardText(string text)
        {
            if (!OpenClipboard(IntPtr.Zero))
                return false;

            if (!EmptyClipboard())
            {
                CloseClipboard();
                return false;
            }

            // Allocate global memory for the text
            var bytes = Encoding.ASCII.GetBytes(text + '\0'); // CF_TEXT must be null-terminated
            IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes.Length);

            if (hGlobal == IntPtr.Zero)
            {
                CloseClipboard();
                return false;
            }

            // Lock, copy, unlock
            IntPtr ptr = GlobalLock(hGlobal);
            if (ptr == IntPtr.Zero)
            {
                CloseClipboard();
                return false;
            }

            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            GlobalUnlock(hGlobal);

            // Set clipboard data
            if (SetClipboardData(CF_TEXT, hGlobal) == IntPtr.Zero)
            {
                CloseClipboard();
                return false;
            }

            CloseClipboard();
            return true;
        }
    }
}
