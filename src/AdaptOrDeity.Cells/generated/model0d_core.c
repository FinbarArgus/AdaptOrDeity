/* The content of this file was generated using the C profile of libCellML 0.7.1. */

#include "model0d_core.h"

#include <math.h>
#include <stdlib.h>

const char VERSION[] = "0.8.0";
const char LIBCELLML_VERSION[] = "0.7.1";

const size_t STATE_COUNT = 26;
const size_t VARIABLE_COUNT = 575;

const VariableInfo VOI_INFO = {"time", "second", "environment", VARIABLE_OF_INTEGRATION};

const VariableInfo STATE_INFO[] = {
    {"C_P", "millimolar", "vol_0_module", STATE},
    {"membrane__V", "milliV", "cell_0_module", STATE},
    {"Na_K__Ki", "millimolar", "cell_0_module", STATE},
    {"Na_K__Nai", "millimolar", "cell_0_module", STATE},
    {"Ca__B_Cai", "millimolar", "cell_0_module", STATE},
    {"Ca__Cai", "millimolar", "cell_0_module", STATE},
    {"H__H_i", "millimolar", "cell_0_module", STATE},
    {"cytosol__C_Pi", "millimolar", "cell_0_module", STATE},
    {"cytosol__C_ADP", "millimolar", "cell_0_module", STATE},
    {"cytosol__C_glc", "millimolar", "cell_0_module", STATE},
    {"cytosol__C_ATP", "millimolar", "cell_0_module", STATE},
    {"cytosol__C_lac", "millimolar", "cell_0_module", STATE},
    {"pool_Glyc__m", "dimensionless", "cell_0_module", STATE},
    {"cytosol__C_glyc", "millimolar", "cell_0_module", STATE},
    {"bio__D", "dimensionless", "cell_0_module", STATE},
    {"bio__P_growth", "mole", "cell_0_module", STATE},
    {"pool_proteome__m", "dimensionless", "cell_0_module", STATE},
    {"pool_H_ATPase__m", "dimensionless", "cell_0_module", STATE},
    {"pool_NhaA__m", "dimensionless", "cell_0_module", STATE},
    {"pool_KefC__m", "dimensionless", "cell_0_module", STATE},
    {"pool_Trk__m", "dimensionless", "cell_0_module", STATE},
    {"pool_NCX__m", "dimensionless", "cell_0_module", STATE},
    {"pool_Glf__m", "dimensionless", "cell_0_module", STATE},
    {"pool_LacT__m", "dimensionless", "cell_0_module", STATE},
    {"i_flagellum__a_eff", "dimensionless", "cell_0_module", STATE},
    {"pool_Flagellum__m", "dimensionless", "cell_0_module", STATE}
};

const VariableInfo VARIABLE_INFO[] = {
    {"C_P_init_vol_0", "millimolar", "parameters", CONSTANT},
    {"M_vol_0", "millimolar_per_s", "parameters", CONSTANT},
    {"C50_vol_0", "millimolar", "parameters", CONSTANT},
    {"V_P_vol_0", "m3", "parameters", CONSTANT},
    {"flux_c_vol_0", "mol_per_s", "parameters", CONSTANT},
    {"ATP_floor_mod_bio_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_floor_mod_protein_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_bio_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_Flagellum_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_Glf_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_Glyc_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_H_ATPase_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_KefC_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_LacT_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_NCX_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_NhaA_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_Trk_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_proteome_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_protein_cell_0", "millimolar", "parameters", CONSTANT},
    {"B_Ca_init_mod_Ca_cell_0", "millimolar", "parameters", CONSTANT},
    {"B_Ca_total_mod_Ca_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_ADP_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_ATP_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_Pi_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_e_mod_membrane_face_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glyc_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glyc_max_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_lac_e_mod_membrane_face_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_lac_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_repair_mod_bio_cell_0", "mole", "parameters", CONSTANT},
    {"Cai_init_mod_Ca_cell_0", "millimolar", "parameters", CONSTANT},
    {"Cao_mod_i_NaCa_cell_0", "millimolar", "parameters", CONSTANT},
    {"Cao_mod_i_leak_Ca_cell_0", "millimolar", "parameters", CONSTANT},
    {"D_init_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"F_cell_0", "C_per_mol", "parameters", CONSTANT},
    {"H_e_mod_membrane_face_cell_0", "millimolar", "parameters", CONSTANT},
    {"H_i_init_mod_H_cell_0", "millimolar", "parameters", CONSTANT},
    {"H_o_mod_H_cell_0", "millimolar", "parameters", CONSTANT},
    {"H_ref_mod_membrane_face_cell_0", "millimolar", "parameters", CONSTANT},
    {"I_in_mod_membrane_cell_0", "nanoA", "parameters", CONSTANT},
    {"J_eps_mod_protein_cell_0", "mol_per_s", "parameters", CONSTANT},
    {"J_ref_mod_bio_cell_0", "mol_per_s", "parameters", CONSTANT},
    {"J_ref_mod_protein_cell_0", "mol_per_s", "parameters", CONSTANT},
    {"K_ADP_gly_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ADP_hyd_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ADP_mob_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ATP_gly_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ATP_hyd_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ATP_store_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_D_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"K_Pi_gly_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_Pi_hyd_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_Pi_mob_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_act_H_mod_i_H_ATPase_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_glc_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_glc_store_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_glc_t_mod_membrane_face_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_glyc_mob_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_i_ATP_mob_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_lac_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_lac_t_mod_membrane_face_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_H_mod_i_H_ATPase_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_bio_mod_bio_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_Flagellum_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_Glf_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_Glyc_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_H_ATPase_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_KefC_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_LacT_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_NCX_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_NhaA_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_Trk_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_proteome_cell_0", "millimolar", "parameters", CONSTANT},
    {"Ki_init_mod_Na_K_cell_0", "millimolar", "parameters", CONSTANT},
    {"KmCa_mod_i_NaCa_cell_0", "millimolar", "parameters", CONSTANT},
    {"KmNai_mod_i_NaCa_cell_0", "millimolar", "parameters", CONSTANT},
    {"Ko_mod_Na_K_cell_0", "millimolar", "parameters", CONSTANT},
    {"Ksat_mod_i_NaCa_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mRNA_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_rRNA_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"N_A_mod_bio_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_i_flagellum_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_Flagellum_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_Glf_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_Glyc_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_H_ATPase_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_KefC_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_LacT_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_NCX_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_NhaA_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_Trk_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_proteome_cell_0", "per_mol", "parameters", CONSTANT},
    {"Nai_init_mod_Na_K_cell_0", "millimolar", "parameters", CONSTANT},
    {"Nao_mod_Na_K_cell_0", "millimolar", "parameters", CONSTANT},
    {"Nao_mod_i_NaCa_cell_0", "millimolar", "parameters", CONSTANT},
    {"P_init_mod_bio_cell_0", "mole", "parameters", CONSTANT},
    {"Q10_dmg_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"Q10_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
    {"R_cell_0", "J_per_mol_K", "parameters", CONSTANT},
    {"R_body_mod_i_flagellum_cell_0", "um", "parameters", CONSTANT},
    {"R_cell_mod_membrane_cell_0", "um", "parameters", CONSTANT},
    {"T_cell_0", "kelvin", "parameters", CONSTANT},
    {"T_ref_dmg_mod_bio_cell_0", "kelvin", "parameters", CONSTANT},
    {"T_ref_mod_cytosol_cell_0", "kelvin", "parameters", CONSTANT},
    {"V_gly_mod_cytosol_cell_0", "millimolar_per_s", "parameters", CONSTANT},
    {"V_hyd_mod_cytosol_cell_0", "millimolar_per_s", "parameters", CONSTANT},
    {"V_mob_mod_cytosol_cell_0", "millimolar_per_s", "parameters", CONSTANT},
    {"V_rest_approx_mod_membrane_cell_0", "milliV", "parameters", CONSTANT},
    {"V_store_max_mod_cytosol_cell_0", "millimolar_per_s", "parameters", CONSTANT},
    {"a_init_mod_i_flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"a_motor_mod_i_flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"alpha_NaCa_mod_i_NaCa_cell_0", "dimensionless", "parameters", CONSTANT},
    {"area_max_fraction_mod_protein_cell_0", "dimensionless", "parameters", CONSTANT},
    {"area_per_lipid_mod_bio_cell_0", "um2", "parameters", CONSTANT},
    {"beta_H_mod_H_cell_0", "millimolar", "parameters", CONSTANT},
    {"build_capacity_mod_protein_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_lipid_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_m_mod_membrane_cell_0", "uF_per_cm2", "parameters", CONSTANT},
    {"c_nt_denovo_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_nt_recycle_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_std_mod_H_cell_0", "millimolar", "parameters", CONSTANT},
    {"c_std_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"c_std_mod_i_H_ATPase_cell_0", "millimolar", "parameters", CONSTANT},
    {"c_syn_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"dG0_ferm_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"dG0_hyd_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"dG0_hyd_mod_i_H_ATPase_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"dG0_store_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"dT_Q10_mod_bio_cell_0", "kelvin", "parameters", CONSTANT},
    {"dT_Q10_mod_cytosol_cell_0", "kelvin", "parameters", CONSTANT},
    {"eta_mod_i_flagellum_cell_0", "pascal_second", "parameters", CONSTANT},
    {"f_ref_mod_i_flagellum_cell_0", "per_s", "parameters", CONSTANT},
    {"footprint_mod_pool_Flagellum_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_Glf_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_Glyc_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_H_ATPase_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_KefC_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_LacT_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_NCX_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_NhaA_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_Trk_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_proteome_cell_0", "nm2", "parameters", CONSTANT},
    {"g_spec_leak_H_mod_i_leak_H_cell_0", "microS_per_um2", "parameters", CONSTANT},
    {"g_spec_mod_i_KH_cell_0", "microS_per_um2", "parameters", CONSTANT},
    {"g_spec_mod_i_NaH_cell_0", "microS_per_um2", "parameters", CONSTANT},
    {"gamma_NaCa_mod_i_NaCa_cell_0", "dimensionless", "parameters", CONSTANT},
    {"gamma_leak_Ca_mod_i_leak_Ca_cell_0", "picoS", "parameters", CONSTANT},
    {"gamma_leak_K_mod_i_K_Trk_cell_0", "picoS", "parameters", CONSTANT},
    {"gamma_leak_Na_mod_i_leak_Na_cell_0", "picoS", "parameters", CONSTANT},
    {"genome_nt_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"growth_level_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"h_ATP_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"h_cell_mod_membrane_cell_0", "um", "parameters", CONSTANT},
    {"h_glyc_poly_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"h_lac_gly_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"jmax_glc_mod_membrane_face_cell_0", "mol_per_s_per_um2", "parameters", CONSTANT},
    {"jmax_lac_mod_membrane_face_cell_0", "mol_per_s_per_um2", "parameters", CONSTANT},
    {"k_bind_Ca_mod_Ca_cell_0", "per_millimolar_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_Flagellum_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_Glf_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_Glyc_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_H_ATPase_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_KefC_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_LacT_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_NCX_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_NhaA_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_Trk_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_proteome_cell_0", "per_s", "parameters", CONSTANT},
    {"k_lipid_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"k_ribo_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"k_unbind_Ca_mod_Ca_cell_0", "per_s", "parameters", CONSTANT},
    {"m_init_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_target_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_target_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_target_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_target_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_target_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_target_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_target_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_target_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_target_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_target_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_H_mod_i_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_H_rev_mod_i_flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_half_mod_i_flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_mRNA_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_ribo_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"pmf_ref_mod_i_flagellum_cell_0", "milliV", "parameters", CONSTANT},
    {"r_H_ATPase_mod_i_H_ATPase_cell_0", "per_s", "parameters", CONSTANT},
    {"r_NaCa_mod_i_NaCa_cell_0", "per_s", "parameters", CONSTANT},
    {"r_dmg_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"r_rep_max_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"repair_level_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"rho_H_ATPase_mod_i_H_ATPase_cell_0", "per_um2", "parameters", CONSTANT},
    {"rho_NaCa_mod_i_NaCa_cell_0", "per_um2", "parameters", CONSTANT},
    {"rho_leak_Ca_mod_i_leak_Ca_cell_0", "per_um2", "parameters", CONSTANT},
    {"rho_leak_K_mod_i_K_Trk_cell_0", "per_um2", "parameters", CONSTANT},
    {"rho_leak_Na_mod_i_leak_Na_cell_0", "per_um2", "parameters", CONSTANT},
    {"store_alloc_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
    {"surface_factor_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"surface_factor_mod_protein_cell_0", "dimensionless", "parameters", CONSTANT},
    {"t_half_mRNA_mod_bio_cell_0", "second", "parameters", CONSTANT},
    {"tau_bundle_mod_i_flagellum_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_Flagellum_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_Glf_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_Glyc_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_H_ATPase_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_KefC_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_LacT_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_NCX_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_NhaA_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_Trk_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_proteome_cell_0", "second", "parameters", CONSTANT},
    {"v_one_mod_i_flagellum_cell_0", "m_per_s", "parameters", CONSTANT},
    {"J_out", "mol_per_s", "multiport_sum_vol_0_J_out", COMPUTED_CONSTANT},
    {"J_in", "mol_per_s", "multiport_sum_vol_0_J_in", COMPUTED_CONSTANT},
    {"membrane__A_mem", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"membrane__Vol", "m3", "cell_0_module", COMPUTED_CONSTANT},
    {"membrane__Cm", "picoF", "cell_0_module", COMPUTED_CONSTANT},
    {"H__pH_o", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"cytosol__f_T", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"cytosol__K_eq_gly", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"cytosol__K_eq_hyd", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"cytosol__K_eq_store", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"cytosol__K_eq_mob", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"multiport_sum_mod_cytosol_J_glc_out__J_glc_out", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"multiport_sum_mod_cytosol_J_lac_out__J_lac_out", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"multiport_sum_mod_cytosol_J_ATP_out__J_ATP_out", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"multiport_sum_mod_cytosol_J_ATP_in__J_ATP_in", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"multiport_sum_mod_cytosol_J_ADP_out__J_ADP_out", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"multiport_sum_mod_cytosol_J_ADP_in__J_ADP_in", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"multiport_sum_mod_cytosol_J_Pi_out__J_Pi_out", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"multiport_sum_mod_cytosol_J_Pi_in__J_Pi_in", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"bio__n_lipid", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"bio__f_T_dmg", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"bio__M_build_bio", "mole", "cell_0_module", COMPUTED_CONSTANT},
    {"protein__A_surface", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"protein__A_protein_max", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_proteome__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Glyc__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"i_leak_Na__g_leak_Na", "microS", "cell_0_module", COMPUTED_CONSTANT},
    {"i_leak_Ca__g_leak_Ca", "microS", "cell_0_module", COMPUTED_CONSTANT},
    {"i_leak_H__g_leak_H", "microS", "cell_0_module", COMPUTED_CONSTANT},
    {"membrane_face__X_lac_e", "millimolar", "cell_0_module", COMPUTED_CONSTANT},
    {"membrane_face__J_ATP_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"membrane_face__J_ADP_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"membrane_face__J_Pi_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"i_flagellum__xi", "newton_s_per_m", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_H_ATPase__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_NhaA__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_KefC__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Trk__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_NCX__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Glf__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_LacT__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Flagellum__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"M_reduced", "millimolar_per_s", "vol_0_module", ALGEBRAIC},
    {"multiport_sum_mod_membrane_i_ion__i_ion", "nanoA", "cell_0_module", ALGEBRAIC},
    {"Na_K__E_K", "milliV", "cell_0_module", ALGEBRAIC},
    {"Na_K__E_Na", "milliV", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_Na_K_j_Na_total__j_Na_total", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_Na_K_j_K_total__j_K_total", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"Ca__CaB_Cai", "millimolar", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_Ca_j_Ca_in__j_Ca_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"Ca__Ca_total", "millimolar", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_H_j_H_prod__j_H_prod", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_H_j_H_in__j_H_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"H__pH_i", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"H__E_H", "milliV", "cell_0_module", ALGEBRAIC},
    {"H__H_total_rel", "millimolar", "cell_0_module", ALGEBRAIC},
    {"cytosol__Gamma_gly", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"cytosol__Gamma_hyd", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"cytosol__v_gly", "millimolar_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__v_hyd", "millimolar_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__A_gly", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"cytosol__A_hyd", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"cytosol__Gamma_store", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"cytosol__Gamma_mob", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"cytosol__v_store", "millimolar_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__v_mob", "millimolar_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__A_store", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"cytosol__A_mob", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"cytosol__P_diss", "watt", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_cytosol_j_ATP_ext__j_ATP_ext", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__J_ATP_turnover", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__j_H_met", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__Q_met", "watt", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_cytosol_J_glc_in__J_glc_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_cytosol_J_lac_in__J_lac_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_cytosol_j_ADP_ext__j_ADP_ext", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_cytosol_j_Pi_ext__j_Pi_ext", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__J_ATP_gly", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__J_ATP_mob", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__J_ATP_store", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__J_ATP_maint", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__M_glyc_ATP", "mole", "cell_0_module", ALGEBRAIC},
    {"bio__f_ATP_bio", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__J_RNA", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__J_ribo", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__J_lipid", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__R_repair", "per_s", "cell_0_module", ALGEBRAIC},
    {"bio__J_repair", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__x_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__throttle", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_bio_M_build_in__M_build_in", "mole", "cell_0_module", ALGEBRAIC},
    {"bio__M_build_protein", "mole", "cell_0_module", ALGEBRAIC},
    {"bio__J_growth", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__J_growth_used", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__M_daughter", "mole", "cell_0_module", ALGEBRAIC},
    {"bio__growth_progress", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__glycogen_progress", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__J_ATP_bio", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__j_ATP_bio", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__j_ADP_bio", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__j_Pi_bio", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_J_demand_in__J_demand_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__J_demand_total", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_J_ATP_in__J_ATP_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__J_ATP_total", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_J_keep_in__J_keep_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__J_keep_total", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_J_build_in__J_build_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__J_build_total", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_M_build_in__M_build_in", "mole", "cell_0_module", ALGEBRAIC},
    {"protein__M_build_protein", "mole", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_A_protein_in__A_protein_in", "um2", "cell_0_module", ALGEBRAIC},
    {"protein__A_protein_total", "um2", "cell_0_module", ALGEBRAIC},
    {"protein__x_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"protein__throttle", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"protein__J_cap", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__s_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"protein__f_area", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"protein__j_ATP_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__j_ADP_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__j_Pi_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"i_leak_Na__i_leak_Na", "nanoA", "cell_0_module", ALGEBRAIC},
    {"i_leak_Na__j_leak_Na", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_leak_Ca__E_Ca", "milliV", "cell_0_module", ALGEBRAIC},
    {"i_leak_Ca__i_leak_Ca", "nanoA", "cell_0_module", ALGEBRAIC},
    {"i_leak_Ca__j_leak_Ca", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_leak_H__i_leak_H", "nanoA", "cell_0_module", ALGEBRAIC},
    {"i_leak_H__j_leak_H", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_H_ATPase__k_H_ATPase", "nanoA", "cell_0_module", ALGEBRAIC},
    {"i_H_ATPase__A_ATP", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"i_H_ATPase__W_H", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"i_H_ATPase__f_thermo", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"i_H_ATPase__i_H_ATPase", "nanoA", "cell_0_module", ALGEBRAIC},
    {"i_H_ATPase__v_ATP_H", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_H_ATPase__j_H_pumped", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_H_ATPase__j_H_ATPase", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_H_ATPase__j_ATP_H", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_H_ATPase__j_ADP_H", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_H_ATPase__j_Pi_H", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_NaH__drive", "milliV", "cell_0_module", ALGEBRAIC},
    {"i_NaH__r_cycle", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_NaH__i_NaH", "nanoA", "cell_0_module", ALGEBRAIC},
    {"i_NaH__j_NaH_Na", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_NaH__j_NaH_H", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_KH__drive", "milliV", "cell_0_module", ALGEBRAIC},
    {"i_KH__r_cycle", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_KH__j_KH_K", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_KH__j_KH_H", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_K_Trk__g_leak_K", "microS", "cell_0_module", ALGEBRAIC},
    {"i_K_Trk__i_leak_K", "nanoA", "cell_0_module", ALGEBRAIC},
    {"i_K_Trk__j_leak_K", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_NaCa__k_NaCa", "nanoA", "cell_0_module", ALGEBRAIC},
    {"i_NaCa__i_NaCa", "nanoA", "cell_0_module", ALGEBRAIC},
    {"i_NaCa__j_NaCa_Ca", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_NaCa__j_NaCa_Na", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"membrane_face__J_glc", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"membrane_face__X_lac_i", "millimolar", "cell_0_module", ALGEBRAIC},
    {"membrane_face__J_lac", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"membrane_face__J_H_lac", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"membrane_face__J_glc_e", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"membrane_face__J_lac_e", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"membrane_face__J_glc_c", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"membrane_face__J_lac_c", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_flagellum__pmf", "milliV", "cell_0_module", ALGEBRAIC},
    {"i_flagellum__f_rot", "per_s", "cell_0_module", ALGEBRAIC},
    {"i_flagellum__j_H_flag", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"i_flagellum__i_flag", "nanoA", "cell_0_module", ALGEBRAIC},
    {"i_flagellum__P_flag", "watt", "cell_0_module", ALGEBRAIC},
    {"i_flagellum__F_one", "newton", "cell_0_module", ALGEBRAIC},
    {"i_flagellum__F_thrust", "newton", "cell_0_module", ALGEBRAIC},
    {"i_flagellum__v_swim", "m_per_s", "cell_0_module", ALGEBRAIC},
    {"i_flagellum__P_mech", "watt", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__A_protein", "um2", "cell_0_module", ALGEBRAIC}
};

double * createStatesArray()
{
    double *res = (double *) malloc(STATE_COUNT*sizeof(double));

    for (size_t i = 0; i < STATE_COUNT; ++i) {
        res[i] = NAN;
    }

    return res;
}

double * createVariablesArray()
{
    double *res = (double *) malloc(VARIABLE_COUNT*sizeof(double));

    for (size_t i = 0; i < VARIABLE_COUNT; ++i) {
        res[i] = NAN;
    }

    return res;
}

void deleteArray(double *array)
{
    free(array);
}

void initialiseVariables(double *states, double *rates, double *variables)
{
    variables[0] = 0.0597;
    states[0] = variables[0];
    variables[118] = -105.0;
    states[1] = variables[118];
    variables[74] = 200.0;
    states[2] = variables[74];
    variables[103] = 20.0;
    states[3] = variables[103];
    variables[19] = 7.991010113622174;
    states[4] = variables[19];
    variables[31] = 9.0e-05;
    states[5] = variables[31];
    variables[37] = 2.5118864315095822e-05;
    states[6] = variables[37];
    variables[23] = 10.0;
    states[7] = variables[23];
    variables[21] = 0.56;
    states[8] = variables[21];
    variables[25] = 0.002;
    states[9] = variables[25];
    variables[22] = 9.6;
    states[10] = variables[22];
    variables[29] = 0.26990316647694707;
    states[11] = variables[29];
    variables[223] = 1.0;
    states[12] = variables[223];
    variables[26] = 0.0;
    states[13] = variables[26];
    variables[34] = 0.011111111111111112;
    states[14] = variables[34];
    variables[106] = 0.0;
    states[15] = variables[106];
    variables[230] = 1.0;
    states[16] = variables[230];
    variables[224] = 1.0;
    states[17] = variables[224];
    variables[228] = 1.0;
    states[18] = variables[228];
    variables[225] = 1.0;
    states[19] = variables[225];
    variables[229] = 1.0;
    states[20] = variables[229];
    variables[227] = 1.0;
    states[21] = variables[227];
    variables[222] = 1.0;
    states[22] = variables[222];
    variables[226] = 1.0;
    states[23] = variables[226];
    variables[120] = 0.0;
    states[24] = variables[120];
    variables[221] = 1.0;
    states[25] = variables[221];
    variables[1] = -0.0558;
    variables[2] = 0.0133;
    variables[3] = 8.0e-15;
    variables[4] = 4.46e-16;
    variables[5] = 4.0;
    variables[6] = 4.0;
    variables[7] = 9.6;
    variables[8] = 9.6;
    variables[9] = 9.6;
    variables[10] = 9.6;
    variables[11] = 9.6;
    variables[12] = 9.6;
    variables[13] = 9.6;
    variables[14] = 9.6;
    variables[15] = 9.6;
    variables[16] = 9.6;
    variables[17] = 9.6;
    variables[18] = 9.6;
    variables[20] = 8.0;
    variables[24] = 0.1;
    variables[27] = 200.0;
    variables[28] = 0.0;
    variables[30] = 3.0028924454486115e-14;
    variables[32] = 10.28;
    variables[33] = 10.28;
    variables[35] = 96485.0;
    variables[36] = 5.011872336272725e-05;
    variables[38] = 5.011872336272725e-05;
    variables[39] = 0.0001;
    variables[40] = 0.0;
    variables[41] = 1.0e-30;
    variables[42] = 9.06587001723412e-19;
    variables[43] = 9.06587001723412e-19;
    variables[44] = 0.1;
    variables[45] = 0.5;
    variables[46] = 0.5;
    variables[47] = 1.0;
    variables[48] = 0.5;
    variables[49] = 1.0;
    variables[50] = 0.1;
    variables[51] = 1.0;
    variables[52] = 5.0;
    variables[53] = 5.0;
    variables[54] = 2.5118864315095822e-05;
    variables[55] = 0.1;
    variables[56] = 0.1;
    variables[57] = 4.1;
    variables[58] = 10.0;
    variables[59] = 1.0;
    variables[60] = 10.0;
    variables[61] = 4.0;
    variables[62] = 0.2;
    variables[63] = 0.1;
    variables[64] = 0.1;
    variables[65] = 0.1;
    variables[66] = 0.1;
    variables[67] = 0.1;
    variables[68] = 0.1;
    variables[69] = 0.1;
    variables[70] = 0.1;
    variables[71] = 0.1;
    variables[72] = 0.1;
    variables[73] = 0.1;
    variables[75] = 1.38;
    variables[76] = 87.5;
    variables[77] = 10.21;
    variables[78] = 0.1;
    variables[79] = 1000.0;
    variables[80] = 10128240.0;
    variables[81] = 473.0;
    variables[82] = 350.0;
    variables[83] = 4895.0;
    variables[84] = 620.0;
    variables[85] = 551.0;
    variables[86] = 302.0;
    variables[87] = 388.0;
    variables[88] = 2798.0;
    variables[89] = 300.0;
    variables[90] = 4566.0;
    variables[91] = 6.02214076e+23;
    variables[92] = 6.02214076e+23;
    variables[93] = 6.02214076e+23;
    variables[94] = 6.02214076e+23;
    variables[95] = 6.02214076e+23;
    variables[96] = 6.02214076e+23;
    variables[97] = 6.02214076e+23;
    variables[98] = 6.02214076e+23;
    variables[99] = 6.02214076e+23;
    variables[100] = 6.02214076e+23;
    variables[101] = 6.02214076e+23;
    variables[102] = 6.02214076e+23;
    variables[104] = 469.0;
    variables[105] = 469.0;
    variables[107] = 2.0;
    variables[108] = 2.0;
    variables[109] = 8.314;
    variables[110] = 1.0;
    variables[111] = 1.0;
    variables[112] = 293.15;
    variables[113] = 293.15;
    variables[114] = 293.15;
    variables[115] = 6.319127470321385;
    variables[116] = 0.08367164468728808;
    variables[117] = 0.14428780266714164;
    variables[119] = 0.05050073093349957;
    variables[121] = 0.0;
    variables[122] = 1.2;
    variables[123] = 0.15;
    variables[124] = 5.0e-07;
    variables[125] = 50.0;
    variables[126] = 5.0;
    variables[127] = 1.0;
    variables[128] = 1.0;
    variables[129] = 1.0;
    variables[130] = 1.0;
    variables[131] = 1.0;
    variables[132] = 1.0;
    variables[133] = 1.0;
    variables[134] = 1.0;
    variables[135] = 1.0;
    variables[136] = 1.0;
    variables[137] = 100.0;
    variables[138] = 1.0;
    variables[139] = 50.0;
    variables[140] = 2.0;
    variables[141] = 4.0;
    variables[142] = 4.0;
    variables[143] = 4.0;
    variables[144] = 4.0;
    variables[145] = 4.0;
    variables[146] = 4.0;
    variables[147] = 4.0;
    variables[148] = 4.0;
    variables[149] = 4.0;
    variables[150] = 4.0;
    variables[151] = 0.5;
    variables[152] = 0.5;
    variables[153] = 0.5;
    variables[154] = 0.5;
    variables[155] = 0.5;
    variables[156] = 0.5;
    variables[157] = 0.5;
    variables[158] = 0.5;
    variables[159] = 0.5;
    variables[160] = 0.5;
    variables[161] = 1000.0;
    variables[162] = 1000.0;
    variables[163] = 1000.0;
    variables[164] = 24.0;
    variables[165] = 24.0;
    variables[166] = 24.0;
    variables[167] = 24.0;
    variables[168] = 24.0;
    variables[169] = 24.0;
    variables[170] = 24.0;
    variables[171] = 24.0;
    variables[172] = 24.0;
    variables[173] = 24.0;
    variables[174] = -196000.0;
    variables[175] = -30500.0;
    variables[176] = -30500.0;
    variables[177] = -42000.0;
    variables[178] = 10.0;
    variables[179] = 10.0;
    variables[180] = 0.00108;
    variables[181] = 100.0;
    variables[182] = 2000.0;
    variables[183] = 12.0;
    variables[184] = 0.0;
    variables[185] = 100.0;
    variables[186] = 20.0;
    variables[187] = 15.0;
    variables[188] = 12.0;
    variables[189] = 15.0;
    variables[190] = 60.0;
    variables[191] = 0.0;
    variables[192] = 2.3686015649131477e-08;
    variables[193] = 1.2528761581650615e-08;
    variables[194] = 1.4452236359013608e-08;
    variables[195] = 0.35;
    variables[196] = 5.0;
    variables[197] = 50.0;
    variables[198] = 27.0;
    variables[199] = 9200000.0;
    variables[200] = 0.0;
    variables[201] = 20500.0;
    variables[202] = 2.0;
    variables[203] = 0.0;
    variables[204] = 63000.0;
    variables[205] = 1.5466948602977742e-18;
    variables[206] = 4.32863408001425e-18;
    variables[207] = 100000.0;
    variables[208] = 2.777777777777778e-06;
    variables[209] = 2.777777777777778e-06;
    variables[210] = 2.777777777777778e-06;
    variables[211] = 2.777777777777778e-06;
    variables[212] = 2.777777777777778e-06;
    variables[213] = 2.777777777777778e-06;
    variables[214] = 2.777777777777778e-06;
    variables[215] = 2.777777777777778e-06;
    variables[216] = 2.777777777777778e-06;
    variables[217] = 2.777777777777778e-06;
    variables[218] = 2.777777777777778e-06;
    variables[219] = 2.777777777777778e-06;
    variables[220] = 8000.0;
    variables[231] = 1.0;
    variables[232] = 1.0;
    variables[233] = 1.0;
    variables[234] = 1.0;
    variables[235] = 1.0;
    variables[236] = 1.0;
    variables[237] = 1.0;
    variables[238] = 1.0;
    variables[239] = 1.0;
    variables[240] = 1.0;
    variables[241] = 1.0;
    variables[242] = 11704.838040881496;
    variables[243] = 1884955.592153876;
    variables[244] = 2850.255770975698;
    variables[245] = 909.932422594125;
    variables[246] = 32757.567213388495;
    variables[247] = 2801.9209327573735;
    variables[248] = 682.4493169455936;
    variables[249] = 90.9932422594125;
    variables[250] = 16964600.329384882;
    variables[251] = 3.3333333333333335;
    variables[252] = 1200.0;
    variables[253] = 2.0;
    variables[254] = 8670.79572390783;
    variables[255] = 43982.2971502571;
    variables[256] = 122.44926189223139;
    variables[257] = 100.0;
    variables[258] = 5000.0;
    variables[259] = 1.1574074074074074e-06;
    variables[260] = 1.1574074074074073e-05;
    variables[261] = 1.0;
    variables[262] = 226.81614751349176;
    variables[263] = 222.96978330049507;
    variables[264] = 1.3806187541439416e-05;
    variables[265] = 0.0007772676368068118;
    variables[266] = 0.00016924908710165128;
    variables[267] = 0.0;
    variables[268] = 1.5;
    variables[269] = 1.5;
    variables[270] = 300.0;
    variables[271] = 0.3;
    variables[272] = 10.0;
    variables[273] = 10.0;
    variables[274] = 10.0;
    variables[275] = 10.0;
    variables[276] = 10.0;
    variables[277] = 10.0;
    variables[278] = 10.0;
    variables[279] = 10.0;
    variables[280] = 10.0;
    variables[281] = 10.0;
    variables[282] = 1.5e-05;
    variables[313] = 0.0;
    variables[314] = 0.0;
    variables[315] = 0.0;
    variables[294] = 0.0;
    variables[295] = 0.0;
    variables[296] = 0.0;
    variables[298] = 0.0;
    variables[300] = 0.0;
    variables[284] = 0.0;
    variables[283] = 0.0;
}

void computeComputedConstants(double voi, double *states, double *rates, double *variables)
{
    variables[285] = 2.0*3.14159265358979*variables[111]*variables[202];
    variables[286] = 3.14159265358979*pow(variables[111], 2.0)*variables[202]*1.0e-18;
    variables[287] = variables[138]*variables[285]*0.01;
    variables[288] = -log(variables[38]/variables[161])/log(10.0);
    variables[289] = pow(variables[108], (variables[112]-variables[114])/variables[179]);
    variables[290] = exp(-(variables[174]-2.0*variables[175])/(variables[109]*variables[112]));
    variables[291] = exp(-variables[175]/(variables[109]*variables[112]));
    variables[292] = exp(-variables[177]/(variables[109]*variables[112]));
    variables[293] = exp(-(variables[174]-variables[177]-variables[175])/(variables[109]*variables[112]));
    variables[302] = 2.0*variables[285]*variables[268]/variables[124];
    variables[303] = pow(variables[107], (variables[112]-variables[113])/variables[178]);
    variables[304] = ((variables[254]*variables[79]+variables[255]*variables[90]+variables[199])*variables[139]+variables[302]*variables[137])/variables[91];
    variables[305] = variables[285]*variables[269];
    variables[306] = variables[123]*variables[305];
    variables[307] = variables[250]*variables[191]*1.0e-6;
    variables[308] = variables[243]*variables[184]*1.0e-6;
    variables[309] = variables[285]*variables[266]*variables[198]*1.0e-6;
    variables[310] = variables[285]*variables[264]*variables[196]*1.0e-6;
    variables[311] = variables[285]*variables[192];
    variables[312] = variables[28]*variables[36]/variables[39];
    variables[316] = 6.0*3.14159265358979*variables[180]*variables[110]*1.0e-6;
    variables[317] = variables[244]*variables[185]*1.0e-6;
    variables[318] = variables[248]*variables[189]*1.0e-6;
    variables[319] = variables[245]*variables[186]*1.0e-6;
    variables[320] = variables[249]*variables[190]*1.0e-6;
    variables[321] = variables[247]*variables[188]*1.0e-6;
    variables[322] = variables[242]*variables[183]*1.0e-6;
    variables[323] = variables[246]*variables[187]*1.0e-6;
    variables[324] = variables[241]*variables[182]*1.0e-6;
    variables[297] = variables[313];
    variables[299] = variables[314];
    variables[301] = variables[315];
}

void computeRates(double voi, double *states, double *rates, double *variables)
{
    variables[325] = variables[1]*states[0]/(states[0]+variables[2]);
    rates[0] = (variables[284]+variables[283]+variables[4])/variables[3]+variables[325];
    variables[337] = variables[109]*variables[112]/variables[35]*log(variables[38]/states[6])*1000.0;
    variables[470] = variables[337]-states[1];
    variables[471] = states[24]*variables[181]*variables[470]/variables[256];
    variables[472] = states[25]*variables[471]*variables[252]/variables[92];
    variables[473] = -variables[472]*variables[35]*1000000000.0;
    variables[328] = variables[109]*variables[112]/variables[35]*log(variables[104]/states[3])*1000.0;
    variables[446] = -(states[1]+variables[328]-2.0*variables[337]);
    variables[447] = variables[285]*variables[194]*states[18]*variables[446]/variables[35]*1.0e-9;
    variables[448] = -variables[447]*variables[35]*1000000000.0;
    variables[437] = variables[251]*variables[35]*(variables[337]-states[1])*0.001;
    variables[436] = -variables[176]-variables[109]*variables[112]*log(fabs(states[8]/variables[163]*states[7]/variables[163]/(states[10]/variables[163]))+1.0e-300);
    variables[438] = 1.0-exp(-(variables[436]-variables[437])/(variables[109]*variables[112]));
    variables[435] = variables[285]*variables[262]*states[17]*variables[257]*variables[251]*1.602176634e-10;
    variables[439] = variables[435]*states[10]/(variables[62]+states[10])*states[6]/(variables[54]+states[6])*variables[438];
    variables[433] = variables[311]*(states[1]-variables[337]);
    variables[430] = variables[109]*variables[112]/(2.0*variables[35])*log(variables[33]/states[5])*1000.0;
    variables[431] = variables[310]*(states[1]-variables[430]);
    variables[458] = variables[285]*variables[263]*states[21]*variables[258]*1.602176634e-10;
    variables[459] = variables[458]*(exp(variables[195]*states[1]*1.0e-3*variables[35]/(variables[109]*variables[112]))*pow(states[3], 3.0)*variables[32]-exp((variables[195]-1.0)*states[1]*1.0e-3*variables[35]/(variables[109]*variables[112]))*pow(variables[105], 3.0)*states[5]*variables[122])/((pow(variables[76], 3.0)+pow(variables[105], 3.0))*(variables[75]+variables[32])*(1.0+variables[78]*exp((variables[195]-1.0)*states[1]*1.0e-3*variables[35]/(variables[109]*variables[112]))));
    variables[327] = variables[109]*variables[112]/variables[35]*log(variables[77]/states[2])*1000.0;
    variables[455] = variables[285]*variables[265]*states[20]*variables[197]*1.0e-6;
    variables[456] = variables[455]*(states[1]-variables[327]);
    variables[428] = variables[309]*(states[1]-variables[328]);
    variables[326] = variables[428]+variables[431]+variables[433]+variables[439]+variables[448]+variables[456]+variables[459]+variables[473];
    rates[1] = 1000000.0*(variables[40]-variables[326])/variables[287];
    variables[449] = -variables[447];
    variables[461] = -3.0*variables[459]/variables[35]*1.0e-9;
    variables[429] = -variables[428]/variables[35]*1.0e-9;
    variables[329] = variables[429]+variables[449]+variables[461];
    rates[3] = variables[329]/variables[286];
    variables[451] = variables[337]-variables[327];
    variables[452] = variables[285]*variables[193]*states[19]*variables[451]/variables[35]*1.0e-9;
    variables[453] = -variables[452];
    variables[457] = -variables[456]/variables[35]*1.0e-9;
    variables[330] = variables[457]+variables[453];
    rates[2] = variables[330]/variables[286];
    variables[331] = variables[20]-states[4];
    rates[4] = variables[220]*variables[331]-variables[207]*states[5]*states[4];
    variables[432] = -variables[431]/(2.0*variables[35])*1.0e-9;
    variables[460] = variables[459]/variables[35]*1.0e-9;
    variables[332] = variables[432]+variables[460];
    rates[5] = variables[332]/variables[286]+variables[220]*variables[331]-variables[207]*states[5]*states[4];
    variables[463] = states[11]*states[6]/variables[39];
    variables[464] = variables[206]*states[23]*variables[285]*(variables[312]/(variables[61]+variables[312])-variables[463]/(variables[61]+variables[463]));
    variables[465] = variables[464];
    variables[454] = variables[452];
    variables[450] = 2.0*variables[447];
    variables[440] = variables[439]/(variables[251]*variables[35])*1.0e-9;
    variables[441] = -variables[251]*variables[440];
    variables[442] = variables[441];
    variables[434] = -variables[433]/variables[35]*1.0e-9;
    variables[335] = variables[434]+variables[442]+variables[450]+variables[454]+variables[472]+variables[465];
    variables[346] = pow(states[11]/variables[162], 2.0)*pow(states[10]/variables[162], 3.0)/(pow(states[8]/variables[162], 3.0)*pow(states[7]/variables[162], 3.0));
    variables[348] = variables[117]*variables[289]*states[13]/(variables[58]+states[13])*states[8]/(variables[46]+states[8])*states[7]/(variables[53]+states[7])*variables[59]/(variables[59]+states[10])*(1.0-variables[346]/variables[293]);
    variables[345] = pow(states[8]/variables[162], 2.0)*pow(states[7]/variables[162], 2.0)/(states[9]/variables[162]*pow(states[10]/variables[162], 2.0));
    variables[347] = variables[119]*variables[267]*variables[289]*states[9]/(variables[56]+states[9])*states[10]/(variables[49]+states[10])/(1.0+pow(states[13]/variables[27], 8.0))*(1.0-variables[345]/variables[292]);
    variables[340] = states[8]/variables[162]*states[7]/variables[162]/(states[10]/variables[162]);
    variables[342] = variables[116]*variables[289]*states[10]/variables[48]*(1.0-variables[340]/variables[291])/(1.0+states[10]/variables[48]+(1.0+states[8]/variables[45])*(1.0+states[7]/variables[52])-1.0);
    variables[443] = -variables[440];
    variables[365] = states[10]/(variables[63]+states[10])*(variables[63]+variables[7])/variables[7];
    variables[371] = (states[10]-variables[5])/(variables[7]-variables[5]);
    variables[372] = ((variables[371]+fabs(variables[371]))/2.0+1.0-fabs((variables[371]+fabs(variables[371]))/2.0-1.0))/2.0;
    variables[375] = variables[200]*variables[42]*variables[372];
    variables[376] = variables[375]*variables[365];
    variables[369] = variables[261]*variables[260]*states[14]/(variables[50]+states[14])*variables[365];
    variables[370] = variables[369]*variables[30];
    variables[368] = variables[302]*variables[137]*variables[218]/variables[91]*variables[365];
    variables[367] = variables[255]*variables[90]*variables[140]*variables[219]/variables[91]*variables[365];
    variables[366] = variables[254]*variables[79]*variables[140]*log(2.0)/variables[270]/variables[91]*variables[365];
    variables[380] = variables[366]+variables[367]+variables[368]+variables[370]+variables[376];
    variables[381] = -variables[380];
    variables[567] = states[10]/(variables[64]+states[10])*(variables[64]+variables[8])/variables[8];
    variables[563] = variables[241]*states[25];
    variables[569] = variables[563]*variables[80]*(variables[141]+variables[127]+variables[151])*variables[208]/variables[93]*variables[567];
    variables[568] = (variables[567]+1.0-fabs(variables[567]-1.0))/2.0;
    variables[564] = (variables[231]-states[25])/variables[272];
    variables[565] = (variables[564]+fabs(variables[564]))/2.0;
    variables[570] = variables[565]*variables[241]*variables[80]*(variables[164]+variables[141])/variables[93];
    variables[396] = (states[10]-variables[6])/(variables[18]-variables[6]);
    variables[397] = ((variables[396]+fabs(variables[396]))/2.0+1.0-fabs((variables[396]+fabs(variables[396]))/2.0-1.0))/2.0;
    variables[398] = variables[126]*variables[43]*variables[397];
    variables[552] = (variables[236]-states[23])/variables[277];
    variables[553] = (variables[552]+fabs(variables[552]))/2.0;
    variables[558] = variables[553]*variables[246]*variables[85]*(variables[169]+variables[146])/variables[98];
    variables[540] = (variables[232]-states[22])/variables[273];
    variables[541] = (variables[540]+fabs(variables[540]))/2.0;
    variables[546] = variables[541]*variables[242]*variables[81]*(variables[165]+variables[142])/variables[94];
    variables[528] = (variables[237]-states[21])/variables[278];
    variables[529] = (variables[528]+fabs(variables[528]))/2.0;
    variables[534] = variables[529]*variables[247]*variables[86]*(variables[170]+variables[147])/variables[99];
    variables[516] = (variables[239]-states[20])/variables[280];
    variables[517] = (variables[516]+fabs(variables[516]))/2.0;
    variables[522] = variables[517]*variables[249]*variables[88]*(variables[172]+variables[149])/variables[101];
    variables[504] = (variables[235]-states[19])/variables[276];
    variables[505] = (variables[504]+fabs(variables[504]))/2.0;
    variables[510] = variables[505]*variables[245]*variables[84]*(variables[168]+variables[145])/variables[97];
    variables[492] = (variables[238]-states[18])/variables[279];
    variables[493] = (variables[492]+fabs(variables[492]))/2.0;
    variables[498] = variables[493]*variables[248]*variables[87]*(variables[171]+variables[148])/variables[100];
    variables[480] = (variables[234]-states[17])/variables[275];
    variables[481] = (variables[480]+fabs(variables[480]))/2.0;
    variables[486] = variables[481]*variables[244]*variables[83]*(variables[167]+variables[144])/variables[96];
    variables[417] = (variables[233]-states[12])/variables[274];
    variables[418] = (variables[417]+fabs(variables[417]))/2.0;
    variables[423] = variables[418]*variables[243]*variables[82]*(variables[166]+variables[143])/variables[95];
    variables[405] = (variables[240]-states[16])/variables[281];
    variables[406] = (variables[405]+fabs(variables[405]))/2.0;
    variables[411] = variables[406]*variables[250]*variables[89]*(variables[173]+variables[150])/variables[102];
    variables[384] = variables[411]+variables[423]+variables[486]+variables[498]+variables[510]+variables[522]+variables[534]+variables[546]+variables[558]+variables[570];
    variables[385] = variables[384];
    variables[399] = (variables[398]+variables[41])/((variables[398]+variables[385]+fabs(variables[398]-variables[385]))/2.0+variables[41]);
    variables[571] = variables[570]*variables[399]*variables[568];
    variables[572] = variables[569]+variables[571];
    variables[555] = states[10]/(variables[69]+states[10])*(variables[69]+variables[13])/variables[13];
    variables[551] = variables[246]*states[23];
    variables[557] = variables[551]*variables[85]*(variables[146]+variables[132]+variables[156])*variables[213]/variables[98]*variables[555];
    variables[556] = (variables[555]+1.0-fabs(variables[555]-1.0))/2.0;
    variables[559] = variables[558]*variables[399]*variables[556];
    variables[560] = variables[557]+variables[559];
    variables[543] = states[10]/(variables[65]+states[10])*(variables[65]+variables[9])/variables[9];
    variables[539] = variables[242]*states[22];
    variables[545] = variables[539]*variables[81]*(variables[142]+variables[128]+variables[152])*variables[209]/variables[94]*variables[543];
    variables[544] = (variables[543]+1.0-fabs(variables[543]-1.0))/2.0;
    variables[547] = variables[546]*variables[399]*variables[544];
    variables[548] = variables[545]+variables[547];
    variables[531] = states[10]/(variables[70]+states[10])*(variables[70]+variables[14])/variables[14];
    variables[527] = variables[247]*states[21];
    variables[533] = variables[527]*variables[86]*(variables[147]+variables[133]+variables[157])*variables[214]/variables[99]*variables[531];
    variables[532] = (variables[531]+1.0-fabs(variables[531]-1.0))/2.0;
    variables[535] = variables[534]*variables[399]*variables[532];
    variables[536] = variables[533]+variables[535];
    variables[519] = states[10]/(variables[72]+states[10])*(variables[72]+variables[16])/variables[16];
    variables[515] = variables[249]*states[20];
    variables[521] = variables[515]*variables[88]*(variables[149]+variables[135]+variables[159])*variables[216]/variables[101]*variables[519];
    variables[520] = (variables[519]+1.0-fabs(variables[519]-1.0))/2.0;
    variables[523] = variables[522]*variables[399]*variables[520];
    variables[524] = variables[521]+variables[523];
    variables[507] = states[10]/(variables[68]+states[10])*(variables[68]+variables[12])/variables[12];
    variables[503] = variables[245]*states[19];
    variables[509] = variables[503]*variables[84]*(variables[145]+variables[131]+variables[155])*variables[212]/variables[97]*variables[507];
    variables[508] = (variables[507]+1.0-fabs(variables[507]-1.0))/2.0;
    variables[511] = variables[510]*variables[399]*variables[508];
    variables[512] = variables[509]+variables[511];
    variables[495] = states[10]/(variables[71]+states[10])*(variables[71]+variables[15])/variables[15];
    variables[491] = variables[248]*states[18];
    variables[497] = variables[491]*variables[87]*(variables[148]+variables[134]+variables[158])*variables[215]/variables[100]*variables[495];
    variables[496] = (variables[495]+1.0-fabs(variables[495]-1.0))/2.0;
    variables[499] = variables[498]*variables[399]*variables[496];
    variables[500] = variables[497]+variables[499];
    variables[483] = states[10]/(variables[67]+states[10])*(variables[67]+variables[11])/variables[11];
    variables[479] = variables[244]*states[17];
    variables[485] = variables[479]*variables[83]*(variables[144]+variables[130]+variables[154])*variables[211]/variables[96]*variables[483];
    variables[484] = (variables[483]+1.0-fabs(variables[483]-1.0))/2.0;
    variables[487] = variables[486]*variables[399]*variables[484];
    variables[488] = variables[485]+variables[487];
    variables[420] = states[10]/(variables[66]+states[10])*(variables[66]+variables[10])/variables[10];
    variables[416] = variables[243]*states[12];
    variables[422] = variables[416]*variables[82]*(variables[143]+variables[129]+variables[153])*variables[210]/variables[95]*variables[420];
    variables[421] = (variables[420]+1.0-fabs(variables[420]-1.0))/2.0;
    variables[424] = variables[423]*variables[399]*variables[421];
    variables[425] = variables[422]+variables[424];
    variables[408] = states[10]/(variables[73]+states[10])*(variables[73]+variables[17])/variables[17];
    variables[404] = variables[250]*states[16];
    variables[410] = variables[404]*variables[89]*(variables[150]+variables[136]+variables[160])*variables[217]/variables[102]*variables[408];
    variables[409] = (variables[408]+1.0-fabs(variables[408]-1.0))/2.0;
    variables[412] = variables[411]*variables[399]*variables[409];
    variables[413] = variables[410]+variables[412];
    variables[386] = variables[413]+variables[425]+variables[488]+variables[500]+variables[512]+variables[524]+variables[536]+variables[548]+variables[560]+variables[572];
    variables[387] = variables[386];
    variables[401] = -variables[387];
    variables[352] = variables[401]+variables[381]+variables[443];
    variables[354] = variables[286]*(variables[342]+2.0*variables[347]-variables[348])-variables[352];
    variables[334] = variables[354];
    rates[6] = (variables[335]+variables[334])/variables[286]/(1.0+variables[125]/(log(10.0)*states[6]));
    variables[339] = pow(states[11]/variables[162], 2.0)*pow(states[10]/variables[162], 2.0)/(states[9]/variables[162]*pow(states[8]/variables[162], 2.0)*pow(states[7]/variables[162], 2.0));
    variables[341] = variables[115]*states[12]*variables[289]*states[9]/variables[55]*pow(states[8]/variables[44], 2.0)*pow(states[7]/variables[51], 2.0)*(1.0-variables[339]/variables[290])/((1.0+states[9]/variables[55])*pow(1.0+states[8]/variables[44], 2.0)*pow(1.0+states[7]/variables[51], 2.0)+pow(1.0+states[11]/variables[60], 2.0)*pow(1.0+states[10]/variables[47], 2.0)-1.0);
    variables[462] = variables[205]*states[22]*variables[285]*(variables[24]/(variables[57]+variables[24])-states[9]/(variables[57]+states[9]));
    variables[468] = variables[462];
    variables[356] = variables[468];
    rates[9] = (variables[356]+variables[294])/variables[286]-variables[341]-variables[347];
    variables[469] = variables[464];
    variables[357] = variables[469];
    rates[11] = (variables[357]+variables[295])/variables[286]+2.0*variables[341]+2.0*variables[348];
    rates[10] = (variables[297]+variables[296]+variables[352])/variables[286]+2.0*variables[341]-variables[342]-2.0*variables[347]+3.0*variables[348];
    variables[444] = variables[440];
    variables[382] = variables[380];
    variables[402] = variables[387];
    variables[358] = variables[402]+variables[382]+variables[444];
    rates[8] = (variables[299]+variables[298]+variables[358])/variables[286]-2.0*variables[341]+variables[342]+2.0*variables[347]-3.0*variables[348];
    variables[445] = variables[440];
    variables[383] = variables[380];
    variables[403] = variables[387];
    variables[359] = variables[403]+variables[383]+variables[445];
    rates[7] = (variables[301]+variables[300]+variables[359])/variables[286]-2.0*variables[341]+variables[342]+2.0*variables[347]-3.0*variables[348];
    rates[13] = variables[347]-variables[348];
    rates[14] = variables[259]*variables[303]-variables[369];
    rates[15] = variables[376];
    variables[407] = (variables[405]-fabs(variables[405]))/2.0;
    rates[16] = variables[406]*variables[399]*variables[409]+variables[407];
    variables[419] = (variables[417]-fabs(variables[417]))/2.0;
    rates[12] = variables[418]*variables[399]*variables[421]+variables[419];
    rates[24] = (variables[121]-states[24])/variables[271];
    variables[482] = (variables[480]-fabs(variables[480]))/2.0;
    rates[17] = variables[481]*variables[399]*variables[484]+variables[482];
    variables[494] = (variables[492]-fabs(variables[492]))/2.0;
    rates[18] = variables[493]*variables[399]*variables[496]+variables[494];
    variables[506] = (variables[504]-fabs(variables[504]))/2.0;
    rates[19] = variables[505]*variables[399]*variables[508]+variables[506];
    variables[518] = (variables[516]-fabs(variables[516]))/2.0;
    rates[20] = variables[517]*variables[399]*variables[520]+variables[518];
    variables[530] = (variables[528]-fabs(variables[528]))/2.0;
    rates[21] = variables[529]*variables[399]*variables[532]+variables[530];
    variables[542] = (variables[540]-fabs(variables[540]))/2.0;
    rates[22] = variables[541]*variables[399]*variables[544]+variables[542];
    variables[554] = (variables[552]-fabs(variables[552]))/2.0;
    rates[23] = variables[553]*variables[399]*variables[556]+variables[554];
    variables[566] = (variables[564]-fabs(variables[564]))/2.0;
    rates[25] = variables[565]*variables[399]*variables[568]+variables[566];
}

void computeVariables(double voi, double *states, double *rates, double *variables)
{
    variables[325] = variables[1]*states[0]/(states[0]+variables[2]);
    variables[327] = variables[109]*variables[112]/variables[35]*log(variables[77]/states[2])*1000.0;
    variables[328] = variables[109]*variables[112]/variables[35]*log(variables[104]/states[3])*1000.0;
    variables[331] = variables[20]-states[4];
    variables[333] = states[5]+variables[331];
    variables[336] = -log(states[6]/variables[161])/log(10.0);
    variables[337] = variables[109]*variables[112]/variables[35]*log(variables[38]/states[6])*1000.0;
    variables[338] = states[6]-variables[125]*variables[336];
    variables[339] = pow(states[11]/variables[162], 2.0)*pow(states[10]/variables[162], 2.0)/(states[9]/variables[162]*pow(states[8]/variables[162], 2.0)*pow(states[7]/variables[162], 2.0));
    variables[340] = states[8]/variables[162]*states[7]/variables[162]/(states[10]/variables[162]);
    variables[341] = variables[115]*states[12]*variables[289]*states[9]/variables[55]*pow(states[8]/variables[44], 2.0)*pow(states[7]/variables[51], 2.0)*(1.0-variables[339]/variables[290])/((1.0+states[9]/variables[55])*pow(1.0+states[8]/variables[44], 2.0)*pow(1.0+states[7]/variables[51], 2.0)+pow(1.0+states[11]/variables[60], 2.0)*pow(1.0+states[10]/variables[47], 2.0)-1.0);
    variables[342] = variables[116]*variables[289]*states[10]/variables[48]*(1.0-variables[340]/variables[291])/(1.0+states[10]/variables[48]+(1.0+states[8]/variables[45])*(1.0+states[7]/variables[52])-1.0);
    variables[343] = variables[109]*variables[112]*(log(variables[290])-log(fabs(variables[339])+1.0e-300));
    variables[344] = variables[109]*variables[112]*(log(variables[291])-log(fabs(variables[340])+1.0e-300));
    variables[345] = pow(states[8]/variables[162], 2.0)*pow(states[7]/variables[162], 2.0)/(states[9]/variables[162]*pow(states[10]/variables[162], 2.0));
    variables[346] = pow(states[11]/variables[162], 2.0)*pow(states[10]/variables[162], 3.0)/(pow(states[8]/variables[162], 3.0)*pow(states[7]/variables[162], 3.0));
    variables[347] = variables[119]*variables[267]*variables[289]*states[9]/(variables[56]+states[9])*states[10]/(variables[49]+states[10])/(1.0+pow(states[13]/variables[27], 8.0))*(1.0-variables[345]/variables[292]);
    variables[348] = variables[117]*variables[289]*states[13]/(variables[58]+states[13])*states[8]/(variables[46]+states[8])*states[7]/(variables[53]+states[7])*variables[59]/(variables[59]+states[10])*(1.0-variables[346]/variables[293]);
    variables[349] = variables[109]*variables[112]*(log(variables[292])-log(fabs(variables[345])+1.0e-300));
    variables[350] = variables[109]*variables[112]*(log(variables[293])-log(fabs(variables[346])+1.0e-300));
    variables[351] = variables[286]*(variables[341]*variables[343]+variables[342]*variables[344]+variables[347]*variables[349]+variables[348]*variables[350]);
    variables[437] = variables[251]*variables[35]*(variables[337]-states[1])*0.001;
    variables[436] = -variables[176]-variables[109]*variables[112]*log(fabs(states[8]/variables[163]*states[7]/variables[163]/(states[10]/variables[163]))+1.0e-300);
    variables[438] = 1.0-exp(-(variables[436]-variables[437])/(variables[109]*variables[112]));
    variables[435] = variables[285]*variables[262]*states[17]*variables[257]*variables[251]*1.602176634e-10;
    variables[439] = variables[435]*states[10]/(variables[62]+states[10])*states[6]/(variables[54]+states[6])*variables[438];
    variables[440] = variables[439]/(variables[251]*variables[35])*1.0e-9;
    variables[443] = -variables[440];
    variables[365] = states[10]/(variables[63]+states[10])*(variables[63]+variables[7])/variables[7];
    variables[371] = (states[10]-variables[5])/(variables[7]-variables[5]);
    variables[372] = ((variables[371]+fabs(variables[371]))/2.0+1.0-fabs((variables[371]+fabs(variables[371]))/2.0-1.0))/2.0;
    variables[375] = variables[200]*variables[42]*variables[372];
    variables[376] = variables[375]*variables[365];
    variables[369] = variables[261]*variables[260]*states[14]/(variables[50]+states[14])*variables[365];
    variables[370] = variables[369]*variables[30];
    variables[368] = variables[302]*variables[137]*variables[218]/variables[91]*variables[365];
    variables[367] = variables[255]*variables[90]*variables[140]*variables[219]/variables[91]*variables[365];
    variables[366] = variables[254]*variables[79]*variables[140]*log(2.0)/variables[270]/variables[91]*variables[365];
    variables[380] = variables[366]+variables[367]+variables[368]+variables[370]+variables[376];
    variables[381] = -variables[380];
    variables[567] = states[10]/(variables[64]+states[10])*(variables[64]+variables[8])/variables[8];
    variables[563] = variables[241]*states[25];
    variables[569] = variables[563]*variables[80]*(variables[141]+variables[127]+variables[151])*variables[208]/variables[93]*variables[567];
    variables[568] = (variables[567]+1.0-fabs(variables[567]-1.0))/2.0;
    variables[564] = (variables[231]-states[25])/variables[272];
    variables[565] = (variables[564]+fabs(variables[564]))/2.0;
    variables[570] = variables[565]*variables[241]*variables[80]*(variables[164]+variables[141])/variables[93];
    variables[396] = (states[10]-variables[6])/(variables[18]-variables[6]);
    variables[397] = ((variables[396]+fabs(variables[396]))/2.0+1.0-fabs((variables[396]+fabs(variables[396]))/2.0-1.0))/2.0;
    variables[398] = variables[126]*variables[43]*variables[397];
    variables[552] = (variables[236]-states[23])/variables[277];
    variables[553] = (variables[552]+fabs(variables[552]))/2.0;
    variables[558] = variables[553]*variables[246]*variables[85]*(variables[169]+variables[146])/variables[98];
    variables[540] = (variables[232]-states[22])/variables[273];
    variables[541] = (variables[540]+fabs(variables[540]))/2.0;
    variables[546] = variables[541]*variables[242]*variables[81]*(variables[165]+variables[142])/variables[94];
    variables[528] = (variables[237]-states[21])/variables[278];
    variables[529] = (variables[528]+fabs(variables[528]))/2.0;
    variables[534] = variables[529]*variables[247]*variables[86]*(variables[170]+variables[147])/variables[99];
    variables[516] = (variables[239]-states[20])/variables[280];
    variables[517] = (variables[516]+fabs(variables[516]))/2.0;
    variables[522] = variables[517]*variables[249]*variables[88]*(variables[172]+variables[149])/variables[101];
    variables[504] = (variables[235]-states[19])/variables[276];
    variables[505] = (variables[504]+fabs(variables[504]))/2.0;
    variables[510] = variables[505]*variables[245]*variables[84]*(variables[168]+variables[145])/variables[97];
    variables[492] = (variables[238]-states[18])/variables[279];
    variables[493] = (variables[492]+fabs(variables[492]))/2.0;
    variables[498] = variables[493]*variables[248]*variables[87]*(variables[171]+variables[148])/variables[100];
    variables[480] = (variables[234]-states[17])/variables[275];
    variables[481] = (variables[480]+fabs(variables[480]))/2.0;
    variables[486] = variables[481]*variables[244]*variables[83]*(variables[167]+variables[144])/variables[96];
    variables[417] = (variables[233]-states[12])/variables[274];
    variables[418] = (variables[417]+fabs(variables[417]))/2.0;
    variables[423] = variables[418]*variables[243]*variables[82]*(variables[166]+variables[143])/variables[95];
    variables[405] = (variables[240]-states[16])/variables[281];
    variables[406] = (variables[405]+fabs(variables[405]))/2.0;
    variables[411] = variables[406]*variables[250]*variables[89]*(variables[173]+variables[150])/variables[102];
    variables[384] = variables[411]+variables[423]+variables[486]+variables[498]+variables[510]+variables[522]+variables[534]+variables[546]+variables[558]+variables[570];
    variables[385] = variables[384];
    variables[399] = (variables[398]+variables[41])/((variables[398]+variables[385]+fabs(variables[398]-variables[385]))/2.0+variables[41]);
    variables[571] = variables[570]*variables[399]*variables[568];
    variables[572] = variables[569]+variables[571];
    variables[555] = states[10]/(variables[69]+states[10])*(variables[69]+variables[13])/variables[13];
    variables[551] = variables[246]*states[23];
    variables[557] = variables[551]*variables[85]*(variables[146]+variables[132]+variables[156])*variables[213]/variables[98]*variables[555];
    variables[556] = (variables[555]+1.0-fabs(variables[555]-1.0))/2.0;
    variables[559] = variables[558]*variables[399]*variables[556];
    variables[560] = variables[557]+variables[559];
    variables[543] = states[10]/(variables[65]+states[10])*(variables[65]+variables[9])/variables[9];
    variables[539] = variables[242]*states[22];
    variables[545] = variables[539]*variables[81]*(variables[142]+variables[128]+variables[152])*variables[209]/variables[94]*variables[543];
    variables[544] = (variables[543]+1.0-fabs(variables[543]-1.0))/2.0;
    variables[547] = variables[546]*variables[399]*variables[544];
    variables[548] = variables[545]+variables[547];
    variables[531] = states[10]/(variables[70]+states[10])*(variables[70]+variables[14])/variables[14];
    variables[527] = variables[247]*states[21];
    variables[533] = variables[527]*variables[86]*(variables[147]+variables[133]+variables[157])*variables[214]/variables[99]*variables[531];
    variables[532] = (variables[531]+1.0-fabs(variables[531]-1.0))/2.0;
    variables[535] = variables[534]*variables[399]*variables[532];
    variables[536] = variables[533]+variables[535];
    variables[519] = states[10]/(variables[72]+states[10])*(variables[72]+variables[16])/variables[16];
    variables[515] = variables[249]*states[20];
    variables[521] = variables[515]*variables[88]*(variables[149]+variables[135]+variables[159])*variables[216]/variables[101]*variables[519];
    variables[520] = (variables[519]+1.0-fabs(variables[519]-1.0))/2.0;
    variables[523] = variables[522]*variables[399]*variables[520];
    variables[524] = variables[521]+variables[523];
    variables[507] = states[10]/(variables[68]+states[10])*(variables[68]+variables[12])/variables[12];
    variables[503] = variables[245]*states[19];
    variables[509] = variables[503]*variables[84]*(variables[145]+variables[131]+variables[155])*variables[212]/variables[97]*variables[507];
    variables[508] = (variables[507]+1.0-fabs(variables[507]-1.0))/2.0;
    variables[511] = variables[510]*variables[399]*variables[508];
    variables[512] = variables[509]+variables[511];
    variables[495] = states[10]/(variables[71]+states[10])*(variables[71]+variables[15])/variables[15];
    variables[491] = variables[248]*states[18];
    variables[497] = variables[491]*variables[87]*(variables[148]+variables[134]+variables[158])*variables[215]/variables[100]*variables[495];
    variables[496] = (variables[495]+1.0-fabs(variables[495]-1.0))/2.0;
    variables[499] = variables[498]*variables[399]*variables[496];
    variables[500] = variables[497]+variables[499];
    variables[483] = states[10]/(variables[67]+states[10])*(variables[67]+variables[11])/variables[11];
    variables[479] = variables[244]*states[17];
    variables[485] = variables[479]*variables[83]*(variables[144]+variables[130]+variables[154])*variables[211]/variables[96]*variables[483];
    variables[484] = (variables[483]+1.0-fabs(variables[483]-1.0))/2.0;
    variables[487] = variables[486]*variables[399]*variables[484];
    variables[488] = variables[485]+variables[487];
    variables[420] = states[10]/(variables[66]+states[10])*(variables[66]+variables[10])/variables[10];
    variables[416] = variables[243]*states[12];
    variables[422] = variables[416]*variables[82]*(variables[143]+variables[129]+variables[153])*variables[210]/variables[95]*variables[420];
    variables[421] = (variables[420]+1.0-fabs(variables[420]-1.0))/2.0;
    variables[424] = variables[423]*variables[399]*variables[421];
    variables[425] = variables[422]+variables[424];
    variables[408] = states[10]/(variables[73]+states[10])*(variables[73]+variables[17])/variables[17];
    variables[404] = variables[250]*states[16];
    variables[410] = variables[404]*variables[89]*(variables[150]+variables[136]+variables[160])*variables[217]/variables[102]*variables[408];
    variables[409] = (variables[408]+1.0-fabs(variables[408]-1.0))/2.0;
    variables[412] = variables[411]*variables[399]*variables[409];
    variables[413] = variables[410]+variables[412];
    variables[386] = variables[413]+variables[425]+variables[488]+variables[500]+variables[512]+variables[524]+variables[536]+variables[548]+variables[560]+variables[572];
    variables[387] = variables[386];
    variables[401] = -variables[387];
    variables[352] = variables[401]+variables[381]+variables[443];
    variables[353] = variables[286]*variables[342]-variables[352];
    variables[354] = variables[286]*(variables[342]+2.0*variables[347]-variables[348])-variables[352];
    variables[355] = variables[286]*(variables[341]*(2.0*variables[204]-2.0*variables[201])+variables[342]*variables[201]+variables[347]*(2.0*variables[201]+variables[203])+variables[348]*(2.0*variables[204]-variables[203]-3.0*variables[201]))-variables[352]*variables[201];
    variables[360] = 2.0*variables[286]*variables[341];
    variables[361] = 3.0*variables[286]*variables[348];
    variables[362] = 2.0*variables[286]*variables[347];
    variables[363] = variables[286]*variables[342];
    variables[364] = 3.0*variables[286]*states[13];
    variables[573] = variables[563]*variables[80]*(variables[164]+variables[141])/variables[93];
    variables[561] = variables[551]*variables[85]*(variables[169]+variables[146])/variables[98];
    variables[549] = variables[539]*variables[81]*(variables[165]+variables[142])/variables[94];
    variables[537] = variables[527]*variables[86]*(variables[170]+variables[147])/variables[99];
    variables[525] = variables[515]*variables[88]*(variables[172]+variables[149])/variables[101];
    variables[513] = variables[503]*variables[84]*(variables[168]+variables[145])/variables[97];
    variables[501] = variables[491]*variables[87]*(variables[171]+variables[148])/variables[100];
    variables[489] = variables[479]*variables[83]*(variables[167]+variables[144])/variables[96];
    variables[426] = variables[416]*variables[82]*(variables[166]+variables[143])/variables[95];
    variables[414] = variables[404]*variables[89]*(variables[173]+variables[150])/variables[102];
    variables[373] = variables[414]+variables[426]+variables[489]+variables[501]+variables[513]+variables[525]+variables[537]+variables[549]+variables[561]+variables[573];
    variables[374] = variables[373];
    variables[377] = variables[304]+variables[374];
    variables[378] = states[15]/variables[377];
    variables[379] = variables[364]/variables[377];
    variables[382] = variables[380];
    variables[383] = variables[380];
    variables[388] = variables[410]+variables[422]+variables[485]+variables[497]+variables[509]+variables[521]+variables[533]+variables[545]+variables[557]+variables[569];
    variables[389] = variables[388];
    variables[390] = variables[412]+variables[424]+variables[487]+variables[499]+variables[511]+variables[523]+variables[535]+variables[547]+variables[559]+variables[571];
    variables[391] = variables[390];
    variables[392] = variables[414]+variables[426]+variables[489]+variables[501]+variables[513]+variables[525]+variables[537]+variables[549]+variables[561]+variables[573];
    variables[393] = variables[392];
    variables[574] = variables[324]*states[25];
    variables[562] = variables[323]*states[23];
    variables[550] = variables[322]*states[22];
    variables[538] = variables[321]*states[21];
    variables[526] = variables[320]*states[20];
    variables[514] = variables[319]*states[19];
    variables[502] = variables[318]*states[18];
    variables[490] = variables[317]*states[17];
    variables[427] = variables[308]*states[12];
    variables[415] = variables[307]*states[16];
    variables[394] = variables[415]+variables[427]+variables[490]+variables[502]+variables[514]+variables[526]+variables[538]+variables[550]+variables[562]+variables[574];
    variables[395] = variables[394];
    variables[400] = variables[395]/variables[306];
    variables[402] = variables[387];
    variables[403] = variables[387];
    variables[407] = (variables[405]-fabs(variables[405]))/2.0;
    variables[419] = (variables[417]-fabs(variables[417]))/2.0;
    variables[428] = variables[309]*(states[1]-variables[328]);
    variables[429] = -variables[428]/variables[35]*1.0e-9;
    variables[430] = variables[109]*variables[112]/(2.0*variables[35])*log(variables[33]/states[5])*1000.0;
    variables[431] = variables[310]*(states[1]-variables[430]);
    variables[432] = -variables[431]/(2.0*variables[35])*1.0e-9;
    variables[433] = variables[311]*(states[1]-variables[337]);
    variables[434] = -variables[433]/variables[35]*1.0e-9;
    variables[441] = -variables[251]*variables[440];
    variables[442] = variables[441];
    variables[444] = variables[440];
    variables[445] = variables[440];
    variables[446] = -(states[1]+variables[328]-2.0*variables[337]);
    variables[447] = variables[285]*variables[194]*states[18]*variables[446]/variables[35]*1.0e-9;
    variables[448] = -variables[447]*variables[35]*1000000000.0;
    variables[449] = -variables[447];
    variables[450] = 2.0*variables[447];
    variables[451] = variables[337]-variables[327];
    variables[452] = variables[285]*variables[193]*states[19]*variables[451]/variables[35]*1.0e-9;
    variables[453] = -variables[452];
    variables[454] = variables[452];
    variables[455] = variables[285]*variables[265]*states[20]*variables[197]*1.0e-6;
    variables[456] = variables[455]*(states[1]-variables[327]);
    variables[457] = -variables[456]/variables[35]*1.0e-9;
    variables[458] = variables[285]*variables[263]*states[21]*variables[258]*1.602176634e-10;
    variables[459] = variables[458]*(exp(variables[195]*states[1]*1.0e-3*variables[35]/(variables[109]*variables[112]))*pow(states[3], 3.0)*variables[32]-exp((variables[195]-1.0)*states[1]*1.0e-3*variables[35]/(variables[109]*variables[112]))*pow(variables[105], 3.0)*states[5]*variables[122])/((pow(variables[76], 3.0)+pow(variables[105], 3.0))*(variables[75]+variables[32])*(1.0+variables[78]*exp((variables[195]-1.0)*states[1]*1.0e-3*variables[35]/(variables[109]*variables[112]))));
    variables[460] = variables[459]/variables[35]*1.0e-9;
    variables[461] = -3.0*variables[459]/variables[35]*1.0e-9;
    variables[462] = variables[205]*states[22]*variables[285]*(variables[24]/(variables[57]+variables[24])-states[9]/(variables[57]+states[9]));
    variables[463] = states[11]*states[6]/variables[39];
    variables[464] = variables[206]*states[23]*variables[285]*(variables[312]/(variables[61]+variables[312])-variables[463]/(variables[61]+variables[463]));
    variables[465] = variables[464];
    variables[466] = -variables[462];
    variables[467] = -variables[464];
    variables[468] = variables[462];
    variables[469] = variables[464];
    variables[470] = variables[337]-states[1];
    variables[471] = states[24]*variables[181]*variables[470]/variables[256];
    variables[472] = states[25]*variables[471]*variables[252]/variables[92];
    variables[473] = -variables[472]*variables[35]*1000000000.0;
    variables[474] = variables[472]*variables[35]*variables[470]*0.001;
    variables[475] = variables[316]*variables[282]*variables[471]/variables[181]*(1.0+variables[253])/(states[25]+variables[253]);
    variables[476] = states[25]*variables[475];
    variables[477] = variables[476]/variables[316];
    variables[478] = variables[476]*variables[477];
    variables[482] = (variables[480]-fabs(variables[480]))/2.0;
    variables[494] = (variables[492]-fabs(variables[492]))/2.0;
    variables[506] = (variables[504]-fabs(variables[504]))/2.0;
    variables[518] = (variables[516]-fabs(variables[516]))/2.0;
    variables[530] = (variables[528]-fabs(variables[528]))/2.0;
    variables[542] = (variables[540]-fabs(variables[540]))/2.0;
    variables[554] = (variables[552]-fabs(variables[552]))/2.0;
    variables[566] = (variables[564]-fabs(variables[564]))/2.0;
    variables[326] = variables[428]+variables[431]+variables[433]+variables[439]+variables[448]+variables[456]+variables[459]+variables[473];
    variables[329] = variables[429]+variables[449]+variables[461];
    variables[330] = variables[457]+variables[453];
    variables[332] = variables[432]+variables[460];
    variables[335] = variables[434]+variables[442]+variables[450]+variables[454]+variables[472]+variables[465];
    variables[334] = variables[354];
    variables[358] = variables[402]+variables[382]+variables[444];
    variables[359] = variables[403]+variables[383]+variables[445];
    variables[356] = variables[468];
    variables[357] = variables[469];
}
