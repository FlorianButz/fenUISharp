using FenUISharp.Behavior;
using FenUISharp.Components.Text.Layout;
using FenUISharp.Mathematics;
using FenUISharp.Objects.Buttons;
using FenUISharp.Objects.Text;
using FenUISharp.Objects.Text.Model;
using FenUISharp.States;
using FenUISharp.WinFeatures;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace FenUISharp.Objects
{
    public class FTextInputField : Button, IStateListener
    {
        public FText Label { get; init; }

        public string Text
        {
            get
            {
                return text.ToString();
            }
            set
            {
                text = new(value);
                UpdateText();
            }
        }

        private string _Text
        {
            get
            {
                if (TextInputMode == TextInputFieldMode.Password)
                {
                    string returnString = "";
                    for (int i = 0; i < Text.Length; i++) returnString += "*";
                    return returnString;
                }
                return Text;
            }
        }

        public string PlaceholderText { get; set; } = "Type in text...";

        public (Vector2 HorizontalPadding, Vector2 VerticalPadding) TextPadding = (new(new(10f, 10f)), new(new(5f, 5f)));

        public enum TextInputFieldMode { Any, Alphabetic, Numeric, Alphanumeric, Password }
        public TextInputFieldMode TextInputMode { get; set; } = TextInputFieldMode.Any;

        private int _caretIndex = 0;
        public int CaretIndex
        {
            get => _caretIndex; set
            {
                _caretIndex = RMath.Clamp(value, 0, text.Length);
                if (!FContext.GetKeyboardInputManager().IsShiftPressed) _selectionIndex = _caretIndex;
            }
        }

        public State<SKColor> TextSelectionColor { get; init; }

        public State<SKColor> CaretColor { get; init; }
        public State<float> CaretWidth { get; init; }
        public State<float> CaretHeight { get; init; }
        public State<float> CaretBlinkSpeed { get; init; }

        public Action<string>? OnTextChanged { get; set; }
        public Action<string>? OnEnter { get; set; }

        private const char ARROW_LEFT = (char)37;
        private const char ARROW_RIGHT = (char)39;
        private const char ARROW_UP = (char)0x26;
        private const char ARROW_DOWN = (char)0x28;
        private const char BACKSPACE = (char)8;
        private const char CONTROL_BACKSPACE = (char)127;
        private const char ESCAPE = '\u001B';

        private const char PASTE = '\u0016'; // Ctrl + V
        private const char COPY = '\u0003'; // Ctrl + C
        private const char CUT = '\u0018';
        private const char UNDO = '\u001A';
        private const char ENTER = '\u000D';

        private const char SELALL = '\u0001'; // Ctrl + A
        // private const char TAB = '\u0009'; // Tabulator

        private StringBuilder text = new();

        private int _selectionIndex = 0;
        private float _lastTypedTimer = 0f;
        private float _typedTimerResetLength = 0.6f;

        public FTextInputField(FText label)
        {
            this.Label = label;
            label.LayoutModel = new WrapLayout(label) { AllowLinebreakChar = false, AllowLinebreakOnOverflow = false, AllowEllipsis = false };
            label.Model = TextModelFactory.CreateBasic(text.ToString());
            label.SetParent(this);

            HoverMix.Value = () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.HoveredMix.WithAlpha(75);
            RenderMaterial.Value = () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.PanelMaterial();

            CaretBlinkSpeed = new(() => 2.5f, this);
            CaretWidth = new(() => 1, this);
            CaretHeight = new(() => 18, this);
            CaretColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Primary.AddMix(new(50, 50, 50)), this);
            TextSelectionColor = new(() => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.Primary.MultiplyMix(new(180, 180, 180)), this);

            label.Padding.SetStaticState(0);
            label.Layout.StretchVertical.SetStaticState(true);
            // label.Layout.AbsoluteMarginHorizontal.SetStaticState(TextPadding.HorizontalPadding);
            // Label.Layout.AbsoluteMarginVertical.SetResponsiveState(() => TextPadding.VerticalPadding);

            new CursorComponent(this, Cursor.IBEAM);

            Transform.Size.SetStaticState(new Vector2(300, 30));
            // Transform.Size.SetResponsiveState(() => new Vector2(300 + (TextPadding.Item1.x + TextPadding.Item1.y), 25 + (TextPadding.Item2.x + TextPadding.Item2.y)));

            InteractiveSurface.EnableMouseActions.SetStaticState(true);
            InteractiveSurface.OnMouseAction += OnMouseAction;
            InteractiveSurface.OnDrag += OnDrag;
            InteractiveSurface.OnDoubleMouseAction += OnDoubleMouseAction;
            // InteractiveSurface.OnMouseEnter += () => { };
            // InteractiveSurface.OnMouseExit += () => { isSelected = false;  };

            FContext.GetKeyboardInputManager().OnTextTyped += OnKeyTyped;
            FContext.GetKeyboardInputManager().OnKeyTyped += OnKeyPressed;
        }

        public override void OnInternalStateChanged<T>(T value)
        {
            base.OnInternalStateChanged(value);
            UpdateText();
        }

        private void OnDoubleMouseAction(MouseInputButton button)
        {
            if (button == MouseInputButton.Left)
            {
                CaretIndex--;
                while (CaretIndex > 0 && !char.IsLetterOrDigit(_Text[CaretIndex]))
                    CaretIndex--;

                while (CaretIndex > 0 && char.IsLetterOrDigit(_Text[CaretIndex - 1]))
                    CaretIndex--;

                var selectionIndex = CaretIndex;

                CaretIndex++;
                while (CaretIndex < _Text.Length && !char.IsLetterOrDigit(_Text[CaretIndex - 1]))
                    CaretIndex++;

                while (CaretIndex < _Text.Length && char.IsLetterOrDigit(_Text[CaretIndex]))
                    CaretIndex++;

                _selectionIndex = selectionIndex;
                UpdateText();
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            CaretBlinkSpeed.Dispose();
            CaretColor.Dispose();
            CaretWidth.Dispose();
            CaretHeight.Dispose();
            TextSelectionColor.Dispose();

            FContext.GetKeyboardInputManager().OnTextTyped -= OnKeyTyped;
            FContext.GetKeyboardInputManager().OnKeyTyped -= OnKeyPressed;
        }

        private void OnKeyPressed(char obj)
        {
            if (!selectableComponent.IsSelected) return;
            
            switch (obj)
            {
                case ARROW_LEFT:
                    CaretIndex--;

                    if (FContext.GetKeyboardInputManager().IsControlPressed)
                    {
                        while (CaretIndex > 0 && !char.IsLetterOrDigit(_Text[CaretIndex]))
                            CaretIndex--;

                        while (CaretIndex > 0 && char.IsLetterOrDigit(_Text[CaretIndex - 1]))
                            CaretIndex--;
                    }

                    UpdateText();
                    break;
                case ARROW_RIGHT:
                    CaretIndex++;

                    if (FContext.GetKeyboardInputManager().IsControlPressed)
                    {
                        while (CaretIndex < _Text.Length && !char.IsLetterOrDigit(_Text[CaretIndex - 1]))
                            CaretIndex++;

                        while (CaretIndex < _Text.Length && char.IsLetterOrDigit(_Text[CaretIndex]))
                            CaretIndex++;
                    }
                    UpdateText();
                    break;
                case ARROW_UP:
                    CaretIndex = 0;
                    UpdateText();
                    break;
                case ARROW_DOWN:
                    CaretIndex = _Text.Length;
                    UpdateText();
                    break;
                case ENTER:
                    OnEnter?.Invoke(Text);
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

            if(selectableComponent.IsSelected)
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
            Vector2 localMousePos = Label.Transform.GlobalToDrawLocal(mousePos);
            List<Glyph> glyphs = Label.LayoutModel.ProcessModel(Label.Model, Shape.LocalBounds);

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
            if (!selectableComponent.IsSelected) return;
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

                // case TAB:
                //     string tabInsert = "    ";

                //     text.Insert(CaretIndex, tabInsert);
                //     CaretIndex += tabInsert.Length;
                //     break;

                case BACKSPACE:
                    if (_Text.Length == 0) return;

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
                    if (_Text.Length == 0)
                        return;

                    if (_selectionIndex == CaretIndex)
                    {
                        CaretIndex--;
                        while (CaretIndex > 0 && !char.IsLetterOrDigit(_Text[CaretIndex]))
                            CaretIndex--;

                        while (CaretIndex > 0 && char.IsLetterOrDigit(_Text[CaretIndex - 1]))
                            CaretIndex--;

                        int lengthToRemove = (oldCaretPos - CaretIndex);
                        text.Remove(CaretIndex, lengthToRemove);
                    }
                    else
                        RemoveSelectedText();
                    break;

                case SELALL:
                    CaretIndex = _Text.Length;
                    _selectionIndex = 0;
                    break;

                default:
                    if (c < 32) break; // Non-printable control key that wasn't handled above

                    RemoveSelectedText();

                    text.Insert(CaretIndex, c);
                    CaretIndex++;
                    _selectionIndex = CaretIndex;

                    if (GetWidthFromLeftToCaret() > GetVisibleBounds().Width)
                    {
                        float widthOfChar;
                        if (_Text.Length == 0 || _Text.Length - CaretIndex <= 0)
                            widthOfChar = 0;
                        else
                        {
                            var model = CreateTextModel(_Text[CaretIndex - 1].ToString());
                            var measuredBound = Label.LayoutModel.GetBoundingRect(model, SKRect.Create(0, 0, 99999, Label.Shape.LocalBounds.Height), 0);
                            widthOfChar = measuredBound.Width;
                        }

                        viewPosition = MathF.Max(viewPosition + widthOfChar, 0); // Add the width of the char to the view pos
                    }
                    break;
            }

            OnTextChanged?.Invoke(Text);
            UpdateText();
        }

        private string GetSelectedText()
        {
            if (_selectionIndex == CaretIndex) return ""; // Return empty

            int selStart = (int)MathF.Min(CaretIndex, _selectionIndex);
            int selEnd = (int)MathF.Max(CaretIndex, _selectionIndex);
            int selLength = selEnd - selStart;

            return _Text.Substring(selStart, selLength);
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
            if (CaretIndex == 0 || _Text.Length == 0)
                return Transform.GlobalToDrawLocal(Label.Transform.DrawLocalToGlobal(new Vector2(0, Label.Shape.LocalBounds.MidY)));

            var textToCaret = _Text.Substring(0, CaretIndex);

            var model = CreateTextModel(textToCaret);
            var bound = Label.LayoutModel.GetBoundingRect(model, SKRect.Create(0, 0, 99999, Label.Shape.LocalBounds.Height), 1);
            float xPos = bound.Width;

            if ((CaretIndex - 1) >= 0)
            {
                var textToCaretS = _Text.Substring(CaretIndex - 1, 1);
                var modelS = CreateTextModel(textToCaretS);
                xPos -= Label.LayoutModel.GetBoundingRect(modelS, SKRect.Create(0, 0, 99999, Label.Shape.LocalBounds.Height), 0).Width / 2;
            }
            xPos += 2f;

            return Transform.GlobalToDrawLocal(Label.Transform.DrawLocalToGlobal(new Vector2(xPos, Label.Shape.LocalBounds.MidY)));
        }

        private float viewPosition = 0f;

        private void UpdateText()
        {
            ValidateText();

            if (string.IsNullOrWhiteSpace(_Text))
                Label.Model = CreateTextModel(PlaceholderText);
            else
                Label.Model = CreateTextModel();

            var bound = Label.LayoutModel.GetBoundingRect(Label.Model, SKRect.Create(0, 0, 99999, Label.Shape.LocalBounds.Height));
            Label.Layout.Alignment.SetStaticState(new(1f, 0.5f));
            Label.Layout.AlignmentAnchor.SetStaticState(new(1f, 0.5f));
            Label.Layout.AbsoluteMarginVertical.SetStaticState(TextPadding.VerticalPadding);

            Label.Transform.Size.SetStaticState(new(MathF.Max(bound.Width + TextPadding.Item1.x + TextPadding.Item1.y, Shape.LocalBounds.Width), 0));

            // Make sure caret stays in-bounds
            // Calculate text (from right to caret pos); subtract the width of the label and apply that as offset

            var visibleBounds = GetVisibleBounds();

            float textWidthFromRightToCaret = GetWidthFromRightToCaret();

            if ((textWidthFromRightToCaret - visibleBounds.Width) > 0)
                viewPosition = RMath.Clamp(MathF.Max(textWidthFromRightToCaret + 1 /* Add back a small padding to account for inaccuracies*/ - visibleBounds.Width, viewPosition), 0, float.MaxValue);
            if ((textWidthFromRightToCaret - viewPosition) < 0)
                viewPosition = RMath.Clamp(MathF.Min(textWidthFromRightToCaret, viewPosition), float.MinValue, viewPosition);

            Label.Transform.LocalPosition.SetStaticState(new(viewPosition + TextPadding.HorizontalPadding.x, 0));
        }

        string lastValidInput = "";
        void ValidateText()
        {
            switch (TextInputMode)
            {
                case TextInputFieldMode.Alphabetic:
                    text = new(Regex.Replace(text.ToString(), @"[^A-Za-z]", ""));
                    break;
                case TextInputFieldMode.Numeric:
                    if (string.IsNullOrWhiteSpace(text.ToString()))
                    {
                        text = new("0");
                        CaretIndex = 1;
                        break;
                    }
                    if (!float.TryParse(text.ToString(), out var _))
                    {
                        text = new(lastValidInput);
                        if (!float.TryParse(text.ToString(), out var __))
                            text = new(Regex.Replace(text.ToString(), @"[^0-9.,]", ""));
                    }
                    break;
                case TextInputFieldMode.Alphanumeric:
                    text = new(Regex.Replace(text.ToString(), @"[^A-Za-z0-9,.]", ""));
                    break;
            }

            lastValidInput = _Text;

            var lastSelection = _selectionIndex;
            CaretIndex = _caretIndex; // Make sure to trigger setter
            _selectionIndex = RMath.Clamp(lastSelection, 0, _Text.Length);
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

                returnList.Add(new TextSpan(_Text.Substring(0, selStart), style));
                returnList.Add(new TextSpan(_Text.Substring(selStart, selLength), selectedStyle));
                returnList.Add(new TextSpan(_Text.Substring(selEnd, _Text.Length - selEnd), style));
            }
            else
                returnList.Add(new TextSpan(overrideText, style));

            return new(returnList, algn, FTypeface.Default);
        }

        SKRect GetVisibleBounds()
        {
            var visibleBounds = Shape.LocalBounds;
            return new SKRect(
                visibleBounds.Left + TextPadding.Item1.x,
                visibleBounds.Top + TextPadding.Item2.x,
                visibleBounds.Right - TextPadding.Item1.y,
                visibleBounds.Bottom - TextPadding.Item2.y
            );
        }

        float GetWidthFromRightToCaret()
        {
            float textWidthFromRightToCaret;
            if (_Text.Length == 0 || _Text.Length - CaretIndex <= 0)
                textWidthFromRightToCaret = 0;
            else
            {
                var textToCaret = _Text.Substring(CaretIndex, _Text.Length - CaretIndex);

                var model = CreateTextModel(textToCaret);
                var measuredBound = Label.LayoutModel.GetBoundingRect(model, SKRect.Create(0, 0, 99999, Label.Shape.LocalBounds.Height), 0);
                textWidthFromRightToCaret = measuredBound.Width;
            }
            return textWidthFromRightToCaret;
        }

        float GetWidthFromLeftToCaret()
        {
            float textWidthFromLeftToCaret;
            if (_Text.Length == 0 || CaretIndex == 0)
                textWidthFromLeftToCaret = 0;
            else
            {
                var textToCaret = _Text.Substring(0, CaretIndex);

                var model = CreateTextModel(textToCaret);
                var measuredBound = Label.LayoutModel.GetBoundingRect(model, SKRect.Create(0, 0, 99999, Label.Shape.LocalBounds.Height), 0);
                textWidthFromLeftToCaret = measuredBound.Width;
            }
            return textWidthFromLeftToCaret;
        }

        public override void DrawChildren(SKCanvas? canvas)
        {
            canvas?.ClipRect(GetVisibleBounds(), antialias: true);

            base.DrawChildren(canvas);
        }

        public override void AfterRender(SKCanvas canvas)
        {
            base.AfterRender(canvas);

            if (!selectableComponent.IsSelected) return;

            var caretPos = GetCaretPos();
            var caretRect = SKRect.Create(caretPos.x - CaretWidth.CachedValue / 2, caretPos.y - CaretHeight.CachedValue / 2, CaretWidth.CachedValue, CaretHeight.CachedValue);
            
            using var paint = GetRenderPaint();
            paint.Color = CaretColor.CachedValue.WithAlpha((byte)RMath.Clamp(_lastTypedTimer > 0 ? 255 : (MathF.Sin(FContext.Time * MathF.PI * CaretBlinkSpeed.CachedValue) + 1) * 255, 0, 255));

            using var caretRoundRect = new SKRoundRect(caretRect, CaretWidth.CachedValue / 2);

            canvas.DrawRoundRect(caretRoundRect, paint);
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

        // TODO: Maybe have to write a proper function in the future
        static string CleanInput(string strIn)
        {
            return strIn;

            try
            {
                return Regex.Replace(strIn, @"[^ -~äöüÄÖÜß]", "",
                                        RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            catch (RegexMatchTimeoutException)
            {
                return String.Empty;
            }
        }

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

            result = CleanInput(Marshal.PtrToStringAnsi(ptr) ?? "");
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
