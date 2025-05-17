using System.Runtime.InteropServices;
using System.Text;
using FenUISharp.Components.Text;
using FenUISharp.Components.Text.Layout;
using FenUISharp.Components.Text.Model;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.Components
{
    public class FInputField : UIComponent
    {
        private StringBuilder _text;
        public string Text { get => _text.ToString(); }
        private string _displayedText = "";

        public const float TextInsetHorizontal = 8;

        public float CaretBlinkSpeed = 5;
        public float CaretWidth = 2;
        public float CaretHeight = 15f;
        private int _caretIndex = 0;
        private int _offset = 0;

        private FText label;

        private const char BACKSPACE = '\b';
        private const char NEWLINE1 = '\r';
        private const char NEWLINE2 = '\n';
        private const char ESCAPE = '\u001B';
        private const char PASTE = '\u0016'; // Ctrl + V

        private bool _caretBlinkIsOnRightNow = true;

        public FInputField(Window rootWindow, Vector2 pos, float width = 150) : base(rootWindow, pos, new(width, 20))
        {
            WindowRoot.Char += OnCharReceived;
            _text = new();

            label = new(rootWindow, Vector2.Zero, Vector2.Zero, TextModelFactory.CreateBasic(""));
            label.Transform.SetParent(this.Transform);
            label.Transform.StretchVertical = true;
            label.Transform.StretchHorizontal = true;

            label.Transform.MarginVertical = 2;
            label.Transform.MarginHorizontal = TextInsetHorizontal;
        }

        public void OnCharReceived(char c)
        {
            if (char.IsControl(c) && (c != BACKSPACE && c != PASTE)) return;
            // if (c == NEWLINE1 || c == NEWLINE2)
            //     // Optional, enter as submit
            //     return;

            int oldCaretIndex = _caretIndex;

            if (c == PASTE)
            {
                TryGetClipboardText(out string clipboardText);
                _text.Insert(_caretIndex, clipboardText);

                _caretIndex += clipboardText.Length;
            }
            else if (c == BACKSPACE)
            {
                if (_text.Length == 0) return;

                _text.Remove(RMath.Clamp(_caretIndex - 1, 0, _text.Length - 1), 1);
                _caretIndex--;
            }
            else
            {
                _text.Insert(_caretIndex, c);
                _caretIndex++;
            }

            OnTextChanged();
        }

        public void OnTextChanged()
        {
            int disIndex = 0;
            for (int i = 0; i < Text.Length; i++)
            {
                var rect = label.Layout.GetBoundingRect(TextModelFactory.CreateBasic(Text.Substring(0, i)), Transform.LocalBounds);
                rect.Inflate(TextInsetHorizontal, 0);
                disIndex = i;

                if (rect.Width > Transform.LocalBounds.Width) break;
            }

            _displayedText = Text.Substring(0, disIndex);

            label.Model = TextModelFactory.CreateBasic(_displayedText, align: new() { HorizontalAlign = TextAlign.AlignType.Start, VerticalAlign = TextAlign.AlignType.Middle });
            Invalidate();
        }

        protected override void ComponentDestroy()
        {
            WindowRoot.Char -= OnCharReceived;
            base.ComponentDestroy();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            float x = (float)Math.Sin(WindowRoot.Time * CaretBlinkSpeed) / 2 + 0.5f;

            if (x >= 0.9f && _caretBlinkIsOnRightNow == false)
            {
                _caretBlinkIsOnRightNow = true;
                Invalidate();
            }
            else if (x <= 0.1f && _caretBlinkIsOnRightNow == true)
            {
                _caretBlinkIsOnRightNow = false;
                Invalidate();
            }

            // label.Transform.Size = new(500, label.Transform.Size.y);
            // label.Transform.Anchor = new(0, 0.5f);
            // label.Transform.LocalPosition = new(Transform.LocalBounds.Left - 10, 0);
        }

        protected override void DrawToSurface(SKCanvas canvas)
        {
            SkPaint.Color = SKColors.Black;
            canvas.DrawPath(SKSquircle.CreateSquircle(Transform.LocalBounds, 5), SkPaint);

            var rect = label.Layout.GetBoundingRect(TextModelFactory.CreateBasic(_displayedText.Substring(0, RMath.Clamp(_caretIndex, 0, _displayedText.Length))), Transform.LocalBounds);
            rect.Offset(TextInsetHorizontal, 0);

            float x = rect.Right;
            if (_displayedText.Length <= 0) x = TextInsetHorizontal + 2; // Plus a small offset

            // SkPaint.Color = SKColors.Cyan.WithAlpha(75);
            // canvas.DrawRect(rect, SkPaint);

            if (_caretBlinkIsOnRightNow)
            {
                SkPaint.Color = SKColors.White.WithAlpha(100);

                using (var rr = new SKRoundRect(SKRect.Create(new SKPoint(x, Transform.LocalBounds.MidY - CaretHeight / 2), new SKSize(CaretWidth, CaretHeight)), 5))
                    canvas.DrawRoundRect(rr, SkPaint);
            }
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