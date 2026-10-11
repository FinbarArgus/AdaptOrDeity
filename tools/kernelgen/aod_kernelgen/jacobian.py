"""The exact Jacobian of a kernel's rates, df/dy, as C# (docs/plans/symbolic-jacobians.md).

Forward mode over the analyser's equations: every algebraic variable a gets a tangent da_j = da/dy_j for each state
y_j it depends on (structurally, through the equations it reads), and each rate's row gets its non-zero entries. Each
tangent is the chain rule applied to its equation's expression tree, so the code is exact and as long as the equations
times their dependence (no expression swell beyond one level). Inputs, constants and time have zero tangents. A
piecewise expression differentiates the branch that is active.
"""

from __future__ import annotations

import re

import libcellml as lc

from .module import AnalysedModule, ModuleError

T = lc.AnalyserEquationAst.Type


class Differentiator:
    """d(expression)/dy_j as C#, given the emitter that writes values (``emitter.expr``)."""

    def __init__(self, module: AnalysedModule, emitter, state_index: dict[str, int],
                 algebraic_index: dict[str, int], deps: dict[str, set[int]]):
        self.m = module
        self.e = emitter
        self.state_index = state_index
        self.algebraic_index = algebraic_index
        self.deps = deps

    def d(self, ast, j: int) -> str | None:
        """The derivative of ast with respect to state j, or None where it is structurally zero."""
        t = ast.type()
        left, right = ast.leftChild(), ast.rightChild()
        if t == T.CI:
            name = ast.variable().name()
            if name in self.state_index:
                return "1.0" if self.state_index[name] == j else None
            if name in self.algebraic_index and j in self.deps.get(name, ()):
                return f"da{self.algebraic_index[name]}_{j}"
            return None
        if t in (T.CN, T.E, T.PI, T.INF, T.NAN, T.TRUE, T.FALSE) or t in _BOOLEAN:
            return None
        v = self.e.expr
        if t == T.PLUS:
            return _add(self.d(left, j), self.d(right, j)) if right else self.d(left, j)
        if t == T.MINUS:
            if right is None:
                du = self.d(left, j)
                return f"(-{du})" if du else None
            du, dv = self.d(left, j), self.d(right, j)
            if dv is None:
                return du
            return f"({du} - {dv})" if du else f"(-{dv})"
        if t == T.TIMES:
            return _add(_mul(self.d(left, j), v(right)), _mul(v(left), self.d(right, j)))
        if t == T.DIVIDE:
            du, dv = self.d(left, j), self.d(right, j)
            if du is None and dv is None:
                return None
            if dv is None:
                return f"({du} / {v(right)})"
            # (du - (u/v) dv) / v: no v² to underflow when v is tiny (a concentration near zero in a denominator)
            quotient = f"({v(left)} / {v(right)})"
            return f"({_sub(du, _mul(quotient, dv))} / {v(right)})"
        if t == T.POWER:
            du, dv = self.d(left, j), self.d(right, j)
            if du is None and dv is None:
                return None
            base, exp = v(left), v(right)
            if dv is None:
                if exp == "2.0":
                    return f"(2.0 * {base} * {du})"
                return f"({exp} * Math.Pow({base}, {exp} - 1.0) * {du})"
            inner = _add(_mul(dv, f"Math.Log({base})"), f"({exp} * {du} / {base})" if du else None)
            return f"(Math.Pow({base}, {exp}) * {inner})"
        if t == T.ROOT:
            if right is None:
                du = self.d(left, j)
                return f"({du} / (2.0 * Math.Sqrt({v(left)})))" if du else None
            du = self.d(right, j)
            if du is None:
                return None
            n = v(left.leftChild())
            return f"((1.0 / {n}) * Math.Pow({v(right)}, 1.0 / {n} - 1.0) * {du})"
        if t == T.LOG:
            if right is None:
                du = self.d(left, j)
                return f"({du} / ({v(left)} * Math.Log(10.0)))" if du else None
            du = self.d(right, j)
            return f"({du} / ({v(right)} * Math.Log({v(left.leftChild())})))" if du else None
        if t in _CHAIN:
            du = self.d(left, j)
            return f"({_CHAIN[t](v(left))} * {du})" if du else None
        if t in _RECIPROCAL:
            du = self.d(left, j)
            if du is None:
                return None
            g, dg = _RECIPROCAL[t]
            return f"(-({dg(v(left))}) * {du} / ({g}({v(left)}) * {g}({v(left)})))"
        if t in (T.CEILING, T.FLOOR):
            return None
        if t in (T.MIN, T.MAX):
            du, dv = self.d(left, j), self.d(right, j)
            if du is None and dv is None:
                return None
            op = "<=" if t == T.MIN else ">="
            return f"({v(left)} {op} {v(right)} ? {du or '0.0'} : {dv or '0.0'})"
        if t == T.REM:
            du, dv = self.d(left, j), self.d(right, j)
            if dv is None:
                return du
            return _sub(du, f"(Math.Truncate({v(left)} / {v(right)}) * {dv})")
        if t == T.PIECEWISE:
            return self.piecewise(ast, j)
        if t == T.PIECE:
            dl = self.d(left, j)
            return f"({self.e.cond(right)} ? {dl} : 0.0)" if dl else None
        raise ModuleError(f"{self.m.module_type}/{self.m.version}: no derivative for MathML element "
                          f"{lc.AnalyserEquationAst.typeAsString(t)}")

    def piecewise(self, ast, j: int) -> str | None:
        left, right = ast.leftChild(), ast.rightChild()
        dl = self.d(left.leftChild(), j)
        if right is None:
            rest = None
        elif right.type() == T.OTHERWISE:
            rest = self.d(right.leftChild(), j)
        elif right.type() == T.PIECE:
            rest = self.d(right, j)
        else:
            rest = self.piecewise(right, j)
        if dl is None and rest is None:
            return None
        return f"({self.e.cond(left.rightChild())} ? {dl or '0.0'} : {rest or '0.0'})"


_BOOLEAN = {T.EQ, T.NEQ, T.LT, T.LEQ, T.GT, T.GEQ, T.AND, T.OR, T.XOR, T.NOT}

_CHAIN = {   # d f(u) = f'(u) du
    T.EXP: lambda u: f"Math.Exp({u})",
    T.LN: lambda u: f"(1.0 / {u})",
    T.ABS: lambda u: f"(double)Math.Sign({u})",
    T.SIN: lambda u: f"Math.Cos({u})",
    T.COS: lambda u: f"(-Math.Sin({u}))",
    T.TAN: lambda u: f"(1.0 / (Math.Cos({u}) * Math.Cos({u})))",
    T.SINH: lambda u: f"Math.Cosh({u})",
    T.COSH: lambda u: f"Math.Sinh({u})",
    T.TANH: lambda u: f"(1.0 - Math.Tanh({u}) * Math.Tanh({u}))",
    T.ASIN: lambda u: f"(1.0 / Math.Sqrt(1.0 - {u} * {u}))",
    T.ACOS: lambda u: f"(-1.0 / Math.Sqrt(1.0 - {u} * {u}))",
    T.ATAN: lambda u: f"(1.0 / (1.0 + {u} * {u}))",
}

_RECIPROCAL = {   # f = 1/g(u): f' = -g'(u) / g(u)²
    T.SEC: ("Math.Cos", lambda u: f"(-Math.Sin({u}))"),
    T.CSC: ("Math.Sin", lambda u: f"Math.Cos({u})"),
    T.COT: ("Math.Tan", lambda u: f"(1.0 / (Math.Cos({u}) * Math.Cos({u})))"),
    T.SECH: ("Math.Cosh", lambda u: f"Math.Sinh({u})"),
    T.CSCH: ("Math.Sinh", lambda u: f"Math.Cosh({u})"),
    T.COTH: ("Math.Tanh", lambda u: f"(1.0 - Math.Tanh({u}) * Math.Tanh({u}))"),
}


def _add(a: str | None, b: str | None) -> str | None:
    if a is None:
        return b
    if b is None:
        return a
    return f"({a} + {b})"


def _sub(a: str | None, b: str | None) -> str | None:
    if b is None:
        return a
    return f"({a} - {b})" if a else f"(-{b})"


def _mul(a: str | None, b: str | None) -> str | None:
    if a is None or b is None:
        return None
    if a == "1.0":
        return b
    if b == "1.0":
        return a
    return f"({a} * {b})"


def _refs(ast, out: set[str]) -> None:
    if ast is None:
        return
    if ast.type() == T.CI:
        out.add(ast.variable().name())
    _refs(ast.leftChild(), out)
    _refs(ast.rightChild(), out)


def body(module: AnalysedModule, emitter, equations) -> tuple[str, int]:
    """C# statements computing the Jacobian of the rates for instance k into ``jacobian`` (n x n, zeroed by the
    caller), from ``equations`` (the rate equations and the algebraic ones they need, in order), with the values
    written by ``emitter`` (local algebraic variables). Returns the code and the number of non-zero entries."""
    state_index = {v.name: i for i, v in enumerate(module.states)}
    algebraic_index = {v.name: i for i, v in enumerate(module.algebraic)}
    deps: dict[str, set[int]] = {}
    lines: list[str] = []
    nonzeros = 0
    diff = Differentiator(module, emitter, state_index, algebraic_index, deps)
    for q in equations:
        rhs = q.ast.rightChild()
        names: set[str] = set()
        _refs(rhs, names)
        d: set[int] = set()
        for x in names:
            if x in state_index:
                d.add(state_index[x])
            elif x in deps:
                d |= deps[x]
        if q.kind != "ode":
            lines.append(emitter.assignment(q))
        if q.kind == "ode":
            row = state_index[q.target]
            for j in sorted(d):
                expr = diff.d(rhs, j)
                if expr is not None:
                    lines.append(f"jacobian[{row}, {j}] = {expr}; // d(d{q.target}/dt)/d{module.states[j].name}")
                    nonzeros += 1
        else:
            deps[q.target] = d
            for j in sorted(d):
                expr = diff.d(rhs, j)
                lines.append(f"double da{algebraic_index[q.target]}_{j} = {expr or '0.0'};")
    return "\n".join(_prune(lines)), nonzeros


def _prune(lines: list[str]) -> list[str]:
    """Drop local values and tangents nothing reads (a value whose only use was in a derivative that is zero),
    repeatedly, since dropping one can leave another unused."""
    defined = re.compile(r"^double (d?a\d+(?:_\d+)?) = ")
    while True:
        uses: dict[str, int] = {}
        for line in lines:
            for name in re.findall(r"\b(d?a\d+(?:_\d+)?)\b", line.split(" = ", 1)[1] if " = " in line else line):
                uses[name] = uses.get(name, 0) + 1
        kept = [line for line in lines if not ((m := defined.match(line)) and uses.get(m.group(1), 0) == 0)]
        if len(kept) == len(lines):
            return kept
        lines = kept
