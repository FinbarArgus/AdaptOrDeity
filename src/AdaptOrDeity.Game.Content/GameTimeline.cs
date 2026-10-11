using System;
using System.Collections.Generic;
using System.Globalization;

namespace AdaptOrDeity.Game.Content
{
    /// <summary>What appears in the world per second (content/timeline.json levels[].arrivals).</summary>
    public readonly struct Arrivals
    {
        public Arrivals(double foodPerSecond, double carriedShare)
        {
            FoodPerSecond = foodPerSecond;
            CarriedShare = carriedShare;
        }

        /// <summary>Food packets arriving per second [1/s], whatever is already in the field.</summary>
        public double FoodPerSecond { get; }

        /// <summary>Share of them brought by other creatures; the rest lie on the ground and come into view.</summary>
        public double CarriedShare { get; }
    }

    /// <summary>The level timeline (content/timeline.json, embedded): what each level's world supplies.</summary>
    public sealed class GameTimeline
    {
        private static GameTimeline? _default;
        private readonly Dictionary<int, Dictionary<string, object?>> _levels = new Dictionary<int, Dictionary<string, object?>>();
        private readonly Dictionary<string, string> _explanations = new Dictionary<string, string>();

        private GameTimeline(string json)
        {
            var root = (Dictionary<string, object?>)Json.Parse(json)!;
            foreach (var item in (List<object?>)root["levels"]!)
            {
                var level = (Dictionary<string, object?>)item!;
                _levels[(int)Number(level["level"])] = level;
            }
            foreach (var item in (List<object?>)root["features"]!)
            {
                var feature = (Dictionary<string, object?>)item!;
                _explanations[(string)feature["id"]!] = feature.TryGetValue("explanation", out var e) ? (string)e! : "";
            }
        }

        /// <summary>The embedded content/timeline.json.</summary>
        public static GameTimeline Default => _default ??= new GameTimeline(ContentFiles.Read("timeline.json"));

        /// <summary>The arrivals at a level, in a region if it overrides them.</summary>
        public Arrivals ArrivalsAt(int level, string? region = null)
        {
            if (!_levels.TryGetValue(level, out var entry))
                throw new ArgumentOutOfRangeException(nameof(level), $"content/timeline.json has no level {level}");
            var arrivals = (Dictionary<string, object?>)entry["arrivals"]!;
            if (region != null && entry.TryGetValue("regions", out var regions) && regions is List<object?> list)
                foreach (var r in list)
                    if (r is Dictionary<string, object?> d && Equals(d["id"], region) && d.TryGetValue("arrivals", out var own))
                        arrivals = Merge(arrivals, (Dictionary<string, object?>)own!);
            return new Arrivals(Number(arrivals["food_per_s"]), Number(arrivals["carried_share"]));
        }

        /// <summary>The feature's full explanation for the player ("" if it has none).</summary>
        public string Explanation(string feature) => _explanations.TryGetValue(feature, out var e) ? e : "";

        private static Dictionary<string, object?> Merge(Dictionary<string, object?> level, Dictionary<string, object?> region)
        {
            var merged = new Dictionary<string, object?>(level);
            foreach (var pair in region)
                merged[pair.Key] = pair.Value;
            return merged;
        }

        private static double Number(object? value) => Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }
}
