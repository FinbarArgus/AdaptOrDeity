"""Unit tests for the CellML → C# generator. Run: tools/kernelgen/.venv/bin/pytest tools/kernelgen/tests"""

from pathlib import Path

import libcellml as lc
import pytest

from aod_kernelgen import units
from aod_kernelgen.module import ModuleError, load

REPO = Path(__file__).resolve().parents[3]
MODULES = REPO / "external" / "circulatory-autogen-modules" / "modules"


def _units_model(xml_units: str) -> lc.Model:
    text = f"""<?xml version='1.0'?>
<model name="u" xmlns="http://www.cellml.org/cellml/2.0#">{xml_units}</model>"""
    return lc.Parser().parseModel(text)


def test_millimolar_is_mol_per_m3():
    m = _units_model("""<units name="mM"><unit prefix="milli" units="mole"/><unit exponent="-1" units="litre"/></units>""")
    factor, dim = units.resolve("mM", m)
    assert factor == pytest.approx(1.0)
    assert dim == units._dim(mol=1, m=-3)


def test_micrometre_and_minutes():
    m = _units_model("""<units name="um"><unit prefix="micro" units="metre"/></units>
<units name="min"><unit multiplier="60" units="second"/></units>""")
    assert units.resolve("um", m) == (pytest.approx(1e-6), units._dim(m=1))
    assert units.resolve("min", m)[0] == pytest.approx(60.0)


def test_undefined_units_raise():
    with pytest.raises(units.UnitsError):
        units.resolve("nope", _units_model(""))


def test_licence_gate_rejects_unlisted_licence():
    with pytest.raises(ModuleError, match="licence"):
        load(MODULES, "transport", "tissue_diffusion_volume", "nn", licence_allowlist=["MIT"])


def test_tissue_diffusion_volume_inputs_and_outputs():
    m = load(MODULES, "transport", "tissue_diffusion_volume", "nn", licence_allowlist=["CC0-1.0"])
    assert [s.name for s in m.states] == ["C_P"]
    assert {e.name for e in m.externals} == {"C_P_init", "J_in", "J_out", "flux_c", "V_P", "M", "C50"}
    assert [a.name for a in m.algebraic] == ["M_reduced"]
    assert m.state_init == [("C_P_init", 0.0)]
    assert m.voi == "t"


def test_module_without_states_has_a_constant_golden():
    from aod_kernelgen import golden

    m = load(REPO / "content" / "upstream", "", "extracellular_face", "biofilm_ArgusUNPUBLISHED_v01",
             licence_allowlist=["CC0-1.0"])
    assert m.states == []
    header, rows = golden.run(m, "default", 3.0, 1.0)
    assert header[0] == "time" and "G_glc" in header
    assert [r[0] for r in rows] == [0.0, 1.0, 2.0, 3.0]
    g = header.index("G_glc")
    values = m.instances["default"]
    expected = values["A_f"] * values["D_glc"] / (values["d_up"] + values["d_down"])   # f_up = f_down = 1
    assert all(r[g] == pytest.approx(expected, rel=1e-12) for r in rows)
