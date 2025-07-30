using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using SkiaSharp;

namespace FenUISharp.AnimatedVectors
{
    public class AnimatedVectorParser
    {
        public static AnimatedVector ParseFAV(string fav)
        {
            var doc = new XmlDocument();
            doc.LoadXml(fav);

            var svgNode = doc.SelectSingleNode("//svg");
            var pathNodes = doc.SelectNodes("./path");

            if (pathNodes == null) throw new Exception("FAV must contain at least one path node");

            foreach (XmlNode pathNode in pathNodes)
            {
                string pathData = pathNode?.Attributes?["d"]?.Value ?? "";
                SKPath parsedPath = new();

                pathData.Trim();

                var regex = new Regex(@"([MmLlHhVvCcSsQqTtAaZz])\s*([^MmLlHhVvCcSsQqTtAaZz]*)", RegexOptions.IgnoreCase);
                var matches = regex.Matches(pathData);

                foreach (Match match in matches)
                {
                    char command = match.Groups[1].Value[0];
                    string parameters = match.Groups[2].Value.Trim();

                    if (string.IsNullOrEmpty(parameters) && command != 'Z' && command != 'z')
                        continue;

                    var coords = ParseCoordinates(parameters);

                    Console.WriteLine($"{command} ");
                    coords.ForEach(x => Console.Write(x));
                    Console.WriteLine();
                }

            }

            AnimatedVector favParsed = new();

            return favParsed;
        }

        private static List<double> ParseCoordinates(string coordinates)
        {
            var coords = new List<double>();

            if (string.IsNullOrWhiteSpace(coordinates))
                return coords;

            // Replace commas with spaces and handle multiple spaces
            coordinates = Regex.Replace(coordinates, @"[,\s]+", " ").Trim();

            // Handle negative numbers that might be concatenated
            coordinates = Regex.Replace(coordinates, @"(?<!^|[\s,])-", " -");

            var parts = coordinates.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                if (double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    coords.Add(value);
                }
            }

            return coords;
        }
    }
}