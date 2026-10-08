"""Turn a supermodule, flattened by libcuflynx into a multi-component CellML 2.0 model, into one module version.

libcuflynx (circulatory_autogen) assembles a supermodule's submodules and writes a flat model: one component per
submodule, a ``parameters`` component holding every parameter, multiport-sum components, and connections between them.
kernelgen compiles single-component modules, so this merges the model into one component. Each connected variable set
becomes one variable: a parameter keeps its parameter name (``rho_H_ATPase_mod_i_H_ATPase``), anything else is named
``<submodule>__<variable>`` after the component that defines it (``membrane__V``). The result is written in the module
library's layout (``<type>_<version>_modules.cellml``, ``_units.cellml``, ``_modules_config.json``, and the default
instance), so ``module.load`` and the emitter treat it like any other version.

    aod-kernelgen-flatten <flat.cellml> <out_root> <module_type> <version> [--source <text>]
"""

from __future__ import annotations

import argparse
import csv
import json
import xml.etree.ElementTree as ET
from pathlib import Path

CELLML = "http://www.cellml.org/cellml/2.0#"
MATHML = "http://www.w3.org/1998/Math/MathML"
PARAM_COMPONENTS = ("parameters", "parameters_global")
TIME = ("environment", "time")


def _short(component: str) -> str:
    name = component
    if name.startswith("mod_"):
        name = name[4:]
    if name.endswith("_module"):
        name = name[:-7]
    return name


class _Sets:
    def __init__(self):
        self.parent: dict[tuple[str, str], tuple[str, str]] = {}

    def find(self, x):
        self.parent.setdefault(x, x)
        while self.parent[x] != x:
            self.parent[x] = self.parent[self.parent[x]]
            x = self.parent[x]
        return x

    def union(self, a, b):
        ra, rb = self.find(a), self.find(b)
        if ra != rb:
            self.parent[rb] = ra


def flatten(flat_path: Path, out_root: Path, module_type: str, version: str, source: str = "",
            modules_root: Path | None = None, supermodule_config: Path | None = None) -> Path:
    ET.register_namespace("", CELLML)
    ET.register_namespace("cellml", CELLML)
    tree = ET.parse(flat_path)
    root = tree.getroot()
    q = lambda tag: f"{{{CELLML}}}{tag}"

    units = [u for u in root.findall(q("units"))]
    components = {c.get("name"): c for c in root.findall(q("component"))}
    sets = _Sets()
    variables: dict[tuple[str, str], ET.Element] = {}
    defined: set[tuple[str, str]] = set()
    for cname, comp in components.items():
        for v in comp.findall(q("variable")):
            key = (cname, v.get("name"))
            variables[key] = v
            sets.find(key)
            if v.get("initial_value") is not None:
                defined.add(key)
        for math in comp.findall(f"{{{MATHML}}}math"):
            for eq in math.findall(f"{{{MATHML}}}apply"):
                lhs = list(eq)[1] if len(eq) > 1 else None
                if lhs is None:
                    continue
                if lhs.tag == f"{{{MATHML}}}ci":
                    defined.add((cname, lhs.text.strip()))
                elif lhs.tag == f"{{{MATHML}}}apply":   # d/dt x
                    ci = lhs.find(f"{{{MATHML}}}ci")
                    if ci is not None:
                        defined.add((cname, ci.text.strip()))
    for conn in root.findall(q("connection")):
        c1, c2 = conn.get("component_1"), conn.get("component_2")
        for mv in conn.findall(q("map_variables")):
            sets.union((c1, mv.get("variable_1")), (c2, mv.get("variable_2")))

    members: dict[tuple[str, str], list[tuple[str, str]]] = {}
    for key in variables:
        members.setdefault(sets.find(key), []).append(key)

    if modules_root is not None and supermodule_config is not None:
        _restore_literal_units(components, units, modules_root, supermodule_config)

    rep_name: dict[tuple[str, str], str] = {}
    params: set[str] = set()
    out_vars: list[tuple[str, str, str | None]] = []   # name, units, initial value (raw, in member-local names)
    init_owner: dict[str, tuple[str, str]] = {}
    for root_key, keys in members.items():
        param = [k for k in keys if k[0] in PARAM_COMPONENTS]
        time = [k for k in keys if k == TIME]
        owner = [k for k in keys if k in defined and k[0] not in PARAM_COMPONENTS]
        if time:
            name = "t"
        elif param:
            name = param[0][1]
            params.add(name)
        elif owner:
            name = f"{_short(owner[0][0])}__{owner[0][1]}"
        else:
            name = f"{_short(keys[0][0])}__{keys[0][1]}"   # unconnected input: stays at its instance value (none)
        for k in keys:
            rep_name[k] = name
        source_var = (param or owner or keys)[0]
        init = variables[source_var].get("initial_value")
        if init is not None:
            init_owner[name] = source_var
        out_vars.append((name, variables[source_var].get("units"), init))
    if len({n for n, _, _ in out_vars}) != len(out_vars):
        raise ValueError("variable names collide after merging")

    # One component with every variable and every equation, names rewritten.
    comp_out = ET.Element(q("component"), {"name": "cell"})
    instance: dict[str, tuple[str, float]] = {}
    for name, unit, init in sorted(out_vars):
        attrs = {"name": name, "units": unit}
        if init is not None:
            if name in params:
                instance[name] = (unit, float(init))     # parameters become instance values, not literals
            else:
                try:
                    float(init)
                    attrs["initial_value"] = init
                except ValueError:                        # initialised from another variable of its component
                    attrs["initial_value"] = rep_name[(init_owner[name][0], init)]
        ET.SubElement(comp_out, q("variable"), attrs)
    for cname, comp in components.items():
        for math in comp.findall(f"{{{MATHML}}}math"):
            for ci in math.iter(f"{{{MATHML}}}ci"):
                ci.text = rep_name[(cname, ci.text.strip())]
            comp_out.append(math)
    maths = [m for m in comp_out.findall(f"{{{MATHML}}}math")]
    for m in maths:
        comp_out.remove(m)

    d = out_root / module_type / "versions" / version
    (d / "instances" / "default").mkdir(parents=True, exist_ok=True)
    stem = f"{module_type}_{version}"
    (d / f"{stem}_modules.cellml").write_text(_serialise(f"{module_type}_{version}", units, comp_out, maths))
    (d / f"{stem}_units.cellml").write_text(
        f'<?xml version="1.0" encoding="UTF-8"?>\n<model xmlns="{CELLML}" name="{stem}_units"/>\n')
    config = [{
        "module_type": module_type, "module_subtype": version, "module_format": "cellml",
        "component_file": f"{stem}_modules.cellml", "component_type": "cell", "default_instance": "default",
        "licence": "CC0-1.0", "assembled_from": source,
        "entrance_ports": [], "exit_ports": [], "general_ports": [],
        "variables_and_units": [[n, u, "access", "constant" if n in params else "variable"]
                                for n, u, _ in sorted(out_vars) if n != "t"],
    }]
    (d / f"{stem}_modules_config.json").write_text(json.dumps(config, indent=1) + "\n")
    with (d / "instances" / "default" / "default_parameters.csv").open("w", newline="") as f:
        w = csv.writer(f, lineterminator="\n")
        w.writerow(["variable_name", "units", "value", "data_reference", "sourced"])
        for name, (unit, value) in sorted(instance.items()):
            w.writerow([name, unit, repr(value), "from the supermodule default instance", "no"])
    return d


def _restore_literal_units(components, units, modules_root: Path, supermodule_config: Path) -> None:
    """Give each literal in a submodule's equations the units it has in the source module.

    libcuflynx renames units while flattening and, for some conversion factors, attaches the inverse or a dimensionless
    unit to the number (e.g. 1e-18 tagged um3_per_m3 for m3_per_um3), which leaves the flat model unit-inconsistent.
    The literals of each submodule appear in the same order as in its source file, so units are restored by position;
    the values must agree, or flattening stops.
    """
    known = {u.get("name") for u in units}
    cfg = json.loads(supermodule_config.read_text())[0]
    for sub in cfg["submodules"]:
        comp = components.get(f"mod_{sub['name']}_module")
        if comp is None:
            continue
        matches = [d for d in modules_root.rglob(f"{sub['module_type']}_{sub['module_subtype']}_modules.cellml")
                   if d.parent.name == sub["module_subtype"]]
        if len(matches) != 1:
            raise ValueError(f"source of {sub['name']}: found {matches}")
        src = ET.parse(matches[0]).getroot()
        src_cn = [c for c in src.iter() if c.tag.endswith("}cn")]
        flat_cn = [c for m in comp.findall(f"{{{MATHML}}}math") for c in m.iter(f"{{{MATHML}}}cn")]
        if len(src_cn) != len(flat_cn):
            raise ValueError(f"{sub['name']}: {len(src_cn)} literals in the source, {len(flat_cn)} in the flat model")
        def value(c):
            parts = [t.strip() for t in c.itertext() if t.strip()]
            return float(parts[0]) * 10 ** float(parts[1]) if len(parts) == 2 else float(parts[0])
        unit_attr = [k for k in ("units", f"{{{CELLML}}}units")]
        src_units_file = matches[0].with_name(matches[0].name.replace("_modules.cellml", "_units.cellml"))
        src_units = {u.get("name"): u for u in ET.parse(src_units_file).getroot() if u.tag.endswith("}units")}
        for a, b in zip(src_cn, flat_cn):
            va, vb = value(a), value(b)
            if abs(va - vb) > 1e-12 * max(abs(va), abs(vb), 1e-300):
                raise ValueError(f"{sub['name']}: literal {va} in the source but {vb} in the flat model")
            ua = next(a.get(k) for k in a.attrib if k.endswith("units"))
            kb = next(k for k in b.attrib if k.endswith("units"))
            b.set(kb, ua)
            if ua not in known and ua not in ("dimensionless",) and ua in src_units:
                el = ET.Element(f"{{{CELLML}}}units", {"name": ua})
                for x in src_units[ua]:
                    ET.SubElement(el, f"{{{CELLML}}}unit", dict(x.attrib))
                units.append(el)
                known.add(ua)


def _serialise(name: str, units, component, maths) -> str:
    """CellML 2.0 text, written out directly: libcellml needs MathML as the default namespace inside each <math>."""
    from xml.sax.saxutils import quoteattr
    attrs = lambda el: "".join(f" {k.rsplit('}', 1)[-1]}={quoteattr(v)}" for k, v in el.attrib.items())
    out = ['<?xml version="1.0" encoding="UTF-8"?>',
           f'<model xmlns="{CELLML}" xmlns:cellml="{CELLML}" name="{name}">']
    for u in units:
        out.append(f"  <units{attrs(u)}>")
        out += [f"    <unit{attrs(x)}/>" for x in u]
        out.append("  </units>")
    out.append(f'  <component name="{component.get("name")}">')
    out += [f"    <variable{attrs(v)}/>" for v in component.findall(f"{{{CELLML}}}variable")]
    ET.register_namespace("", MATHML)
    ET.register_namespace("cellml", CELLML)
    for m in maths:
        text = ET.tostring(m, encoding="unicode")
        if "xmlns:cellml=" not in text.split(">", 1)[0]:
            text = text.replace("<math", f'<math xmlns:cellml="{CELLML}"', 1)
        out.append(text)
    out += ["  </component>", "</model>"]
    return "\n".join(out) + "\n"


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("flat", type=Path)
    ap.add_argument("out_root", type=Path)
    ap.add_argument("module_type")
    ap.add_argument("version")
    ap.add_argument("--source", default="", help="provenance recorded in the config")
    ap.add_argument("--modules-root", type=Path, help="module library, to restore literal units from the sources")
    ap.add_argument("--supermodule-config", type=Path, help="the supermodule's modules_config.json")
    ap.add_argument("--variant", action="append", default=[], metavar="NAME:PARAM=VALUE[,PARAM=VALUE]",
                    help="extra instance: the default with these parameters changed (e.g. famine:C_glc_e_mod_membrane_face=0)")
    args = ap.parse_args(argv)
    d = flatten(args.flat, args.out_root, args.module_type, args.version, args.source, args.modules_root,
                args.supermodule_config)
    for spec in args.variant:
        name, changes = spec.split(":", 1)
        values = dict(c.split("=", 1) for c in changes.split(","))
        rows = list(csv.reader((d / "instances" / "default" / "default_parameters.csv").open()))
        for r in rows[1:]:
            if r[0] in values:
                r[2], r[3] = repr(float(values.pop(r[0]))), f"variant {name}"
        if values:
            raise SystemExit(f"variant {name}: unknown parameters {sorted(values)}")
        (d / "instances" / name).mkdir(exist_ok=True)
        with (d / "instances" / name / f"{name}_parameters.csv").open("w", newline="") as f:
            csv.writer(f, lineterminator="\n").writerows(rows)
    print(f"wrote {d}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
