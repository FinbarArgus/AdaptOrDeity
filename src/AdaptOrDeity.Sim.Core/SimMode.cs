namespace AdaptOrDeity.Sim.Core
{
    /// <summary>How simulated time advances. See adapting_plan.md ("Time: the evolutionary time bar").</summary>
    public enum SimMode
    {
        /// <summary>Continuous operator-split simulation, seconds-scale steps.</summary>
        RealTime,

        /// <summary>Months-to-years epochs; the organism is solved to physiological steady state each epoch.</summary>
        EpochSteadyState,
    }
}
