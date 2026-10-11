using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// Text the player reads (content/timeline.json: features' effects and explanations, level summaries) is checked by a
    /// human, who sets its "reviewed" flag (adapting_plan.md, Rules for content). These tests fail until every entry is.
    /// </summary>
    public class ContentReviewTests
    {
        private static JsonDocument Timeline()
        {
            var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "global.json")))
                d = d.Parent;
            return JsonDocument.Parse(File.ReadAllText(Path.Combine(d!.FullName, "content", "timeline.json")));
        }

        [Test]
        public void EveryFeatureHasAnExplanation()
        {
            using var doc = Timeline();
            foreach (var f in doc.RootElement.GetProperty("features").EnumerateArray())
                Assert.That(f.TryGetProperty("explanation", out var e) ? e.GetString() : "", Is.Not.Empty,
                    f.GetProperty("id").GetString());
        }

        [Test]
        public void EveryFeatureTextIsReviewed()
        {
            using var doc = Timeline();
            var unreviewed = doc.RootElement.GetProperty("features").EnumerateArray()
                .Where(f => !f.TryGetProperty("reviewed", out var r) || !r.GetBoolean())
                .Select(f => f.GetProperty("id").GetString()).ToList();
            Assert.That(unreviewed, Is.Empty, "content/timeline.json features a human has not reviewed (set \"reviewed\": true)");
        }

        [Test]
        public void EveryLevelSummaryIsReviewed()
        {
            using var doc = Timeline();
            var unreviewed = doc.RootElement.GetProperty("levels").EnumerateArray()
                .Where(l => !l.TryGetProperty("reviewed", out var r) || !r.GetBoolean())
                .Select(l => l.GetProperty("level").GetInt32()).ToList();
            Assert.That(unreviewed, Is.Empty, "content/timeline.json levels a human has not reviewed (set \"reviewed\": true)");
        }
    }
}
