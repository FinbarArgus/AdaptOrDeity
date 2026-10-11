using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AdaptOrDeity.ArchitectureTests
{
    /// <summary>
    /// Enforces the layering in adapting_plan.md (Architecture): a project may only reference projects in a lower
    /// layer, Sim.* never references Game.*, and nothing outside unity/ references UnityEngine.
    /// Checked on the csproj graph because the compiler drops unused assembly references.
    /// </summary>
    public class DependencyRuleTests
    {
        private static readonly Dictionary<string, int> Layer = new Dictionary<string, int>
        {
            ["AdaptOrDeity.Sim.Core"] = 0,
            ["AdaptOrDeity.Sim.Mesh"] = 1,
            ["AdaptOrDeity.Sim.Modules"] = 1,
            ["AdaptOrDeity.Sim.Transport"] = 2,
            ["AdaptOrDeity.Sim.Physiology"] = 3,
            ["AdaptOrDeity.Sim.World"] = 3,
            ["AdaptOrDeity.Game.Rules"] = 4,
            ["AdaptOrDeity.Game.Content"] = 5,
            ["AdaptOrDeity.Game.Persistence"] = 5,
            ["AdaptOrDeity.Game.Session"] = 6,
            ["AdaptOrDeity.Headless"] = 7,
            ["AdaptOrDeity.Native"] = 7,
        };

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "AdaptOrDeity.sln")))
                dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("AdaptOrDeity.sln not found above test directory");
        }

        private static IEnumerable<string> SrcProjects() =>
            Directory.GetFiles(Path.Combine(RepoRoot(), "src"), "*.csproj", SearchOption.AllDirectories);

        private static IEnumerable<string> References(string csproj, string itemType) =>
            XDocument.Load(csproj).Descendants(itemType)
                .Select(e => (string?)e.Attribute("Include"))
                .Where(s => s != null)
                .Select(s => s!);

        [Test]
        public void EverySrcProjectHasALayer()
        {
            var unknown = SrcProjects().Select(Path.GetFileNameWithoutExtension).Where(n => !Layer.ContainsKey(n!)).ToList();
            Assert.That(unknown, Is.Empty, "add new projects to the Layer table (and the plan)");
        }

        [Test]
        public void ProjectsOnlyReferenceLowerLayers()
        {
            var violations = new List<string>();
            foreach (var csproj in SrcProjects())
            {
                string from = Path.GetFileNameWithoutExtension(csproj)!;
                foreach (var include in References(csproj, "ProjectReference"))
                {
                    string to = Path.GetFileNameWithoutExtension(include.Replace('\\', '/'));
                    bool simToGame = from.StartsWith("AdaptOrDeity.Sim.") && to.StartsWith("AdaptOrDeity.Game.");
                    if (simToGame || !Layer.TryGetValue(to, out int toLayer) || toLayer >= Layer[from])
                        violations.Add($"{from} -> {to}");
                }
            }
            Assert.That(violations, Is.Empty);
        }

        [Test]
        public void NoSrcProjectReferencesUnity()
        {
            var violations = SrcProjects()
                .SelectMany(p => References(p, "Reference").Concat(References(p, "PackageReference"))
                    .Where(r => r.IndexOf("Unity", StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(r => $"{Path.GetFileName(p)}: {r}"))
                .ToList();
            Assert.That(violations, Is.Empty);
        }

        [Test]
        public void SimAssembliesDoNotLoadUnityAtRuntime()
        {
            var assemblies = new[]
            {
                typeof(Sim.Core.AssemblyMarker).Assembly, typeof(Sim.Mesh.AssemblyMarker).Assembly,
                typeof(Sim.Modules.AssemblyMarker).Assembly,
                typeof(Sim.Transport.AssemblyMarker).Assembly, typeof(Sim.Physiology.AssemblyMarker).Assembly,
                typeof(Sim.World.AssemblyMarker).Assembly, typeof(Game.Rules.AssemblyMarker).Assembly,
                typeof(Game.Content.AssemblyMarker).Assembly, typeof(Game.Persistence.AssemblyMarker).Assembly,
                typeof(Game.Session.AssemblyMarker).Assembly,
            };
            var unityRefs = assemblies
                .SelectMany(a => a.GetReferencedAssemblies().Select(r => (a.GetName().Name, r.Name)))
                .Where(x => x.Item2 != null && x.Item2.StartsWith("Unity", StringComparison.Ordinal))
                .ToList();
            Assert.That(unityRefs, Is.Empty);
        }
    }
}
