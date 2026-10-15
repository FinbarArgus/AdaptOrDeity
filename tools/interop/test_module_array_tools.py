"""
The tools we build models with read the module arrays the game writes (FermenterWorld.ToModuleArray):
libcuflynx's schema accepts them, libcuflynx builds their CellML from content/assembled, libcellml validates and
analyses that model with the player's values in it, and PhLynx builds and exports them.

Run with a Python that has libcuflynx (libCellML 0.7 support: circulatory_autogen branch feat/libcellml-0-7 until it
merges), our combined libcellml build (libcellml/perf/build-combined: the speed-ups and the imported-units fix; 0.6.3
gives some conversion factors the inverse of their units), jsonschema and pytest:
    <venv>/bin/python -m pytest tools/interop
PhLynx needs node >= 22.15, a PhLynx checkout with node_modules (PHLYNX_DIR, default ../phlynx) and jsdom (JSDOM_DIR,
default ../circulatory-autogen-modules/tools/phlynx_bridge); without them that test skips.
"""
import csv
import json
import os
import shutil
import subprocess
from pathlib import Path

import jsonschema
import libcellml
import pytest
from libcuflynx.schemas import MODULE_ARRAY_SCHEMA, load_schema
from libcuflynx.scripts.script_generate_with_new_architecture import generate_with_new_architecture

REPO = Path(__file__).resolve().parents[2]
SIBLINGS = REPO.parent
LIBRARY = REPO / 'content' / 'assembled'
CELL = LIBRARY / 'generic_adaptive_cell' / 'versions' / 'fermenter'
BRIDGE = REPO / 'external' / 'circulatory-autogen-modules' / 'tools' / 'phlynx_bridge' / 'export_omex.mjs'
PREFIX = 'creature'
STATES_PER_CELL = 34   # with the recycled amino acids (protein__N_aa) and four damage types (bio__D_phys, _rad, _ribo, _energy)


@pytest.fixture(scope='module')
def game_model(tmp_path_factory):
    """The game's model of a one-cell creature with some settings changed (the headless `model` command)."""
    out = tmp_path_factory.mktemp('game')
    dotnet = shutil.which('dotnet') or os.path.expanduser('~/.dotnet/dotnet')
    subprocess.run([dotnet, 'run', '-c', 'Release', '--project', str(REPO / 'src' / 'AdaptOrDeity.Headless'), '--',
                    'model', str(out), '10', 'H_ATPase_expression=2', 'flagella=3', 'repair=0.5'],
                   check=True, capture_output=True, text=True)
    return out


def read(directory):
    with open(directory / f'{PREFIX}_module_array.json') as f:
        records = json.load(f)
    with open(directory / f'{PREFIX}_parameters.csv') as f:
        rows = list(csv.DictReader(f))
    return records, rows


@pytest.fixture(params=[1, 2], ids=['one_cell', 'two_cells'])
def model(request, game_model, tmp_path):
    """The game's array, and for two cells a copy of its cell named as the game names a daughter (ModuleArrayTests)."""
    records, rows = read(game_model)
    for n in range(1, request.param):
        records.append(dict(records[0], name=f'cell_{n}'))
        rows += [dict(r, variable_name=r['variable_name'].removesuffix('_cell_0') + f'_cell_{n}') for r in rows
                 if r['variable_name'].endswith('_cell_0')]
    with open(tmp_path / f'{PREFIX}_module_array.json', 'w') as f:
        json.dump(records, f)
    with open(tmp_path / f'{PREFIX}_parameters.csv', 'w', newline='') as f:
        w = csv.DictWriter(f, fieldnames=['variable_name', 'units', 'value', 'data_reference'])
        w.writeheader()
        w.writerows(rows)
    return tmp_path, request.param


def test_libcuflynx_schema_accepts_the_array(model):
    directory, _ = model
    records, _ = read(directory)
    jsonschema.Draft202012Validator(load_schema(MODULE_ARRAY_SCHEMA)).validate(records)


def generate(directory, generated):
    """libcuflynx's CellML for the model, flattened by libcellml (libcellml.Model)."""
    try:
        ok = generate_with_new_architecture(False, {
            'file_prefix': PREFIX, 'input_param_file': f'{PREFIX}_parameters.csv', 'model_type': 'cellml',
            'solver': 'CVODE_myokit', 'resources_dir': str(directory), 'generated_models_dir': str(generated),
            'module_library_dirs': [str(LIBRARY)], 'use_builtin_modules': False, 'DEBUG': False})
    except SystemExit as e:   # libcuflynx exits on errors
        pytest.fail(f'libcuflynx exited ({e.code})')
    assert ok
    return libcellml.Parser(False).parseModel((generated / PREFIX / f'{PREFIX}_flat.cellml').read_text())


def test_libcuflynx_builds_a_model_libcellml_validates_and_analyses(model, tmp_path):
    directory, cells = model
    model = generate(directory, tmp_path)

    validator = libcellml.Validator()
    validator.validateModel(model)
    assert validator.errorCount() == 0, [validator.error(i).description() for i in range(validator.errorCount())]
    analyser = libcellml.Analyser()
    analyser.analyseModel(model)
    assert analyser.errorCount() == 0, [analyser.error(i).description() for i in range(analyser.errorCount())]
    analysed = analyser.analyserModel() if hasattr(analyser, 'analyserModel') else analyser.model()   # 0.7 / 0.6
    assert analysed.stateCount() == STATES_PER_CELL * cells

    _, rows = read(directory)
    for n in range(cells):
        h_atpase = 2.0 * next(float(r['value']) for r in csv.DictReader((CELL / 'instances' / 'default' / 'default_parameters.csv').open())
                              if r['variable_name'] == 's_target_mod_pool_H_ATPase')   # twice its starting share
        for name, value in [('s_target_mod_pool_H_ATPase', h_atpase), ('m_target_mod_pool_Flagellum', 3.0),
                            ('repair_level_mod_bio', 0.5)]:
            row = next(r for r in rows if r['variable_name'] == f'{name}_cell_{n}')
            assert float(row['value']) == value
            assert float(initial_value(model, f'{name}_cell_{n}')) == value


def test_libcuflynx_keeps_the_units_of_literals(game_model, tmp_path):
    analyser = libcellml.Analyser()
    analyser.analyseModel(generate(game_model, tmp_path))
    unit_issues = [analyser.issue(i).description() for i in range(analyser.issueCount())
                   if 'are not equivalent' in analyser.issue(i).description()]
    assert unit_issues == []


def initial_value(model, name):
    for c in range(model.componentCount()):
        v = model.component(c).variable(name)
        if v is not None:
            return v.initialValue()
    raise KeyError(name)


def test_phlynx_builds_and_exports_the_model(model, tmp_path):
    directory, cells = model
    node = shutil.which('node')
    phlynx = Path(os.environ.get('PHLYNX_DIR') or SIBLINGS / 'phlynx')
    jsdom = Path(os.environ.get('JSDOM_DIR') or SIBLINGS / 'circulatory-autogen-modules' / 'tools' / 'phlynx_bridge')
    if not node or not (phlynx / 'node_modules').is_dir() or not (jsdom / 'node_modules' / 'jsdom').is_dir():
        pytest.skip('PhLynx bridge needs node, a PhLynx checkout with node_modules (PHLYNX_DIR) and jsdom (JSDOM_DIR)')
    records, rows = read(directory)
    job = {
        'cellml': [str(CELL / 'generic_adaptive_cell_fermenter_modules.cellml')],
        'units': [str(CELL / 'generic_adaptive_cell_fermenter_units.cellml')],
        'configs': [str(CELL / 'generic_adaptive_cell_fermenter_modules_config.json')],
        # PhLynx's instance-array importer takes inputs and outputs as space-separated names
        'instances': [{'name': r['name'], 'module_type': r['module_type'], 'module_subtype': r['module_subtype'],
                       'inp_instances': ' '.join(r['inp_instances']), 'out_instances': ' '.join(r['out_instances'])}
                      for r in records],
        'parameters': rows, 'sim_time': 1.0, 'dt': 0.1}
    job_path = tmp_path / 'job.json'
    job_path.write_text(json.dumps(job))
    omex = tmp_path / f'{PREFIX}.omex'
    p = subprocess.run([node, str(BRIDGE), str(job_path), str(omex)], capture_output=True, text=True, timeout=600,
                       env=dict(os.environ, PHLYNX_DIR=str(phlynx), JSDOM_DIR=str(jsdom)))
    if p.returncode == 3:
        pytest.skip(p.stderr.strip()[:500])
    report = json.loads(p.stdout.strip().splitlines()[-1])
    assert report.get('ok'), report
    assert omex.is_file()
