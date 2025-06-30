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
    public class FTextInputField : UIObject
    {
        private FText label;
        private StringBuilder text = new();

        public string PlaceholderText = "Type in text...";

        public Vector2 TextPadding = new(15f, 10f);

        private const char BACKSPACE = (char)8;
        private const char NEWLINE1 = '\r';
        private const char NEWLINE2 = '\n';
        private const char ESCAPE = '\u001B';
        private const char PASTE = '\u0016'; // Ctrl + V



        private int _caretIndex = 0;

        public FTextInputField(FText label)
        {
            this.label = label;
            label.LayoutModel = new WrapLayout(label) { AllowLinebreakChar = false, AllowLinebreakOnOverflow = false, AllowEllipsis = false };
            label.Model = TextModelFactory.CreateBasic(text.ToString());
            label.SetParent(this);
            label.Layout.StretchVertical.SetStaticState(true);

            new CursorComponent(this, Cursor.IBEAM);

            Transform.Size.SetResponsiveState(() => new Vector2(300 + TextPadding.x * 2, 25 + TextPadding.y * 2));

            InteractiveSurface.EnableMouseActions.SetStaticState(true);
            InteractiveSurface.OnMouseAction += OnMouseAction;
            InteractiveSurface.OnMouseEnter += () => { };
            // InteractiveSurface.OnMouseExit += () => { isSelected = false;  };

            FContext.GetCurrentWindow().Char += OnKeyTyped;
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
                Vector2 localMousePos = Transform.GlobalToDrawLocal(FContext.GetCurrentWindow().ClientMousePosition);
                List<Glyph> glyphs = label.LayoutModel.ProcessModel(label.Model, Shape.LocalBounds);

                // Find caret index by mouse X
                // caretIndex = 0;
                // for (int i = 0; i < glyphs.Count; i++)
                // {
                //     if (localMousePos.x > glyphs[i].Bounds.Right)
                //         caretIndex = i + 1;
                // }
                // selectionStart = -1;
                Invalidate(Invalidation.SurfaceDirty);
            }
        }

        private void OnKeyTyped(char c)
        {
            if (char.IsControl(c) && (c != BACKSPACE && c != PASTE)) return;

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
            else
            {
                text.Insert(_caretIndex, c);
                _caretIndex++;
            }

            UpdateText();
        }

        private void UpdateText()
        {
            if (string.IsNullOrWhiteSpace(text.ToString()))
                label.Model = TextModelFactory.CreateBasic(PlaceholderText, align: new() { HorizontalAlign = Components.Text.Layout.TextAlign.AlignType.Start, VerticalAlign = Components.Text.Layout.TextAlign.AlignType.Middle }, textColor: () => FContext.GetCurrentWindow().WindowThemeManager.CurrentTheme.OnSurface.WithAlpha(100));
            else
                label.Model = TextModelFactory.CreateBasic(text.ToString(), align: new() { HorizontalAlign = Components.Text.Layout.TextAlign.AlignType.Start, VerticalAlign = Components.Text.Layout.TextAlign.AlignType.Middle });

            var bound = label.LayoutModel.GetBoundingRect(label.Model, SKRect.Create(0, 0, 99999, label.Shape.LocalBounds.Height));
            label.Layout.Alignment.SetStaticState(new(1f, 0.5f));
            label.Layout.AlignmentAnchor.SetStaticState(new(1f, 0.5f));

            label.Transform.LocalPosition.SetStaticState(bound.Width > (Shape.LocalBounds.Width - TextPadding.x*3) ? new(-TextPadding.x, 0) : new(0, 0));

            label.Transform.Size.SetStaticState(new(MathF.Max(bound.Width, Shape.LocalBounds.Width) - TextPadding.x, bound.Height - (TextPadding.y)));
        }

        public override void Render(SKCanvas canvas)
        {
            base.Render(canvas);

            canvas.DrawRect(Shape.LocalBounds, new SKPaint() { Color = SKColors.Black });
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
