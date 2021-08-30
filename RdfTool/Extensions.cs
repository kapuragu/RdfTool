using System.Globalization;

namespace RdfTool
{
    public static class Extensions
    {
        public static float ParseFloatRoundtrip(string text)
        {
            if (text == "-0")
            {
                return -0f;
            }

            return float.Parse(text, CultureInfo.InvariantCulture);
        }
    }
}