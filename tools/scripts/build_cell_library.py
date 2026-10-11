"""The game's cell in libcuflynx's generated C (docs/plans/libcuflynx-solver.md).

Regenerates src/AdaptOrDeity.Cells/generated from content/assembled through libcuflynx (a one-cell module array,
BE_batched_c's C with the lock's outputs), then builds it with CMake into src/AdaptOrDeity.Native/bin/cells:
libaod_cell.so (.dll, .dylib) and the static libaod_cell_static.a (aod_cell_static.lib) for the NativeAOT library.

    <libcuflynx venv>/bin/python tools/scripts/build_cell_library.py            # regenerate and build
    <libcuflynx venv>/bin/python tools/scripts/build_cell_library.py --build    # build the committed sources only

--build needs only CMake and a C compiler (no libcuflynx, no libCellML): what CI does on every platform.
"""
import json
import os
import shutil
import subprocess
import sys
import tempfile
import time

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CELLS = os.path.join(REPO, 'src', 'AdaptOrDeity.Cells')
GENERATED = os.path.join(CELLS, 'generated')
SOURCES = ('model0d_core.c', 'model0d_core.h', 'batched_kernels.c', 'batched_kernels.h', 'batched_runtime.c')


def regenerate():
    from libcuflynx.scripts.script_generate_with_new_architecture import generate_with_new_architecture
    from libcuflynx.generators import kernel_ir
    from libcuflynx.generators.cpp import batched

    work = tempfile.mkdtemp(prefix='aod_cell_')
    res = os.path.join(work, 'res')
    os.makedirs(res)
    records = [{'name': 'cell_0', 'module_type': 'generic_adaptive_cell', 'module_subtype': 'fermenter',
                'instance': 'default', 'inp_instances': [], 'out_instances': []}]
    with open(os.path.join(res, 'cell_module_array.json'), 'w') as f:
        json.dump(records, f)
    with open(os.path.join(res, 'cell_parameters.csv'), 'w') as f:
        f.write('variable_name,units,value,data_reference\n')
    assert generate_with_new_architecture(False, {
        'file_prefix': 'cell', 'input_param_file': 'cell_parameters.csv', 'model_type': 'cellml',
        'solver': 'CVODE_myokit', 'resources_dir': res, 'generated_models_dir': os.path.join(work, 'gen'),
        'module_library_dirs': [os.path.join(REPO, 'content', 'assembled')], 'use_builtin_modules': False,
        'DEBUG': False})
    # The values the game reads after every step (modules.lock.json "outputs"); the rest are computed on demand.
    with open(os.path.join(REPO, 'content', 'modules.lock.json')) as f:
        lock = json.load(f)
    entries = lock['modules'] if isinstance(lock, dict) and 'modules' in lock else lock
    outputs = next(e for e in entries if isinstance(e, dict) and e.get('version') == 'fermenter').get('outputs', [])
    _, am = kernel_ir.analyse_cellml(os.path.join(work, 'gen', 'cell', 'cell.cellml'))
    files = batched.sources(am, kernel_ir.build_kernel_model(am), 'generic_adaptive_cell_fermenter', outputs)
    os.makedirs(GENERATED, exist_ok=True)
    for name in SOURCES:
        with open(os.path.join(GENERATED, name), 'w', newline='\n') as f:
            f.write(files[name])
    shutil.rmtree(work, ignore_errors=True)


def build():
    build_dir = os.path.join(REPO, 'build', 'cells')
    subprocess.run(['cmake', '-S', CELLS, '-B', build_dir, '-DCMAKE_BUILD_TYPE=Release'], check=True,
                   stdout=subprocess.DEVNULL)
    subprocess.run(['cmake', '--build', build_dir, '--config', 'Release'], check=True, stdout=subprocess.DEVNULL)


if __name__ == '__main__':
    t0 = time.perf_counter()
    if '--build' not in sys.argv[1:]:
        regenerate()
    t1 = time.perf_counter()
    build()
    out = os.path.join(REPO, 'src', 'AdaptOrDeity.Native', 'bin', 'cells')
    print(f'generated in {t1 - t0:.1f} s, built in {time.perf_counter() - t1:.1f} s:',
          ', '.join(sorted(n for n in os.listdir(out) if 'aod_cell' in n)))
