"""Resolve CellML units to (SI multiplier, SI dimension) so the build can gate port units."""

from __future__ import annotations

import libcellml as lc

# Base SI dimensions, in this order.
DIMENSIONS = ("m", "kg", "s", "A", "K", "mol", "cd")

PREFIXES = {
    "yotta": 24, "zetta": 21, "exa": 18, "peta": 15, "tera": 12, "giga": 9, "mega": 6, "kilo": 3,
    "hecto": 2, "deca": 1, "deci": -1, "centi": -2, "milli": -3, "micro": -6, "nano": -9,
    "pico": -12, "femto": -15, "atto": -18, "zepto": -21, "yocto": -24,
}


def _dim(**exps: float) -> tuple[float, ...]:
    return tuple(float(exps.get(d, 0)) for d in DIMENSIONS)


# Built-in CellML units: name -> (multiplier to SI, dimension exponents).
BUILTIN: dict[str, tuple[float, tuple[float, ...]]] = {
    "dimensionless": (1.0, _dim()),
    "metre": (1.0, _dim(m=1)), "meter": (1.0, _dim(m=1)),
    "kilogram": (1.0, _dim(kg=1)), "gram": (1e-3, _dim(kg=1)),
    "second": (1.0, _dim(s=1)),
    "ampere": (1.0, _dim(A=1)),
    "kelvin": (1.0, _dim(K=1)), "celsius": (1.0, _dim(K=1)),
    "mole": (1.0, _dim(mol=1)),
    "candela": (1.0, _dim(cd=1)),
    "litre": (1e-3, _dim(m=3)), "liter": (1e-3, _dim(m=3)),
    "hertz": (1.0, _dim(s=-1)),
    "newton": (1.0, _dim(kg=1, m=1, s=-2)),
    "pascal": (1.0, _dim(kg=1, m=-1, s=-2)),
    "joule": (1.0, _dim(kg=1, m=2, s=-2)),
    "watt": (1.0, _dim(kg=1, m=2, s=-3)),
    "coulomb": (1.0, _dim(s=1, A=1)),
    "volt": (1.0, _dim(kg=1, m=2, s=-3, A=-1)),
    "farad": (1.0, _dim(kg=-1, m=-2, s=4, A=2)),
    "ohm": (1.0, _dim(kg=1, m=2, s=-3, A=-2)),
    "siemens": (1.0, _dim(kg=-1, m=-2, s=3, A=2)),
    "katal": (1.0, _dim(mol=1, s=-1)),
    "radian": (1.0, _dim()), "steradian": (1.0, _dim()),
}


class UnitsError(ValueError):
    pass


def resolve(name: str, model: lc.Model, _stack: tuple[str, ...] = ()) -> tuple[float, tuple[float, ...]]:
    """Returns (multiplier, dimension) such that 1 [name] = multiplier × SI base units^dimension."""
    if name in BUILTIN:
        return BUILTIN[name]
    if name in _stack:
        raise UnitsError(f"circular units definition: {' -> '.join(_stack + (name,))}")
    units = model.units(name)
    if units is None:
        raise UnitsError(f"units '{name}' are not defined")
    factor = 1.0
    dim = [0.0] * len(DIMENSIONS)
    for i in range(units.unitCount()):
        reference, prefix, exponent, multiplier = units.unitAttributes(i)[:4]
        ref_factor, ref_dim = resolve(reference, model, _stack + (name,))
        if prefix in ("", None):
            p = 0
        elif prefix in PREFIXES:
            p = PREFIXES[prefix]
        else:
            p = int(prefix)
        factor *= (multiplier * 10.0 ** p * ref_factor) ** exponent
        for k in range(len(DIMENSIONS)):
            dim[k] += ref_dim[k] * exponent
    return factor, tuple(dim)


def describe(dim: tuple[float, ...]) -> str:
    parts = [f"{d}^{e:g}" if e != 1 else d for d, e in zip(DIMENSIONS, dim) if e != 0]
    return "·".join(parts) or "1"
