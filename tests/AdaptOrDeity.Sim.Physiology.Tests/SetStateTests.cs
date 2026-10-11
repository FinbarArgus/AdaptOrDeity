using AdaptOrDeity.Game.Content;
using AdaptOrDeity.Sim.Physiology;
using NUnit.Framework;

namespace AdaptOrDeity.Sim.Physiology.Tests
{
    /// <summary>A state set from outside is where the cell's integration continues from, and what values are read from.</summary>
    public class SetStateTests
    {
        [Test]
        public void ASetStateIsWhereTheCellContinuesFrom()
        {
            var cell = new FermenterCell(TraitCatalog.Fermenter);
            cell.MembraneGlucose = 20.0;
            for (int s = 0; s < 100; s++)
                cell.Step(0.1);
            cell.SetState("bio__P_growth", cell.DaughterCost);
            Assert.That(cell.CanDivide, Is.True, "values read right after the state is set are those of the new state");
            cell.SetState("bio__P_growth", 0.0);
            Assert.That(cell.GrowthProgress, Is.EqualTo(0.0));
            cell.Step(0.1);
            Assert.That(cell.GrowthInvested, Is.LessThan(1e-3 * cell.DaughterCost), "growth starts again from nothing");
        }
    }
}
