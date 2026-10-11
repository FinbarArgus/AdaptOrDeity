"""
Our integrator and CVODES agree on the same colony and protocol, and how long each takes.

A colony of N fermenter cells (N separate cells, as the game's module array describes them) runs the protocol of
data/colony_food_protocol_obs_data.json: cell 0 is fed (glucose 0.1 -> 50 -> 0.1 mM at its membrane) while the
others stay at background. The same obs_data.json drives both:
  - libcuflynx: the colony's CellML built from a module array of content/assembled, run by its ProtocolExecutor with
    Myokit's CVODES (one core);
  - ours: `AdaptOrDeity.Headless protocol` with the game's adaptive backward Euler integrator, on 1 worker thread and
    on the default number.
Every state of every cell is compared each second. Ours converges onto CVODES as its tolerance tightens (measured
2026-10-11: worst 5.2%, 1.2%, 0.12% at 1e-2, 1e-4, 1e-6), the worst being cell 0's ADP in the second after its food
is removed: backward Euler is first order, so the game's tolerance (1e-2) allows a few percent in sharp transients.
Our integrator is built for inputs that change all the time (restarting cheaply), CVODES for long runs of a fixed
protocol, so the timings are reported, not asserted.

Run with the libcuflynx venv (libcuflynx + our combined libcellml + Myokit): <venv>/bin/python -m pytest -s tools/interop
CELLS (default 16) sets the colony size.
"""
import csv
import json
import os
import shutil
import subprocess
import time
from pathlib import Path

import numpy as np
import pytest

REPO = Path(__file__).resolve().parents[2]
DATA = REPO / 'tools' / 'interop' / 'data'
PROTOCOL = DATA / 'colony_food_protocol_obs_data.json'       # cell 0 fed
ALL_FED = DATA / 'colony_all_fed_obs_data.json'             # every cell fed (names up to cell_63)
CELLS = int(os.environ.get('CELLS', '16'))
GAME_TOLERANCE = 1e-2       # the game's relative tolerance
TIGHT_TOLERANCE = 1e-4


def dotnet():
    return shutil.which('dotnet') or os.path.expanduser('~/.dotnet/dotnet')


def run_ours(tmp: Path, workers: int, rtol: float, protocol: Path = PROTOCOL):
    out = tmp / f'ours_{protocol.stem}_{workers}_{rtol:g}.csv'
    p = subprocess.run([dotnet(), 'run', '-c', 'Release', '--project', str(REPO / 'src' / 'AdaptOrDeity.Headless'), '--',
                        'protocol', str(protocol), str(CELLS), str(out), str(workers), repr(rtol)],
                       check=True, capture_output=True, text=True)
    summary = json.loads(p.stdout.strip().splitlines()[-1])
    with open(out) as f:
        rows = list(csv.reader(f))
    names = rows[0][1:]
    values = np.array(rows[1:], dtype=float)
    return summary, values[:, 0], dict(zip(names, values[:, 1:].T))


@pytest.fixture(scope='module')
def cvodes(tmp_path_factory):
    """The colony's states each second under the protocol, from libcuflynx + Myokit CVODES, and the wall times."""
    from libcuflynx.protocol_runners.protocol_executor import ProtocolExecutor
    from libcuflynx.scripts.script_generate_with_new_architecture import generate_with_new_architecture
    from libcuflynx.solver_wrappers import get_simulation_helper

    tmp = tmp_path_factory.mktemp('cvodes')
    (tmp / 'res').mkdir()
    records = [{'name': f'cell_{i}', 'module_type': 'generic_adaptive_cell', 'module_subtype': 'fermenter',
                'instance': 'default', 'inp_instances': [], 'out_instances': []} for i in range(CELLS)]
    (tmp / 'res' / 'colony_module_array.json').write_text(json.dumps(records))
    (tmp / 'res' / 'colony_parameters.csv').write_text('variable_name,units,value,data_reference\n')
    assert generate_with_new_architecture(False, {
        'file_prefix': 'colony', 'input_param_file': 'colony_parameters.csv', 'model_type': 'cellml',
        'solver': 'CVODE_myokit', 'resources_dir': str(tmp / 'res'), 'generated_models_dir': str(tmp / 'gen'),
        'module_library_dirs': [str(REPO / 'content' / 'assembled')], 'use_builtin_modules': False, 'DEBUG': False})

    first = json.loads(PROTOCOL.read_text())['protocol_info']
    t0 = time.perf_counter()
    helper = get_simulation_helper(str(tmp / 'gen' / 'colony' / 'colony.cellml'), 'CVODE_myokit', 'cellml',
                                   dt=1.0, sim_time=first['sim_times'][0][0], solver_info={})
    build = time.perf_counter() - t0
    results = {path: run_cvodes(helper, path) for path in (PROTOCOL, ALL_FED)}
    return {'build_s': build, **results[PROTOCOL], 'all_fed': results[ALL_FED]}


def run_cvodes(helper, path: Path):
    """States each second under the protocol in ``path`` (only the parameters of cells in the colony), and wall time."""
    from libcuflynx.protocol_runners.protocol_executor import ProtocolExecutor
    protocol = json.loads(path.read_text())['protocol_info']
    protocol['params_to_change'] = {k: v for k, v in protocol['params_to_change'].items()
                                    if int(k.split('/')[0][5:]) < CELLS}
    variables = [[f'cell_{i}/{s}' for i in range(CELLS) for s in _state_names()]]
    t0 = time.perf_counter()
    ok, by_sub, _, t_by_exp = ProtocolExecutor(helper).run_protocol(protocol, result_variables=variables)
    run = time.perf_counter() - t0
    assert ok
    names = variables[0]
    series = {name: [] for name in names}
    for sub in range(len(protocol['sim_times'][0])):
        result = by_sub[(0, sub)]
        for k, name in enumerate(names):
            trace = np.asarray(result[0][k] if len(result) == 1 else result[k], dtype=float).ravel()
            series[name].append(trace if sub == 0 else trace[1:])
    return {'run_s': run, 't': np.asarray(t_by_exp[0]), 'series': {k: np.concatenate(v) for k, v in series.items()}}


def _state_names():
    """The cell's states, from the game's module library (tools/scripts/build_cell_library.py)."""
    import ctypes
    import glob
    lib = ctypes.CDLL(glob.glob(str(REPO / 'src' / 'AdaptOrDeity.Native' / 'bin' / 'cells' / '*aod_modules.*'))[0])
    names = []
    for i in range(lib.bk_state_count()):
        n, u, c = ctypes.c_char_p(), ctypes.c_char_p(), ctypes.c_char_p()
        lib.bk_state_info(i, ctypes.byref(n), ctypes.byref(u), ctypes.byref(c))
        if c.value == b'cell_0_module':
            names.append(n.value.decode())
    return names


def worst_differences(t_ours, ours, cvodes):
    """Largest difference over time of each cell's state, relative to the largest size the state reaches (CVODES)."""
    assert np.allclose(t_ours, cvodes['t']), 'output times differ'
    out = []
    for name, reference in cvodes['series'].items():
        scale = max(np.max(np.abs(reference)), 1e-12)
        out.append((float(np.max(np.abs(ours[name] - reference)) / scale), name))
    return sorted(out, reverse=True)


def test_our_integrator_converges_onto_cvodes(cvodes, tmp_path):
    previous = None
    for rtol, limit in [(GAME_TOLERANCE, 8e-2), (TIGHT_TOLERANCE, 2e-2), (1e-6, 3e-3)]:
        summary, t, ours = run_ours(tmp_path, 1, rtol)
        worst = worst_differences(t, ours, cvodes)
        print(f'\nrelative tolerance {rtol:g}: worst differences from CVODES '
              + ', '.join(f'{name} {err:.1e}' for err, name in worst[:4]))
        assert worst[0][0] < limit, worst[:5]
        if previous is not None:
            assert worst[0][0] < previous, 'a tighter tolerance must bring ours closer to CVODES'
        previous = worst[0][0]


@pytest.mark.parametrize('protocol', [PROTOCOL, ALL_FED], ids=['cell_0_fed', 'every_cell_fed'])
def test_timings(cvodes, tmp_path, protocol):
    reference = cvodes if protocol == PROTOCOL else cvodes['all_fed']
    workers = max(1, min(8, os.cpu_count() // 2 - 1))
    rows = [('CVODES (libcuflynx + Myokit), 1 core', reference['run_s'],
             f"+ {cvodes['build_s']:.1f} s to build and compile the model once")]
    t = None
    for rtol in (GAME_TOLERANCE, TIGHT_TOLERANCE):
        one, t, ours1 = run_ours(tmp_path, 1, rtol, protocol)
        many, _, ours_many = run_ours(tmp_path, workers, rtol, protocol)
        for name in ours1:   # threads do not change results
            assert np.array_equal(ours1[name], ours_many[name]), name
        if rtol == TIGHT_TOLERANCE:
            worst = worst_differences(t, ours1, reference)
            assert worst[0][0] < 2e-2, worst[:5]
        rows.append((f'ours, tolerance {rtol:g}, 1 worker', one['wall_s'], f"{one['accepted_steps']} steps"))
        rows.append((f'ours, tolerance {rtol:g}, {workers} workers', many['wall_s'], ''))
    simulated = float(t[-1] - t[0])
    print(f'\n{CELLS} cells, {simulated:.0f} simulated s, protocol {protocol.name}')
    for label, wall, note in rows:
        print(f'  {label:42s} {wall:8.3f} s  {simulated / wall:9.0f}x real time  '
              f'{reference["run_s"] / wall:6.1f}x CVODES  {note}')
