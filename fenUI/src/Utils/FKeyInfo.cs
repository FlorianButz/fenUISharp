namespace FenUISharp
{
    public static class FKeyInfo
    {
        public static string GetKeyName(int vkCode)
        {
            return Enum.IsDefined(typeof(ConsoleKey), vkCode) ? ((ConsoleKey)vkCode).ToString() : $"VK_{vkCode}";
        }
    }
}