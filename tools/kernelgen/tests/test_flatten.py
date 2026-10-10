"""aod_kernelgen.flatten: a libcuflynx-style flat model (parameters + submodule + sum component) becomes one module."""

from pathlib import Path

from aod_kernelgen import emit_csharp
from aod_kernelgen.flatten import flatten
from aod_kernelgen.module import load

FLAT = """<?xml version="1.0" encoding="UTF-8"?>
<model xmlns="http://www.cellml.org/cellml/2.0#" xmlns:cellml="http://www.cellml.org/cellml/2.0#" name="flat">
  <units name="per_s"><unit exponent="-1" units="second"/></units>
  <component name="parameters">
    <variable name="k_mod_pool" units="per_s" initial_value="2.0" interface="public"/>
    <variable name="x_init_mod_pool" units="dimensionless" initial_value="1.0" interface="public"/>
    <variable name="j_mod_src" units="per_s" initial_value="0.5" interface="public"/>
  </component>
  <component name="environment"><variable name="time" units="second" interface="public"/></component>
  <component name="mod_pool_module">
    <variable name="t" units="second" interface="public"/>
    <variable name="k" units="per_s" interface="public"/>
    <variable name="x_init" units="dimensionless" interface="public"/>
    <variable name="x" units="dimensionless" initial_value="x_init" interface="public"/>
    <variable name="j_in" units="per_s" interface="public"/>
    <math xmlns="http://www.w3.org/1998/Math/MathML">
      <apply><eq/><apply><diff/><bvar><ci>t</ci></bvar><ci>x</ci></apply>
        <apply><minus/><ci>j_in</ci><apply><times/><ci>k</ci><ci>x</ci></apply></apply></apply>
    </math>
  </component>
  <component name="multiport_sum_mod_pool_j_in">
    <variable name="j_in" units="per_s" interface="public"/>
    <variable name="j_src" units="per_s" interface="public"/>
    <math xmlns="http://www.w3.org/1998/Math/MathML">
      <apply><eq/><ci>j_in</ci><apply><times/><cn cellml:units="dimensionless">1.0</cn><ci>j_src</ci></apply></apply>
    </math>
  </component>
  <connection component_1="parameters" component_2="mod_pool_module">
    <map_variables variable_1="k_mod_pool" variable_2="k"/>
    <map_variables variable_1="x_init_mod_pool" variable_2="x_init"/>
  </connection>
  <connection component_1="environment" component_2="mod_pool_module"><map_variables variable_1="time" variable_2="t"/></connection>
  <connection component_1="multiport_sum_mod_pool_j_in" component_2="mod_pool_module">
    <map_variables variable_1="j_in" variable_2="j_in"/>
  </connection>
  <connection component_1="parameters" component_2="multiport_sum_mod_pool_j_in">
    <map_variables variable_1="j_mod_src" variable_2="j_src"/>
  </connection>
</model>
"""


def test_flat_model_becomes_one_module(tmp_path: Path):
    src = tmp_path / "flat.cellml"
    src.write_text(FLAT)
    flatten(src, tmp_path / "out", "toy", "v1", source="test")
    m = load(tmp_path / "out", "", "toy", "v1", ["CC0-1.0"])
    assert [s.name for s in m.states] == ["pool__x"]
    assert {e.name for e in m.externals} == {"k_mod_pool", "x_init_mod_pool", "j_mod_src"}
    assert m.instances["default"] == {"j_mod_src": 0.5, "k_mod_pool": 2.0, "x_init_mod_pool": 1.0}
    assert m.state_init == [("x_init_mod_pool", 0.0)]
    text = emit_csharp.emit(m)
    assert "class Toy_v1" in text and "new PortInfo[0]" in text


ORDER = FLAT.replace(
    """    <math xmlns="http://www.w3.org/1998/Math/MathML">
      <apply><eq/><apply><diff/><bvar><ci>t</ci></bvar><ci>x</ci></apply>""",
    """    <variable name="y" units="per_s" interface="public"/>
    <math xmlns="http://www.w3.org/1998/Math/MathML">
      <apply><eq/><ci>y</ci><apply><times/><cn cellml:units="dimensionless">2.0</cn><ci>j_in</ci></apply></apply>
      <apply><eq/><apply><diff/><bvar><ci>t</ci></bvar><ci>x</ci></apply>""")


def test_algebraic_equations_follow_what_they_read(tmp_path: Path):
    # y = 2 j_in is written in the submodule, before the sum component that defines j_in: the emitted kernel must still
    # compute j_in first (libcellml's analyser keeps model order).
    flat = tmp_path / "flat.cellml"
    flat.write_text(ORDER)
    flatten(flat, tmp_path / "out", "toy", "v1")
    m = load(tmp_path / "out", "", "toy", "v1", ["CC0-1.0"])
    order = [q.target for q in m.equations if q.kind != "ode"]
    assert order.index("multiport_sum_mod_pool_j_in__j_in") < order.index("pool__y"), order
