using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using FenUISharp.Logging;
using FenUISharp.Mathematics;
using SkiaSharp;

namespace FenUISharp.AnimatedVectors
{
    public class AnimatedVectorParser
    {
        public static AnimatedVector ParseFAV(System.IO.Stream inputStream)
        {
            if (inputStream == null) throw new Exception("Cannot parse null stream.");

            using StreamReader reader = new StreamReader(inputStream);
            return ParseFAV(reader.ReadToEnd());
        }

        public static AnimatedVector ParseFAV(string rawFavXML)
        {
            XDocument doc = XDocument.Parse(rawFavXML);
            XElement fav = doc?.Root ?? throw new Exception("Input FAV is invalid");
            XElement symbol = fav?.Element("symbol") ?? throw new Exception("Input FAV is invalid");
            XElement animations = fav?.Element("animations") ?? throw new Exception("Input FAV is invalid");

            AnimatedVector favParsed = new();

            var viewBox = (symbol?.Attribute(XName.Get("viewBox"))?.Value ?? "0 0 0 0").Split(' ');
            favParsed.ViewBox = new SKRect(
                float.Parse(viewBox[0], CultureInfo.InvariantCulture),
                float.Parse(viewBox[1], CultureInfo.InvariantCulture),
                float.Parse(viewBox[2], CultureInfo.InvariantCulture),
                float.Parse(viewBox[3], CultureInfo.InvariantCulture));

            int.TryParse(symbol?.Attribute(XName.Get("extend-bounds"))?.Value, CultureInfo.InvariantCulture, out int extendBounds);
            favParsed.ExtendBounds = extendBounds;

            int pathCount = 0;

            switch (symbol?.Attribute(XName.Get("stroke-linecap"))?.Value)
            {
                case "round":
                    favParsed.LineCap = SKStrokeCap.Round;
                    break;
                case "butt":
                    favParsed.LineCap = SKStrokeCap.Butt;
                    break;
                case "square":
                    favParsed.LineCap = SKStrokeCap.Square;
                    break;
                default:
                    favParsed.LineCap = SKStrokeCap.Round;
                    break;
            }

            switch (symbol?.Attribute(XName.Get("stroke-linejoin"))?.Value)
            {
                case "round":
                    favParsed.LineJoin = SKStrokeJoin.Round;
                    break;
                case "bevel":
                    favParsed.LineJoin = SKStrokeJoin.Bevel;
                    break;
                case "miter":
                    favParsed.LineJoin = SKStrokeJoin.Miter;
                    break;
                default:
                    favParsed.LineJoin = SKStrokeJoin.Round;
                    break;
            }

            float.TryParse(symbol?.Attribute(XName.Get("stroke-width"))?.Value ?? "0", CultureInfo.InvariantCulture, out float strokeWidth);
            string rawStroke = symbol?.Attribute("stroke")?.Value ?? "#FFFFFF00";
            string rawFill = symbol?.Attribute("fill")?.Value ?? "#FFFFFF00";

            SKColor.TryParse(ConvertSvgHexToSkia(rawStroke), out SKColor stroke);
            SKColor.TryParse(ConvertSvgHexToSkia(rawFill), out SKColor fill);

            foreach (var element in symbol.Elements())
            {
                SKPath parsedPath = null;

                switch (element.Name.LocalName)
                {
                    case "path":
                        var pathData = element.Attribute("d")?.Value ?? "";
                        parsedPath = ParsePath(ParsePathData(pathData));
                        break;

                    case "rect":
                        float.TryParse(element.Attribute("x")?.Value ?? "0", CultureInfo.InvariantCulture, out float x);
                        float.TryParse(element.Attribute("y")?.Value ?? "0", CultureInfo.InvariantCulture, out float y);
                        float.TryParse(element.Attribute("width")?.Value ?? "0", CultureInfo.InvariantCulture, out float width);
                        float.TryParse(element.Attribute("height")?.Value ?? "0", CultureInfo.InvariantCulture, out float height);
                        float.TryParse(element.Attribute("rx")?.Value ?? "0", CultureInfo.InvariantCulture, out float rx);
                        float.TryParse(element.Attribute("ry")?.Value ?? rx.ToString(), CultureInfo.InvariantCulture, out float ry);

                        parsedPath = new SKPath();
                        parsedPath.AddRoundRect(SKRect.Create(x, y, width, height), rx, ry);
                        break;

                    case "circle":
                        float.TryParse(element.Attribute("cx")?.Value ?? "0", CultureInfo.InvariantCulture, out float cx);
                        float.TryParse(element.Attribute("cy")?.Value ?? "0", CultureInfo.InvariantCulture, out float cy);
                        float.TryParse(element.Attribute("r")?.Value ?? "0", CultureInfo.InvariantCulture, out float radius);

                        if(radius == 0)
                            float.TryParse(element.Attribute("rx")?.Value ?? "0", CultureInfo.InvariantCulture, out radius);

                        parsedPath = new SKPath();
                        parsedPath.AddCircle(cx, cy, radius);
                        break;

                    case "ellipse":
                        float.TryParse(element.Attribute("cx")?.Value ?? "0", CultureInfo.InvariantCulture, out float ex);
                        float.TryParse(element.Attribute("cy")?.Value ?? "0", CultureInfo.InvariantCulture, out float ey);
                        float.TryParse(element.Attribute("rx")?.Value ?? "0", CultureInfo.InvariantCulture, out float erx);
                        float.TryParse(element.Attribute("ry")?.Value ?? "0", CultureInfo.InvariantCulture, out float ery);

                        parsedPath = new SKPath();
                        parsedPath.AddOval(SKRect.Create(ex - erx, ey - ery, erx * 2, ery * 2));
                        break;
                }

                if (parsedPath != null)
                {
                    favParsed.Paths.Add(new()
                    {
                        Fill = fill,
                        Stroke = stroke,
                        SKPath = parsedPath,
                        StrokeWidth = strokeWidth
                    });
                    pathCount++;
                }
            }

            if (animations != null && animations.Elements("animation") != null)
            {
                foreach (var animation in animations.Elements("animation"))
                {
                    Func<Func<float, float>>? createEasing = null;

                    // Get affected paths
                    string[] affectedPathsString = (animation.Attribute("affected-paths")?.Value ?? "").Split(' ');
                    int[] affectedPaths = new int[affectedPathsString.Length];

                    // Check for special case
                    if ((animation.Attribute("affected-paths")?.Value ?? "").Contains("*"))
                    {
                        affectedPaths = new int[pathCount];
                        for (int i = 0; i < affectedPaths.Length; i++) affectedPaths[i] = i;
                    }
                    // Convert to int array
                    else for (int i = 0; i < affectedPathsString.Length; i++) affectedPaths[i] = int.Parse(affectedPathsString[i], CultureInfo.InvariantCulture);

                    // Get animation duration
                    float.TryParse(animation.Attribute(XName.Get("duration"))?.Value ?? "", CultureInfo.InvariantCulture, out float duration);
                    // Get extended duration
                    float.TryParse(animation.Attribute("extend-duration")?.Value ?? "", CultureInfo.InvariantCulture, out float extendDuration);

                    // Get extra properties
                    bool useObjAnchor = (animation.Attribute("use-object-anchor")?.Value ?? "") == "true";
                    bool useObjSizeTranslation = (animation.Attribute("use-object-size-translation")?.Value ?? "") == "true";
                    bool dontResetValues = (animation.Attribute("dont-reset")?.Value ?? "") == "true";
                    bool perKeyframeEase = (animation.Attribute("per-keyframe-ease")?.Value ?? "") == "true";

                    string easingRaw = (animation.Attribute("easing")?.Value ?? "");
                    // Test for different special easing cases
                    // Cubic easing
                    if (easingRaw.StartsWith("cubic-bezier(") && easingRaw.EndsWith(")"))
                    {
                        // Remove unecessary parts
                        easingRaw = easingRaw.Replace("cubic-bezier(", "");
                        easingRaw = easingRaw.Replace(")", "");
                        // Extract values
                        var easingValues = easingRaw.Split(',');

                        // Parse values and create easing function
                        try
                        {
                            createEasing = () => BezierEasing.CreateEasing(
                                float.Parse(easingValues[0], CultureInfo.InvariantCulture),
                                float.Parse(easingValues[1], CultureInfo.InvariantCulture),
                                float.Parse(easingValues[2], CultureInfo.InvariantCulture),
                                float.Parse(easingValues[3], CultureInfo.InvariantCulture));
                        }
                        catch (Exception e) { createEasing = null; FLogger.Error("Unexpected values while trying to parse FAV animation easing"); }
                    }
                    else if (easingRaw.StartsWith("spring(") && easingRaw.EndsWith(")"))
                    {
                        // Remove unecessary parts
                        easingRaw = easingRaw.Replace("spring(", "");
                        easingRaw = easingRaw.Replace(")", "");
                        // Extract values
                        var easingValues = easingRaw.Split(',');

                        // Parse values and create spring
                        try
                        {
                            createEasing = () =>
                            {
                                Spring spring = new Spring(
                                    /* Springs are non plottable. To avoid it clipping too much out of the animation duration, divide speed through duration */
                                    float.Parse(easingValues[0], CultureInfo.InvariantCulture) / duration,
                                    float.Parse(easingValues[1], CultureInfo.InvariantCulture));
                                return (x) => spring.Update(FContext.DeltaTime, Vector2.One * x).x;
                            };
                        }
                        catch (Exception e) { createEasing = null; FLogger.Error("Unexpected values while trying to parse FAV animation easing"); }
                    }
                    else if (easingRaw.StartsWith("snap-spring(") && easingRaw.EndsWith(")"))
                    {
                        // Remove unecessary parts
                        easingRaw = easingRaw.Replace("snap-spring(", "");
                        easingRaw = easingRaw.Replace(")", "");
                        // Extract values
                        var easingValues = easingRaw.Split(',');

                        // Parse values and create spring
                        try
                        {
                            createEasing = () =>
                            {
                                Spring spring = new Spring(
                                    /* Springs are non plottable. To avoid it clipping too much out of the animation duration, divide speed through duration */
                                    float.Parse(easingValues[0], CultureInfo.InvariantCulture) / duration,
                                    float.Parse(easingValues[1], CultureInfo.InvariantCulture));
                                return (x) => { return spring.Update(FContext.DeltaTime, Vector2.One).x; };
                            };
                        }
                        catch (Exception e) { createEasing = null; FLogger.Error("Unexpected values while trying to parse FAV animation easing"); }
                    }
                    // Other common easing types
                    else if (easingRaw.Equals("linear"))
                        createEasing = () => (x) => x;
                    else if (easingRaw.Equals("ease"))
                        createEasing = () => BezierEasing.Ease;
                    else if (easingRaw.Equals("ease-in"))
                        createEasing = () => BezierEasing.EaseIn;
                    else if (easingRaw.Equals("ease-out"))
                        createEasing = () => BezierEasing.EaseOut;
                    else if (easingRaw.Equals("ease-in-out"))
                        createEasing = () => BezierEasing.EaseInOut;
                    else if (easingRaw.Equals("snap"))
                        createEasing = () => (x) => x >= 0.5f ? 1 : 0;

                    string animationID = animation.Attribute("id")?.Value ?? throw new Exception("Every animation must specify an ID");

                    // Get keyframes

                    List<AVKeyframe> keyframes = new();
                    foreach (var keyframe in animation.Elements("keyframe"))
                    {
                        if (keyframe == null) continue;

                        float.TryParse(keyframe.Attribute("time")?.Value ?? "0", CultureInfo.InvariantCulture, out float keyframeTime);

                        var attributes = new List<(string id, object value)>();
                        string[] keys = { "translate-x", "translate-y", "scale-x", "scale-y", "anchor-x", "anchor-y", "rotate", "opacity", "blur-radius", "stroke-trace" };

                        foreach (var key in keys)
                        {
                            var element = keyframe.Element(key);
                            if (element != null)
                                if (float.TryParse(element.Attribute("value")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                                    attributes.Add((key, val));
                        }

                        keyframes.Add(new() { time = keyframeTime, attributes = attributes });
                    }

                    if (!keyframes.Any(x => x.time == 1)) keyframes.Add(new() { time = 1f });
                    if (!keyframes.Any(x => x.time == 0)) keyframes.Add(new() { time = 0f });

                    favParsed.Animations.Add((animationID, new AVAnimation()
                    {
                        AffectedPathIDs = affectedPaths,
                        Duration = duration,
                        Keyframes = keyframes,
                        UseObjectAnchor = useObjAnchor,
                        UseObjectSizeTranslation = useObjSizeTranslation,
                        DontResetValues = dontResetValues,
                        ExtendDuration = extendDuration,
                        PerKeyframeEase = perKeyframeEase,
                        CreateEasing = createEasing ?? (() => (x) => x)
                    }));
                }
            }

            return favParsed;
        }

        private static string ConvertSvgHexToSkia(string svgColor)
        {
            if (string.IsNullOrWhiteSpace(svgColor) || !svgColor.StartsWith("#"))
                return svgColor; // Skip named colors or empty strings

            svgColor = svgColor.Trim();

            if (svgColor.Length == 7) // #RRGGBB
            {
                return "#FF" + svgColor.Substring(1); // Add full alpha
            }
            else if (svgColor.Length == 9) // #RRGGBBAA
            {
                string rrggbb = svgColor.Substring(1, 6);
                string aa = svgColor.Substring(7, 2);
                return "#" + aa + rrggbb; // Make SkiaSharp compatible
            }

            return svgColor;
        }

        private static SKPath ParsePath(List<(char command, List<float> arguments)> data)
        {
            SKPath parsedPath = new();

            SKPoint currentPoint = new(0, 0);
            SKPoint subpathStart = new(0, 0);
            SKPoint lastControlPoint = new(0, 0);
            SKPoint lastQuadControlPoint = new(0, 0);

            bool hasLastControlPoint = false;
            bool hasLastQuadControlPoint = false;

            foreach (var pathCommand in data)
            {
                char cmd = pathCommand.command;
                var args = pathCommand.arguments;

                if (char.ToUpper(pathCommand.command) == 'Z')
                {
                    parsedPath.Close();
                    continue;
                }

                int i = 0;
                while (i < args.Count)
                {
                    switch (cmd)
                    {
                        case 'M':
                            currentPoint = new SKPoint(args[i], args[i + 1]);
                            parsedPath.MoveTo(currentPoint);
                            subpathStart = currentPoint;
                            i += 2;
                            cmd = 'L';
                            break;

                        case 'm':
                            currentPoint = new SKPoint(currentPoint.X + args[i], currentPoint.Y + args[i + 1]);
                            parsedPath.MoveTo(currentPoint);
                            subpathStart = currentPoint;
                            i += 2;
                            cmd = 'l';
                            break;

                        case 'L':
                            currentPoint = new SKPoint(args[i], args[i + 1]);
                            parsedPath.LineTo(currentPoint);
                            i += 2;
                            break;

                        case 'l':
                            currentPoint = new SKPoint(currentPoint.X + args[i], currentPoint.Y + args[i + 1]);
                            parsedPath.LineTo(currentPoint);
                            i += 2;
                            break;

                        case 'H':
                            currentPoint = new SKPoint(args[i], currentPoint.Y);
                            parsedPath.LineTo(currentPoint);
                            i++;
                            break;

                        case 'h':
                            currentPoint = new SKPoint(currentPoint.X + args[i], currentPoint.Y);
                            parsedPath.LineTo(currentPoint);
                            i++;
                            break;

                        case 'V':
                            currentPoint = new SKPoint(currentPoint.X, args[i]);
                            parsedPath.LineTo(currentPoint);
                            i++;
                            break;

                        case 'v':
                            currentPoint = new SKPoint(currentPoint.X, currentPoint.Y + args[i]);
                            parsedPath.LineTo(currentPoint);
                            i++;
                            break;

                        case 'C':
                            {
                                var cp1 = new SKPoint(args[i], args[i + 1]);
                                var cp2 = new SKPoint(args[i + 2], args[i + 3]);
                                var end = new SKPoint(args[i + 4], args[i + 5]);
                                parsedPath.CubicTo(cp1, cp2, end);
                                lastControlPoint = cp2;
                                hasLastControlPoint = true;
                                currentPoint = end;
                                hasLastQuadControlPoint = false;
                                i += 6;
                                break;
                            }

                        case 'c':
                            {
                                var cp1 = new SKPoint(currentPoint.X + args[i], currentPoint.Y + args[i + 1]);
                                var cp2 = new SKPoint(currentPoint.X + args[i + 2], currentPoint.Y + args[i + 3]);
                                var end = new SKPoint(currentPoint.X + args[i + 4], currentPoint.Y + args[i + 5]);
                                parsedPath.CubicTo(cp1, cp2, end);
                                lastControlPoint = cp2;
                                hasLastControlPoint = true;
                                currentPoint = end;
                                hasLastQuadControlPoint = false;
                                i += 6;
                                break;
                            }

                        case 'S':
                            {
                                var cp1 = hasLastControlPoint
                                    ? new SKPoint(2 * currentPoint.X - lastControlPoint.X, 2 * currentPoint.Y - lastControlPoint.Y)
                                    : currentPoint;
                                var cp2 = new SKPoint(args[i], args[i + 1]);
                                var end = new SKPoint(args[i + 2], args[i + 3]);
                                parsedPath.CubicTo(cp1, cp2, end);
                                lastControlPoint = cp2;
                                hasLastControlPoint = true;
                                currentPoint = end;
                                hasLastQuadControlPoint = false;
                                i += 4;
                                break;
                            }

                        case 's':
                            {
                                var cp1 = hasLastControlPoint
                                    ? new SKPoint(2 * currentPoint.X - lastControlPoint.X, 2 * currentPoint.Y - lastControlPoint.Y)
                                    : currentPoint;
                                var cp2 = new SKPoint(currentPoint.X + args[i], currentPoint.Y + args[i + 1]);
                                var end = new SKPoint(currentPoint.X + args[i + 2], currentPoint.Y + args[i + 3]);
                                parsedPath.CubicTo(cp1, cp2, end);
                                lastControlPoint = cp2;
                                hasLastControlPoint = true;
                                currentPoint = end;
                                hasLastQuadControlPoint = false;
                                i += 4;
                                break;
                            }

                        case 'Q':
                            {
                                var cp = new SKPoint(args[i], args[i + 1]);
                                var end = new SKPoint(args[i + 2], args[i + 3]);
                                parsedPath.QuadTo(cp, end);
                                lastQuadControlPoint = cp;
                                hasLastQuadControlPoint = true;
                                hasLastControlPoint = false;
                                currentPoint = end;
                                i += 4;
                                break;
                            }

                        case 'q':
                            {
                                var cp = new SKPoint(currentPoint.X + args[i], currentPoint.Y + args[i + 1]);
                                var end = new SKPoint(currentPoint.X + args[i + 2], currentPoint.Y + args[i + 3]);
                                parsedPath.QuadTo(cp, end);
                                lastQuadControlPoint = cp;
                                hasLastQuadControlPoint = true;
                                hasLastControlPoint = false;
                                currentPoint = end;
                                i += 4;
                                break;
                            }

                        case 'T':
                            {
                                var cp = hasLastQuadControlPoint
                                    ? new SKPoint(2 * currentPoint.X - lastQuadControlPoint.X, 2 * currentPoint.Y - lastQuadControlPoint.Y)
                                    : currentPoint;
                                var end = new SKPoint(args[i], args[i + 1]);
                                parsedPath.QuadTo(cp, end);
                                lastQuadControlPoint = cp;
                                hasLastQuadControlPoint = true;
                                hasLastControlPoint = false;
                                currentPoint = end;
                                i += 2;
                                break;
                            }

                        case 't':
                            {
                                var cp = hasLastQuadControlPoint
                                    ? new SKPoint(2 * currentPoint.X - lastQuadControlPoint.X, 2 * currentPoint.Y - lastQuadControlPoint.Y)
                                    : currentPoint;
                                var end = new SKPoint(currentPoint.X + args[i], currentPoint.Y + args[i + 1]);
                                parsedPath.QuadTo(cp, end);
                                lastQuadControlPoint = cp;
                                hasLastQuadControlPoint = true;
                                hasLastControlPoint = false;
                                currentPoint = end;
                                i += 2;
                                break;
                            }

                        case 'A':
                            {
                                float rx = args[i];
                                float ry = args[i + 1];
                                float xAxisRotation = args[i + 2];
                                bool largeArcFlag = args[i + 3] != 0;
                                bool sweepFlag = args[i + 4] != 0;
                                SKPoint endPoint = new(args[i + 5], args[i + 6]);

                                parsedPath.ArcTo(
                                    new SKPoint(rx, ry),
                                    xAxisRotation,
                                    largeArcFlag ? SKPathArcSize.Large : SKPathArcSize.Small,
                                    sweepFlag ? SKPathDirection.Clockwise : SKPathDirection.CounterClockwise,
                                    endPoint
                                );

                                currentPoint = endPoint;
                                hasLastControlPoint = false;
                                hasLastQuadControlPoint = false;
                                i += 7;
                                break;
                            }

                        case 'a':
                            {
                                float rx = args[i];
                                float ry = args[i + 1];
                                float xAxisRotation = args[i + 2];
                                bool largeArcFlag = args[i + 3] != 0;
                                bool sweepFlag = args[i + 4] != 0;
                                SKPoint endPoint = new(currentPoint.X + args[i + 5], currentPoint.Y + args[i + 6]);

                                parsedPath.ArcTo(
                                    new SKPoint(rx, ry),
                                    xAxisRotation,
                                    largeArcFlag ? SKPathArcSize.Large : SKPathArcSize.Small,
                                    sweepFlag ? SKPathDirection.Clockwise : SKPathDirection.CounterClockwise,
                                    endPoint
                                );

                                currentPoint = endPoint;
                                hasLastControlPoint = false;
                                hasLastQuadControlPoint = false;
                                i += 7;
                                break;
                            }

                        default:
                            throw new NotSupportedException($"SVG command '{cmd}' is not supported.");
                    }
                }
            }

            return parsedPath;
        }

        public static List<(char command, List<float> arguments)> ParsePathData(string path)
        {
            var result = new List<(char command, List<float> arguments)>();

            var regex = new Regex(@"([MmZzLlHhVvCcSsQqTtAa])|(-?\d*\.?\d+(?:e[-+]?\d+)?)", RegexOptions.IgnoreCase);
            var matches = regex.Matches(path);

            char currentCommand = '\0';
            var currentParams = new List<float>();

            foreach (Match match in matches)
            {
                if (match.Groups[1].Success)
                {
                    if (currentCommand != '\0')
                        result.Add((currentCommand, new List<float>(currentParams)));

                    currentCommand = match.Groups[1].Value[0];
                    currentParams.Clear();
                }
                else if (match.Groups[2].Success)
                {
                    currentParams.Add(float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
                }
            }

            if (currentCommand != '\0')
                result.Add((currentCommand, currentParams));

            return result;
        }
    }
}