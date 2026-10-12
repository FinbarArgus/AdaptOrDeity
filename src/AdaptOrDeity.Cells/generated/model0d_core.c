/* The content of this file was generated using the C profile of libCellML 0.7.1. */

#include "model0d_core.h"

#include <math.h>
#include <stdlib.h>

const char VERSION[] = "0.8.0";
const char LIBCELLML_VERSION[] = "0.7.1";

const size_t STATE_COUNT = 32;
const size_t VARIABLE_COUNT = 654;

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
    {"pool_Flagellum__m", "dimensionless", "cell_0_module", STATE},
    {"C_mx", "millimolar", "ecs_0_module", STATE},
    {"C_H", "millimolar", "ecs_0_module", STATE},
    {"C_glc", "millimolar", "ecs_0_module", STATE},
    {"C_lac", "millimolar", "ecs_0_module", STATE},
    {"C_O2", "millimolar", "ecs_0_module", STATE},
    {"N_mx_lost", "mole", "ecs_0_module", STATE}
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
    {"K_ATP_mx_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ATP_store_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_D_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"K_Pi_gly_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_Pi_hyd_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_Pi_mob_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_act_H_mod_i_H_ATPase_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_glc_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_glc_mx_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
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
    {"V_mx_max_mod_cytosol_cell_0", "millimolar_per_s", "parameters", CONSTANT},
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
    {"dG0_mx_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
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
    {"h_mx_poly_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
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
    {"mx_alloc_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
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
    {"n_ATP_mx_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
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
    {"C_glc_init_ecs_0", "millimolar", "parameters", CONSTANT},
    {"C_lac_init_ecs_0", "millimolar", "parameters", CONSTANT},
    {"C_H_init_ecs_0", "millimolar", "parameters", CONSTANT},
    {"C_O2_init_ecs_0", "millimolar", "parameters", CONSTANT},
    {"C_mx_init_ecs_0", "millimolar", "parameters", CONSTANT},
    {"V_e_ecs_0", "m3", "parameters", CONSTANT},
    {"b_H_ecs_0", "dimensionless", "parameters", CONSTANT},
    {"c_std_ecs_0", "millimolar", "parameters", CONSTANT},
    {"v_mx_ecs_0", "per_millimolar", "parameters", CONSTANT},
    {"k_mx_ecs_0", "per_s", "parameters", CONSTANT},
    {"f_mx_glc_ecs_0", "dimensionless", "parameters", CONSTANT},
    {"C_mx_max_ecs_0", "millimolar", "parameters", CONSTANT},
    {"k_ovf_ecs_0", "per_s", "parameters", CONSTANT},
    {"j_glc_cell_ecs_0", "mol_per_s", "parameters", CONSTANT},
    {"j_lac_cell_ecs_0", "mol_per_s", "parameters", CONSTANT},
    {"j_H_cell_ecs_0", "mol_per_s", "parameters", CONSTANT},
    {"j_O2_cell_ecs_0", "mol_per_s", "parameters", CONSTANT},
    {"j_mx_cell_ecs_0", "mol_per_s", "parameters", CONSTANT},
    {"D_glc_ecs_face_0", "m2_per_s", "parameters", CONSTANT},
    {"D_lac_ecs_face_0", "m2_per_s", "parameters", CONSTANT},
    {"D_H_ecs_face_0", "m2_per_s", "parameters", CONSTANT},
    {"D_O2_ecs_face_0", "m2_per_s", "parameters", CONSTANT},
    {"A_f_ecs_face_0", "m2", "parameters", CONSTANT},
    {"d_up_ecs_face_0", "metre", "parameters", CONSTANT},
    {"d_down_ecs_face_0", "metre", "parameters", CONSTANT},
    {"open_flag_ecs_face_0", "dimensionless", "parameters", CONSTANT},
    {"C_glc_up_ecs_face_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_down_ecs_face_0", "millimolar", "parameters", CONSTANT},
    {"C_lac_up_ecs_face_0", "millimolar", "parameters", CONSTANT},
    {"C_lac_down_ecs_face_0", "millimolar", "parameters", CONSTANT},
    {"C_H_up_ecs_face_0", "millimolar", "parameters", CONSTANT},
    {"C_H_down_ecs_face_0", "millimolar", "parameters", CONSTANT},
    {"C_O2_up_ecs_face_0", "millimolar", "parameters", CONSTANT},
    {"C_O2_down_ecs_face_0", "millimolar", "parameters", CONSTANT},
    {"f_up_ecs_face_0", "dimensionless", "parameters", CONSTANT},
    {"f_down_ecs_face_0", "dimensionless", "parameters", CONSTANT},
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
    {"cytosol__K_eq_mx", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
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
    {"J_glc_out", "mol_per_s", "multiport_sum_ecs_0_J_glc_out", COMPUTED_CONSTANT},
    {"J_glc_in", "mol_per_s", "multiport_sum_ecs_0_J_glc_in", COMPUTED_CONSTANT},
    {"J_lac_out", "mol_per_s", "multiport_sum_ecs_0_J_lac_out", COMPUTED_CONSTANT},
    {"J_lac_in", "mol_per_s", "multiport_sum_ecs_0_J_lac_in", COMPUTED_CONSTANT},
    {"J_H_out", "mol_per_s", "multiport_sum_ecs_0_J_H_out", COMPUTED_CONSTANT},
    {"J_H_in", "mol_per_s", "multiport_sum_ecs_0_J_H_in", COMPUTED_CONSTANT},
    {"J_O2_out", "mol_per_s", "multiport_sum_ecs_0_J_O2_out", COMPUTED_CONSTANT},
    {"J_O2_in", "mol_per_s", "multiport_sum_ecs_0_J_O2_in", COMPUTED_CONSTANT},
    {"G_glc", "m3_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"J_glc", "mol_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"J_glc_down", "mol_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"J_glc_up", "mol_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"G_lac", "m3_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"J_lac", "mol_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"J_lac_down", "mol_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"J_lac_up", "mol_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"G_H", "m3_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"J_H", "mol_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"J_H_down", "mol_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"J_H_up", "mol_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"G_O2", "m3_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"J_O2", "mol_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"J_O2_down", "mol_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
    {"J_O2_up", "mol_per_s", "ecs_face_0_module", COMPUTED_CONSTANT},
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
    {"cytosol__Gamma_mx", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"cytosol__v_mx", "millimolar_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__A_mx", "J_per_mol", "cell_0_module", ALGEBRAIC},
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
    {"cytosol__J_mx", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__J_ATP_mx", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"pool_Flagellum__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"phi", "dimensionless", "ecs_0_module", ALGEBRAIC},
    {"f_hind", "dimensionless", "ecs_0_module", ALGEBRAIC},
    {"r_mx", "millimolar_per_s", "ecs_0_module", ALGEBRAIC},
    {"r_ovf", "millimolar_per_s", "ecs_0_module", ALGEBRAIC},
    {"J_mx_lost", "mol_per_s", "ecs_0_module", ALGEBRAIC},
    {"pH", "dimensionless", "ecs_0_module", ALGEBRAIC}
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
    variables[121] = -105.0;
    states[1] = variables[121];
    variables[76] = 200.0;
    states[2] = variables[76];
    variables[105] = 20.0;
    states[3] = variables[105];
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
    variables[228] = 1.0;
    states[12] = variables[228];
    variables[26] = 0.0;
    states[13] = variables[26];
    variables[34] = 0.011111111111111112;
    states[14] = variables[34];
    variables[108] = 0.0;
    states[15] = variables[108];
    variables[235] = 1.0;
    states[16] = variables[235];
    variables[229] = 1.0;
    states[17] = variables[229];
    variables[233] = 1.0;
    states[18] = variables[233];
    variables[230] = 1.0;
    states[19] = variables[230];
    variables[234] = 1.0;
    states[20] = variables[234];
    variables[232] = 1.0;
    states[21] = variables[232];
    variables[227] = 1.0;
    states[22] = variables[227];
    variables[231] = 1.0;
    states[23] = variables[231];
    variables[123] = 0.0;
    states[24] = variables[123];
    variables[226] = 1.0;
    states[25] = variables[226];
    variables[294] = 0.0;
    states[26] = variables[294];
    variables[292] = 5.011872336272725e-05;
    states[27] = variables[292];
    variables[290] = 0.1;
    states[28] = variables[290];
    variables[291] = 0.0;
    states[29] = variables[291];
    variables[293] = 0.003;
    states[30] = variables[293];
    states[31] = 0.0;
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
    variables[50] = 1.0;
    variables[51] = 0.1;
    variables[52] = 1.0;
    variables[53] = 5.0;
    variables[54] = 5.0;
    variables[55] = 2.5118864315095822e-05;
    variables[56] = 0.1;
    variables[57] = 0.1;
    variables[58] = 0.1;
    variables[59] = 4.1;
    variables[60] = 10.0;
    variables[61] = 1.0;
    variables[62] = 10.0;
    variables[63] = 4.0;
    variables[64] = 0.2;
    variables[65] = 0.1;
    variables[66] = 0.1;
    variables[67] = 0.1;
    variables[68] = 0.1;
    variables[69] = 0.1;
    variables[70] = 0.1;
    variables[71] = 0.1;
    variables[72] = 0.1;
    variables[73] = 0.1;
    variables[74] = 0.1;
    variables[75] = 0.1;
    variables[77] = 1.38;
    variables[78] = 87.5;
    variables[79] = 10.21;
    variables[80] = 0.1;
    variables[81] = 1000.0;
    variables[82] = 10128240.0;
    variables[83] = 473.0;
    variables[84] = 350.0;
    variables[85] = 4895.0;
    variables[86] = 620.0;
    variables[87] = 551.0;
    variables[88] = 302.0;
    variables[89] = 388.0;
    variables[90] = 2798.0;
    variables[91] = 300.0;
    variables[92] = 4566.0;
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
    variables[103] = 6.02214076e+23;
    variables[104] = 6.02214076e+23;
    variables[106] = 469.0;
    variables[107] = 469.0;
    variables[109] = 2.0;
    variables[110] = 2.0;
    variables[111] = 8.314;
    variables[112] = 1.0;
    variables[113] = 1.0;
    variables[114] = 293.15;
    variables[115] = 293.15;
    variables[116] = 293.15;
    variables[117] = 6.319127470321385;
    variables[118] = 0.08367164468728808;
    variables[119] = 0.14428780266714164;
    variables[120] = 0.0072143901333570825;
    variables[122] = 0.05050073093349957;
    variables[124] = 0.0;
    variables[125] = 1.2;
    variables[126] = 0.15;
    variables[127] = 5.0e-07;
    variables[128] = 50.0;
    variables[129] = 5.0;
    variables[130] = 1.0;
    variables[131] = 1.0;
    variables[132] = 1.0;
    variables[133] = 1.0;
    variables[134] = 1.0;
    variables[135] = 1.0;
    variables[136] = 1.0;
    variables[137] = 1.0;
    variables[138] = 1.0;
    variables[139] = 1.0;
    variables[140] = 100.0;
    variables[141] = 1.0;
    variables[142] = 50.0;
    variables[143] = 2.0;
    variables[144] = 4.0;
    variables[145] = 4.0;
    variables[146] = 4.0;
    variables[147] = 4.0;
    variables[148] = 4.0;
    variables[149] = 4.0;
    variables[150] = 4.0;
    variables[151] = 4.0;
    variables[152] = 4.0;
    variables[153] = 4.0;
    variables[154] = 0.5;
    variables[155] = 0.5;
    variables[156] = 0.5;
    variables[157] = 0.5;
    variables[158] = 0.5;
    variables[159] = 0.5;
    variables[160] = 0.5;
    variables[161] = 0.5;
    variables[162] = 0.5;
    variables[163] = 0.5;
    variables[164] = 1000.0;
    variables[165] = 1000.0;
    variables[166] = 1000.0;
    variables[167] = 24.0;
    variables[168] = 24.0;
    variables[169] = 24.0;
    variables[170] = 24.0;
    variables[171] = 24.0;
    variables[172] = 24.0;
    variables[173] = 24.0;
    variables[174] = 24.0;
    variables[175] = 24.0;
    variables[176] = 24.0;
    variables[177] = -196000.0;
    variables[178] = -30500.0;
    variables[179] = -30500.0;
    variables[180] = -72500.0;
    variables[181] = -42000.0;
    variables[182] = 10.0;
    variables[183] = 10.0;
    variables[184] = 0.00108;
    variables[185] = 100.0;
    variables[186] = 2000.0;
    variables[187] = 12.0;
    variables[188] = 0.0;
    variables[189] = 100.0;
    variables[190] = 20.0;
    variables[191] = 15.0;
    variables[192] = 12.0;
    variables[193] = 15.0;
    variables[194] = 60.0;
    variables[195] = 0.0;
    variables[196] = 2.3686015649131477e-08;
    variables[197] = 1.2528761581650615e-08;
    variables[198] = 1.4452236359013608e-08;
    variables[199] = 0.35;
    variables[200] = 5.0;
    variables[201] = 50.0;
    variables[202] = 27.0;
    variables[203] = 9200000.0;
    variables[204] = 0.0;
    variables[205] = 20500.0;
    variables[206] = 2.0;
    variables[207] = 0.0;
    variables[208] = 63000.0;
    variables[209] = 0.0;
    variables[210] = 1.5466948602977742e-18;
    variables[211] = 4.32863408001425e-18;
    variables[212] = 100000.0;
    variables[213] = 2.777777777777778e-06;
    variables[214] = 2.777777777777778e-06;
    variables[215] = 2.777777777777778e-06;
    variables[216] = 2.777777777777778e-06;
    variables[217] = 2.777777777777778e-06;
    variables[218] = 2.777777777777778e-06;
    variables[219] = 2.777777777777778e-06;
    variables[220] = 2.777777777777778e-06;
    variables[221] = 2.777777777777778e-06;
    variables[222] = 2.777777777777778e-06;
    variables[223] = 2.777777777777778e-06;
    variables[224] = 2.777777777777778e-06;
    variables[225] = 8000.0;
    variables[236] = 1.0;
    variables[237] = 1.0;
    variables[238] = 1.0;
    variables[239] = 1.0;
    variables[240] = 1.0;
    variables[241] = 1.0;
    variables[242] = 1.0;
    variables[243] = 1.0;
    variables[244] = 1.0;
    variables[245] = 1.0;
    variables[246] = 0.0;
    variables[247] = 1.0;
    variables[248] = 11704.838040881496;
    variables[249] = 1884955.592153876;
    variables[250] = 2850.255770975698;
    variables[251] = 909.932422594125;
    variables[252] = 32757.567213388495;
    variables[253] = 2801.9209327573735;
    variables[254] = 682.4493169455936;
    variables[255] = 90.9932422594125;
    variables[256] = 16964600.329384882;
    variables[257] = 3.0;
    variables[258] = 3.3333333333333335;
    variables[259] = 1200.0;
    variables[260] = 2.0;
    variables[261] = 8670.79572390783;
    variables[262] = 43982.2971502571;
    variables[263] = 122.44926189223139;
    variables[264] = 100.0;
    variables[265] = 5000.0;
    variables[266] = 1.1574074074074074e-06;
    variables[267] = 1.1574074074074073e-05;
    variables[268] = 1.0;
    variables[269] = 226.81614751349176;
    variables[270] = 222.96978330049507;
    variables[271] = 1.3806187541439416e-05;
    variables[272] = 0.0007772676368068118;
    variables[273] = 0.00016924908710165128;
    variables[274] = 0.0;
    variables[275] = 1.5;
    variables[276] = 1.5;
    variables[277] = 300.0;
    variables[278] = 0.3;
    variables[279] = 10.0;
    variables[280] = 10.0;
    variables[281] = 10.0;
    variables[282] = 10.0;
    variables[283] = 10.0;
    variables[284] = 10.0;
    variables[285] = 10.0;
    variables[286] = 10.0;
    variables[287] = 10.0;
    variables[288] = 10.0;
    variables[289] = 1.5e-05;
    variables[295] = 1.4396817324298869e-18;
    variables[296] = 2599.594240021476;
    variables[297] = 1000.0;
    variables[298] = 9.719999999999999e-05;
    variables[299] = 1.0e-06;
    variables[300] = 0.5;
    variables[301] = 308.641975308642;
    variables[302] = 0.001;
    variables[303] = 0.0;
    variables[304] = 0.0;
    variables[305] = 0.0;
    variables[306] = 0.0;
    variables[307] = 0.0;
    variables[308] = 5.9e-10;
    variables[309] = 9.0e-10;
    variables[310] = 2.607594240021476e-06;
    variables[311] = 1.75e-09;
    variables[312] = 6.0e-13;
    variables[313] = 6.639528095680696e-07;
    variables[314] = 6.639528095680696e-07;
    variables[315] = 1.0;
    variables[316] = 0.1;
    variables[317] = 0.1;
    variables[318] = 0.0;
    variables[319] = 0.0;
    variables[320] = 5.011872336272725e-05;
    variables[321] = 5.011872336272725e-05;
    variables[322] = 0.003;
    variables[323] = 0.003;
    variables[324] = 1.0;
    variables[325] = 1.0;
    variables[357] = 0.0;
    variables[358] = 0.0;
    variables[359] = 0.0;
    variables[338] = 0.0;
    variables[339] = 0.0;
    variables[340] = 0.0;
    variables[342] = 0.0;
    variables[344] = 0.0;
    variables[327] = 0.0;
    variables[326] = 0.0;
    variables[370] = 0.0;
    variables[372] = 0.0;
    variables[374] = 0.0;
    variables[376] = 0.0;
    variables[369] = 0.0;
    variables[371] = 0.0;
    variables[373] = 0.0;
    variables[375] = 0.0;
}

void computeComputedConstants(double voi, double *states, double *rates, double *variables)
{
    variables[328] = 2.0*3.14159265358979*variables[113]*variables[206];
    variables[329] = 3.14159265358979*pow(variables[113], 2.0)*variables[206]*1.0e-18;
    variables[330] = variables[141]*variables[328]*0.01;
    variables[331] = -log(variables[38]/variables[164])/log(10.0);
    variables[332] = pow(variables[110], (variables[114]-variables[116])/variables[183]);
    variables[333] = exp(-(variables[177]-2.0*variables[178])/(variables[111]*variables[114]));
    variables[334] = exp(-variables[178]/(variables[111]*variables[114]));
    variables[335] = exp(-variables[181]/(variables[111]*variables[114]));
    variables[336] = exp(-(variables[177]-variables[181]-variables[178])/(variables[111]*variables[114]));
    variables[337] = exp(-variables[180]/(variables[111]*variables[114]));
    variables[346] = 2.0*variables[328]*variables[275]/variables[127];
    variables[347] = pow(variables[109], (variables[114]-variables[115])/variables[182]);
    variables[348] = ((variables[261]*variables[81]+variables[262]*variables[92]+variables[203])*variables[142]+variables[346]*variables[140])/variables[93];
    variables[349] = variables[328]*variables[276];
    variables[350] = variables[126]*variables[349];
    variables[351] = variables[256]*variables[195]*1.0e-6;
    variables[352] = variables[249]*variables[188]*1.0e-6;
    variables[353] = variables[328]*variables[273]*variables[202]*1.0e-6;
    variables[354] = variables[328]*variables[271]*variables[200]*1.0e-6;
    variables[355] = variables[328]*variables[196];
    variables[356] = variables[28]*variables[36]/variables[39];
    variables[360] = 6.0*3.14159265358979*variables[184]*variables[112]*1.0e-6;
    variables[361] = variables[250]*variables[189]*1.0e-6;
    variables[362] = variables[254]*variables[193]*1.0e-6;
    variables[363] = variables[251]*variables[190]*1.0e-6;
    variables[364] = variables[255]*variables[194]*1.0e-6;
    variables[365] = variables[253]*variables[192]*1.0e-6;
    variables[366] = variables[248]*variables[187]*1.0e-6;
    variables[367] = variables[252]*variables[191]*1.0e-6;
    variables[368] = variables[247]*variables[186]*1.0e-6;
    variables[341] = variables[357];
    variables[343] = variables[358];
    variables[345] = variables[359];
    variables[377] = variables[315]*variables[312]*variables[308]*variables[324]*variables[325]/(variables[313]*variables[325]+variables[314]*variables[324]);
    variables[378] = variables[377]*(variables[316]-variables[317]);
    variables[379] = variables[378];
    variables[380] = -variables[378];
    variables[381] = variables[315]*variables[312]*variables[309]*variables[324]*variables[325]/(variables[313]*variables[325]+variables[314]*variables[324]);
    variables[382] = variables[381]*(variables[318]-variables[319]);
    variables[383] = variables[382];
    variables[384] = -variables[382];
    variables[385] = variables[315]*variables[312]*variables[310]*variables[324]*variables[325]/(variables[313]*variables[325]+variables[314]*variables[324]);
    variables[386] = variables[385]*(variables[320]-variables[321]);
    variables[387] = variables[386];
    variables[388] = -variables[386];
    variables[389] = variables[315]*variables[312]*variables[311]*variables[324]*variables[325]/(variables[313]*variables[325]+variables[314]*variables[324]);
    variables[390] = variables[389]*(variables[322]-variables[323]);
    variables[391] = variables[390];
    variables[392] = -variables[390];
}

void computeRates(double voi, double *states, double *rates, double *variables)
{
    variables[393] = variables[1]*states[0]/(states[0]+variables[2]);
    rates[0] = (variables[327]+variables[326]+variables[4])/variables[3]+variables[393];
    variables[405] = variables[111]*variables[114]/variables[35]*log(variables[38]/states[6])*1000.0;
    variables[543] = variables[405]-states[1];
    variables[544] = states[24]*variables[185]*variables[543]/variables[263];
    variables[545] = states[25]*variables[544]*variables[259]/variables[94];
    variables[546] = -variables[545]*variables[35]*1000000000.0;
    variables[396] = variables[111]*variables[114]/variables[35]*log(variables[106]/states[3])*1000.0;
    variables[519] = -(states[1]+variables[396]-2.0*variables[405]);
    variables[520] = variables[328]*variables[198]*states[18]*variables[519]/variables[35]*1.0e-9;
    variables[521] = -variables[520]*variables[35]*1000000000.0;
    variables[510] = variables[258]*variables[35]*(variables[405]-states[1])*0.001;
    variables[509] = -variables[179]-variables[111]*variables[114]*log(fabs(states[8]/variables[166]*states[7]/variables[166]/(states[10]/variables[166]))+1.0e-300);
    variables[511] = 1.0-exp(-(variables[509]-variables[510])/(variables[111]*variables[114]));
    variables[508] = variables[328]*variables[269]*states[17]*variables[264]*variables[258]*1.602176634e-10;
    variables[512] = variables[508]*states[10]/(variables[64]+states[10])*states[6]/(variables[55]+states[6])*variables[511];
    variables[506] = variables[355]*(states[1]-variables[405]);
    variables[503] = variables[111]*variables[114]/(2.0*variables[35])*log(variables[33]/states[5])*1000.0;
    variables[504] = variables[354]*(states[1]-variables[503]);
    variables[531] = variables[328]*variables[270]*states[21]*variables[265]*1.602176634e-10;
    variables[532] = variables[531]*(exp(variables[199]*states[1]*1.0e-3*variables[35]/(variables[111]*variables[114]))*pow(states[3], 3.0)*variables[32]-exp((variables[199]-1.0)*states[1]*1.0e-3*variables[35]/(variables[111]*variables[114]))*pow(variables[107], 3.0)*states[5]*variables[125])/((pow(variables[78], 3.0)+pow(variables[107], 3.0))*(variables[77]+variables[32])*(1.0+variables[80]*exp((variables[199]-1.0)*states[1]*1.0e-3*variables[35]/(variables[111]*variables[114]))));
    variables[395] = variables[111]*variables[114]/variables[35]*log(variables[79]/states[2])*1000.0;
    variables[528] = variables[328]*variables[272]*states[20]*variables[201]*1.0e-6;
    variables[529] = variables[528]*(states[1]-variables[395]);
    variables[501] = variables[353]*(states[1]-variables[396]);
    variables[394] = variables[501]+variables[504]+variables[506]+variables[512]+variables[521]+variables[529]+variables[532]+variables[546];
    rates[1] = 1000000.0*(variables[40]-variables[394])/variables[330];
    variables[522] = -variables[520];
    variables[534] = -3.0*variables[532]/variables[35]*1.0e-9;
    variables[502] = -variables[501]/variables[35]*1.0e-9;
    variables[397] = variables[502]+variables[522]+variables[534];
    rates[3] = variables[397]/variables[329];
    variables[524] = variables[405]-variables[395];
    variables[525] = variables[328]*variables[197]*states[19]*variables[524]/variables[35]*1.0e-9;
    variables[526] = -variables[525];
    variables[530] = -variables[529]/variables[35]*1.0e-9;
    variables[398] = variables[530]+variables[526];
    rates[2] = variables[398]/variables[329];
    variables[399] = variables[20]-states[4];
    rates[4] = variables[225]*variables[399]-variables[212]*states[5]*states[4];
    variables[505] = -variables[504]/(2.0*variables[35])*1.0e-9;
    variables[533] = variables[532]/variables[35]*1.0e-9;
    variables[400] = variables[505]+variables[533];
    rates[5] = variables[400]/variables[329]+variables[225]*variables[399]-variables[212]*states[5]*states[4];
    variables[536] = states[11]*states[6]/variables[39];
    variables[537] = variables[211]*states[23]*variables[328]*(variables[356]/(variables[63]+variables[356])-variables[536]/(variables[63]+variables[536]));
    variables[538] = variables[537];
    variables[527] = variables[525];
    variables[523] = 2.0*variables[520];
    variables[513] = variables[512]/(variables[258]*variables[35])*1.0e-9;
    variables[514] = -variables[258]*variables[513];
    variables[515] = variables[514];
    variables[507] = -variables[506]/variables[35]*1.0e-9;
    variables[403] = variables[507]+variables[515]+variables[523]+variables[527]+variables[545]+variables[538];
    variables[419] = pow(states[8]/variables[165]*states[7]/variables[165], variables[257])/(states[9]/variables[165]*pow(states[10]/variables[165], variables[257]));
    variables[420] = variables[120]*variables[246]*variables[332]*states[9]/(variables[57]+states[9])*states[10]/(variables[49]+states[10])*(1.0-variables[419]/variables[337]+fabs(1.0-variables[419]/variables[337]))/2.0;
    variables[414] = pow(states[11]/variables[165], 2.0)*pow(states[10]/variables[165], 3.0)/(pow(states[8]/variables[165], 3.0)*pow(states[7]/variables[165], 3.0));
    variables[416] = variables[119]*variables[332]*states[13]/(variables[60]+states[13])*states[8]/(variables[46]+states[8])*states[7]/(variables[54]+states[7])*variables[61]/(variables[61]+states[10])*(1.0-variables[414]/variables[336]);
    variables[413] = pow(states[8]/variables[165], 2.0)*pow(states[7]/variables[165], 2.0)/(states[9]/variables[165]*pow(states[10]/variables[165], 2.0));
    variables[415] = variables[122]*variables[274]*variables[332]*states[9]/(variables[58]+states[9])*states[10]/(variables[50]+states[10])/(1.0+pow(states[13]/variables[27], 8.0))*(1.0-variables[413]/variables[335]);
    variables[408] = states[8]/variables[165]*states[7]/variables[165]/(states[10]/variables[165]);
    variables[410] = variables[118]*variables[332]*states[10]/variables[48]*(1.0-variables[408]/variables[334])/(1.0+states[10]/variables[48]+(1.0+states[8]/variables[45])*(1.0+states[7]/variables[53])-1.0);
    variables[516] = -variables[513];
    variables[438] = states[10]/(variables[65]+states[10])*(variables[65]+variables[7])/variables[7];
    variables[444] = (states[10]-variables[5])/(variables[7]-variables[5]);
    variables[445] = ((variables[444]+fabs(variables[444]))/2.0+1.0-fabs((variables[444]+fabs(variables[444]))/2.0-1.0))/2.0;
    variables[448] = variables[204]*variables[42]*variables[445];
    variables[449] = variables[448]*variables[438];
    variables[442] = variables[268]*variables[267]*states[14]/(variables[51]+states[14])*variables[438];
    variables[443] = variables[442]*variables[30];
    variables[441] = variables[346]*variables[140]*variables[223]/variables[93]*variables[438];
    variables[440] = variables[262]*variables[92]*variables[143]*variables[224]/variables[93]*variables[438];
    variables[439] = variables[261]*variables[81]*variables[143]*log(2.0)/variables[277]/variables[93]*variables[438];
    variables[453] = variables[439]+variables[440]+variables[441]+variables[443]+variables[449];
    variables[454] = -variables[453];
    variables[640] = states[10]/(variables[66]+states[10])*(variables[66]+variables[8])/variables[8];
    variables[636] = variables[247]*states[25];
    variables[642] = variables[636]*variables[82]*(variables[144]+variables[130]+variables[154])*variables[213]/variables[95]*variables[640];
    variables[641] = (variables[640]+1.0-fabs(variables[640]-1.0))/2.0;
    variables[637] = (variables[236]-states[25])/variables[279];
    variables[638] = (variables[637]+fabs(variables[637]))/2.0;
    variables[643] = variables[638]*variables[247]*variables[82]*(variables[167]+variables[144])/variables[95];
    variables[469] = (states[10]-variables[6])/(variables[18]-variables[6]);
    variables[470] = ((variables[469]+fabs(variables[469]))/2.0+1.0-fabs((variables[469]+fabs(variables[469]))/2.0-1.0))/2.0;
    variables[471] = variables[129]*variables[43]*variables[470];
    variables[625] = (variables[241]-states[23])/variables[284];
    variables[626] = (variables[625]+fabs(variables[625]))/2.0;
    variables[631] = variables[626]*variables[252]*variables[87]*(variables[172]+variables[149])/variables[100];
    variables[613] = (variables[237]-states[22])/variables[280];
    variables[614] = (variables[613]+fabs(variables[613]))/2.0;
    variables[619] = variables[614]*variables[248]*variables[83]*(variables[168]+variables[145])/variables[96];
    variables[601] = (variables[242]-states[21])/variables[285];
    variables[602] = (variables[601]+fabs(variables[601]))/2.0;
    variables[607] = variables[602]*variables[253]*variables[88]*(variables[173]+variables[150])/variables[101];
    variables[589] = (variables[244]-states[20])/variables[287];
    variables[590] = (variables[589]+fabs(variables[589]))/2.0;
    variables[595] = variables[590]*variables[255]*variables[90]*(variables[175]+variables[152])/variables[103];
    variables[577] = (variables[240]-states[19])/variables[283];
    variables[578] = (variables[577]+fabs(variables[577]))/2.0;
    variables[583] = variables[578]*variables[251]*variables[86]*(variables[171]+variables[148])/variables[99];
    variables[565] = (variables[243]-states[18])/variables[286];
    variables[566] = (variables[565]+fabs(variables[565]))/2.0;
    variables[571] = variables[566]*variables[254]*variables[89]*(variables[174]+variables[151])/variables[102];
    variables[553] = (variables[239]-states[17])/variables[282];
    variables[554] = (variables[553]+fabs(variables[553]))/2.0;
    variables[559] = variables[554]*variables[250]*variables[85]*(variables[170]+variables[147])/variables[98];
    variables[490] = (variables[238]-states[12])/variables[281];
    variables[491] = (variables[490]+fabs(variables[490]))/2.0;
    variables[496] = variables[491]*variables[249]*variables[84]*(variables[169]+variables[146])/variables[97];
    variables[478] = (variables[245]-states[16])/variables[288];
    variables[479] = (variables[478]+fabs(variables[478]))/2.0;
    variables[484] = variables[479]*variables[256]*variables[91]*(variables[176]+variables[153])/variables[104];
    variables[457] = variables[484]+variables[496]+variables[559]+variables[571]+variables[583]+variables[595]+variables[607]+variables[619]+variables[631]+variables[643];
    variables[458] = variables[457];
    variables[472] = (variables[471]+variables[41])/((variables[471]+variables[458]+fabs(variables[471]-variables[458]))/2.0+variables[41]);
    variables[644] = variables[643]*variables[472]*variables[641];
    variables[645] = variables[642]+variables[644];
    variables[628] = states[10]/(variables[71]+states[10])*(variables[71]+variables[13])/variables[13];
    variables[624] = variables[252]*states[23];
    variables[630] = variables[624]*variables[87]*(variables[149]+variables[135]+variables[159])*variables[218]/variables[100]*variables[628];
    variables[629] = (variables[628]+1.0-fabs(variables[628]-1.0))/2.0;
    variables[632] = variables[631]*variables[472]*variables[629];
    variables[633] = variables[630]+variables[632];
    variables[616] = states[10]/(variables[67]+states[10])*(variables[67]+variables[9])/variables[9];
    variables[612] = variables[248]*states[22];
    variables[618] = variables[612]*variables[83]*(variables[145]+variables[131]+variables[155])*variables[214]/variables[96]*variables[616];
    variables[617] = (variables[616]+1.0-fabs(variables[616]-1.0))/2.0;
    variables[620] = variables[619]*variables[472]*variables[617];
    variables[621] = variables[618]+variables[620];
    variables[604] = states[10]/(variables[72]+states[10])*(variables[72]+variables[14])/variables[14];
    variables[600] = variables[253]*states[21];
    variables[606] = variables[600]*variables[88]*(variables[150]+variables[136]+variables[160])*variables[219]/variables[101]*variables[604];
    variables[605] = (variables[604]+1.0-fabs(variables[604]-1.0))/2.0;
    variables[608] = variables[607]*variables[472]*variables[605];
    variables[609] = variables[606]+variables[608];
    variables[592] = states[10]/(variables[74]+states[10])*(variables[74]+variables[16])/variables[16];
    variables[588] = variables[255]*states[20];
    variables[594] = variables[588]*variables[90]*(variables[152]+variables[138]+variables[162])*variables[221]/variables[103]*variables[592];
    variables[593] = (variables[592]+1.0-fabs(variables[592]-1.0))/2.0;
    variables[596] = variables[595]*variables[472]*variables[593];
    variables[597] = variables[594]+variables[596];
    variables[580] = states[10]/(variables[70]+states[10])*(variables[70]+variables[12])/variables[12];
    variables[576] = variables[251]*states[19];
    variables[582] = variables[576]*variables[86]*(variables[148]+variables[134]+variables[158])*variables[217]/variables[99]*variables[580];
    variables[581] = (variables[580]+1.0-fabs(variables[580]-1.0))/2.0;
    variables[584] = variables[583]*variables[472]*variables[581];
    variables[585] = variables[582]+variables[584];
    variables[568] = states[10]/(variables[73]+states[10])*(variables[73]+variables[15])/variables[15];
    variables[564] = variables[254]*states[18];
    variables[570] = variables[564]*variables[89]*(variables[151]+variables[137]+variables[161])*variables[220]/variables[102]*variables[568];
    variables[569] = (variables[568]+1.0-fabs(variables[568]-1.0))/2.0;
    variables[572] = variables[571]*variables[472]*variables[569];
    variables[573] = variables[570]+variables[572];
    variables[556] = states[10]/(variables[69]+states[10])*(variables[69]+variables[11])/variables[11];
    variables[552] = variables[250]*states[17];
    variables[558] = variables[552]*variables[85]*(variables[147]+variables[133]+variables[157])*variables[216]/variables[98]*variables[556];
    variables[557] = (variables[556]+1.0-fabs(variables[556]-1.0))/2.0;
    variables[560] = variables[559]*variables[472]*variables[557];
    variables[561] = variables[558]+variables[560];
    variables[493] = states[10]/(variables[68]+states[10])*(variables[68]+variables[10])/variables[10];
    variables[489] = variables[249]*states[12];
    variables[495] = variables[489]*variables[84]*(variables[146]+variables[132]+variables[156])*variables[215]/variables[97]*variables[493];
    variables[494] = (variables[493]+1.0-fabs(variables[493]-1.0))/2.0;
    variables[497] = variables[496]*variables[472]*variables[494];
    variables[498] = variables[495]+variables[497];
    variables[481] = states[10]/(variables[75]+states[10])*(variables[75]+variables[17])/variables[17];
    variables[477] = variables[256]*states[16];
    variables[483] = variables[477]*variables[91]*(variables[153]+variables[139]+variables[163])*variables[222]/variables[104]*variables[481];
    variables[482] = (variables[481]+1.0-fabs(variables[481]-1.0))/2.0;
    variables[485] = variables[484]*variables[472]*variables[482];
    variables[486] = variables[483]+variables[485];
    variables[459] = variables[486]+variables[498]+variables[561]+variables[573]+variables[585]+variables[597]+variables[609]+variables[621]+variables[633]+variables[645];
    variables[460] = variables[459];
    variables[474] = -variables[460];
    variables[423] = variables[474]+variables[454]+variables[516];
    variables[425] = variables[329]*(variables[410]+2.0*variables[415]-variables[416]+variables[257]*variables[420])-variables[423];
    variables[402] = variables[425];
    rates[6] = (variables[403]+variables[402])/variables[329]/(1.0+variables[128]/(log(10.0)*states[6]));
    variables[407] = pow(states[11]/variables[165], 2.0)*pow(states[10]/variables[165], 2.0)/(states[9]/variables[165]*pow(states[8]/variables[165], 2.0)*pow(states[7]/variables[165], 2.0));
    variables[409] = variables[117]*states[12]*variables[332]*states[9]/variables[56]*pow(states[8]/variables[44], 2.0)*pow(states[7]/variables[52], 2.0)*(1.0-variables[407]/variables[333])/((1.0+states[9]/variables[56])*pow(1.0+states[8]/variables[44], 2.0)*pow(1.0+states[7]/variables[52], 2.0)+pow(1.0+states[11]/variables[62], 2.0)*pow(1.0+states[10]/variables[47], 2.0)-1.0);
    variables[535] = variables[210]*states[22]*variables[328]*(variables[24]/(variables[59]+variables[24])-states[9]/(variables[59]+states[9]));
    variables[541] = variables[535];
    variables[427] = variables[541];
    rates[9] = (variables[427]+variables[338])/variables[329]-variables[409]-variables[415]-variables[420];
    variables[542] = variables[537];
    variables[428] = variables[542];
    rates[11] = (variables[428]+variables[339])/variables[329]+2.0*variables[409]+2.0*variables[416];
    rates[10] = (variables[341]+variables[340]+variables[423])/variables[329]+2.0*variables[409]-variables[410]-2.0*variables[415]+3.0*variables[416]-variables[257]*variables[420];
    variables[517] = variables[513];
    variables[455] = variables[453];
    variables[475] = variables[460];
    variables[429] = variables[475]+variables[455]+variables[517];
    rates[8] = (variables[343]+variables[342]+variables[429])/variables[329]-2.0*variables[409]+variables[410]+2.0*variables[415]-3.0*variables[416]+variables[257]*variables[420];
    variables[518] = variables[513];
    variables[456] = variables[453];
    variables[476] = variables[460];
    variables[430] = variables[476]+variables[456]+variables[518];
    rates[7] = (variables[345]+variables[344]+variables[430])/variables[329]-2.0*variables[409]+variables[410]+2.0*variables[415]-3.0*variables[416]+variables[257]*variables[420];
    rates[13] = variables[415]-variables[416];
    rates[14] = variables[266]*variables[347]-variables[442];
    rates[15] = variables[449];
    variables[480] = (variables[478]-fabs(variables[478]))/2.0;
    rates[16] = variables[479]*variables[472]*variables[482]+variables[480];
    variables[492] = (variables[490]-fabs(variables[490]))/2.0;
    rates[12] = variables[491]*variables[472]*variables[494]+variables[492];
    rates[24] = (variables[124]-states[24])/variables[278];
    variables[555] = (variables[553]-fabs(variables[553]))/2.0;
    rates[17] = variables[554]*variables[472]*variables[557]+variables[555];
    variables[567] = (variables[565]-fabs(variables[565]))/2.0;
    rates[18] = variables[566]*variables[472]*variables[569]+variables[567];
    variables[579] = (variables[577]-fabs(variables[577]))/2.0;
    rates[19] = variables[578]*variables[472]*variables[581]+variables[579];
    variables[591] = (variables[589]-fabs(variables[589]))/2.0;
    rates[20] = variables[590]*variables[472]*variables[593]+variables[591];
    variables[603] = (variables[601]-fabs(variables[601]))/2.0;
    rates[21] = variables[602]*variables[472]*variables[605]+variables[603];
    variables[615] = (variables[613]-fabs(variables[613]))/2.0;
    rates[22] = variables[614]*variables[472]*variables[617]+variables[615];
    variables[627] = (variables[625]-fabs(variables[625]))/2.0;
    rates[23] = variables[626]*variables[472]*variables[629]+variables[627];
    variables[639] = (variables[637]-fabs(variables[637]))/2.0;
    rates[25] = variables[638]*variables[472]*variables[641]+variables[639];
    variables[650] = variables[299]*states[26];
    rates[28] = (variables[370]+variables[369]+variables[303])/variables[295]+variables[300]*variables[650];
    rates[29] = (variables[372]+variables[371]+variables[304])/variables[295];
    rates[27] = (variables[374]+variables[373]+variables[305])/(variables[295]*(1.0+variables[296]));
    rates[30] = (variables[376]+variables[375]+variables[306])/variables[295];
    variables[651] = variables[302]*states[26]*pow(states[26]/variables[301], 8.0);
    rates[26] = variables[307]/variables[295]-variables[650]-variables[651];
    variables[652] = variables[295]*((1.0-variables[300])*variables[650]+variables[651]);
    rates[31] = variables[652];
}

void computeVariables(double voi, double *states, double *rates, double *variables)
{
    variables[393] = variables[1]*states[0]/(states[0]+variables[2]);
    variables[395] = variables[111]*variables[114]/variables[35]*log(variables[79]/states[2])*1000.0;
    variables[396] = variables[111]*variables[114]/variables[35]*log(variables[106]/states[3])*1000.0;
    variables[399] = variables[20]-states[4];
    variables[401] = states[5]+variables[399];
    variables[404] = -log(states[6]/variables[164])/log(10.0);
    variables[405] = variables[111]*variables[114]/variables[35]*log(variables[38]/states[6])*1000.0;
    variables[406] = states[6]-variables[128]*variables[404];
    variables[407] = pow(states[11]/variables[165], 2.0)*pow(states[10]/variables[165], 2.0)/(states[9]/variables[165]*pow(states[8]/variables[165], 2.0)*pow(states[7]/variables[165], 2.0));
    variables[408] = states[8]/variables[165]*states[7]/variables[165]/(states[10]/variables[165]);
    variables[409] = variables[117]*states[12]*variables[332]*states[9]/variables[56]*pow(states[8]/variables[44], 2.0)*pow(states[7]/variables[52], 2.0)*(1.0-variables[407]/variables[333])/((1.0+states[9]/variables[56])*pow(1.0+states[8]/variables[44], 2.0)*pow(1.0+states[7]/variables[52], 2.0)+pow(1.0+states[11]/variables[62], 2.0)*pow(1.0+states[10]/variables[47], 2.0)-1.0);
    variables[410] = variables[118]*variables[332]*states[10]/variables[48]*(1.0-variables[408]/variables[334])/(1.0+states[10]/variables[48]+(1.0+states[8]/variables[45])*(1.0+states[7]/variables[53])-1.0);
    variables[411] = variables[111]*variables[114]*(log(variables[333])-log(fabs(variables[407])+1.0e-300));
    variables[412] = variables[111]*variables[114]*(log(variables[334])-log(fabs(variables[408])+1.0e-300));
    variables[413] = pow(states[8]/variables[165], 2.0)*pow(states[7]/variables[165], 2.0)/(states[9]/variables[165]*pow(states[10]/variables[165], 2.0));
    variables[414] = pow(states[11]/variables[165], 2.0)*pow(states[10]/variables[165], 3.0)/(pow(states[8]/variables[165], 3.0)*pow(states[7]/variables[165], 3.0));
    variables[415] = variables[122]*variables[274]*variables[332]*states[9]/(variables[58]+states[9])*states[10]/(variables[50]+states[10])/(1.0+pow(states[13]/variables[27], 8.0))*(1.0-variables[413]/variables[335]);
    variables[416] = variables[119]*variables[332]*states[13]/(variables[60]+states[13])*states[8]/(variables[46]+states[8])*states[7]/(variables[54]+states[7])*variables[61]/(variables[61]+states[10])*(1.0-variables[414]/variables[336]);
    variables[417] = variables[111]*variables[114]*(log(variables[335])-log(fabs(variables[413])+1.0e-300));
    variables[418] = variables[111]*variables[114]*(log(variables[336])-log(fabs(variables[414])+1.0e-300));
    variables[419] = pow(states[8]/variables[165]*states[7]/variables[165], variables[257])/(states[9]/variables[165]*pow(states[10]/variables[165], variables[257]));
    variables[420] = variables[120]*variables[246]*variables[332]*states[9]/(variables[57]+states[9])*states[10]/(variables[49]+states[10])*(1.0-variables[419]/variables[337]+fabs(1.0-variables[419]/variables[337]))/2.0;
    variables[421] = variables[111]*variables[114]*(log(variables[337])-log(fabs(variables[419])+1.0e-300));
    variables[422] = variables[329]*(variables[409]*variables[411]+variables[410]*variables[412]+variables[415]*variables[417]+variables[416]*variables[418]+variables[420]*variables[421]);
    variables[510] = variables[258]*variables[35]*(variables[405]-states[1])*0.001;
    variables[509] = -variables[179]-variables[111]*variables[114]*log(fabs(states[8]/variables[166]*states[7]/variables[166]/(states[10]/variables[166]))+1.0e-300);
    variables[511] = 1.0-exp(-(variables[509]-variables[510])/(variables[111]*variables[114]));
    variables[508] = variables[328]*variables[269]*states[17]*variables[264]*variables[258]*1.602176634e-10;
    variables[512] = variables[508]*states[10]/(variables[64]+states[10])*states[6]/(variables[55]+states[6])*variables[511];
    variables[513] = variables[512]/(variables[258]*variables[35])*1.0e-9;
    variables[516] = -variables[513];
    variables[438] = states[10]/(variables[65]+states[10])*(variables[65]+variables[7])/variables[7];
    variables[444] = (states[10]-variables[5])/(variables[7]-variables[5]);
    variables[445] = ((variables[444]+fabs(variables[444]))/2.0+1.0-fabs((variables[444]+fabs(variables[444]))/2.0-1.0))/2.0;
    variables[448] = variables[204]*variables[42]*variables[445];
    variables[449] = variables[448]*variables[438];
    variables[442] = variables[268]*variables[267]*states[14]/(variables[51]+states[14])*variables[438];
    variables[443] = variables[442]*variables[30];
    variables[441] = variables[346]*variables[140]*variables[223]/variables[93]*variables[438];
    variables[440] = variables[262]*variables[92]*variables[143]*variables[224]/variables[93]*variables[438];
    variables[439] = variables[261]*variables[81]*variables[143]*log(2.0)/variables[277]/variables[93]*variables[438];
    variables[453] = variables[439]+variables[440]+variables[441]+variables[443]+variables[449];
    variables[454] = -variables[453];
    variables[640] = states[10]/(variables[66]+states[10])*(variables[66]+variables[8])/variables[8];
    variables[636] = variables[247]*states[25];
    variables[642] = variables[636]*variables[82]*(variables[144]+variables[130]+variables[154])*variables[213]/variables[95]*variables[640];
    variables[641] = (variables[640]+1.0-fabs(variables[640]-1.0))/2.0;
    variables[637] = (variables[236]-states[25])/variables[279];
    variables[638] = (variables[637]+fabs(variables[637]))/2.0;
    variables[643] = variables[638]*variables[247]*variables[82]*(variables[167]+variables[144])/variables[95];
    variables[469] = (states[10]-variables[6])/(variables[18]-variables[6]);
    variables[470] = ((variables[469]+fabs(variables[469]))/2.0+1.0-fabs((variables[469]+fabs(variables[469]))/2.0-1.0))/2.0;
    variables[471] = variables[129]*variables[43]*variables[470];
    variables[625] = (variables[241]-states[23])/variables[284];
    variables[626] = (variables[625]+fabs(variables[625]))/2.0;
    variables[631] = variables[626]*variables[252]*variables[87]*(variables[172]+variables[149])/variables[100];
    variables[613] = (variables[237]-states[22])/variables[280];
    variables[614] = (variables[613]+fabs(variables[613]))/2.0;
    variables[619] = variables[614]*variables[248]*variables[83]*(variables[168]+variables[145])/variables[96];
    variables[601] = (variables[242]-states[21])/variables[285];
    variables[602] = (variables[601]+fabs(variables[601]))/2.0;
    variables[607] = variables[602]*variables[253]*variables[88]*(variables[173]+variables[150])/variables[101];
    variables[589] = (variables[244]-states[20])/variables[287];
    variables[590] = (variables[589]+fabs(variables[589]))/2.0;
    variables[595] = variables[590]*variables[255]*variables[90]*(variables[175]+variables[152])/variables[103];
    variables[577] = (variables[240]-states[19])/variables[283];
    variables[578] = (variables[577]+fabs(variables[577]))/2.0;
    variables[583] = variables[578]*variables[251]*variables[86]*(variables[171]+variables[148])/variables[99];
    variables[565] = (variables[243]-states[18])/variables[286];
    variables[566] = (variables[565]+fabs(variables[565]))/2.0;
    variables[571] = variables[566]*variables[254]*variables[89]*(variables[174]+variables[151])/variables[102];
    variables[553] = (variables[239]-states[17])/variables[282];
    variables[554] = (variables[553]+fabs(variables[553]))/2.0;
    variables[559] = variables[554]*variables[250]*variables[85]*(variables[170]+variables[147])/variables[98];
    variables[490] = (variables[238]-states[12])/variables[281];
    variables[491] = (variables[490]+fabs(variables[490]))/2.0;
    variables[496] = variables[491]*variables[249]*variables[84]*(variables[169]+variables[146])/variables[97];
    variables[478] = (variables[245]-states[16])/variables[288];
    variables[479] = (variables[478]+fabs(variables[478]))/2.0;
    variables[484] = variables[479]*variables[256]*variables[91]*(variables[176]+variables[153])/variables[104];
    variables[457] = variables[484]+variables[496]+variables[559]+variables[571]+variables[583]+variables[595]+variables[607]+variables[619]+variables[631]+variables[643];
    variables[458] = variables[457];
    variables[472] = (variables[471]+variables[41])/((variables[471]+variables[458]+fabs(variables[471]-variables[458]))/2.0+variables[41]);
    variables[644] = variables[643]*variables[472]*variables[641];
    variables[645] = variables[642]+variables[644];
    variables[628] = states[10]/(variables[71]+states[10])*(variables[71]+variables[13])/variables[13];
    variables[624] = variables[252]*states[23];
    variables[630] = variables[624]*variables[87]*(variables[149]+variables[135]+variables[159])*variables[218]/variables[100]*variables[628];
    variables[629] = (variables[628]+1.0-fabs(variables[628]-1.0))/2.0;
    variables[632] = variables[631]*variables[472]*variables[629];
    variables[633] = variables[630]+variables[632];
    variables[616] = states[10]/(variables[67]+states[10])*(variables[67]+variables[9])/variables[9];
    variables[612] = variables[248]*states[22];
    variables[618] = variables[612]*variables[83]*(variables[145]+variables[131]+variables[155])*variables[214]/variables[96]*variables[616];
    variables[617] = (variables[616]+1.0-fabs(variables[616]-1.0))/2.0;
    variables[620] = variables[619]*variables[472]*variables[617];
    variables[621] = variables[618]+variables[620];
    variables[604] = states[10]/(variables[72]+states[10])*(variables[72]+variables[14])/variables[14];
    variables[600] = variables[253]*states[21];
    variables[606] = variables[600]*variables[88]*(variables[150]+variables[136]+variables[160])*variables[219]/variables[101]*variables[604];
    variables[605] = (variables[604]+1.0-fabs(variables[604]-1.0))/2.0;
    variables[608] = variables[607]*variables[472]*variables[605];
    variables[609] = variables[606]+variables[608];
    variables[592] = states[10]/(variables[74]+states[10])*(variables[74]+variables[16])/variables[16];
    variables[588] = variables[255]*states[20];
    variables[594] = variables[588]*variables[90]*(variables[152]+variables[138]+variables[162])*variables[221]/variables[103]*variables[592];
    variables[593] = (variables[592]+1.0-fabs(variables[592]-1.0))/2.0;
    variables[596] = variables[595]*variables[472]*variables[593];
    variables[597] = variables[594]+variables[596];
    variables[580] = states[10]/(variables[70]+states[10])*(variables[70]+variables[12])/variables[12];
    variables[576] = variables[251]*states[19];
    variables[582] = variables[576]*variables[86]*(variables[148]+variables[134]+variables[158])*variables[217]/variables[99]*variables[580];
    variables[581] = (variables[580]+1.0-fabs(variables[580]-1.0))/2.0;
    variables[584] = variables[583]*variables[472]*variables[581];
    variables[585] = variables[582]+variables[584];
    variables[568] = states[10]/(variables[73]+states[10])*(variables[73]+variables[15])/variables[15];
    variables[564] = variables[254]*states[18];
    variables[570] = variables[564]*variables[89]*(variables[151]+variables[137]+variables[161])*variables[220]/variables[102]*variables[568];
    variables[569] = (variables[568]+1.0-fabs(variables[568]-1.0))/2.0;
    variables[572] = variables[571]*variables[472]*variables[569];
    variables[573] = variables[570]+variables[572];
    variables[556] = states[10]/(variables[69]+states[10])*(variables[69]+variables[11])/variables[11];
    variables[552] = variables[250]*states[17];
    variables[558] = variables[552]*variables[85]*(variables[147]+variables[133]+variables[157])*variables[216]/variables[98]*variables[556];
    variables[557] = (variables[556]+1.0-fabs(variables[556]-1.0))/2.0;
    variables[560] = variables[559]*variables[472]*variables[557];
    variables[561] = variables[558]+variables[560];
    variables[493] = states[10]/(variables[68]+states[10])*(variables[68]+variables[10])/variables[10];
    variables[489] = variables[249]*states[12];
    variables[495] = variables[489]*variables[84]*(variables[146]+variables[132]+variables[156])*variables[215]/variables[97]*variables[493];
    variables[494] = (variables[493]+1.0-fabs(variables[493]-1.0))/2.0;
    variables[497] = variables[496]*variables[472]*variables[494];
    variables[498] = variables[495]+variables[497];
    variables[481] = states[10]/(variables[75]+states[10])*(variables[75]+variables[17])/variables[17];
    variables[477] = variables[256]*states[16];
    variables[483] = variables[477]*variables[91]*(variables[153]+variables[139]+variables[163])*variables[222]/variables[104]*variables[481];
    variables[482] = (variables[481]+1.0-fabs(variables[481]-1.0))/2.0;
    variables[485] = variables[484]*variables[472]*variables[482];
    variables[486] = variables[483]+variables[485];
    variables[459] = variables[486]+variables[498]+variables[561]+variables[573]+variables[585]+variables[597]+variables[609]+variables[621]+variables[633]+variables[645];
    variables[460] = variables[459];
    variables[474] = -variables[460];
    variables[423] = variables[474]+variables[454]+variables[516];
    variables[424] = variables[329]*variables[410]-variables[423];
    variables[425] = variables[329]*(variables[410]+2.0*variables[415]-variables[416]+variables[257]*variables[420])-variables[423];
    variables[426] = variables[329]*(variables[409]*(2.0*variables[208]-2.0*variables[205])+variables[410]*variables[205]+variables[415]*(2.0*variables[205]+variables[207])+variables[416]*(2.0*variables[208]-variables[207]-3.0*variables[205])+variables[420]*(variables[257]*variables[205]+variables[209]))-variables[423]*variables[205];
    variables[431] = 2.0*variables[329]*variables[409];
    variables[432] = 3.0*variables[329]*variables[416];
    variables[433] = 2.0*variables[329]*variables[415];
    variables[434] = variables[329]*variables[410];
    variables[435] = 3.0*variables[329]*states[13];
    variables[436] = variables[329]*variables[420];
    variables[437] = variables[257]*variables[329]*variables[420];
    variables[646] = variables[636]*variables[82]*(variables[167]+variables[144])/variables[95];
    variables[634] = variables[624]*variables[87]*(variables[172]+variables[149])/variables[100];
    variables[622] = variables[612]*variables[83]*(variables[168]+variables[145])/variables[96];
    variables[610] = variables[600]*variables[88]*(variables[173]+variables[150])/variables[101];
    variables[598] = variables[588]*variables[90]*(variables[175]+variables[152])/variables[103];
    variables[586] = variables[576]*variables[86]*(variables[171]+variables[148])/variables[99];
    variables[574] = variables[564]*variables[89]*(variables[174]+variables[151])/variables[102];
    variables[562] = variables[552]*variables[85]*(variables[170]+variables[147])/variables[98];
    variables[499] = variables[489]*variables[84]*(variables[169]+variables[146])/variables[97];
    variables[487] = variables[477]*variables[91]*(variables[176]+variables[153])/variables[104];
    variables[446] = variables[487]+variables[499]+variables[562]+variables[574]+variables[586]+variables[598]+variables[610]+variables[622]+variables[634]+variables[646];
    variables[447] = variables[446];
    variables[450] = variables[348]+variables[447];
    variables[451] = states[15]/variables[450];
    variables[452] = variables[435]/variables[450];
    variables[455] = variables[453];
    variables[456] = variables[453];
    variables[461] = variables[483]+variables[495]+variables[558]+variables[570]+variables[582]+variables[594]+variables[606]+variables[618]+variables[630]+variables[642];
    variables[462] = variables[461];
    variables[463] = variables[485]+variables[497]+variables[560]+variables[572]+variables[584]+variables[596]+variables[608]+variables[620]+variables[632]+variables[644];
    variables[464] = variables[463];
    variables[465] = variables[487]+variables[499]+variables[562]+variables[574]+variables[586]+variables[598]+variables[610]+variables[622]+variables[634]+variables[646];
    variables[466] = variables[465];
    variables[647] = variables[368]*states[25];
    variables[635] = variables[367]*states[23];
    variables[623] = variables[366]*states[22];
    variables[611] = variables[365]*states[21];
    variables[599] = variables[364]*states[20];
    variables[587] = variables[363]*states[19];
    variables[575] = variables[362]*states[18];
    variables[563] = variables[361]*states[17];
    variables[500] = variables[352]*states[12];
    variables[488] = variables[351]*states[16];
    variables[467] = variables[488]+variables[500]+variables[563]+variables[575]+variables[587]+variables[599]+variables[611]+variables[623]+variables[635]+variables[647];
    variables[468] = variables[467];
    variables[473] = variables[468]/variables[350];
    variables[475] = variables[460];
    variables[476] = variables[460];
    variables[480] = (variables[478]-fabs(variables[478]))/2.0;
    variables[492] = (variables[490]-fabs(variables[490]))/2.0;
    variables[501] = variables[353]*(states[1]-variables[396]);
    variables[502] = -variables[501]/variables[35]*1.0e-9;
    variables[503] = variables[111]*variables[114]/(2.0*variables[35])*log(variables[33]/states[5])*1000.0;
    variables[504] = variables[354]*(states[1]-variables[503]);
    variables[505] = -variables[504]/(2.0*variables[35])*1.0e-9;
    variables[506] = variables[355]*(states[1]-variables[405]);
    variables[507] = -variables[506]/variables[35]*1.0e-9;
    variables[514] = -variables[258]*variables[513];
    variables[515] = variables[514];
    variables[517] = variables[513];
    variables[518] = variables[513];
    variables[519] = -(states[1]+variables[396]-2.0*variables[405]);
    variables[520] = variables[328]*variables[198]*states[18]*variables[519]/variables[35]*1.0e-9;
    variables[521] = -variables[520]*variables[35]*1000000000.0;
    variables[522] = -variables[520];
    variables[523] = 2.0*variables[520];
    variables[524] = variables[405]-variables[395];
    variables[525] = variables[328]*variables[197]*states[19]*variables[524]/variables[35]*1.0e-9;
    variables[526] = -variables[525];
    variables[527] = variables[525];
    variables[528] = variables[328]*variables[272]*states[20]*variables[201]*1.0e-6;
    variables[529] = variables[528]*(states[1]-variables[395]);
    variables[530] = -variables[529]/variables[35]*1.0e-9;
    variables[531] = variables[328]*variables[270]*states[21]*variables[265]*1.602176634e-10;
    variables[532] = variables[531]*(exp(variables[199]*states[1]*1.0e-3*variables[35]/(variables[111]*variables[114]))*pow(states[3], 3.0)*variables[32]-exp((variables[199]-1.0)*states[1]*1.0e-3*variables[35]/(variables[111]*variables[114]))*pow(variables[107], 3.0)*states[5]*variables[125])/((pow(variables[78], 3.0)+pow(variables[107], 3.0))*(variables[77]+variables[32])*(1.0+variables[80]*exp((variables[199]-1.0)*states[1]*1.0e-3*variables[35]/(variables[111]*variables[114]))));
    variables[533] = variables[532]/variables[35]*1.0e-9;
    variables[534] = -3.0*variables[532]/variables[35]*1.0e-9;
    variables[535] = variables[210]*states[22]*variables[328]*(variables[24]/(variables[59]+variables[24])-states[9]/(variables[59]+states[9]));
    variables[536] = states[11]*states[6]/variables[39];
    variables[537] = variables[211]*states[23]*variables[328]*(variables[356]/(variables[63]+variables[356])-variables[536]/(variables[63]+variables[536]));
    variables[538] = variables[537];
    variables[539] = -variables[535];
    variables[540] = -variables[537];
    variables[541] = variables[535];
    variables[542] = variables[537];
    variables[543] = variables[405]-states[1];
    variables[544] = states[24]*variables[185]*variables[543]/variables[263];
    variables[545] = states[25]*variables[544]*variables[259]/variables[94];
    variables[546] = -variables[545]*variables[35]*1000000000.0;
    variables[547] = variables[545]*variables[35]*variables[543]*0.001;
    variables[548] = variables[360]*variables[289]*variables[544]/variables[185]*(1.0+variables[260])/(states[25]+variables[260]);
    variables[549] = states[25]*variables[548];
    variables[550] = variables[549]/variables[360];
    variables[551] = variables[549]*variables[550];
    variables[555] = (variables[553]-fabs(variables[553]))/2.0;
    variables[567] = (variables[565]-fabs(variables[565]))/2.0;
    variables[579] = (variables[577]-fabs(variables[577]))/2.0;
    variables[591] = (variables[589]-fabs(variables[589]))/2.0;
    variables[603] = (variables[601]-fabs(variables[601]))/2.0;
    variables[615] = (variables[613]-fabs(variables[613]))/2.0;
    variables[627] = (variables[625]-fabs(variables[625]))/2.0;
    variables[639] = (variables[637]-fabs(variables[637]))/2.0;
    variables[394] = variables[501]+variables[504]+variables[506]+variables[512]+variables[521]+variables[529]+variables[532]+variables[546];
    variables[397] = variables[502]+variables[522]+variables[534];
    variables[398] = variables[530]+variables[526];
    variables[400] = variables[505]+variables[533];
    variables[403] = variables[507]+variables[515]+variables[523]+variables[527]+variables[545]+variables[538];
    variables[402] = variables[425];
    variables[429] = variables[475]+variables[455]+variables[517];
    variables[430] = variables[476]+variables[456]+variables[518];
    variables[427] = variables[541];
    variables[428] = variables[542];
    variables[648] = variables[298]*states[26];
    variables[649] = pow((1.0-variables[648])/(1.0+variables[648]), 2.0);
    variables[650] = variables[299]*states[26];
    variables[651] = variables[302]*states[26]*pow(states[26]/variables[301], 8.0);
    variables[652] = variables[295]*((1.0-variables[300])*variables[650]+variables[651]);
    variables[653] = -log(states[27]/variables[297])/2.302585092994046;
}
