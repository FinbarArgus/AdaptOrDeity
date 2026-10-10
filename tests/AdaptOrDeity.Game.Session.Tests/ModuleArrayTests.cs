using System;
using System.IO;
using System.Linq;
using AdaptOrDeity.Game.Content;
using NUnit.Framework;
using static AdaptOrDeity.Game.Session.Tests.FermenterSessionTests;

namespace AdaptOrDeity.Game.Session.Tests
{
    /// <summary>
    /// The creature as a module array (libcuflynx and PhLynx's format). tools/interop/test_module_array_tools.py checks
    /// that those tools and libcellml build and read the arrays the game writes.
    /// </summary>
    public class ModuleArrayTests
    {
        private static string TempDir() => Path.Combine(Path.GetTempPath(), "aod-module-array-" + Guid.NewGuid().ToString("N"));

        [Test]
        public void EachLivingCellIsAnInstanceOfTheAssembledModuleWithThePlayersParameters()
        {
            var loop = NewLoop(out var world);
            loop.Enqueue(SimCommands.SetLevel(2));
            loop.ApplyCommands();
            FinishDaughter(world, 0);
            loop.Enqueue(SimCommands.Divide(0));
            int trait = loop.Schema.Traits.ToList().FindIndex(t => t.Id == "H_ATPase_expression");
            loop.Enqueue(SimCommands.SetTrait(0, trait, 2.0));
            loop.ApplyCommands();

            var array = world.ToModuleArray();

            Assert.That(array.Records.Select(r => r.Name), Is.EqualTo(new[] { "cell_0", "cell_1" }));
            foreach (var r in array.Records)
            {
                Assert.That((r.ModuleType, r.ModuleSubtype, r.Instance), Is.EqualTo(("generic_adaptive_cell", "fermenter", "default")));
                Assert.That(r.Inputs, Is.Empty);
                Assert.That(r.Outputs, Is.Empty);
            }
            var player = world.Cell(0).PlayerParameters;
            Assert.That(array.Parameters.Select(p => p.Name),
                Is.EqualTo(player.Select(p => p + "_cell_0").Concat(player.Select(p => p + "_cell_1"))));
            var h = array.Parameters.Single(p => p.Name == "m_target_mod_pool_H_ATPase_cell_1");
            Assert.That((h.Value, h.Units), Is.EqualTo((2.0, "dimensionless")));
        }

        [Test]
        public void WritingThenReadingGivesTheSameModel()
        {
            var loop = NewLoop(out var world);
            loop.Enqueue(SimCommands.SetControl(0, FermenterWorld.RepairControl, 0.3));
            loop.ApplyCommands();
            var array = world.ToModuleArray();
            array.Parameters.Add(new ParameterRow("odd_name", "per_s", 1.0 / 3.0, "a \"quoted\", reference"));
            string dir = TempDir();
            try
            {
                array.Write(dir, "creature");
                var read = ModuleArray.Read(dir, "creature");
                Assert.That(read.ToJson(), Is.EqualTo(array.ToJson()));
                Assert.That(read.ToCsv(), Is.EqualTo(array.ToCsv()));
                Assert.That(read.Parameters.Last().Value, Is.EqualTo(1.0 / 3.0));
                Assert.That(read.Parameters.Last().Reference, Is.EqualTo("a \"quoted\", reference"));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Test]
        public void ExportCommandWritesTheFilesOrIsRejected()
        {
            var loop = NewLoop(out _);
            string dir = TempDir();
            try
            {
                loop.Enqueue(SimCommands.ExportModel(dir, "creature"));
                loop.ApplyCommands();
                Assert.That(loop.TryDequeueEvent(out var e), Is.True);
                Assert.That(((ModelExported)e!).Path, Is.EqualTo(Path.Combine(dir, "creature_module_array.json")));
                Assert.That(File.Exists(Path.Combine(dir, "creature_parameters.csv")), Is.True);

                string file = Path.Combine(dir, "a_file");
                File.WriteAllText(file, "");
                loop.Enqueue(SimCommands.ExportModel(file, "creature"));   // a file where the directory should be
                loop.ApplyCommands();
                Assert.That(loop.TryDequeueEvent(out e), Is.True);
                Assert.That(e, Is.InstanceOf<CommandRejected>());
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [TestCase("default")]
        [TestCase("famine")]
        public void EveryInstanceHasUnitsForThePlayersParameters(string instance)
        {
            var units = ModuleLibrary.InstanceUnits("generic_adaptive_cell", "fermenter", instance);
            NewLoop(out var world);
            Assert.That(world.Cell(0).PlayerParameters.Where(p => !units.ContainsKey(p)), Is.Empty);
        }
    }
}
