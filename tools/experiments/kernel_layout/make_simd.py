"""Derive SimdFermenter.cs from LeanFermenter.cs: the same rate loop computing four cells at once in AVX2 registers
(Vector256<double>, .NET 9+ for vectorised Exp/Log). For measuring only.

    python make_simd.py LeanFermenter.cs > simd/SimdFermenter.cs
"""
import re
import sys

src = open(sys.argv[1]).read()
head, loop = src[:src.index('for (int k')], src[src.index('for (int k'):]
loop = loop[loop.index('{') + 1:]
loop = loop[:loop.rindex('}')]; loop = loop[:loop.rindex('}')]; loop = loop[:loop.rindex('}')]


def call_args(txt, name):
    """(start, end, inner) of each top-level call to name(...)."""
    out, i = [], 0
    while (i := txt.find(name + '(', i)) >= 0:
        j = i + len(name) + 1
        depth = 1
        while depth:
            depth += {'(': 1, ')': -1}.get(txt[j], 0)
            j += 1
        out.append((i, j, txt[i + len(name) + 1:j - 1]))
        i = j
    return out


def split_last_comma(args):
    depth = 0
    for i in range(len(args) - 1, -1, -1):
        c = args[i]
        depth += {')': 1, '(': -1}.get(c, 0)
        if c == ',' and depth == 0:
            return args[:i], args[i + 1:].strip()
    raise ValueError(args)


while 'Math.Pow(' in loop:      # innermost first: replace the last call, whose arguments hold no Pow
    i, j, inner = call_args(loop, 'Math.Pow')[-1]
    base, exp = split_last_comma(inner)
    rep = {'2.0': f'Sq({base})', '3.0': f'Cu({base})', '8.0': f'P8({base})'}.get(exp, f'Pw({base}, {exp})')
    loop = loop[:i] + rep + loop[j:]
loop = loop.replace('Math.Exp(', 'Vector256.Exp(').replace('Math.Log(', 'Vector256.Log(')
loop = loop.replace('Math.Abs(', 'Vector256.Abs(').replace('Math.PI', '3.141592653589793')
consts = {}
def const(m):
    consts.setdefault(m.group(1), f'c{len(consts)}')
    return consts[m.group(1)]
loop = re.sub(r'(?<![\w.])(\d+\.\d*(?:[eE][-+]?\d+)?|\d+[eE][-+]?\d+|\d+)(?![\w.])', const, loop)
loop = re.sub(r'\b([se]\d+)\[k\]', r'L(\1, u)', loop)
loop = re.sub(r'\b(r\d+)\[k\] = (.*?);( //[^\n]*)?\n', r'St(\1, u, \2);\3\n', loop)
loop = re.sub(r'\bdouble (a\d+) = ', r'var \1 = ', loop)
head = re.sub(r'double (p\d+) = (batch\.Externals\[\d+\]\[0\]);', r'Vector256<double> \1 = Vector256.Create(\2);', head)
head = head[head.index('    {') + 6:]          # the hoists, after the method's opening brace
print('''using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using AdaptOrDeity.Sim.Modules;

public static class SimdFermenter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Vector256<double> L(double[] a, nuint u) => Vector256.LoadUnsafe(ref MemoryMarshal.GetArrayDataReference(a), u);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void St(double[] a, nuint u, Vector256<double> v) => v.StoreUnsafe(ref MemoryMarshal.GetArrayDataReference(a), u);
    static Vector256<double> Sq(Vector256<double> x) => x * x;
    static Vector256<double> Cu(Vector256<double> x) => x * x * x;
    static Vector256<double> P8(Vector256<double> x) { var y = x * x; y *= y; return y * y; }
    static Vector256<double> Pw(Vector256<double> x, Vector256<double> y) => Vector256.Exp(y * Vector256.Log(x));

    // Four cells per iteration; end - start must be a multiple of 4.
    public static void ComputeRates(double t, KernelBatch batch, int start, int end)
    {
''' + head.rstrip() + '\n' + '\n'.join(f'        var {n} = Vector256.Create({v});' for v, n in consts.items()) + '''
        for (int k = start; k < end; k += 4)
        {
            nuint u = (nuint)k;''' + loop.rstrip() + '''
        }
    }
}''')
