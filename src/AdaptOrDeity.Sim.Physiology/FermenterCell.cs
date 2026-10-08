using System;
using System.Collections.Generic;
using AdaptOrDeity.Sim.Modules;

namespace AdaptOrDeity.Sim.Physiology
{
    /// <summary>The cell's energy budget at one instant: where its ATP comes from and goes [mol/s, W].</summary>
    public struct EnergyBudget
    {
        /// <summary>ATP made by glycolysis (2 per glucose fermented to lactate) [mol ATP/s].</summary>
        public double AtpProduced;

        /// <summary>ATP hydrolysed by the F1Fo H+-ATPase pumping protons out [mol/s].</summary>
        public double AtpPumps;

        /// <summary>ATP to turn over (degrade and remake) the cell's proteins [mol/s].</summary>
        public double AtpProteinTurnover;

        /// <summary>ATP spent building new protein copies while expression rises [mol/s].</summary>
        public double AtpProteinBuild;

        /// <summary>The remaining maintenance ATPase: DNA, RNA and lipid upkeep and everything not yet resolved [mol/s].</summary>
        public double AtpOtherMaintenance;

        /// <summary>Heat released [W].</summary>
        public double Heat;

        public double AtpConsumed => AtpPumps + AtpProteinTurnover + AtpProteinBuild + AtpOtherMaintenance;
    }

    /// <summary>
    /// The fermenter cell (circulatory-autogen-modules cell/prokaryote fermenter, assembled into one kernel) with its
    /// adaptable traits. Stepped by backward Euler, which stays stable at macro steps far longer than the Ca buffer's
    /// microsecond time constant. Expression traits approach their targets with a first-order time constant, and the
    /// ATP to build the extra copies is drawn from the cytosol over that time (protein_expression's J_build); midpoint
    /// traits change at once. All values exchanged here are in the kernel's module units unless named SI.
    /// </summary>
    public sealed class FermenterCell
    {
        public const double AvogadroConstant = 6.02214076e23;

        private readonly IKernel _kernel;
        private readonly BackwardEulerIntegrator _integrator = new BackwardEulerIntegrator();
        private readonly int[][] _traitExternals;
        private readonly double[][] _traitBase;
        private readonly double[] _value;
        private readonly double[] _target;
        private readonly double[] _planned;
        private readonly double[] _buildPerUnit;   // mol ATP to build the baseline copies of each expression trait
        private readonly int _jBuild;
        private readonly int _aVgly, _aVhyd, _aVol, _aPump, _aProtein, _aProteinFactor, _aHeat;

        public FermenterCell(IKernel kernel, string instance = "default", IReadOnlyList<TraitDefinition>? traits = null)
        {
            _kernel = kernel;
            var d = kernel.Descriptor;
            Traits = traits ?? FermenterTraits.All;
            Batch = new KernelBatch(d, 1) { Count = 1 };
            Batch.ApplyInstance(instance, 0, 1);

            _traitExternals = new int[Traits.Count][];
            _traitBase = new double[Traits.Count][];
            _value = new double[Traits.Count];
            _target = new double[Traits.Count];
            _planned = new double[Traits.Count];
            _buildPerUnit = new double[Traits.Count];
            double cBuild = External("c_syn_mod_protein") + External("c_poly_mod_protein");
            for (int i = 0; i < Traits.Count; i++)
            {
                var t = Traits[i];
                _traitExternals[i] = new int[t.Parameters.Count];
                _traitBase[i] = new double[t.Parameters.Count];
                for (int p = 0; p < t.Parameters.Count; p++)
                {
                    int e = Require(d.ExternalIndex(FermenterTraits.KernelName(t.Parameters[p])), t.Parameters[p]);
                    _traitExternals[i][p] = e;
                    _traitBase[i][p] = Batch.Externals[e][0];
                }
                _value[i] = _target[i] = 1.0;
                if (t.CopyKey != null)
                    _buildPerUnit[i] = External($"n_{t.CopyKey}_mod_protein") * External($"L_{t.CopyKey}_mod_protein")
                                       * cBuild / AvogadroConstant;
            }
            _jBuild = Require(d.ExternalIndex("J_build_mod_protein"), "J_build_mod_protein");
            _aVgly = Algebraic("cytosol__v_gly");
            _aVhyd = Algebraic("cytosol__v_hyd");
            _aVol = Algebraic("membrane__Vol");
            _aPump = Algebraic("i_H_ATPase__v_ATP_H");
            _aProtein = Algebraic("protein__J_ATP_protein");
            _aProteinFactor = Algebraic("protein__f_ATP_prot");
            _aHeat = Algebraic("cytosol__Q_met");

            kernel.Initialise(Batch, 0, 1);
            kernel.ComputeVariables(0.0, Batch, 0, 1);
        }

        public KernelBatch Batch { get; }

        public IReadOnlyList<TraitDefinition> Traits { get; }

        public double Time { get; private set; }

        /// <summary>Time constant with which expression approaches its target [s] (placeholder: order of a generation).</summary>
        public double ExpressionTimeConstant { get; set; } = 1800.0;

        /// <summary>True if the last step's Newton iterations converged.</summary>
        public bool Converged => _integrator.LastConverged;

        /// <summary>Current multiplier of trait <paramref name="trait"/> on its baseline parameters.</summary>
        public double TraitValue(int trait) => _value[trait];

        /// <summary>The multiplier the cell is adapting towards.</summary>
        public double TraitTarget(int trait) => _target[trait];

        /// <summary>
        /// Sets the trait's target multiplier (clamped to its range). Midpoints change at once; expression approaches
        /// the target over <see cref="ExpressionTimeConstant"/>, paying to build any extra copies.
        /// </summary>
        public void SetTrait(int trait, double multiplier)
        {
            var t = Traits[trait];
            if (double.IsNaN(multiplier))
                throw new ArgumentException("multiplier is NaN", nameof(multiplier));
            double m = Math.Min(t.Max, Math.Max(t.Min, multiplier));
            _target[trait] = m;
            if (t.Kind == TraitKind.Midpoint)
            {
                _value[trait] = m;
                Apply(trait);
            }
        }

        /// <summary>Sets an external (module units) by its kernel name, e.g. the medium glucose.</summary>
        public void SetExternal(string kernelName, double value) =>
            Batch.Externals[Require(_kernel.Descriptor.ExternalIndex(kernelName), kernelName)][0] = value;

        public double External(string kernelName) =>
            Batch.Externals[Require(_kernel.Descriptor.ExternalIndex(kernelName), kernelName)][0];

        public double State(string name) => Batch.States[Require(_kernel.Descriptor.StateIndex(name), name)][0];

        public double AlgebraicValue(string name) => Batch.Algebraic[Algebraic(name)][0];

        public void Step(double dt)
        {
            // Expression moves towards its target. Building the extra copies costs ATP over this step; synthesis slows as
            // ATP runs out (protein_expression's f_ATP), so the copies gained scale with the ATP actually spent.
            double build = 0.0;
            double relax = 1.0 - Math.Exp(-dt / ExpressionTimeConstant);
            for (int i = 0; i < Traits.Count; i++)
            {
                _planned[i] = 0.0;
                if (Traits[i].Kind != TraitKind.Expression || _value[i] == _target[i])
                    continue;
                double next = _value[i] + (_target[i] - _value[i]) * relax;
                if (Math.Abs(next - _target[i]) < 1e-9 * _target[i])
                    next = _target[i];
                _planned[i] = next - _value[i];
                if (_planned[i] > 0.0)
                    build += _planned[i] * _buildPerUnit[i];
            }
            Batch.Externals[_jBuild][0] = build / dt;
            _integrator.Step(_kernel, Batch, Time, dt, 0, 1);
            Time += dt;
            double f = Batch.Algebraic[_aProteinFactor][0];
            for (int i = 0; i < Traits.Count; i++)
            {
                if (_planned[i] == 0.0)
                    continue;
                // Rising: as far as the ATP allowed. Falling: degradation needs no ATP and is not refunded.
                _value[i] += _planned[i] > 0.0 ? _planned[i] * Math.Min(1.0, f) : _planned[i];
                if (Math.Abs(_value[i] - _target[i]) < 1e-9 * _target[i])
                    _value[i] = _target[i];
                Apply(i);
            }
        }

        /// <summary>The energy budget now [mol ATP/s, W].</summary>
        public EnergyBudget Budget()
        {
            double[][] a = Batch.Algebraic;
            double vol = a[_aVol][0];   // m³; rates are mM/s = mol/m³/s
            return new EnergyBudget
            {
                AtpProduced = 2.0 * a[_aVgly][0] * vol,
                AtpPumps = a[_aPump][0],
                // Protein synthesis slows as ATP runs out (f = 1 at the resting ATP).
                AtpProteinTurnover = a[_aProtein][0] * a[_aProteinFactor][0],
                AtpProteinBuild = Batch.Externals[_jBuild][0] * Math.Min(1.0, a[_aProteinFactor][0]),
                AtpOtherMaintenance = a[_aVhyd][0] * vol,
                Heat = a[_aHeat][0],
            };
        }

        private void Apply(int trait)
        {
            int[] ext = _traitExternals[trait];
            for (int p = 0; p < ext.Length; p++)
                Batch.Externals[ext[p]][0] = _traitBase[trait][p] * _value[trait];
        }

        private int Algebraic(string name) => Require(_kernel.Descriptor.AlgebraicIndex(name), name);

        private int Require(int index, string name)
        {
            if (index < 0)
                throw new ArgumentException($"{_kernel.Descriptor.Key} has no variable {name}");
            return index;
        }
    }
}
