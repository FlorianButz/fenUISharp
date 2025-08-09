namespace FenUISharp.Logging
{
    public static class FLogger
    {
        public static List<Type> ForbiddenTypes { get; private set; } = new();

        public static void Log<T>(string message)
        {
            if (!ForbiddenTypes.Contains(typeof(T)))
                if (string.IsNullOrWhiteSpace(message)) Log("");
                else Log($"[{typeof(T).Name}] {message}");
        }

        public static void Log(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine();
                return;
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}][LOG] {message}");
            Console.ResetColor();
        }

        public static void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}][WRN] {message}");
            Console.ResetColor();
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}][ERR] {message}");
            Console.ResetColor();
        }
    }
}