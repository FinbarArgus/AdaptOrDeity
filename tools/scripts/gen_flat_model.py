"""The flat CellML of an upstream module version, as libcuflynx assembles it (for aod-kernelgen-flatten).

    PYTHONPATH=<circulatory-autogen-modules checkout> <libcuflynx venv>/bin/python tools/scripts/gen_flat_model.py <type>/<version>

Prints the generated model and the flat model it wrote under /tmp/circulatory_autogen_flattened.
"""
import sys, glob, os, time, warnings
warnings.filterwarnings('ignore')
from cam_testing import checks
from cam_testing.library import load_version
v = load_version(*sys.argv[1].split('/'))
t0 = time.time()
cm = checks.ComponentModel(v)
print('model', cm.model_path)
flats = [f for f in glob.glob('/tmp/circulatory_autogen_flattened/' + sys.argv[1].replace('/', '__') + '_*_flat.cellml')
         if os.path.getmtime(f) >= t0 - 1]
print('flat', flats)
