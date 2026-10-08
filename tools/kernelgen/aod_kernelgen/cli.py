"""aod-kernelgen: regenerate C# kernels (and optionally golden traces) from content/modules.lock.json."""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path

from . import emit_csharp, golden
from .module import ModuleError, load

REPO = Path(__file__).resolve().parents[3]
LOCK = REPO / "content" / "modules.lock.json"
KERNEL_DIR = REPO / "src" / "AdaptOrDeity.Sim.Kernels.Generated" / "Kernels"
GOLDEN_DIR = REPO / "tests" / "golden"


def _modules_commit(root: Path) -> str:
    try:
        return subprocess.run(["git", "-C", str(root), "rev-parse", "HEAD"], check=True,
                              capture_output=True, text=True).stdout.strip()
    except (OSError, subprocess.CalledProcessError):
        return "unknown"


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--goldens", action="store_true", help="also regenerate golden traces with Myokit")
    ap.add_argument("--check", action="store_true",
                    help="fail if generated kernels differ from the committed files (CI)")
    ap.add_argument("--update-lock", action="store_true", help="record current module hashes in the lock file")
    args = ap.parse_args(argv)

    lock = json.loads(LOCK.read_text())
    modules_root = (REPO / lock["modules_root"]).resolve()
    allow = lock["licence_allowlist"]
    commit = _modules_commit(modules_root)

    expected: dict[Path, str] = {}
    classes: list[str] = []
    failures: list[str] = []
    for entry in lock["modules"]:
        label = f"{entry['module_type']}/{entry['version']}"
        try:
            root = (REPO / entry["root"]).resolve() if "root" in entry else modules_root
            module = load(root, entry["category"], entry["module_type"], entry["version"], allow)
        except ModuleError as exc:
            failures.append(str(exc))
            continue
        locked = entry.get("sha256")
        if args.update_lock:
            entry["sha256"] = module.sha256
        elif locked != module.sha256:
            failures.append(f"{label}: module files changed (sha256 {module.sha256} != lock {locked}); "
                            "review the change, then run with --update-lock")
            continue
        cls = emit_csharp.class_name(module.module_type, module.version)
        classes.append(cls)
        expected[KERNEL_DIR / f"{cls}.g.cs"] = emit_csharp.emit(module)
        if args.goldens:
            for instance in entry.get("instances", ["default"]):
                g = entry.get("golden", {})
                replay = {"integrator": g.get("integrator", "rk4"), "dt": float(g.get("dt", 0.01)),
                          "nrmse_tolerance": float(g.get("nrmse_tolerance", 1e-6)),
                          "compare": g.get("compare", "all")}
                path = golden.write(module, instance, float(g.get("duration_s", 100.0)),
                                    float(g.get("interval_s", 1.0)), GOLDEN_DIR, commit, replay)
                print(f"golden  {path.relative_to(REPO)}")
    expected[KERNEL_DIR / "GeneratedKernels.g.cs"] = emit_csharp.emit_registry(classes)

    if failures:
        for f in failures:
            print(f"error: {f}", file=sys.stderr)
        return 1

    if args.check:
        stale = [p for p, text in expected.items() if not p.is_file() or p.read_text() != text]
        extra = [p for p in KERNEL_DIR.glob("*.g.cs") if p not in expected]
        for p in stale + extra:
            print(f"stale: {p.relative_to(REPO)}", file=sys.stderr)
        return 1 if stale or extra else 0

    KERNEL_DIR.mkdir(parents=True, exist_ok=True)
    for p in KERNEL_DIR.glob("*.g.cs"):
        if p not in expected:
            p.unlink()
    for p, text in expected.items():
        p.write_text(text)
        print(f"kernel  {p.relative_to(REPO)}")
    if args.update_lock:
        LOCK.write_text(json.dumps(lock, indent=2) + "\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
