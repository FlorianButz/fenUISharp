using System.Text;

namespace FenUISharp
{
    internal static class ConsoleCapture
    {
        private static StringWriter logWriter = new StringWriter();
        private static TextWriter originalConsoleOut = Console.Out;

        public static void StartCapture()
        {
            var multiWriter = new MultiWriter(originalConsoleOut, logWriter);
            Console.SetOut(multiWriter);
            Console.SetError(Console.Out);
        }

        public static string GetLog() => logWriter.ToString();

        public static void SaveErrorLogToFile(string path)
        {
            string text = $"======== fenUI ERROR LOG: {DateTime.Now.ToLongDateString()}, {DateTime.Now.ToLongTimeString()} ========" + "\r\n\r\n" + logWriter.ToString();
            File.WriteAllText(path, text);
        }

        private class MultiWriter : TextWriter
        {
            private readonly TextWriter[] writers;
            public MultiWriter(params TextWriter[] writers) => this.writers = writers;

            public override Encoding Encoding => Encoding.UTF8;
            public override void Write(char value)
            {
                foreach (var w in writers) w.Write(value);
            }
            public override void Write(string value)
            {
                foreach (var w in writers) w.Write(value);
            }
            public override void WriteLine(string value)
            {
                foreach (var w in writers) w.WriteLine(value);
            }
        }
    }

}