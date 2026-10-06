"""Load one module version from the circulatory-autogen-modules layout and analyse it with libcellml."""

from __future__ import annotations

import csv
import hashlib
import json
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from pathlib import Path

import libcellml as lc

from . import units as U

ROLES = {"variable", "boundary_condition", "constant", "global_constant"}
# Port variables must already be canonical SI (multiplier 1) until the assembler supports declared
# conversions; see docs/architecture-plan.md "CellML pipeline", units gate.
PORT_SI_TOLERANCE = 1e-12


class ModuleError(ValueError):
    pass


@dataclass
class Var:
    name: str
    units: str
    si_multiplier: float
    role: str


@dataclass
class Port:
    port_type: str
    direction: str  # Entrance | Exit | General
    variables: list[str]
    multi_port: str  # None | Many | Sum | Multiply


@dataclass
class Equation:
    kind: str  # algebraic | ode | computed_constant
    target: str  # variable (algebraic) or state name (ode)
    ast: lc.AnalyserEquationAst


@dataclass
class AnalysedModule:
    category: str
    module_type: str
    version: str
    component: str
    licence: str
    sha256: str
    voi: str | None
    states: list[Var]
    externals: list[Var]
    algebraic: list[Var]
    constants: dict[str, float]  # literal-valued variables inlined into code
    state_init: list[tuple[str | None, float]]  # (external name, literal) per state
    equations: list[Equation]
    ports: list[Port]
    instances: dict[str, dict[str, float]] = field(default_factory=dict)
    model: lc.Model | None = None  # analysed CellML model (states' initial values replaced)
    cellml_text: str = ""
    units_text: str = ""


def version_dir(modules_root: Path, category: str, module_type: str, version: str) -> Path:
    return modules_root / category / module_type / "versions" / version


def _issues(obj) -> list[str]:
    return [obj.issue(i).description() for i in range(obj.issueCount())]


def _multi_port(raw) -> str:
    if raw is None:
        return "None"
    values = [str(v).lower() for v in (raw if isinstance(raw, list) else [raw])]
    if "sum" in values:
        return "Sum"
    if "multiply" in values:
        return "Multiply"
    if "true" in values:
        return "Many"
    raise ModuleError(f"unsupported multi_port {raw!r}")


def load(modules_root: Path, category: str, module_type: str, version: str,
         licence_allowlist: list[str]) -> AnalysedModule:
    d = version_dir(modules_root, category, module_type, version)
    stem = f"{module_type}_{version}"
    cellml_path = d / f"{stem}_modules.cellml"
    units_path = d / f"{stem}_units.cellml"
    config_path = d / f"{stem}_modules_config.json"
    for p in (cellml_path, units_path, config_path):
        if not p.is_file():
            raise ModuleError(f"missing {p}")

    configs = json.loads(config_path.read_text())
    matches = [c for c in configs if c.get("module_type") == module_type and c.get("module_subtype") == version]
    if len(matches) != 1:
        raise ModuleError(f"{config_path}: expected one entry for {module_type}/{version}, found {len(matches)}")
    cfg = matches[0]

    licence = cfg.get("licence", "")
    if licence not in licence_allowlist:
        raise ModuleError(f"{module_type}/{version}: licence {licence!r} is not in the allowlist {licence_allowlist}")

    digest = hashlib.sha256()
    for p in (cellml_path, units_path, config_path):
        digest.update(p.read_bytes())

    cellml_text = cellml_path.read_text()
    units_text = units_path.read_text()
    parser = lc.Parser(False)  # non-strict: accepts the CellML 1.1 files in the library
    model = parser.parseModel(cellml_text)
    units_model = parser.parseModel(units_text)
    for i in range(units_model.unitsCount()):
        model.addUnits(units_model.units(i).clone())
    model.linkUnits()

    component = model.component(cfg["component_type"])
    if component is None:
        raise ModuleError(f"{cellml_path}: no component {cfg['component_type']!r}")
    if model.componentCount() != 1 or component.componentCount() != 0:
        raise ModuleError(f"{cellml_path}: supermodules / multiple components are not supported yet")

    roles: dict[str, tuple[str, str]] = {}
    for name, units_name, _access, role in cfg["variables_and_units"]:
        if role not in ROLES:
            raise ModuleError(f"{module_type}/{version}: variable {name} has unknown role {role!r}")
        roles[name] = (units_name, role)

    # Inputs become analyser external variables: every public_interface="in" variable except the
    # variable of integration, plus anything the config marks as a constant or boundary condition.
    # libcellml maps both "in" and "out" to "public", so the direction is read from the XML.
    interfaces, bvars = _interfaces_and_bvars(cellml_text, cfg["component_type"])
    if not bvars and component.variable("t") is not None:
        # Algebraic-only module: the library still declares t; it is unused, so drop it before analysis.
        component.removeVariable("t")
    state_init_ref: dict[str, str] = {}
    external_names: list[str] = []
    for i in range(component.variableCount()):
        v = component.variable(i)
        role = roles.get(v.name(), (None, None))[1]
        if v.initialValue() and not _is_number(v.initialValue()):
            state_init_ref[v.name()] = v.initialValue()
            v.setInitialValue(0.0)  # placeholder; the kernel copies the referenced external at Initialise
        is_input = interfaces.get(v.name()) == "in" and v.name() not in bvars
        if role in ("constant", "global_constant", "boundary_condition") or is_input:
            external_names.append(v.name())

    analyser = lc.Analyser()
    for name in external_names:
        analyser.addExternalVariable(lc.AnalyserExternalVariable(component.variable(name)))
    analyser.analyseModel(model)
    errors = [d for d in _issues(analyser)]
    am = analyser.model()
    if errors or not am.isValid():
        raise ModuleError(f"{module_type}/{version}: libcellml analyser failed: {errors}")

    # Algebraic-only modules (faces, carriers) have no variable of integration; ODE modules must integrate in seconds.
    voi = am.voi().variable().name() if am.voi() else None
    if voi is None and am.stateCount() > 0:
        raise ModuleError(f"{module_type}/{version}: states but no variable of integration")
    if voi is not None:
        voi_factor, voi_dim = U.resolve(am.voi().variable().units().name(), model)
        if voi_factor != 1.0 or voi_dim != U.BUILTIN["second"][1]:
            raise ModuleError(f"{module_type}/{version}: variable of integration must be in seconds")

    def make_var(name: str, units_name: str) -> Var:
        factor, _dim = U.resolve(units_name, model)
        if name not in roles:
            raise ModuleError(f"{module_type}/{version}: variable {name} missing from variables_and_units")
        cfg_units = roles[name][0]
        if cfg_units != units_name:
            raise ModuleError(f"{module_type}/{version}: {name} is in {units_name} but the config says {cfg_units}")
        return Var(name, units_name, factor, roles[name][1])

    states = [make_var(s.variable().name(), s.variable().units().name()) for s in am.states()]
    externals: list[Var] = []
    algebraic: list[Var] = []
    constants: dict[str, float] = {}
    for av in am.variables():
        v = av.variable()
        kind = lc.AnalyserVariable.typeAsString(av.type())
        if v.name() == voi:
            continue
        if kind == "external":
            externals.append(make_var(v.name(), v.units().name()))
        elif kind in ("algebraic", "computed_constant"):
            algebraic.append(make_var(v.name(), v.units().name()))
        elif kind == "constant":
            constants[v.name()] = float(v.initialValue())
        else:
            raise ModuleError(f"{module_type}/{version}: unsupported variable kind {kind!r} for {v.name()}")

    external_set = {e.name for e in externals}
    state_init: list[tuple[str | None, float]] = []
    for s in states:
        ref = state_init_ref.get(s.name)
        if ref is None:
            state_init.append((None, float(component.variable(s.name).initialValue() or 0.0)))
        elif ref in external_set:
            state_init.append((ref, 0.0))
        else:
            raise ModuleError(f"{module_type}/{version}: state {s.name} is initialised from {ref}, which is not an input")

    equations: list[Equation] = []
    for eq in am.equations():
        kind = lc.AnalyserEquation.typeAsString(eq.type())
        if kind == "external":
            continue
        if kind not in ("algebraic", "ode", "computed_constant"):
            raise ModuleError(f"{module_type}/{version}: unsupported equation type {kind!r} (non-linear algebraic?)")
        if kind == "ode":
            target = eq.ast().leftChild().rightChild().variable().name()
        else:
            target = eq.variable(0).variable().name()
        equations.append(Equation(kind, target, eq.ast()))

    ports: list[Port] = []
    for key, direction in (("entrance_ports", "Entrance"), ("exit_ports", "Exit"), ("general_ports", "General")):
        for p in cfg.get(key, []):
            ports.append(Port(p["port_type"], direction, list(p["variables"]), _multi_port(p.get("multi_port"))))

    # Units gate on port variables.
    for p in ports:
        for name in p.variables:
            var = next((v for v in states + externals + algebraic if v.name == name), None)
            if var is None:
                raise ModuleError(f"{module_type}/{version}: port {p.port_type} variable {name} not found")
            if abs(var.si_multiplier - 1.0) > PORT_SI_TOLERANCE:
                raise ModuleError(
                    f"{module_type}/{version}: port variable {name} is in {var.units} "
                    f"(×{var.si_multiplier:g} to SI); port variables must be canonical SI")

    module = AnalysedModule(
        category=category, module_type=module_type, version=version, component=component.name(),
        licence=licence, sha256=digest.hexdigest(), voi=voi, states=states, externals=externals,
        algebraic=algebraic, constants=constants, state_init=state_init, equations=equations, ports=ports,
        model=model, cellml_text=cellml_text, units_text=units_text)
    module.instances = _load_instances(d, module)
    return module


def _load_instances(d: Path, module: AnalysedModule) -> dict[str, dict[str, float]]:
    known = {v.name: v for v in module.states + module.externals + module.algebraic}
    instances: dict[str, dict[str, float]] = {}
    root = d / "instances"
    if not root.is_dir():
        return instances
    for inst_dir in sorted(p for p in root.iterdir() if p.is_dir()):
        csv_path = inst_dir / f"{inst_dir.name}_parameters.csv"
        if not csv_path.is_file():
            continue
        values: dict[str, float] = {}
        with csv_path.open(newline="") as f:
            for row in csv.DictReader(f):
                name = row["variable_name"].strip()
                var = known.get(name)
                if var is None:
                    raise ModuleError(f"{csv_path}: unknown variable {name}")
                if row["units"].strip() != var.units:
                    raise ModuleError(f"{csv_path}: {name} given in {row['units']} but module uses {var.units}")
                values[name] = float(row["value"])
        instances[inst_dir.name] = values
    return instances


def _interfaces_and_bvars(cellml_text: str, component_name: str) -> tuple[dict[str, str], set[str]]:
    """CellML 1.1 public_interface per variable of the component, and names bound in <bvar>."""
    root = ET.fromstring(cellml_text)
    interfaces: dict[str, str] = {}
    bvars: set[str] = set()
    for comp in root.iter():
        if _local(comp.tag) != "component" or comp.get("name") != component_name:
            continue
        for el in comp.iter():
            tag = _local(el.tag)
            if tag == "variable":
                interfaces[el.get("name")] = el.get("public_interface", "none")
            elif tag == "bvar":
                for ci in el.iter():
                    if _local(ci.tag) == "ci" and ci.text:
                        bvars.add(ci.text.strip())
    return interfaces, bvars


def _local(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def _is_number(text: str) -> bool:
    try:
        float(text)
        return True
    except (TypeError, ValueError):
        return False
