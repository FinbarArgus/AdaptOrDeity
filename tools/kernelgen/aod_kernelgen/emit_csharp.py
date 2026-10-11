"""Emit a structure-of-arrays C# kernel class from an analysed module."""

from __future__ import annotations

import re

import libcellml as lc

from . import jacobian
from .module import AnalysedModule, ModuleError

T = lc.AnalyserEquationAst.Type

NAMESPACE = "AdaptOrDeity.Sim.Kernels.Generated"

UNARY_FUNCS = {
    T.ABS: "Math.Abs", T.EXP: "Math.Exp", T.LN: "Math.Log", T.CEILING: "Math.Ceiling",
    T.FLOOR: "Math.Floor", T.SIN: "Math.Sin", T.COS: "Math.Cos", T.TAN: "Math.Tan",
    T.SINH: "Math.Sinh", T.COSH: "Math.Cosh", T.TANH: "Math.Tanh", T.ASIN: "Math.Asin",
    T.ACOS: "Math.Acos", T.ATAN: "Math.Atan",
}
# Reciprocal trigonometric functions: f(x) = 1 / g(x).
RECIPROCAL = {T.SEC: "Math.Cos", T.CSC: "Math.Sin", T.COT: "Math.Tan",
              T.SECH: "Math.Cosh", T.CSCH: "Math.Sinh", T.COTH: "Math.Tanh"}
COMPARISONS = {T.EQ: "==", T.NEQ: "!=", T.LT: "<", T.LEQ: "<=", T.GT: ">", T.GEQ: ">="}
LOGICAL = {T.AND: "&&", T.OR: "||"}


def class_name(module_type: str, version: str) -> str:
    pascal = "".join(part[:1].upper() + part[1:] for part in re.split(r"[^0-9A-Za-z]+", module_type) if part)
    return f"{pascal}_{re.sub(r'[^0-9A-Za-z]', '_', version)}"


class _Emitter:
    """Expressions in one of two layouts. Both read a group input (``group``: the same value for every instance in a
    call's range) once per call as the scalar ``g{i}``. ``local_algebraic`` keeps algebraic variables as locals of the
    loop body (for ComputeRates) instead of writing them to the batch."""

    def __init__(self, module: AnalysedModule, group: set[str], local_algebraic: bool):
        self.m = module
        self.local_algebraic = local_algebraic
        self.refs: dict[str, str] = {}
        for i, v in enumerate(module.states):
            self.refs[v.name] = f"s{i}[k]"
        for i, v in enumerate(module.externals):
            self.refs[v.name] = f"g{i}" if v.name in group else f"e{i}[k]"
        for i, v in enumerate(module.algebraic):
            self.refs[v.name] = f"a{i}" if local_algebraic else f"a{i}[k]"
        self.state_index = {v.name: i for i, v in enumerate(module.states)}

    def var(self, ast) -> str:
        name = ast.variable().name()
        if name == self.m.voi:
            return "t"
        if name in self.m.constants:
            return _literal(self.m.constants[name])
        try:
            return self.refs[name]
        except KeyError:
            raise ModuleError(f"{self.m.module_type}/{self.m.version}: unmapped variable {name}") from None

    def expr(self, ast) -> str:
        t = ast.type()
        left, right = ast.leftChild(), ast.rightChild()
        if t == T.CI:
            return self.var(ast)
        if t == T.CN:
            return _literal(float(ast.value()))
        if t == T.PLUS:
            return f"({self.expr(left)} + {self.expr(right)})" if right else self.expr(left)
        if t == T.MINUS:
            return f"({self.expr(left)} - {self.expr(right)})" if right else f"(-{self.expr(left)})"
        if t == T.TIMES:
            return f"({self.expr(left)} * {self.expr(right)})"
        if t == T.DIVIDE:
            return f"({self.expr(left)} / {self.expr(right)})"
        if t == T.POWER:
            return f"Math.Pow({self.expr(left)}, {self.expr(right)})"
        if t == T.ROOT:
            if right is None:  # square root: the radicand is the only child
                return f"Math.Sqrt({self.expr(left)})"
            # left is <degree>, right is the radicand
            return f"Math.Pow({self.expr(right)}, 1.0 / {self.expr(left.leftChild())})"
        if t == T.LOG:
            if right is None:
                return f"Math.Log10({self.expr(left)})"
            return f"(Math.Log({self.expr(right)}) / Math.Log({self.expr(left.leftChild())}))"
        if t in UNARY_FUNCS:
            return f"{UNARY_FUNCS[t]}({self.expr(left)})"
        if t in RECIPROCAL:
            return f"(1.0 / {RECIPROCAL[t]}({self.expr(left)}))"
        if t == T.MIN:
            return f"Math.Min({self.expr(left)}, {self.expr(right)})"
        if t == T.MAX:
            return f"Math.Max({self.expr(left)}, {self.expr(right)})"
        if t == T.REM:
            return f"({self.expr(left)} % {self.expr(right)})"  # C# % on doubles is fmod, as MathML rem
        if t in COMPARISONS:
            return f"({self.expr(left)} {COMPARISONS[t]} {self.expr(right)})"
        if t in LOGICAL:
            return f"({self.cond(left)} {LOGICAL[t]} {self.cond(right)})"
        if t == T.XOR:
            return f"({self.cond(left)} ^ {self.cond(right)})"
        if t == T.NOT:
            return f"(!{self.cond(left)})"
        if t == T.PIECEWISE:
            return self.piecewise(ast)
        if t == T.PIECE:
            return f"({self.cond(right)} ? {self.expr(left)} : double.NaN)"
        if t == T.TRUE:
            return "true"
        if t == T.FALSE:
            return "false"
        if t == T.E:
            return "Math.E"
        if t == T.PI:
            return "Math.PI"
        if t == T.INF:
            return "double.PositiveInfinity"
        if t == T.NAN:
            return "double.NaN"
        raise ModuleError(f"{self.m.module_type}/{self.m.version}: unsupported MathML element "
                          f"{lc.AnalyserEquationAst.typeAsString(t)}")

    def cond(self, ast) -> str:
        """A boolean context: comparisons/logic stay boolean, numbers are tested against zero."""
        if ast.type() in COMPARISONS or ast.type() in LOGICAL or ast.type() in (T.XOR, T.NOT, T.TRUE, T.FALSE):
            return self.expr(ast)
        return f"({self.expr(ast)} != 0.0)"

    def piecewise(self, ast) -> str:
        # PIECEWISE(left=PIECE, right=PIECE | OTHERWISE | PIECEWISE | None)
        left, right = ast.leftChild(), ast.rightChild()
        piece = f"{self.cond(left.rightChild())} ? {self.expr(left.leftChild())}"
        if right is None:
            rest = "double.NaN"
        elif right.type() == T.OTHERWISE:
            rest = self.expr(right.leftChild())
        elif right.type() == T.PIECE:
            rest = f"({self.cond(right.rightChild())} ? {self.expr(right.leftChild())} : double.NaN)"
        else:
            rest = self.piecewise(right)
        return f"({piece} : {rest})"

    def assignment(self, eq) -> str:
        lhs = eq.ast.leftChild()
        rhs = self.expr(eq.ast.rightChild())
        if eq.kind == "ode":
            return f"r{self.state_index[eq.target]}[k] = {rhs}; // d{eq.target}/d{self.m.voi}"
        if lhs.type() != T.CI or lhs.variable().name() != eq.target:
            raise ModuleError(f"{self.m.module_type}/{self.m.version}: equation for {eq.target} is not in explicit form")
        declare = "double " if self.local_algebraic else ""
        return f"{declare}{self.refs[eq.target]} = {rhs}; // {eq.target}"


def _uses_rate(ast) -> bool:
    if ast is None:
        return False
    return ast.type() == T.DIFF or _uses_rate(ast.leftChild()) or _uses_rate(ast.rightChild())


def _literal(value: float) -> str:
    if value != value:
        return "double.NaN"
    text = repr(float(value))
    if "inf" in text:
        return "double.PositiveInfinity" if value > 0 else "double.NegativeInfinity"
    return text if ("." in text or "e" in text) else text + ".0"


def _cs_string(text: str) -> str:
    return '"' + text.replace("\\", "\\\\").replace('"', '\\"') + '"'


def _var_info(v) -> str:
    role = {"variable": "Variable", "boundary_condition": "BoundaryCondition",
            "constant": "Constant", "global_constant": "GlobalConstant"}[v.role]
    return f"new VariableInfo({_cs_string(v.name)}, {_cs_string(v.units)}, {_literal(v.si_multiplier)}, VariableRole.{role})"


def emit(module: AnalysedModule, group_inputs: list[str] | None = None, outputs: list[str] | None = None) -> str:
    """The kernel class. ``group_inputs``: externals shared by every instance of a group (read once per call);
    the others are per instance. None makes every external per instance. ``outputs``: algebraic variables that
    ComputeOutputs keeps current after every step (with what they depend on), so the rest can be computed on demand."""
    m = module
    group = set(group_inputs or [])
    unknown = group - {v.name for v in m.externals}
    if unknown:
        raise ModuleError(f"{m.module_type}/{m.version}: group inputs that are not externals: {sorted(unknown)}")
    e = _Emitter(module, group, local_algebraic=False)
    lean = _Emitter(module, group, local_algebraic=True)
    cls = class_name(module.module_type, module.version)

    # Algebraic equations run before the ODEs that use them; the rare ones that read a derivative run after.
    algebraic_eqs = [q for q in m.equations if q.kind in ("algebraic", "computed_constant")]
    rates_eqs = [q for q in algebraic_eqs if not _uses_rate(q.ast.rightChild())]
    rates_eqs += [q for q in m.equations if q.kind == "ode"]
    rates_eqs += [q for q in algebraic_eqs if _uses_rate(q.ast.rightChild())]
    variable_eqs = [q for q in m.equations if q.kind in ("algebraic", "computed_constant")]

    # ComputeRates computes only what the rates need: the algebraic equations the ODEs read, directly or not.
    algebraic_index = {v.name: i for i, v in enumerate(m.algebraic)}
    by_index = {algebraic_index[q.target]: q for q in algebraic_eqs}
    needed: set[int] = set()
    pending = [q for q in m.equations if q.kind == "ode"]
    while pending:
        line = lean.assignment(pending.pop()).split(" = ", 1)[1]
        for i in map(int, re.findall(r"\ba(\d+)\b", line)):
            if i not in needed and i in by_index:
                needed.add(i)
                pending.append(by_index[i])
    rates_eqs = [q for q in rates_eqs if q.kind == "ode" or algebraic_index[q.target] in needed]

    # ComputeOutputs: the outputs and the algebraic variables they read, directly or not, in ComputeVariables' order.
    unknown = set(outputs or []) - set(algebraic_index)
    if unknown:
        raise ModuleError(f"{m.module_type}/{m.version}: outputs that are not algebraic variables: {sorted(unknown)}")
    output_closure: set[int] = set()
    pending = [by_index[algebraic_index[n]] for n in outputs or [] if algebraic_index[n] in by_index]
    output_closure.update(algebraic_index[n] for n in outputs or [])
    while pending:
        line = e.assignment(pending.pop()).split(" = ", 1)[1]
        for i in map(int, re.findall(r"\ba(\d+)\b", line)):
            if i not in output_closure and i in by_index:
                output_closure.add(i)
                pending.append(by_index[i])
    output_eqs = [q for q in variable_eqs if algebraic_index[q.target] in output_closure]
    output_indices = ", ".join(str(algebraic_index[n]) for n in outputs or [])

    def locals_for(stmts) -> list[str]:
        used, group_used = set(), set()
        for line in stmts:
            used.update(re.findall(r"\b([searu]\d+)\[k\]", line))
            group_used.update(int(i) for i in re.findall(r"\bg(\d+)\b", line))
        lines = []
        for name in sorted(used, key=lambda n: (n[0], int(n[1:]))):
            array = {"s": "States", "e": "Externals", "a": "Algebraic", "r": "Rates"}[name[0]]
            lines.append(f"double[] {name} = batch.{array}[{name[1:]}];")
        lines += [f"double g{i} = batch.Externals[{i}][start];" for i in sorted(group_used)]
        return lines

    def body(eqs, emitter) -> str:
        stmts = [emitter.assignment(q) for q in eqs]
        if not stmts:
            return "            // No equations."
        out = ["            if (start >= end)", "                return;"]
        out += [f"            {d}" for d in locals_for(stmts)]
        out.append("            for (int k = start; k < end; k++)")
        out.append("            {")
        out.extend(f"                {s}" for s in stmts)
        out.append("            }")
        return "\n".join(out)

    jac_code, jac_nonzeros = jacobian.body(m, lean, rates_eqs)
    jac_stmts = jac_code.splitlines()
    jac_body = "\n".join([f"            {d}" for d in locals_for(jac_stmts)] + [f"            {s}" for s in jac_stmts])

    init_lines = []
    for i, (ext_name, literal) in enumerate(m.state_init):
        if ext_name is None:
            init_lines.append(f"            for (int k = start; k < end; k++) batch.States[{i}][k] = {_literal(literal)};")
        else:
            j = [v.name for v in m.externals].index(ext_name)
            init_lines.append(f"            for (int k = start; k < end; k++) batch.States[{i}][k] = batch.Externals[{j}][k]; // {m.states[i].name} = {ext_name}")
    init_body = "\n".join(init_lines) if init_lines else "            // No states."

    ports = ",\n".join(
        f"                new PortInfo({_cs_string(p.port_type)}, PortDirection.{p.direction}, "
        f"new[] {{ {', '.join(_cs_string(v) for v in p.variables)} }}, MultiPortMode.{p.multi_port})"
        for p in m.ports)

    # A whole assembled cell exposes no ports (it is not connected to anything yet).
    ports_block = ("new[]\n            {\n" + ports + ",\n            }") if m.ports else "new PortInfo[0]"

    def instance(name: str, values: dict[str, float]) -> str:
        pairs = ", ".join(f"[{_cs_string(k)}] = {_literal(v)}" for k, v in values.items())
        return f"                [{_cs_string(name)}] = new Dictionary<string, double> {{ {pairs} }}"
    instances = ",\n".join(instance(k, v) for k, v in m.instances.items())

    def var_list(vs) -> str:
        if not vs:
            return "new VariableInfo[0]"
        return "new[]\n            {\n" + ",\n".join(f"                {_var_info(v)}" for v in vs) + ",\n            }"

    init_ext = ", ".join(str([v.name for v in m.externals].index(n)) if n else "-1" for n, _ in m.state_init)
    init_lit = ", ".join(_literal(lit) for _, lit in m.state_init)

    return f"""// <auto-generated>
// Generated by tools/kernelgen from circulatory-autogen-modules. DO NOT EDIT; regenerate instead.
// Module: {m.category}/{m.module_type}/versions/{m.version} (component {m.component})
// Licence: {m.licence}
// Source SHA-256: {m.sha256}
// </auto-generated>
using System;
using System.Collections.Generic;
using AdaptOrDeity.Sim.Modules;

namespace {NAMESPACE}
{{
    public sealed class {cls} : IJacobianKernel, IOutputKernel
    {{
        public static readonly KernelDescriptor StaticDescriptor = new KernelDescriptor(
            moduleType: {_cs_string(m.module_type)},
            version: {_cs_string(m.version)},
            component: {_cs_string(m.component)},
            licence: {_cs_string(m.licence)},
            sourceSha256: {_cs_string(m.sha256)},
            states: {var_list(m.states)},
            externals: {var_list(m.externals)},
            algebraic: {var_list(m.algebraic)},
            stateInitExternal: new int[] {{ {init_ext} }},
            stateInitLiteral: new double[] {{ {init_lit} }},
            ports: {ports_block},
            groupExternals: new int[] {{ {", ".join(str(i) for i, v in enumerate(m.externals) if v.name in group)} }},
            instances: new Dictionary<string, IReadOnlyDictionary<string, double>>
            {{
{instances},
            }});

        public KernelDescriptor Descriptor => StaticDescriptor;

        public void Initialise(KernelBatch batch, int start, int end)
        {{
{init_body}
        }}

        public void ComputeRates(double t, KernelBatch batch, int start, int end)
        {{
{body(rates_eqs, lean)}
        }}

        public void ComputeVariables(double t, KernelBatch batch, int start, int end)
        {{
{body(variable_eqs, e)}
        }}

        public IReadOnlyList<int> Outputs {{ get; }} = new int[] {{ {output_indices} }};

        // {len(output_eqs)} of {len(variable_eqs)} algebraic equations.
        public void ComputeOutputs(double t, KernelBatch batch, int start, int end)
        {{
{body(output_eqs, e)}
        }}

        // The exact Jacobian of the rates for instance k: {jac_nonzeros} structurally non-zero entries of {len(m.states)}².
        public void ComputeJacobian(double t, KernelBatch batch, int k, double[,] jacobian)
        {{
            int start = k;
{jac_body}
        }}
    }}
}}
"""


def emit_registry(class_names: list[str]) -> str:
    entries = "\n".join(f"            new {c}()," for c in sorted(class_names))
    return f"""// <auto-generated>
// Generated by tools/kernelgen. DO NOT EDIT; regenerate instead.
// </auto-generated>
using AdaptOrDeity.Sim.Modules;

namespace {NAMESPACE}
{{
    /// <summary>Every kernel generated from content/modules.lock.json.</summary>
    public static class GeneratedKernels
    {{
        public static IKernel[] All() => new IKernel[]
        {{
{entries}
        }};
    }}
}}
"""
