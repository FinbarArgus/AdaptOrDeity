using System; using System.IO; using System.Linq;
using AdaptOrDeity.Sim.Kernels.Generated; using AdaptOrDeity.Sim.Modules;
public static class Dump
{
    // One initialised cell: states, externals and the rates the C# kernel computes, as text for the OpenCL test.
    public static void Write(string path)
    {
        var kernel = new GenericAdaptiveCell_fermenter(); var d = kernel.Descriptor;
        var b = new KernelBatch(d, 1) { Count = 1 }; b.ApplyInstance("default", 0, 1); kernel.Initialise(b, 0, 1);
        for (int i = 0; i < b.States.Length; i++)
            b.States[i][0] *= (i % 2 == 0) ? 1.05 : 0.95;   // away from steady state, so the rates are not round-off
        kernel.ComputeRates(1.0, b, 0, 1);
        File.WriteAllLines(path, new[] {
            string.Join(" ", b.States.Select(x => x[0].ToString("R"))),
            string.Join(" ", b.Externals.Select(x => x[0].ToString("R"))),
            string.Join(" ", b.Rates.Select(x => x[0].ToString("R"))) });
    }
}
