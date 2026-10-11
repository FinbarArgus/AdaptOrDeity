"""Derive LeanFermenter.cs from the generated fermenter kernel: the layout docs/plans/parallel-and-gpu.md proposes
(group parameters read once per batch, algebraic variables local to the rate loop). A prototype of the kernelgen change,
for measuring only.

    python make_lean.py <repo>/src/AdaptOrDeity.Sim.Kernels.Generated/Kernels/GenericAdaptiveCell_fermenter.g.cs > LeanFermenter.cs
"""
import re
import sys

PER_CELL = 20   # externals that differ between cells (world inputs, the player's settings); the rest are per group

src = open(sys.argv[1]).read()
body = re.search(r'public void ComputeRates\(double t, KernelBatch batch, int start, int end\)\s*\{(.*?)\n        \}\n',
                 src, re.S).group(1)
loop = body[body.index('for (int k = start; k < end; k++)'):]
used_e = sorted({int(x) for x in re.findall(r'\be(\d+)\[k\]', loop)})
shared = [i for i in used_e if i >= PER_CELL]
for i in shared:
    loop = re.sub(rf'\be{i}\[k\]', f'p{i}', loop)
loop = re.sub(r'(\s)a(\d+)\[k\] = ', r'\1double a\2 = ', loop)
loop = re.sub(r'\ba(\d+)\[k\]', r'a\1', loop)
hoist = [f'        double[] e{i} = batch.Externals[{i}];' for i in used_e if i < PER_CELL]
hoist += [f'        double p{i} = batch.Externals[{i}][0];' for i in shared]
hoist += [f'        double[] s{i} = batch.States[{i}];' for i in sorted({int(x) for x in re.findall(r'\bs(\d+)\[k\]', loop)})]
hoist += [f'        double[] r{i} = batch.Rates[{i}];' for i in sorted({int(x) for x in re.findall(r'\br(\d+)\[k\]', loop)})]
print('using System;\nusing AdaptOrDeity.Sim.Modules;\n\npublic static class LeanFermenter\n{\n'
      '    public static void ComputeRates(double t, KernelBatch batch, int start, int end)\n    {\n'
      + '\n'.join(hoist) + '\n        ' + loop.rstrip() + '\n    }\n}')
