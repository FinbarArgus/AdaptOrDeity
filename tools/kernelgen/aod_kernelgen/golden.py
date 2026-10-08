"""Golden traces: run one module version + instance standalone in Myokit (CVODE) with inputs held constant.

This mirrors how cam_testing runs a module on its own; the C# kernel tests must reproduce these traces.
"""

from __future__ import annotations

import csv
import json
import tempfile
from pathlib import Path

import libcellml as lc
import myokit
import myokit.formats

from .module import AnalysedModule, ModuleError

RTOL = 1e-10
ATOL = 1e-12


def standalone_cellml(module: AnalysedModule, instance: str) -> str:
    """CellML 2.0 model with every input fixed at the instance value and states initialised."""
    values = module.instances.get(instance)
    if values is None:
        raise ModuleError(f"{module.module_type}/{module.version}: no instance {instance!r}")
    model = module.model.clone()
    component = model.component(module.component)
    for i in range(component.variableCount()):
        component.variable(i).setInterfaceType("none")
    for ext in module.externals:
        if ext.name in values:
            value = values[ext.name]
        elif ext.role == "variable":
            value = 0.0  # unconnected port input (e.g. a face flux): nothing flows in
        else:
            raise ModuleError(f"{module.module_type}/{module.version}: instance {instance} lacks {ext.name}")
        component.variable(ext.name).setInitialValue(float(value))
    for state, (ext_name, literal) in zip(module.states, module.state_init):
        start = values[ext_name] if ext_name else literal
        component.variable(state.name).setInitialValue(float(start))
    validator = lc.Validator()
    validator.validateModel(model)
    errors = [validator.issue(i).description() for i in range(validator.errorCount())] if hasattr(validator, "errorCount") else []
    if errors:
        raise ModuleError(f"standalone model invalid: {errors}")
    return lc.Printer().printModel(model)


def run(module: AnalysedModule, instance: str, duration: float, interval: float) -> tuple[list[str], list[list[float]]]:
    text = standalone_cellml(module, instance)
    with tempfile.TemporaryDirectory() as tmp:
        path = Path(tmp) / "standalone.cellml"
        path.write_text(text)
        model = myokit.formats.importer("cellml").model(str(path))
    columns = [v.name for v in module.states] + [v.name for v in module.algebraic]
    # Myokit cannot log quantities that are constant once inputs are fixed (e.g. membrane area); leave them out.
    columns = [n for n in columns if not model.get(f"{module.component}.{n}").is_constant()]
    qnames = [f"{module.component}.{n}" for n in columns]
    sim = myokit.Simulation(model)
    sim.set_tolerance(ATOL, RTOL)
    time_name = model.time().qname()
    log = sim.run(duration + 0.5 * interval, log_interval=interval, log=[time_name] + qnames)
    rows = [[t] + [log[q][i] for q in qnames] for i, t in enumerate(log[time_name])]
    return ["time"] + columns, rows


def write(module: AnalysedModule, instance: str, duration: float, interval: float, out_dir: Path,
          modules_commit: str, replay: dict | None = None) -> Path:
    header, rows = run(module, instance, duration, interval)
    target = out_dir / module.module_type / module.version
    target.mkdir(parents=True, exist_ok=True)
    csv_path = target / f"{instance}.csv"
    with csv_path.open("w", newline="") as f:
        w = csv.writer(f, lineterminator="\n")
        w.writerow(header)
        for r in rows:
            w.writerow([repr(float(x)) for x in r])
    meta = {
        "module": f"{module.category}/{module.module_type}/versions/{module.version}",
        "instance": instance,
        "source_sha256": module.sha256,
        "modules_commit": modules_commit,
        "solver": f"myokit {myokit.__version__} CVODE",
        "rtol": RTOL,
        "atol": ATOL,
        "duration_s": duration,
        "interval_s": interval,
        "inputs": "externals held at instance values; unconnected port inputs = 0",
        # how the C# test replays it: integrator (rk4 | backward_euler), its step, and the NRMSE it must reach
        "replay": replay or {"integrator": "rk4", "dt": 0.01, "nrmse_tolerance": 1e-6},
    }
    (target / f"{instance}.meta.json").write_text(json.dumps(meta, indent=2) + "\n")
    return csv_path
