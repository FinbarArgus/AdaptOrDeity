/* The content of this file was generated using the C profile of libCellML 0.7.1. */

#include "model0d_core.h"

#include <math.h>
#include <stdlib.h>

const char VERSION[] = "0.8.0";
const char LIBCELLML_VERSION[] = "0.7.1";

const size_t STATE_COUNT = 37;
const size_t VARIABLE_COUNT = 985;

const VariableInfo VOI_INFO = {"time", "second", "environment", VARIABLE_OF_INTEGRATION};

const VariableInfo STATE_INFO[] = {
    {"C_P", "millimolar", "vol_0_module", STATE},
    {"membrane__V", "milliV", "cell_0_module", STATE},
    {"Na_K__Ki", "millimolar", "cell_0_module", STATE},
    {"Na_K__Nai", "millimolar", "cell_0_module", STATE},
    {"Ca__B_Cai", "millimolar", "cell_0_module", STATE},
    {"Ca__Cai", "millimolar", "cell_0_module", STATE},
    {"H__H_i", "millimolar", "cell_0_module", STATE},
    {"cytosol__C_ATP", "millimolar", "cell_0_module", STATE},
    {"cytosol__C_glc", "millimolar", "cell_0_module", STATE},
    {"cytosol__C_ADP", "millimolar", "cell_0_module", STATE},
    {"cytosol__C_G6P", "millimolar", "cell_0_module", STATE},
    {"cytosol__C_Pi", "millimolar", "cell_0_module", STATE},
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
    {"pool_Hydrolase__m", "dimensionless", "cell_0_module", STATE},
    {"pool_PTS__m", "dimensionless", "cell_0_module", STATE},
    {"pool_ABC__m", "dimensionless", "cell_0_module", STATE},
    {"pool_GalP__m", "dimensionless", "cell_0_module", STATE},
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
    {"ATP_ref_mod_pool_ABC_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_Flagellum_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_GalP_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_Glf_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_Glyc_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_H_ATPase_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_Hydrolase_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_KefC_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_LacT_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_NCX_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_NhaA_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_PTS_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_Trk_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_pool_proteome_cell_0", "millimolar", "parameters", CONSTANT},
    {"ATP_ref_mod_protein_cell_0", "millimolar", "parameters", CONSTANT},
    {"B_Ca_init_mod_Ca_cell_0", "millimolar", "parameters", CONSTANT},
    {"B_Ca_total_mod_Ca_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_ADP_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_ATP_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_G6P_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_Pi_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_e_mod_glc_ABC_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_e_mod_glc_GalP_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_e_mod_glc_PTS_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_e_mod_membrane_face_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glyc_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glyc_max_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_lac_e_mod_glc_ABC_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_lac_e_mod_glc_GalP_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_lac_e_mod_glc_PTS_cell_0", "millimolar", "parameters", CONSTANT},
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
    {"K_ADP_PEP_mod_glc_PTS_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ADP_gly_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ADP_hyd_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ATP_gly_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ATP_hk_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ATP_hyd_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ATP_mx_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ATP_reg_mod_reg_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_ATP_store_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_D_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"K_G6P_lg_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_G6P_mx_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_G6P_reg_mod_reg_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_G6P_store_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_H_reg_mod_reg_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_Pi_PEP_mod_glc_PTS_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_Pi_gly_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_Pi_hyd_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_Pi_mob_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_act_H_mod_i_H_ATPase_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_glc_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_glc_reg_mod_reg_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_glc_t_mod_glc_ABC_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_glc_t_mod_glc_GalP_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_glc_t_mod_glc_PTS_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_glc_t_mod_membrane_face_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_glyc_mob_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_i_ATP_mob_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_lac_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_lac_t_mod_membrane_face_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_H_mod_i_H_ATPase_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_bio_mod_bio_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_glc_ABC_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_glc_PTS_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_ABC_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_Flagellum_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_GalP_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_Glf_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_Glyc_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_H_ATPase_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_Hydrolase_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_KefC_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_LacT_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_NCX_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_NhaA_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_PTS_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_Trk_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_ATP_mod_pool_proteome_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_m_poly_mod_hydrolase_cell_0", "millimolar", "parameters", CONSTANT},
    {"Ki_init_mod_Na_K_cell_0", "millimolar", "parameters", CONSTANT},
    {"KmCa_mod_i_NaCa_cell_0", "millimolar", "parameters", CONSTANT},
    {"KmNai_mod_i_NaCa_cell_0", "millimolar", "parameters", CONSTANT},
    {"Ko_mod_Na_K_cell_0", "millimolar", "parameters", CONSTANT},
    {"Ksat_mod_i_NaCa_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mRNA_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_Hydrolase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_PTS_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"L_rRNA_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"N_A_mod_bio_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_hydrolase_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_i_flagellum_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_ABC_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_Flagellum_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_GalP_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_Glf_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_Glyc_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_H_ATPase_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_Hydrolase_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_KefC_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_LacT_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_NCX_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_NhaA_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_pool_PTS_cell_0", "per_mol", "parameters", CONSTANT},
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
    {"V_lg_mod_cytosol_cell_0", "millimolar_per_s", "parameters", CONSTANT},
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
    {"c_deg_mod_pool_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_Hydrolase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_PTS_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_deg_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_lipid_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_m_mod_membrane_cell_0", "uF_per_cm2", "parameters", CONSTANT},
    {"c_nt_denovo_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_nt_recycle_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_Hydrolase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_PTS_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_poly_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_Hydrolase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_PTS_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_recycle_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_std_mod_H_cell_0", "millimolar", "parameters", CONSTANT},
    {"c_std_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"c_std_mod_glc_ABC_cell_0", "millimolar", "parameters", CONSTANT},
    {"c_std_mod_glc_GalP_cell_0", "millimolar", "parameters", CONSTANT},
    {"c_std_mod_glc_PTS_cell_0", "millimolar", "parameters", CONSTANT},
    {"c_std_mod_i_H_ATPase_cell_0", "millimolar", "parameters", CONSTANT},
    {"c_syn_mod_pool_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_Hydrolase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_PTS_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_syn_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"dG0_ferm_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"dG0_hk_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"dG0_hk_mod_glc_PTS_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"dG0_hyd_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"dG0_hyd_mod_glc_ABC_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"dG0_hyd_mod_i_H_ATPase_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"dG0_mx_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"dG0_store_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"dT_Q10_mod_bio_cell_0", "kelvin", "parameters", CONSTANT},
    {"dT_Q10_mod_cytosol_cell_0", "kelvin", "parameters", CONSTANT},
    {"eta_mod_i_flagellum_cell_0", "pascal_second", "parameters", CONSTANT},
    {"f_ref_mod_i_flagellum_cell_0", "per_s", "parameters", CONSTANT},
    {"footprint_mod_pool_ABC_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_Flagellum_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_GalP_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_Glf_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_Glyc_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_H_ATPase_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_Hydrolase_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_KefC_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_LacT_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_NCX_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_NhaA_cell_0", "nm2", "parameters", CONSTANT},
    {"footprint_mod_pool_PTS_cell_0", "nm2", "parameters", CONSTANT},
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
    {"h_hk_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"h_lac_gly_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"h_mx_poly_mod_cytosol_cell_0", "J_per_mol", "parameters", CONSTANT},
    {"jmax_glc_mod_glc_ABC_cell_0", "mol_per_s_per_um2", "parameters", CONSTANT},
    {"jmax_glc_mod_glc_GalP_cell_0", "mol_per_s_per_um2", "parameters", CONSTANT},
    {"jmax_glc_mod_glc_PTS_cell_0", "mol_per_s_per_um2", "parameters", CONSTANT},
    {"jmax_glc_mod_membrane_face_cell_0", "mol_per_s_per_um2", "parameters", CONSTANT},
    {"jmax_lac_mod_membrane_face_cell_0", "mol_per_s_per_um2", "parameters", CONSTANT},
    {"k_bind_Ca_mod_Ca_cell_0", "per_millimolar_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_ABC_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_Flagellum_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_GalP_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_Glf_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_Glyc_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_H_ATPase_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_Hydrolase_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_KefC_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_LacT_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_NCX_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_NhaA_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_PTS_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_Trk_cell_0", "per_s", "parameters", CONSTANT},
    {"k_deg_mod_pool_proteome_cell_0", "per_s", "parameters", CONSTANT},
    {"k_lipid_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"k_ribo_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"k_unbind_Ca_mod_Ca_cell_0", "per_s", "parameters", CONSTANT},
    {"kcat_mod_hydrolase_cell_0", "per_s", "parameters", CONSTANT},
    {"m_init_mod_pool_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_Hydrolase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_PTS_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_init_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_target_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_target_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"m_target_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"mx_alloc_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_hydrolase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_Hydrolase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_PTS_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_0_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_ATP_mx_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_G6P_lg_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_H_mod_glc_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_H_mod_i_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_H_rev_mod_i_flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_P_mod_glc_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_half_mod_i_flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_mRNA_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_reg_mod_reg_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_ribo_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"pmf_ref_mod_i_flagellum_cell_0", "milliV", "parameters", CONSTANT},
    {"r_H_ATPase_mod_i_H_ATPase_cell_0", "per_s", "parameters", CONSTANT},
    {"r_NaCa_mod_i_NaCa_cell_0", "per_s", "parameters", CONSTANT},
    {"r_dmg_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"r_rep_max_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"reg_on_ATP_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_Hydrolase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_PTS_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_Hydrolase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_PTS_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_Hydrolase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_PTS_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_Flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_Glyc_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_Hydrolase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_PTS_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_pool_proteome_cell_0", "dimensionless", "parameters", CONSTANT},
    {"repair_level_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"rho_H_ATPase_mod_i_H_ATPase_cell_0", "per_um2", "parameters", CONSTANT},
    {"rho_NaCa_mod_i_NaCa_cell_0", "per_um2", "parameters", CONSTANT},
    {"rho_leak_Ca_mod_i_leak_Ca_cell_0", "per_um2", "parameters", CONSTANT},
    {"rho_leak_K_mod_i_K_Trk_cell_0", "per_um2", "parameters", CONSTANT},
    {"rho_leak_Na_mod_i_leak_Na_cell_0", "per_um2", "parameters", CONSTANT},
    {"s_target_mod_pool_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"s_target_mod_pool_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"s_target_mod_pool_Glf_cell_0", "dimensionless", "parameters", CONSTANT},
    {"s_target_mod_pool_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"s_target_mod_pool_Hydrolase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"s_target_mod_pool_KefC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"s_target_mod_pool_LacT_cell_0", "dimensionless", "parameters", CONSTANT},
    {"s_target_mod_pool_NCX_cell_0", "dimensionless", "parameters", CONSTANT},
    {"s_target_mod_pool_NhaA_cell_0", "dimensionless", "parameters", CONSTANT},
    {"s_target_mod_pool_PTS_cell_0", "dimensionless", "parameters", CONSTANT},
    {"s_target_mod_pool_Trk_cell_0", "dimensionless", "parameters", CONSTANT},
    {"store_alloc_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
    {"surface_factor_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"surface_factor_mod_protein_cell_0", "dimensionless", "parameters", CONSTANT},
    {"t_half_mRNA_mod_bio_cell_0", "second", "parameters", CONSTANT},
    {"tau_bundle_mod_i_flagellum_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_ABC_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_Flagellum_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_GalP_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_Glf_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_Glyc_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_H_ATPase_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_Hydrolase_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_KefC_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_LacT_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_NCX_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_NhaA_cell_0", "second", "parameters", CONSTANT},
    {"tau_mod_pool_PTS_cell_0", "second", "parameters", CONSTANT},
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
    {"cytosol__K_eq_hk", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"cytosol__K_eq_lg", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
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
    {"pool_proteome__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Glyc__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Glyc__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"i_leak_Na__g_leak_Na", "microS", "cell_0_module", COMPUTED_CONSTANT},
    {"i_leak_Ca__g_leak_Ca", "microS", "cell_0_module", COMPUTED_CONSTANT},
    {"i_leak_H__g_leak_H", "microS", "cell_0_module", COMPUTED_CONSTANT},
    {"membrane_face__X_lac_e", "millimolar", "cell_0_module", COMPUTED_CONSTANT},
    {"membrane_face__J_ATP_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"membrane_face__J_ADP_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"membrane_face__J_Pi_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"i_flagellum__xi", "newton_s_per_m", "cell_0_module", COMPUTED_CONSTANT},
    {"glc_PTS__K_eq", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"glc_PTS__J_lac_e", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"glc_PTS__j_Pi", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"glc_ABC__J_lac_e", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"glc_ABC__J_lac_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"glc_ABC__J_ATP_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"glc_ABC__J_ADP_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"glc_ABC__J_Pi_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"glc_GalP__J_lac_e", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"glc_GalP__J_lac_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"glc_GalP__J_ATP_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"glc_GalP__J_ADP_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"glc_GalP__J_Pi_c", "mol_per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_H_ATPase__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_H_ATPase__m_target", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_H_ATPase__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_NhaA__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_NhaA__m_target", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_NhaA__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_KefC__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_KefC__m_target", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_KefC__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Trk__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Trk__m_target", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Trk__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_NCX__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_NCX__m_target", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_NCX__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Glf__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Glf__m_target", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Glf__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_LacT__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_LacT__m_target", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_LacT__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Flagellum__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Flagellum__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Hydrolase__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Hydrolase__m_target", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_Hydrolase__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_PTS__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_PTS__m_target", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_PTS__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_ABC__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_ABC__m_target", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_ABC__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_GalP__a_unit", "um2", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_GalP__m_target", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
    {"pool_GalP__s_unit", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
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
    {"cytosol__Gamma_hk", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"cytosol__Gamma_lg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"cytosol__Gamma_hyd", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"cytosol__v_gly", "millimolar_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__v_lg", "millimolar_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__v_hyd", "millimolar_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__A_hk", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"cytosol__A_lg", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"cytosol__A_hyd", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"cytosol__a_glyc", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"multiport_sum_mod_cytosol_j_G6P_ext__j_G6P_ext", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_cytosol_j_Pi_ext__j_Pi_ext", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__Q_met", "watt", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_cytosol_J_glc_in__J_glc_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_cytosol_J_lac_in__J_lac_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_cytosol_j_ADP_ext__j_ADP_ext", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__J_ATP_hk", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__J_ATP_lg", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__J_ATP_mob", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"cytosol__J_ATP_gly", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"reg__f_H_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__f_glc_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__f_G6P_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__f_ATP_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"pool_proteome__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"pool_proteome__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"pool_Glyc__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__x_ATP_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__x_G6P_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__x_glc_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__x_H_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"hydrolase__n_copies", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"hydrolase__V_hydrolysis_max", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_PTS__Gamma", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"glc_PTS__f_PEP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"glc_PTS__J_glc", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_PTS__A_glc", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"glc_PTS__J_glc_e", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_PTS__J_G6P", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_PTS__J_ATP_cost", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_PTS__j_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_PTS__j_ADP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_ABC__ATP_eq", "millimolar", "cell_0_module", ALGEBRAIC},
    {"glc_ABC__A_ATP", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"glc_ABC__J_glc", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_ABC__A_glc", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"glc_ABC__J_ATP_cost", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_ABC__j_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_ABC__j_ADP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_ABC__j_Pi", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_ABC__J_glc_e", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_ABC__J_glc_c", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_GalP__W_H", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"glc_GalP__J_glc", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_GalP__A_glc", "J_per_mol", "cell_0_module", ALGEBRAIC},
    {"glc_GalP__j_H", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_GalP__i_glc_H", "nanoA", "cell_0_module", ALGEBRAIC},
    {"glc_GalP__J_glc_e", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"glc_GalP__J_glc_c", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"pool_H_ATPase__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"pool_NhaA__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"pool_KefC__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"pool_Trk__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"pool_NCX__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"pool_Glf__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"pool_LacT__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"pool_Flagellum__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__n", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__m_goal", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__r_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__r_up", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__share", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    variables[159] = -105.0;
    states[1] = variables[159];
    variables[104] = 200.0;
    states[2] = variables[104];
    variables[142] = 20.0;
    states[3] = variables[142];
    variables[23] = 7.991010113622174;
    states[4] = variables[23];
    variables[42] = 9.0e-05;
    states[5] = variables[42];
    variables[48] = 2.5118864315095822e-05;
    states[6] = variables[48];
    variables[26] = 9.6;
    states[7] = variables[26];
    variables[33] = 0.002;
    states[8] = variables[33];
    variables[25] = 0.56;
    states[9] = variables[25];
    variables[27] = 1.0;
    states[10] = variables[27];
    variables[28] = 10.0;
    states[11] = variables[28];
    variables[40] = 0.26990316647694707;
    states[12] = variables[40];
    variables[303] = 1.0;
    states[13] = variables[303];
    variables[34] = 0.0;
    states[14] = variables[34];
    variables[45] = 0.011111111111111112;
    states[15] = variables[45];
    variables[145] = 0.0;
    states[16] = variables[145];
    variables[312] = 1.0;
    states[17] = variables[312];
    variables[304] = 1.0;
    states[18] = variables[304];
    variables[309] = 1.0;
    states[19] = variables[309];
    variables[306] = 1.0;
    states[20] = variables[306];
    variables[311] = 1.0;
    states[21] = variables[311];
    variables[308] = 1.0;
    states[22] = variables[308];
    variables[302] = 1.0;
    states[23] = variables[302];
    variables[307] = 1.0;
    states[24] = variables[307];
    variables[161] = 0.0;
    states[25] = variables[161];
    variables[300] = 1.0;
    states[26] = variables[300];
    variables[305] = 1.0;
    states[27] = variables[305];
    variables[310] = 0.0;
    states[28] = variables[310];
    variables[299] = 0.0;
    states[29] = variables[299];
    variables[301] = 0.0;
    states[30] = variables[301];
    variables[448] = 0.0;
    states[31] = variables[448];
    variables[446] = 5.011872336272725e-05;
    states[32] = variables[446];
    variables[444] = 0.1;
    states[33] = variables[444];
    variables[445] = 0.0;
    states[34] = variables[445];
    variables[447] = 0.003;
    states[35] = variables[447];
    states[36] = 0.0;
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
    variables[19] = 9.6;
    variables[20] = 9.6;
    variables[21] = 9.6;
    variables[22] = 9.6;
    variables[24] = 8.0;
    variables[29] = 0.1;
    variables[30] = 0.1;
    variables[31] = 0.1;
    variables[32] = 0.1;
    variables[35] = 200.0;
    variables[36] = 0.0;
    variables[37] = 0.0;
    variables[38] = 0.0;
    variables[39] = 0.0;
    variables[41] = 3.0037809007103e-14;
    variables[43] = 10.28;
    variables[44] = 10.28;
    variables[46] = 96485.0;
    variables[47] = 5.011872336272725e-05;
    variables[49] = 5.011872336272725e-05;
    variables[50] = 0.0001;
    variables[51] = 0.0;
    variables[52] = 1.0e-30;
    variables[53] = 9.06587001723412e-19;
    variables[54] = 9.06587001723412e-19;
    variables[55] = 0.1;
    variables[56] = 0.1;
    variables[57] = 0.5;
    variables[58] = 1.0;
    variables[59] = 0.1;
    variables[60] = 0.5;
    variables[61] = 1.0;
    variables[62] = 4.8;
    variables[63] = 1.0;
    variables[64] = 0.1;
    variables[65] = 4.158318947097563;
    variables[66] = 50.0;
    variables[67] = 0.5;
    variables[68] = 50.0;
    variables[69] = 5.0237728630191644e-05;
    variables[70] = 1.0;
    variables[71] = 1.0;
    variables[72] = 5.0;
    variables[73] = 5.0;
    variables[74] = 2.5118864315095822e-05;
    variables[75] = 0.1;
    variables[76] = 0.001;
    variables[77] = 0.0005;
    variables[78] = 0.1;
    variables[79] = 0.005;
    variables[80] = 4.1;
    variables[81] = 10.0;
    variables[82] = 1.0;
    variables[83] = 10.0;
    variables[84] = 4.0;
    variables[85] = 0.2;
    variables[86] = 0.1;
    variables[87] = 0.1;
    variables[88] = 0.1;
    variables[89] = 0.1;
    variables[90] = 0.1;
    variables[91] = 0.1;
    variables[92] = 0.1;
    variables[93] = 0.1;
    variables[94] = 0.1;
    variables[95] = 0.1;
    variables[96] = 0.1;
    variables[97] = 0.1;
    variables[98] = 0.1;
    variables[99] = 0.1;
    variables[100] = 0.1;
    variables[101] = 0.1;
    variables[102] = 0.1;
    variables[103] = 20.0;
    variables[105] = 1.38;
    variables[106] = 87.5;
    variables[107] = 10.21;
    variables[108] = 0.1;
    variables[109] = 1000.0;
    variables[110] = 1510.0;
    variables[111] = 10128240.0;
    variables[112] = 464.0;
    variables[113] = 473.0;
    variables[114] = 350.0;
    variables[115] = 4895.0;
    variables[116] = 700.0;
    variables[117] = 620.0;
    variables[118] = 551.0;
    variables[119] = 302.0;
    variables[120] = 388.0;
    variables[121] = 1306.0;
    variables[122] = 2798.0;
    variables[123] = 300.0;
    variables[124] = 4566.0;
    variables[125] = 6.02214076e+23;
    variables[126] = 6.02214076e+23;
    variables[127] = 6.02214076e+23;
    variables[128] = 6.02214076e+23;
    variables[129] = 6.02214076e+23;
    variables[130] = 6.02214076e+23;
    variables[131] = 6.02214076e+23;
    variables[132] = 6.02214076e+23;
    variables[133] = 6.02214076e+23;
    variables[134] = 6.02214076e+23;
    variables[135] = 6.02214076e+23;
    variables[136] = 6.02214076e+23;
    variables[137] = 6.02214076e+23;
    variables[138] = 6.02214076e+23;
    variables[139] = 6.02214076e+23;
    variables[140] = 6.02214076e+23;
    variables[141] = 6.02214076e+23;
    variables[143] = 469.0;
    variables[144] = 469.0;
    variables[146] = 2.0;
    variables[147] = 2.0;
    variables[148] = 8.314;
    variables[149] = 1.0;
    variables[150] = 1.0;
    variables[151] = 293.15;
    variables[152] = 293.15;
    variables[153] = 293.15;
    variables[154] = 6.588125835191758;
    variables[155] = 0.0836591948542882;
    variables[156] = 28.05882448294951;
    variables[157] = 0.14428780266714164;
    variables[158] = 0.0072143901333570825;
    variables[160] = 0.05050073093349957;
    variables[162] = 0.0;
    variables[163] = 1.2;
    variables[164] = 0.15;
    variables[165] = 5.0e-07;
    variables[166] = 50.0;
    variables[167] = 5.0;
    variables[168] = 1.0;
    variables[169] = 1.0;
    variables[170] = 1.0;
    variables[171] = 1.0;
    variables[172] = 1.0;
    variables[173] = 1.0;
    variables[174] = 1.0;
    variables[175] = 1.0;
    variables[176] = 1.0;
    variables[177] = 1.0;
    variables[178] = 1.0;
    variables[179] = 1.0;
    variables[180] = 1.0;
    variables[181] = 1.0;
    variables[182] = 100.0;
    variables[183] = 1.0;
    variables[184] = 50.0;
    variables[185] = 2.0;
    variables[186] = 4.0;
    variables[187] = 4.0;
    variables[188] = 4.0;
    variables[189] = 4.0;
    variables[190] = 4.0;
    variables[191] = 4.0;
    variables[192] = 4.0;
    variables[193] = 4.0;
    variables[194] = 4.0;
    variables[195] = 4.0;
    variables[196] = 4.0;
    variables[197] = 4.0;
    variables[198] = 4.0;
    variables[199] = 4.0;
    variables[200] = 0.5;
    variables[201] = 0.5;
    variables[202] = 0.5;
    variables[203] = 0.5;
    variables[204] = 0.5;
    variables[205] = 0.5;
    variables[206] = 0.5;
    variables[207] = 0.5;
    variables[208] = 0.5;
    variables[209] = 0.5;
    variables[210] = 0.5;
    variables[211] = 0.5;
    variables[212] = 0.5;
    variables[213] = 0.5;
    variables[214] = 1000.0;
    variables[215] = 1000.0;
    variables[216] = 1000.0;
    variables[217] = 1000.0;
    variables[218] = 1000.0;
    variables[219] = 1000.0;
    variables[220] = 24.0;
    variables[221] = 24.0;
    variables[222] = 24.0;
    variables[223] = 24.0;
    variables[224] = 24.0;
    variables[225] = 24.0;
    variables[226] = 24.0;
    variables[227] = 24.0;
    variables[228] = 24.0;
    variables[229] = 24.0;
    variables[230] = 24.0;
    variables[231] = 24.0;
    variables[232] = 24.0;
    variables[233] = 24.0;
    variables[234] = -196000.0;
    variables[235] = -16700.0;
    variables[236] = -16700.0;
    variables[237] = -30500.0;
    variables[238] = -30500.0;
    variables[239] = -30500.0;
    variables[240] = -55800.0;
    variables[241] = -42000.0;
    variables[242] = 10.0;
    variables[243] = 10.0;
    variables[244] = 0.00108;
    variables[245] = 100.0;
    variables[246] = 25.0;
    variables[247] = 1700.0;
    variables[248] = 12.0;
    variables[249] = 12.0;
    variables[250] = 0.0;
    variables[251] = 100.0;
    variables[252] = 20.0;
    variables[253] = 20.0;
    variables[254] = 15.0;
    variables[255] = 12.0;
    variables[256] = 15.0;
    variables[257] = 12.0;
    variables[258] = 60.0;
    variables[259] = 0.0;
    variables[260] = 2.3686015649131477e-08;
    variables[261] = 1.2528761581650615e-08;
    variables[262] = 1.4452236359013608e-08;
    variables[263] = 0.35;
    variables[264] = 5.0;
    variables[265] = 50.0;
    variables[266] = 27.0;
    variables[267] = 9200000.0;
    variables[268] = 0.0;
    variables[269] = 20500.0;
    variables[270] = 2.0;
    variables[271] = 0.0;
    variables[272] = 23800.0;
    variables[273] = 63000.0;
    variables[274] = 0.0;
    variables[275] = 3.0933897205955483e-20;
    variables[276] = 7.733474301488869e-20;
    variables[277] = 3.0933897205955476e-19;
    variables[278] = 1.5466948602977742e-18;
    variables[279] = 4.32863408001425e-18;
    variables[280] = 100000.0;
    variables[281] = 2.777777777777778e-06;
    variables[282] = 2.777777777777778e-06;
    variables[283] = 2.777777777777778e-06;
    variables[284] = 2.777777777777778e-06;
    variables[285] = 2.777777777777778e-06;
    variables[286] = 2.777777777777778e-06;
    variables[287] = 2.777777777777778e-06;
    variables[288] = 2.777777777777778e-06;
    variables[289] = 2.777777777777778e-06;
    variables[290] = 2.777777777777778e-06;
    variables[291] = 2.777777777777778e-06;
    variables[292] = 2.777777777777778e-06;
    variables[293] = 2.777777777777778e-06;
    variables[294] = 2.777777777777778e-06;
    variables[295] = 2.777777777777778e-06;
    variables[296] = 2.777777777777778e-06;
    variables[297] = 8000.0;
    variables[298] = 1000.0;
    variables[313] = 1.0;
    variables[314] = 1.0;
    variables[315] = 1.0;
    variables[316] = 0.0;
    variables[317] = 2729.797267782374;
    variables[318] = 11704.838040881496;
    variables[319] = 1.0;
    variables[320] = 11704.838040881496;
    variables[321] = 11704.838040881496;
    variables[322] = 1884955.592153876;
    variables[323] = 2850.255770975698;
    variables[324] = 2729.797267782374;
    variables[325] = 909.932422594125;
    variables[326] = 32757.567213388495;
    variables[327] = 2801.9209327573735;
    variables[328] = 682.4493169455936;
    variables[329] = 11704.838040881496;
    variables[330] = 90.9932422594125;
    variables[331] = 16964600.329384882;
    variables[332] = 2.0;
    variables[333] = 4.0;
    variables[334] = 1.0;
    variables[335] = 3.3333333333333335;
    variables[336] = 1200.0;
    variables[337] = 2.0;
    variables[338] = 2.0;
    variables[339] = 8670.79572390783;
    variables[340] = 4.0;
    variables[341] = 43982.2971502571;
    variables[342] = 122.44926189223139;
    variables[343] = 100.0;
    variables[344] = 5000.0;
    variables[345] = 1.1574074074074074e-06;
    variables[346] = 1.1574074074074073e-05;
    variables[347] = 0.0;
    variables[348] = 0.0;
    variables[349] = 0.0;
    variables[350] = 0.0;
    variables[351] = 0.0;
    variables[352] = 0.0;
    variables[353] = 0.0;
    variables[354] = 0.0;
    variables[355] = 0.0;
    variables[356] = 0.0;
    variables[357] = 0.0;
    variables[358] = 0.0;
    variables[359] = 0.0;
    variables[360] = 0.0;
    variables[361] = 0.0;
    variables[362] = 0.0;
    variables[363] = 0.0;
    variables[364] = 0.0;
    variables[365] = 0.0;
    variables[366] = 0.0;
    variables[367] = 0.0;
    variables[368] = 0.0;
    variables[369] = 0.0;
    variables[370] = 0.0;
    variables[371] = 0.0;
    variables[372] = 0.0;
    variables[373] = 0.0;
    variables[374] = 0.0;
    variables[375] = 0.0;
    variables[376] = 0.0;
    variables[377] = 0.0;
    variables[378] = 0.0;
    variables[379] = 0.0;
    variables[380] = 0.0;
    variables[381] = 0.0;
    variables[382] = 0.0;
    variables[383] = 0.0;
    variables[384] = 0.0;
    variables[385] = 0.0;
    variables[386] = 0.0;
    variables[387] = 0.0;
    variables[388] = 0.0;
    variables[389] = 0.0;
    variables[390] = 0.0;
    variables[391] = 0.0;
    variables[392] = 0.0;
    variables[393] = 0.0;
    variables[394] = 0.0;
    variables[395] = 0.0;
    variables[396] = 0.0;
    variables[397] = 0.0;
    variables[398] = 0.0;
    variables[399] = 0.0;
    variables[400] = 0.0;
    variables[401] = 0.0;
    variables[402] = 0.0;
    variables[403] = 0.0;
    variables[404] = 0.0;
    variables[405] = 0.0;
    variables[406] = 0.0;
    variables[407] = 1.0;
    variables[408] = 226.81614751349176;
    variables[409] = 222.96978330049507;
    variables[410] = 1.3806187541439416e-05;
    variables[411] = 0.0007772676368068118;
    variables[412] = 0.00016924908710165128;
    variables[413] = 0.0;
    variables[414] = 0.0;
    variables[415] = 0.049676875527902564;
    variables[416] = 0.10080717667266298;
    variables[417] = 0.01930936572472512;
    variables[418] = 0.006436455241575042;
    variables[419] = 0.17378429152252606;
    variables[420] = 0.011891721776026404;
    variables[421] = 0.00362050607338596;
    variables[422] = 0.0;
    variables[423] = 0.0019309365724725122;
    variables[424] = 0.0;
    variables[425] = 1.5;
    variables[426] = 1.5;
    variables[427] = 300.0;
    variables[428] = 0.3;
    variables[429] = 10.0;
    variables[430] = 10.0;
    variables[431] = 10.0;
    variables[432] = 10.0;
    variables[433] = 10.0;
    variables[434] = 10.0;
    variables[435] = 10.0;
    variables[436] = 10.0;
    variables[437] = 10.0;
    variables[438] = 10.0;
    variables[439] = 10.0;
    variables[440] = 10.0;
    variables[441] = 10.0;
    variables[442] = 10.0;
    variables[443] = 1.5e-05;
    variables[449] = 1.4396817324298869e-18;
    variables[450] = 2599.594240021476;
    variables[451] = 1000.0;
    variables[452] = 9.719999999999999e-05;
    variables[453] = 1.0e-06;
    variables[454] = 0.5;
    variables[455] = 308.641975308642;
    variables[456] = 0.001;
    variables[457] = 0.0;
    variables[458] = 0.0;
    variables[459] = 0.0;
    variables[460] = 0.0;
    variables[461] = 0.0;
    variables[462] = 5.9e-10;
    variables[463] = 9.0e-10;
    variables[464] = 2.607594240021476e-06;
    variables[465] = 1.75e-09;
    variables[466] = 6.0e-13;
    variables[467] = 6.639528095680696e-07;
    variables[468] = 6.639528095680696e-07;
    variables[469] = 1.0;
    variables[470] = 0.1;
    variables[471] = 0.1;
    variables[472] = 0.0;
    variables[473] = 0.0;
    variables[474] = 5.011872336272725e-05;
    variables[475] = 5.011872336272725e-05;
    variables[476] = 0.003;
    variables[477] = 0.003;
    variables[478] = 1.0;
    variables[479] = 1.0;
    variables[514] = 0.0;
    variables[515] = 0.0;
    variables[516] = 0.0;
    variables[519] = 0.0;
    variables[520] = 0.0;
    variables[521] = 0.0;
    variables[522] = 0.0;
    variables[523] = 0.0;
    variables[524] = 0.0;
    variables[525] = 0.0;
    variables[526] = 0.0;
    variables[527] = 0.0;
    variables[528] = 0.0;
    variables[529] = 0.0;
    variables[530] = 0.0;
    variables[493] = 0.0;
    variables[494] = 0.0;
    variables[495] = 0.0;
    variables[497] = 0.0;
    variables[499] = 0.0;
    variables[481] = 0.0;
    variables[480] = 0.0;
    variables[567] = 0.0;
    variables[569] = 0.0;
    variables[571] = 0.0;
    variables[573] = 0.0;
    variables[566] = 0.0;
    variables[568] = 0.0;
    variables[570] = 0.0;
    variables[572] = 0.0;
}

void computeComputedConstants(double voi, double *states, double *rates, double *variables)
{
    variables[482] = 2.0*3.14159265358979*variables[150]*variables[270];
    variables[483] = 3.14159265358979*pow(variables[150], 2.0)*variables[270]*1.0e-18;
    variables[484] = variables[183]*variables[482]*0.01;
    variables[485] = -log(variables[49]/variables[214])/log(10.0);
    variables[486] = pow(variables[147], (variables[151]-variables[153])/variables[243]);
    variables[487] = exp(-variables[235]/(variables[148]*variables[151]));
    variables[488] = exp(-(variables[234]-2.0*variables[237]-variables[235])/(variables[148]*variables[151]));
    variables[489] = exp(-variables[237]/(variables[148]*variables[151]));
    variables[490] = exp(-(variables[241]-variables[235])/(variables[148]*variables[151]));
    variables[491] = exp(-(variables[235]+variables[237]-variables[241])/(variables[148]*variables[151]));
    variables[492] = exp(-variables[240]/(variables[148]*variables[151]));
    variables[501] = 2.0*variables[482]*variables[425]/variables[165];
    variables[502] = pow(variables[146], (variables[151]-variables[152])/variables[242]);
    variables[503] = ((variables[339]*variables[109]+variables[341]*variables[124]+variables[267])*variables[184]+variables[501]*variables[182])/variables[125];
    variables[504] = variables[482]*variables[426];
    variables[505] = variables[164]*variables[504];
    variables[506] = variables[331]*variables[259]*1.0e-6;
    variables[507] = variables[506]/variables[505];
    variables[508] = variables[322]*variables[250]*1.0e-6;
    variables[509] = variables[508]/variables[505];
    variables[510] = variables[482]*variables[412]*variables[266]*1.0e-6;
    variables[511] = variables[482]*variables[410]*variables[264]*1.0e-6;
    variables[512] = variables[482]*variables[260];
    variables[513] = variables[39]*variables[47]/variables[50];
    variables[517] = 6.0*3.14159265358979*variables[244]*variables[149]*1.0e-6;
    variables[518] = exp(-variables[236]/(variables[148]*variables[151]));
    variables[531] = variables[323]*variables[251]*1.0e-6;
    variables[532] = variables[416]*variables[505]/variables[531];
    variables[533] = variables[531]/variables[505];
    variables[534] = variables[328]*variables[256]*1.0e-6;
    variables[535] = variables[421]*variables[505]/variables[534];
    variables[536] = variables[534]/variables[505];
    variables[537] = variables[325]*variables[253]*1.0e-6;
    variables[538] = variables[418]*variables[505]/variables[537];
    variables[539] = variables[537]/variables[505];
    variables[540] = variables[330]*variables[258]*1.0e-6;
    variables[541] = variables[423]*variables[505]/variables[540];
    variables[542] = variables[540]/variables[505];
    variables[543] = variables[327]*variables[255]*1.0e-6;
    variables[544] = variables[420]*variables[505]/variables[543];
    variables[545] = variables[543]/variables[505];
    variables[546] = variables[321]*variables[249]*1.0e-6;
    variables[547] = variables[415]*variables[505]/variables[546];
    variables[548] = variables[546]/variables[505];
    variables[549] = variables[326]*variables[254]*1.0e-6;
    variables[550] = variables[419]*variables[505]/variables[549];
    variables[551] = variables[549]/variables[505];
    variables[552] = variables[319]*variables[247]*1.0e-6;
    variables[553] = variables[552]/variables[505];
    variables[554] = variables[324]*variables[252]*1.0e-6;
    variables[555] = variables[417]*variables[505]/variables[554];
    variables[556] = variables[554]/variables[505];
    variables[557] = variables[329]*variables[257]*1.0e-6;
    variables[558] = variables[422]*variables[505]/variables[557];
    variables[559] = variables[557]/variables[505];
    variables[560] = variables[318]*variables[246]*1.0e-6;
    variables[561] = variables[413]*variables[505]/variables[560];
    variables[562] = variables[560]/variables[505];
    variables[563] = variables[320]*variables[248]*1.0e-6;
    variables[564] = variables[414]*variables[505]/variables[563];
    variables[565] = variables[563]/variables[505];
    variables[496] = variables[514]+variables[523]+variables[528];
    variables[498] = variables[515]+variables[524]+variables[529];
    variables[500] = variables[516]+variables[525]+variables[530];
    variables[574] = variables[469]*variables[466]*variables[462]*variables[478]*variables[479]/(variables[467]*variables[479]+variables[468]*variables[478]);
    variables[575] = variables[574]*(variables[470]-variables[471]);
    variables[576] = variables[575];
    variables[577] = -variables[575];
    variables[578] = variables[469]*variables[466]*variables[463]*variables[478]*variables[479]/(variables[467]*variables[479]+variables[468]*variables[478]);
    variables[579] = variables[578]*(variables[472]-variables[473]);
    variables[580] = variables[579];
    variables[581] = -variables[579];
    variables[582] = variables[469]*variables[466]*variables[464]*variables[478]*variables[479]/(variables[467]*variables[479]+variables[468]*variables[478]);
    variables[583] = variables[582]*(variables[474]-variables[475]);
    variables[584] = variables[583];
    variables[585] = -variables[583];
    variables[586] = variables[469]*variables[466]*variables[465]*variables[478]*variables[479]/(variables[467]*variables[479]+variables[468]*variables[478]);
    variables[587] = variables[586]*(variables[476]-variables[477]);
    variables[588] = variables[587];
    variables[589] = -variables[587];
}

void computeRates(double voi, double *states, double *rates, double *variables)
{
    variables[590] = variables[1]*states[0]/(states[0]+variables[2]);
    rates[0] = (variables[481]+variables[480]+variables[4])/variables[3]+variables[590];
    variables[602] = variables[148]*variables[151]/variables[46]*log(variables[49]/states[6])*1000.0;
    variables[792] = variables[334]*variables[46]*(variables[602]-states[1])*0.001;
    variables[793] = variables[276]*states[30]*variables[482]*(variables[30]-states[8]*exp(-variables[792]/(variables[148]*variables[151])))/(variables[78]+variables[30]);
    variables[795] = variables[334]*variables[793];
    variables[796] = -variables[46]*variables[795]*1000000000.0;
    variables[762] = variables[602]-states[1];
    variables[763] = states[25]*variables[245]*variables[762]/variables[342];
    variables[764] = states[26]*variables[763]*variables[336]/variables[127];
    variables[765] = -variables[764]*variables[46]*1000000000.0;
    variables[593] = variables[148]*variables[151]/variables[46]*log(variables[143]/states[3])*1000.0;
    variables[738] = -(states[1]+variables[593]-2.0*variables[602]);
    variables[739] = variables[482]*variables[262]*states[19]*variables[738]/variables[46]*1.0e-9;
    variables[740] = -variables[739]*variables[46]*1000000000.0;
    variables[729] = variables[335]*variables[46]*(variables[602]-states[1])*0.001;
    variables[728] = -variables[239]-variables[148]*variables[151]*log(fabs(states[9]/variables[219]*states[11]/variables[219]/(states[7]/variables[219]))+1.0e-300);
    variables[730] = 1.0-exp(-(variables[728]-variables[729])/(variables[148]*variables[151]));
    variables[727] = variables[482]*variables[408]*states[18]*variables[343]*variables[335]*1.602176634e-10;
    variables[731] = variables[727]*states[7]/(variables[85]+states[7])*states[6]/(variables[74]+states[6])*variables[730];
    variables[725] = variables[512]*(states[1]-variables[602]);
    variables[722] = variables[148]*variables[151]/(2.0*variables[46])*log(variables[44]/states[5])*1000.0;
    variables[723] = variables[511]*(states[1]-variables[722]);
    variables[750] = variables[482]*variables[409]*states[22]*variables[344]*1.602176634e-10;
    variables[751] = variables[750]*(exp(variables[263]*states[1]*1.0e-3*variables[46]/(variables[148]*variables[151]))*pow(states[3], 3.0)*variables[43]-exp((variables[263]-1.0)*states[1]*1.0e-3*variables[46]/(variables[148]*variables[151]))*pow(variables[144], 3.0)*states[5]*variables[163])/((pow(variables[106], 3.0)+pow(variables[144], 3.0))*(variables[105]+variables[43])*(1.0+variables[108]*exp((variables[263]-1.0)*states[1]*1.0e-3*variables[46]/(variables[148]*variables[151]))));
    variables[592] = variables[148]*variables[151]/variables[46]*log(variables[107]/states[2])*1000.0;
    variables[747] = variables[482]*variables[411]*states[21]*variables[265]*1.0e-6;
    variables[748] = variables[747]*(states[1]-variables[592]);
    variables[720] = variables[510]*(states[1]-variables[593]);
    variables[591] = variables[720]+variables[723]+variables[725]+variables[731]+variables[740]+variables[748]+variables[751]+variables[765]+variables[796];
    rates[1] = 1000000.0*(variables[51]-variables[591])/variables[484];
    variables[741] = -variables[739];
    variables[753] = -3.0*variables[751]/variables[46]*1.0e-9;
    variables[721] = -variables[720]/variables[46]*1.0e-9;
    variables[594] = variables[721]+variables[741]+variables[753];
    rates[3] = variables[594]/variables[483];
    variables[743] = variables[602]-variables[592];
    variables[744] = variables[482]*variables[261]*states[20]*variables[743]/variables[46]*1.0e-9;
    variables[745] = -variables[744];
    variables[749] = -variables[748]/variables[46]*1.0e-9;
    variables[595] = variables[749]+variables[745];
    rates[2] = variables[595]/variables[483];
    variables[596] = variables[24]-states[4];
    rates[4] = variables[297]*variables[596]-variables[280]*states[5]*states[4];
    variables[724] = -variables[723]/(2.0*variables[46])*1.0e-9;
    variables[752] = variables[751]/variables[46]*1.0e-9;
    variables[597] = variables[724]+variables[752];
    rates[5] = variables[597]/variables[483]+variables[297]*variables[596]-variables[280]*states[5]*states[4];
    variables[755] = states[12]*states[6]/variables[50];
    variables[756] = variables[279]*states[24]*variables[482]*(variables[513]/(variables[84]+variables[513])-variables[755]/(variables[84]+variables[755]));
    variables[757] = variables[756];
    variables[746] = variables[744];
    variables[742] = 2.0*variables[739];
    variables[732] = variables[731]/(variables[335]*variables[46])*1.0e-9;
    variables[733] = -variables[335]*variables[732];
    variables[734] = variables[733];
    variables[726] = -variables[725]/variables[46]*1.0e-9;
    variables[600] = variables[726]+variables[734]+variables[742]+variables[746]+variables[764]+variables[795]+variables[757];
    variables[620] = pow(states[9]/variables[215], variables[332])*pow(states[11]/variables[215], variables[332]+1.0)/(states[10]/variables[215]*pow(states[7]/variables[215], variables[332]));
    variables[621] = variables[158]*variables[316]*variables[486]*states[10]/(variables[66]+states[10])*states[7]/(variables[61]+states[7])*(1.0-variables[620]/variables[492]+fabs(1.0-variables[620]/variables[492]))/2.0;
    variables[613] = states[14]/(variables[81]+states[14]);
    variables[616] = variables[160]*variables[424]*variables[486]*(states[10]*states[7]-variables[613]*states[9]*pow(states[11], 2.0)/(variables[215]*variables[490]))/((variables[68]+states[10])*(variables[63]+states[7]))/(1.0+pow(states[14]/variables[35], 8.0));
    variables[608] = variables[156]*states[13]*variables[486]*pow(fabs(states[10]), variables[333]-1.0)*(states[10]-pow(states[12], 2.0)*pow(states[7], 3.0)*variables[215]/(pow(states[9], 3.0)*pow(states[11], 2.0)*variables[488]))/(pow(variables[65], variables[333])+pow(fabs(states[10]), variables[333]))*states[9]/(variables[56]+states[9])*states[11]/(variables[71]+states[11]);
    variables[607] = variables[154]*states[13]*variables[486]*pow(states[9]/variables[56], 2.0)*pow(states[11]/variables[71], 2.0)*(states[8]*states[7]-states[10]*states[9]/variables[487])/(variables[75]*(variables[59]+states[7]))/((1.0+states[8]/variables[75])*pow(1.0+states[9]/variables[56], 2.0)*pow(1.0+states[11]/variables[71], 2.0)+pow(1.0+states[12]/variables[83], 2.0)*pow(1.0+states[7]/variables[58], 2.0)-1.0);
    variables[606] = states[9]/variables[215]*states[11]/variables[215]/(states[7]/variables[215]);
    variables[609] = variables[155]*variables[486]*states[7]/variables[60]*(1.0-variables[606]/variables[489])/(1.0+states[7]/variables[60]+(1.0+states[9]/variables[57])*(1.0+states[11]/variables[72])-1.0);
    variables[782] = states[9]*states[11]/variables[216]*exp(variables[238]/(variables[148]*variables[151]));
    variables[784] = variables[275]*states[29]*variables[482]*(variables[29]*pow(states[7]/variables[87], variables[337])-states[8]*pow(variables[782]/variables[87], variables[337]))/((variables[77]+variables[29])*pow(1.0+states[7]/variables[87], variables[337]));
    variables[786] = variables[337]*variables[784];
    variables[787] = -variables[786];
    variables[774] = states[9]/(variables[55]+states[9])*states[11]/(variables[70]+states[11]);
    variables[775] = variables[277]*states[28]*variables[482]*variables[774]*(variables[31]*states[7]-states[10]*states[9]/variables[518])/((variables[88]+states[7])*(variables[79]+variables[31]));
    variables[780] = -variables[775];
    variables[735] = -variables[732];
    variables[642] = states[7]/(variables[86]+states[7])*(variables[86]+variables[7])/variables[7];
    variables[646] = variables[407]*variables[346]*states[15]/(variables[64]+states[15])*variables[642];
    variables[647] = variables[646]*variables[41];
    variables[645] = variables[501]*variables[182]*variables[295]/variables[125]*variables[642];
    variables[644] = variables[341]*variables[124]*variables[185]*variables[296]/variables[125]*variables[642];
    variables[643] = variables[339]*variables[109]*variables[185]*log(2.0)/variables[427]/variables[125]*variables[642];
    variables[648] = (states[7]-variables[5])/(variables[7]-variables[5]);
    variables[649] = ((variables[648]+fabs(variables[648]))/2.0+1.0-fabs((variables[648]+fabs(variables[648]))/2.0-1.0))/2.0;
    variables[719] = variables[69]/states[6];
    variables[652] = pow(variables[719], variables[340])/(1.0+pow(variables[719], variables[340]));
    variables[718] = (states[8]+fabs(states[8]))/(2.0*variables[76]);
    variables[653] = pow(variables[718], variables[340])/(1.0+pow(variables[718], variables[340]));
    variables[717] = (states[10]+fabs(states[10]))/(2.0*variables[67]);
    variables[654] = pow(variables[717], variables[340])/(1.0+pow(variables[717], variables[340]));
    variables[716] = (states[7]+fabs(states[7]))/(2.0*variables[62]);
    variables[655] = pow(variables[716], variables[340])/(1.0+pow(variables[716], variables[340]));
    variables[656] = (1.0-variables[347]+variables[347]*variables[655])*(1.0-variables[362]+variables[362]*variables[654])*(1.0-variables[392]+variables[392]*variables[653])*(1.0-variables[377]+variables[377]*variables[652]);
    variables[657] = variables[268]*variables[53]*variables[649]*variables[656];
    variables[658] = variables[657]*variables[642];
    variables[662] = variables[643]+variables[644]+variables[645]+variables[647]+variables[658];
    variables[663] = -variables[662];
    variables[970] = states[7]/(variables[91]+states[7])*(variables[91]+variables[10])/variables[10];
    variables[964] = variables[320]*states[30];
    variables[972] = variables[964]*variables[112]*(variables[188]+variables[170]+variables[202])*variables[283]/variables[130]*variables[970];
    variables[971] = (variables[970]+1.0-fabs(variables[970]-1.0))/2.0;
    variables[965] = (1.0-variables[350]+variables[350]*variables[655])*(1.0-variables[365]+variables[365]*variables[654])*(1.0-variables[395]+variables[395]*variables[653])*(1.0-variables[380]+variables[380]*variables[652]);
    variables[966] = variables[564]*variables[965];
    variables[967] = (variables[966]-states[30])/variables[431];
    variables[968] = (variables[967]+fabs(variables[967]))/2.0;
    variables[973] = variables[968]*variables[320]*variables[112]*(variables[222]+variables[188])/variables[130];
    variables[678] = (states[7]-variables[6])/(variables[22]-variables[6]);
    variables[679] = ((variables[678]+fabs(variables[678]))/2.0+1.0-fabs((variables[678]+fabs(variables[678]))/2.0-1.0))/2.0;
    variables[680] = variables[167]*variables[54]*variables[679];
    variables[905] = (1.0-variables[349]+variables[349]*variables[655])*(1.0-variables[364]+variables[364]*variables[654])*(1.0-variables[394]+variables[394]*variables[653])*(1.0-variables[379]+variables[379]*variables[652]);
    variables[906] = variables[313]*variables[905];
    variables[907] = (variables[906]-states[26])/variables[430];
    variables[908] = (variables[907]+fabs(variables[907]))/2.0;
    variables[913] = variables[908]*variables[319]*variables[111]*(variables[221]+variables[187])/variables[129];
    variables[950] = (1.0-variables[348]+variables[348]*variables[655])*(1.0-variables[363]+variables[363]*variables[654])*(1.0-variables[393]+variables[393]*variables[653])*(1.0-variables[378]+variables[378]*variables[652]);
    variables[951] = variables[561]*variables[950];
    variables[952] = (variables[951]-states[29])/variables[429];
    variables[953] = (variables[952]+fabs(variables[952]))/2.0;
    variables[958] = variables[953]*variables[318]*variables[110]*(variables[220]+variables[186])/variables[128];
    variables[935] = (1.0-variables[359]+variables[359]*variables[655])*(1.0-variables[374]+variables[374]*variables[654])*(1.0-variables[404]+variables[404]*variables[653])*(1.0-variables[389]+variables[389]*variables[652]);
    variables[936] = variables[558]*variables[935];
    variables[937] = (variables[936]-states[28])/variables[440];
    variables[938] = (variables[937]+fabs(variables[937]))/2.0;
    variables[943] = variables[938]*variables[329]*variables[121]*(variables[231]+variables[197])/variables[139];
    variables[920] = (1.0-variables[354]+variables[354]*variables[655])*(1.0-variables[369]+variables[369]*variables[654])*(1.0-variables[399]+variables[399]*variables[653])*(1.0-variables[384]+variables[384]*variables[652]);
    variables[921] = variables[555]*variables[920];
    variables[922] = (variables[921]-states[27])/variables[435];
    variables[923] = (variables[922]+fabs(variables[922]))/2.0;
    variables[928] = variables[923]*variables[324]*variables[116]*(variables[226]+variables[192])/variables[134];
    variables[890] = (1.0-variables[356]+variables[356]*variables[655])*(1.0-variables[371]+variables[371]*variables[654])*(1.0-variables[401]+variables[401]*variables[653])*(1.0-variables[386]+variables[386]*variables[652]);
    variables[891] = variables[550]*variables[890];
    variables[892] = (variables[891]-states[24])/variables[437];
    variables[893] = (variables[892]+fabs(variables[892]))/2.0;
    variables[898] = variables[893]*variables[326]*variables[118]*(variables[228]+variables[194])/variables[136];
    variables[875] = (1.0-variables[351]+variables[351]*variables[655])*(1.0-variables[366]+variables[366]*variables[654])*(1.0-variables[396]+variables[396]*variables[653])*(1.0-variables[381]+variables[381]*variables[652]);
    variables[876] = variables[547]*variables[875];
    variables[877] = (variables[876]-states[23])/variables[432];
    variables[878] = (variables[877]+fabs(variables[877]))/2.0;
    variables[883] = variables[878]*variables[321]*variables[113]*(variables[223]+variables[189])/variables[131];
    variables[860] = (1.0-variables[357]+variables[357]*variables[655])*(1.0-variables[372]+variables[372]*variables[654])*(1.0-variables[402]+variables[402]*variables[653])*(1.0-variables[387]+variables[387]*variables[652]);
    variables[861] = variables[544]*variables[860];
    variables[862] = (variables[861]-states[22])/variables[438];
    variables[863] = (variables[862]+fabs(variables[862]))/2.0;
    variables[868] = variables[863]*variables[327]*variables[119]*(variables[229]+variables[195])/variables[137];
    variables[845] = (1.0-variables[360]+variables[360]*variables[655])*(1.0-variables[375]+variables[375]*variables[654])*(1.0-variables[405]+variables[405]*variables[653])*(1.0-variables[390]+variables[390]*variables[652]);
    variables[846] = variables[541]*variables[845];
    variables[847] = (variables[846]-states[21])/variables[441];
    variables[848] = (variables[847]+fabs(variables[847]))/2.0;
    variables[853] = variables[848]*variables[330]*variables[122]*(variables[232]+variables[198])/variables[140];
    variables[830] = (1.0-variables[355]+variables[355]*variables[655])*(1.0-variables[370]+variables[370]*variables[654])*(1.0-variables[400]+variables[400]*variables[653])*(1.0-variables[385]+variables[385]*variables[652]);
    variables[831] = variables[538]*variables[830];
    variables[832] = (variables[831]-states[20])/variables[436];
    variables[833] = (variables[832]+fabs(variables[832]))/2.0;
    variables[838] = variables[833]*variables[325]*variables[117]*(variables[227]+variables[193])/variables[135];
    variables[815] = (1.0-variables[358]+variables[358]*variables[655])*(1.0-variables[373]+variables[373]*variables[654])*(1.0-variables[403]+variables[403]*variables[653])*(1.0-variables[388]+variables[388]*variables[652]);
    variables[816] = variables[535]*variables[815];
    variables[817] = (variables[816]-states[19])/variables[439];
    variables[818] = (variables[817]+fabs(variables[817]))/2.0;
    variables[823] = variables[818]*variables[328]*variables[120]*(variables[230]+variables[196])/variables[138];
    variables[800] = (1.0-variables[353]+variables[353]*variables[655])*(1.0-variables[368]+variables[368]*variables[654])*(1.0-variables[398]+variables[398]*variables[653])*(1.0-variables[383]+variables[383]*variables[652]);
    variables[801] = variables[532]*variables[800];
    variables[802] = (variables[801]-states[18])/variables[434];
    variables[803] = (variables[802]+fabs(variables[802]))/2.0;
    variables[808] = variables[803]*variables[323]*variables[115]*(variables[225]+variables[191])/variables[133];
    variables[702] = (1.0-variables[352]+variables[352]*variables[655])*(1.0-variables[367]+variables[367]*variables[654])*(1.0-variables[397]+variables[397]*variables[653])*(1.0-variables[382]+variables[382]*variables[652]);
    variables[703] = variables[314]*variables[702];
    variables[704] = (variables[703]-states[13])/variables[433];
    variables[705] = (variables[704]+fabs(variables[704]))/2.0;
    variables[710] = variables[705]*variables[322]*variables[114]*(variables[224]+variables[190])/variables[132];
    variables[687] = (1.0-variables[361]+variables[361]*variables[655])*(1.0-variables[376]+variables[376]*variables[654])*(1.0-variables[406]+variables[406]*variables[653])*(1.0-variables[391]+variables[391]*variables[652]);
    variables[688] = variables[315]*variables[687];
    variables[689] = (variables[688]-states[17])/variables[442];
    variables[690] = (variables[689]+fabs(variables[689]))/2.0;
    variables[695] = variables[690]*variables[331]*variables[123]*(variables[233]+variables[199])/variables[141];
    variables[666] = variables[695]+variables[710]+variables[808]+variables[823]+variables[838]+variables[853]+variables[868]+variables[883]+variables[898]+variables[913]+variables[928]+variables[943]+variables[958]+variables[973];
    variables[667] = variables[666];
    variables[681] = (variables[680]+variables[52])/((variables[680]+variables[667]+fabs(variables[680]-variables[667]))/2.0+variables[52]);
    variables[974] = variables[973]*variables[681]*variables[971];
    variables[975] = variables[972]+variables[974];
    variables[955] = states[7]/(variables[89]+states[7])*(variables[89]+variables[8])/variables[8];
    variables[949] = variables[318]*states[29];
    variables[957] = variables[949]*variables[110]*(variables[186]+variables[168]+variables[200])*variables[281]/variables[128]*variables[955];
    variables[956] = (variables[955]+1.0-fabs(variables[955]-1.0))/2.0;
    variables[959] = variables[958]*variables[681]*variables[956];
    variables[960] = variables[957]+variables[959];
    variables[940] = states[7]/(variables[100]+states[7])*(variables[100]+variables[19])/variables[19];
    variables[934] = variables[329]*states[28];
    variables[942] = variables[934]*variables[121]*(variables[197]+variables[179]+variables[211])*variables[292]/variables[139]*variables[940];
    variables[941] = (variables[940]+1.0-fabs(variables[940]-1.0))/2.0;
    variables[944] = variables[943]*variables[681]*variables[941];
    variables[945] = variables[942]+variables[944];
    variables[925] = states[7]/(variables[95]+states[7])*(variables[95]+variables[14])/variables[14];
    variables[919] = variables[324]*states[27];
    variables[927] = variables[919]*variables[116]*(variables[192]+variables[174]+variables[206])*variables[287]/variables[134]*variables[925];
    variables[926] = (variables[925]+1.0-fabs(variables[925]-1.0))/2.0;
    variables[929] = variables[928]*variables[681]*variables[926];
    variables[930] = variables[927]+variables[929];
    variables[910] = states[7]/(variables[90]+states[7])*(variables[90]+variables[9])/variables[9];
    variables[904] = variables[319]*states[26];
    variables[912] = variables[904]*variables[111]*(variables[187]+variables[169]+variables[201])*variables[282]/variables[129]*variables[910];
    variables[911] = (variables[910]+1.0-fabs(variables[910]-1.0))/2.0;
    variables[914] = variables[913]*variables[681]*variables[911];
    variables[915] = variables[912]+variables[914];
    variables[895] = states[7]/(variables[97]+states[7])*(variables[97]+variables[16])/variables[16];
    variables[889] = variables[326]*states[24];
    variables[897] = variables[889]*variables[118]*(variables[194]+variables[176]+variables[208])*variables[289]/variables[136]*variables[895];
    variables[896] = (variables[895]+1.0-fabs(variables[895]-1.0))/2.0;
    variables[899] = variables[898]*variables[681]*variables[896];
    variables[900] = variables[897]+variables[899];
    variables[880] = states[7]/(variables[92]+states[7])*(variables[92]+variables[11])/variables[11];
    variables[874] = variables[321]*states[23];
    variables[882] = variables[874]*variables[113]*(variables[189]+variables[171]+variables[203])*variables[284]/variables[131]*variables[880];
    variables[881] = (variables[880]+1.0-fabs(variables[880]-1.0))/2.0;
    variables[884] = variables[883]*variables[681]*variables[881];
    variables[885] = variables[882]+variables[884];
    variables[865] = states[7]/(variables[98]+states[7])*(variables[98]+variables[17])/variables[17];
    variables[859] = variables[327]*states[22];
    variables[867] = variables[859]*variables[119]*(variables[195]+variables[177]+variables[209])*variables[290]/variables[137]*variables[865];
    variables[866] = (variables[865]+1.0-fabs(variables[865]-1.0))/2.0;
    variables[869] = variables[868]*variables[681]*variables[866];
    variables[870] = variables[867]+variables[869];
    variables[850] = states[7]/(variables[101]+states[7])*(variables[101]+variables[20])/variables[20];
    variables[844] = variables[330]*states[21];
    variables[852] = variables[844]*variables[122]*(variables[198]+variables[180]+variables[212])*variables[293]/variables[140]*variables[850];
    variables[851] = (variables[850]+1.0-fabs(variables[850]-1.0))/2.0;
    variables[854] = variables[853]*variables[681]*variables[851];
    variables[855] = variables[852]+variables[854];
    variables[835] = states[7]/(variables[96]+states[7])*(variables[96]+variables[15])/variables[15];
    variables[829] = variables[325]*states[20];
    variables[837] = variables[829]*variables[117]*(variables[193]+variables[175]+variables[207])*variables[288]/variables[135]*variables[835];
    variables[836] = (variables[835]+1.0-fabs(variables[835]-1.0))/2.0;
    variables[839] = variables[838]*variables[681]*variables[836];
    variables[840] = variables[837]+variables[839];
    variables[820] = states[7]/(variables[99]+states[7])*(variables[99]+variables[18])/variables[18];
    variables[814] = variables[328]*states[19];
    variables[822] = variables[814]*variables[120]*(variables[196]+variables[178]+variables[210])*variables[291]/variables[138]*variables[820];
    variables[821] = (variables[820]+1.0-fabs(variables[820]-1.0))/2.0;
    variables[824] = variables[823]*variables[681]*variables[821];
    variables[825] = variables[822]+variables[824];
    variables[805] = states[7]/(variables[94]+states[7])*(variables[94]+variables[13])/variables[13];
    variables[799] = variables[323]*states[18];
    variables[807] = variables[799]*variables[115]*(variables[191]+variables[173]+variables[205])*variables[286]/variables[133]*variables[805];
    variables[806] = (variables[805]+1.0-fabs(variables[805]-1.0))/2.0;
    variables[809] = variables[808]*variables[681]*variables[806];
    variables[810] = variables[807]+variables[809];
    variables[707] = states[7]/(variables[93]+states[7])*(variables[93]+variables[12])/variables[12];
    variables[701] = variables[322]*states[13];
    variables[709] = variables[701]*variables[114]*(variables[190]+variables[172]+variables[204])*variables[285]/variables[132]*variables[707];
    variables[708] = (variables[707]+1.0-fabs(variables[707]-1.0))/2.0;
    variables[711] = variables[710]*variables[681]*variables[708];
    variables[712] = variables[709]+variables[711];
    variables[692] = states[7]/(variables[102]+states[7])*(variables[102]+variables[21])/variables[21];
    variables[686] = variables[331]*states[17];
    variables[694] = variables[686]*variables[123]*(variables[199]+variables[181]+variables[213])*variables[294]/variables[141]*variables[692];
    variables[693] = (variables[692]+1.0-fabs(variables[692]-1.0))/2.0;
    variables[696] = variables[695]*variables[681]*variables[693];
    variables[697] = variables[694]+variables[696];
    variables[668] = variables[697]+variables[712]+variables[810]+variables[825]+variables[840]+variables[855]+variables[870]+variables[885]+variables[900]+variables[915]+variables[930]+variables[945]+variables[960]+variables[975];
    variables[669] = variables[668];
    variables[683] = -variables[669];
    variables[624] = variables[683]+variables[663]+variables[735]+variables[780]+variables[787];
    variables[626] = variables[483]*(variables[609]+variables[607]-variables[608]+variables[616]+variables[332]*variables[621])-variables[624];
    variables[599] = variables[626];
    rates[6] = (variables[600]+variables[599])/variables[483]/(1.0+variables[166]/(log(10.0)*states[6]));
    variables[798] = variables[793];
    variables[791] = variables[784];
    variables[754] = variables[278]*states[23]*variables[482]*(variables[32]/(variables[80]+variables[32])-states[8]/(variables[80]+states[8]));
    variables[760] = variables[754];
    variables[630] = variables[760]+variables[791]+variables[798];
    rates[8] = (variables[630]+variables[493])/variables[483]-variables[607];
    variables[617] = variables[157]*variables[486]*states[14]/(variables[81]+states[14])*(states[11]-states[10]/variables[491])/(variables[73]+states[11])*variables[82]/(variables[82]+states[7]);
    variables[778] = variables[775];
    variables[627] = variables[778];
    rates[10] = variables[627]/variables[483]+variables[607]+variables[617]-variables[608]-variables[616]-variables[621];
    variables[761] = variables[756];
    variables[631] = variables[761]+variables[522]+variables[527];
    rates[12] = (variables[631]+variables[494])/variables[483]+2.0*variables[608];
    rates[7] = (variables[496]+variables[495]+variables[624])/variables[483]-variables[607]+3.0*variables[608]-variables[609]-variables[616]-variables[332]*variables[621];
    variables[788] = variables[786];
    variables[781] = variables[775];
    variables[736] = variables[732];
    variables[664] = variables[662];
    variables[684] = variables[669];
    variables[632] = variables[684]+variables[664]+variables[736]+variables[781]+variables[788];
    rates[9] = (variables[498]+variables[497]+variables[632])/variables[483]+variables[607]-3.0*variables[608]+variables[609]+variables[616]+variables[332]*variables[621];
    variables[789] = variables[786];
    variables[737] = variables[732];
    variables[665] = variables[662];
    variables[685] = variables[669];
    variables[628] = variables[685]+variables[665]+variables[737]+variables[520]+variables[789];
    rates[11] = (variables[500]+variables[499]+variables[628])/variables[483]-2.0*variables[608]+variables[609]+2.0*variables[616]-variables[617]+(variables[332]+1.0)*variables[621];
    rates[14] = variables[616]-variables[617];
    rates[15] = variables[345]*variables[502]-variables[646];
    rates[16] = variables[658];
    variables[691] = (variables[689]-fabs(variables[689]))/2.0;
    rates[17] = variables[690]*variables[681]*variables[693]+variables[691];
    variables[706] = (variables[704]-fabs(variables[704]))/2.0;
    rates[13] = variables[705]*variables[681]*variables[708]+variables[706];
    rates[25] = (variables[162]-states[25])/variables[428];
    variables[804] = (variables[802]-fabs(variables[802]))/2.0;
    rates[18] = variables[803]*variables[681]*variables[806]+variables[804];
    variables[819] = (variables[817]-fabs(variables[817]))/2.0;
    rates[19] = variables[818]*variables[681]*variables[821]+variables[819];
    variables[834] = (variables[832]-fabs(variables[832]))/2.0;
    rates[20] = variables[833]*variables[681]*variables[836]+variables[834];
    variables[849] = (variables[847]-fabs(variables[847]))/2.0;
    rates[21] = variables[848]*variables[681]*variables[851]+variables[849];
    variables[864] = (variables[862]-fabs(variables[862]))/2.0;
    rates[22] = variables[863]*variables[681]*variables[866]+variables[864];
    variables[879] = (variables[877]-fabs(variables[877]))/2.0;
    rates[23] = variables[878]*variables[681]*variables[881]+variables[879];
    variables[894] = (variables[892]-fabs(variables[892]))/2.0;
    rates[24] = variables[893]*variables[681]*variables[896]+variables[894];
    variables[909] = (variables[907]-fabs(variables[907]))/2.0;
    rates[26] = variables[908]*variables[681]*variables[911]+variables[909];
    variables[924] = (variables[922]-fabs(variables[922]))/2.0;
    rates[27] = variables[923]*variables[681]*variables[926]+variables[924];
    variables[939] = (variables[937]-fabs(variables[937]))/2.0;
    rates[28] = variables[938]*variables[681]*variables[941]+variables[939];
    variables[954] = (variables[952]-fabs(variables[952]))/2.0;
    rates[29] = variables[953]*variables[681]*variables[956]+variables[954];
    variables[969] = (variables[967]-fabs(variables[967]))/2.0;
    rates[30] = variables[968]*variables[681]*variables[971]+variables[969];
    variables[981] = variables[453]*states[31];
    rates[33] = (variables[567]+variables[566]+variables[457])/variables[449]+variables[454]*variables[981];
    rates[34] = (variables[569]+variables[568]+variables[458])/variables[449];
    rates[32] = (variables[571]+variables[570]+variables[459])/(variables[449]*(1.0+variables[450]));
    rates[35] = (variables[573]+variables[572]+variables[460])/variables[449];
    variables[982] = variables[456]*states[31]*pow(states[31]/variables[455], 8.0);
    rates[31] = variables[461]/variables[449]-variables[981]-variables[982];
    variables[983] = variables[449]*((1.0-variables[454])*variables[981]+variables[982]);
    rates[36] = variables[983];
}

void computeVariables(double voi, double *states, double *rates, double *variables)
{
    variables[590] = variables[1]*states[0]/(states[0]+variables[2]);
    variables[592] = variables[148]*variables[151]/variables[46]*log(variables[107]/states[2])*1000.0;
    variables[593] = variables[148]*variables[151]/variables[46]*log(variables[143]/states[3])*1000.0;
    variables[596] = variables[24]-states[4];
    variables[598] = states[5]+variables[596];
    variables[601] = -log(states[6]/variables[214])/log(10.0);
    variables[602] = variables[148]*variables[151]/variables[46]*log(variables[49]/states[6])*1000.0;
    variables[603] = states[6]-variables[166]*variables[601];
    variables[604] = states[10]/variables[215]*states[9]/variables[215]/(states[8]/variables[215]*states[7]/variables[215]);
    variables[605] = pow(states[12]/variables[215], 2.0)*pow(states[7]/variables[215], 3.0)/(states[10]/variables[215]*pow(states[9]/variables[215], 3.0)*pow(states[11]/variables[215], 2.0));
    variables[606] = states[9]/variables[215]*states[11]/variables[215]/(states[7]/variables[215]);
    variables[607] = variables[154]*states[13]*variables[486]*pow(states[9]/variables[56], 2.0)*pow(states[11]/variables[71], 2.0)*(states[8]*states[7]-states[10]*states[9]/variables[487])/(variables[75]*(variables[59]+states[7]))/((1.0+states[8]/variables[75])*pow(1.0+states[9]/variables[56], 2.0)*pow(1.0+states[11]/variables[71], 2.0)+pow(1.0+states[12]/variables[83], 2.0)*pow(1.0+states[7]/variables[58], 2.0)-1.0);
    variables[608] = variables[156]*states[13]*variables[486]*pow(fabs(states[10]), variables[333]-1.0)*(states[10]-pow(states[12], 2.0)*pow(states[7], 3.0)*variables[215]/(pow(states[9], 3.0)*pow(states[11], 2.0)*variables[488]))/(pow(variables[65], variables[333])+pow(fabs(states[10]), variables[333]))*states[9]/(variables[56]+states[9])*states[11]/(variables[71]+states[11]);
    variables[609] = variables[155]*variables[486]*states[7]/variables[60]*(1.0-variables[606]/variables[489])/(1.0+states[7]/variables[60]+(1.0+states[9]/variables[57])*(1.0+states[11]/variables[72])-1.0);
    variables[610] = variables[148]*variables[151]*(log(variables[487])-log(fabs(variables[604])+1.0e-300));
    variables[611] = variables[148]*variables[151]*(log(variables[488])-log(fabs(variables[605])+1.0e-300));
    variables[612] = variables[148]*variables[151]*(log(variables[489])-log(fabs(variables[606])+1.0e-300));
    variables[613] = states[14]/(variables[81]+states[14]);
    variables[614] = variables[613]*states[9]/variables[215]*pow(states[11]/variables[215], 2.0)/(states[10]/variables[215]*states[7]/variables[215]);
    variables[615] = states[10]/variables[215]/(states[11]/variables[215]);
    variables[616] = variables[160]*variables[424]*variables[486]*(states[10]*states[7]-variables[613]*states[9]*pow(states[11], 2.0)/(variables[215]*variables[490]))/((variables[68]+states[10])*(variables[63]+states[7]))/(1.0+pow(states[14]/variables[35], 8.0));
    variables[617] = variables[157]*variables[486]*states[14]/(variables[81]+states[14])*(states[11]-states[10]/variables[491])/(variables[73]+states[11])*variables[82]/(variables[82]+states[7]);
    variables[618] = variables[148]*variables[151]*(log(variables[490])-log(fabs(variables[614])+1.0e-300));
    variables[619] = variables[148]*variables[151]*(log(variables[491])-log(fabs(variables[615])+1.0e-300));
    variables[620] = pow(states[9]/variables[215], variables[332])*pow(states[11]/variables[215], variables[332]+1.0)/(states[10]/variables[215]*pow(states[7]/variables[215], variables[332]));
    variables[621] = variables[158]*variables[316]*variables[486]*states[10]/(variables[66]+states[10])*states[7]/(variables[61]+states[7])*(1.0-variables[620]/variables[492]+fabs(1.0-variables[620]/variables[492]))/2.0;
    variables[622] = variables[148]*variables[151]*(log(variables[492])-log(fabs(variables[620])+1.0e-300));
    variables[623] = variables[483]*(variables[607]*variables[610]+variables[608]*variables[611]+variables[609]*variables[612]+variables[616]*variables[618]+variables[617]*variables[619]+variables[621]*variables[622]);
    variables[782] = states[9]*states[11]/variables[216]*exp(variables[238]/(variables[148]*variables[151]));
    variables[784] = variables[275]*states[29]*variables[482]*(variables[29]*pow(states[7]/variables[87], variables[337])-states[8]*pow(variables[782]/variables[87], variables[337]))/((variables[77]+variables[29])*pow(1.0+states[7]/variables[87], variables[337]));
    variables[786] = variables[337]*variables[784];
    variables[787] = -variables[786];
    variables[774] = states[9]/(variables[55]+states[9])*states[11]/(variables[70]+states[11]);
    variables[775] = variables[277]*states[28]*variables[482]*variables[774]*(variables[31]*states[7]-states[10]*states[9]/variables[518])/((variables[88]+states[7])*(variables[79]+variables[31]));
    variables[780] = -variables[775];
    variables[729] = variables[335]*variables[46]*(variables[602]-states[1])*0.001;
    variables[728] = -variables[239]-variables[148]*variables[151]*log(fabs(states[9]/variables[219]*states[11]/variables[219]/(states[7]/variables[219]))+1.0e-300);
    variables[730] = 1.0-exp(-(variables[728]-variables[729])/(variables[148]*variables[151]));
    variables[727] = variables[482]*variables[408]*states[18]*variables[343]*variables[335]*1.602176634e-10;
    variables[731] = variables[727]*states[7]/(variables[85]+states[7])*states[6]/(variables[74]+states[6])*variables[730];
    variables[732] = variables[731]/(variables[335]*variables[46])*1.0e-9;
    variables[735] = -variables[732];
    variables[642] = states[7]/(variables[86]+states[7])*(variables[86]+variables[7])/variables[7];
    variables[646] = variables[407]*variables[346]*states[15]/(variables[64]+states[15])*variables[642];
    variables[647] = variables[646]*variables[41];
    variables[645] = variables[501]*variables[182]*variables[295]/variables[125]*variables[642];
    variables[644] = variables[341]*variables[124]*variables[185]*variables[296]/variables[125]*variables[642];
    variables[643] = variables[339]*variables[109]*variables[185]*log(2.0)/variables[427]/variables[125]*variables[642];
    variables[648] = (states[7]-variables[5])/(variables[7]-variables[5]);
    variables[649] = ((variables[648]+fabs(variables[648]))/2.0+1.0-fabs((variables[648]+fabs(variables[648]))/2.0-1.0))/2.0;
    variables[719] = variables[69]/states[6];
    variables[652] = pow(variables[719], variables[340])/(1.0+pow(variables[719], variables[340]));
    variables[718] = (states[8]+fabs(states[8]))/(2.0*variables[76]);
    variables[653] = pow(variables[718], variables[340])/(1.0+pow(variables[718], variables[340]));
    variables[717] = (states[10]+fabs(states[10]))/(2.0*variables[67]);
    variables[654] = pow(variables[717], variables[340])/(1.0+pow(variables[717], variables[340]));
    variables[716] = (states[7]+fabs(states[7]))/(2.0*variables[62]);
    variables[655] = pow(variables[716], variables[340])/(1.0+pow(variables[716], variables[340]));
    variables[656] = (1.0-variables[347]+variables[347]*variables[655])*(1.0-variables[362]+variables[362]*variables[654])*(1.0-variables[392]+variables[392]*variables[653])*(1.0-variables[377]+variables[377]*variables[652]);
    variables[657] = variables[268]*variables[53]*variables[649]*variables[656];
    variables[658] = variables[657]*variables[642];
    variables[662] = variables[643]+variables[644]+variables[645]+variables[647]+variables[658];
    variables[663] = -variables[662];
    variables[970] = states[7]/(variables[91]+states[7])*(variables[91]+variables[10])/variables[10];
    variables[964] = variables[320]*states[30];
    variables[972] = variables[964]*variables[112]*(variables[188]+variables[170]+variables[202])*variables[283]/variables[130]*variables[970];
    variables[971] = (variables[970]+1.0-fabs(variables[970]-1.0))/2.0;
    variables[965] = (1.0-variables[350]+variables[350]*variables[655])*(1.0-variables[365]+variables[365]*variables[654])*(1.0-variables[395]+variables[395]*variables[653])*(1.0-variables[380]+variables[380]*variables[652]);
    variables[966] = variables[564]*variables[965];
    variables[967] = (variables[966]-states[30])/variables[431];
    variables[968] = (variables[967]+fabs(variables[967]))/2.0;
    variables[973] = variables[968]*variables[320]*variables[112]*(variables[222]+variables[188])/variables[130];
    variables[678] = (states[7]-variables[6])/(variables[22]-variables[6]);
    variables[679] = ((variables[678]+fabs(variables[678]))/2.0+1.0-fabs((variables[678]+fabs(variables[678]))/2.0-1.0))/2.0;
    variables[680] = variables[167]*variables[54]*variables[679];
    variables[905] = (1.0-variables[349]+variables[349]*variables[655])*(1.0-variables[364]+variables[364]*variables[654])*(1.0-variables[394]+variables[394]*variables[653])*(1.0-variables[379]+variables[379]*variables[652]);
    variables[906] = variables[313]*variables[905];
    variables[907] = (variables[906]-states[26])/variables[430];
    variables[908] = (variables[907]+fabs(variables[907]))/2.0;
    variables[913] = variables[908]*variables[319]*variables[111]*(variables[221]+variables[187])/variables[129];
    variables[950] = (1.0-variables[348]+variables[348]*variables[655])*(1.0-variables[363]+variables[363]*variables[654])*(1.0-variables[393]+variables[393]*variables[653])*(1.0-variables[378]+variables[378]*variables[652]);
    variables[951] = variables[561]*variables[950];
    variables[952] = (variables[951]-states[29])/variables[429];
    variables[953] = (variables[952]+fabs(variables[952]))/2.0;
    variables[958] = variables[953]*variables[318]*variables[110]*(variables[220]+variables[186])/variables[128];
    variables[935] = (1.0-variables[359]+variables[359]*variables[655])*(1.0-variables[374]+variables[374]*variables[654])*(1.0-variables[404]+variables[404]*variables[653])*(1.0-variables[389]+variables[389]*variables[652]);
    variables[936] = variables[558]*variables[935];
    variables[937] = (variables[936]-states[28])/variables[440];
    variables[938] = (variables[937]+fabs(variables[937]))/2.0;
    variables[943] = variables[938]*variables[329]*variables[121]*(variables[231]+variables[197])/variables[139];
    variables[920] = (1.0-variables[354]+variables[354]*variables[655])*(1.0-variables[369]+variables[369]*variables[654])*(1.0-variables[399]+variables[399]*variables[653])*(1.0-variables[384]+variables[384]*variables[652]);
    variables[921] = variables[555]*variables[920];
    variables[922] = (variables[921]-states[27])/variables[435];
    variables[923] = (variables[922]+fabs(variables[922]))/2.0;
    variables[928] = variables[923]*variables[324]*variables[116]*(variables[226]+variables[192])/variables[134];
    variables[890] = (1.0-variables[356]+variables[356]*variables[655])*(1.0-variables[371]+variables[371]*variables[654])*(1.0-variables[401]+variables[401]*variables[653])*(1.0-variables[386]+variables[386]*variables[652]);
    variables[891] = variables[550]*variables[890];
    variables[892] = (variables[891]-states[24])/variables[437];
    variables[893] = (variables[892]+fabs(variables[892]))/2.0;
    variables[898] = variables[893]*variables[326]*variables[118]*(variables[228]+variables[194])/variables[136];
    variables[875] = (1.0-variables[351]+variables[351]*variables[655])*(1.0-variables[366]+variables[366]*variables[654])*(1.0-variables[396]+variables[396]*variables[653])*(1.0-variables[381]+variables[381]*variables[652]);
    variables[876] = variables[547]*variables[875];
    variables[877] = (variables[876]-states[23])/variables[432];
    variables[878] = (variables[877]+fabs(variables[877]))/2.0;
    variables[883] = variables[878]*variables[321]*variables[113]*(variables[223]+variables[189])/variables[131];
    variables[860] = (1.0-variables[357]+variables[357]*variables[655])*(1.0-variables[372]+variables[372]*variables[654])*(1.0-variables[402]+variables[402]*variables[653])*(1.0-variables[387]+variables[387]*variables[652]);
    variables[861] = variables[544]*variables[860];
    variables[862] = (variables[861]-states[22])/variables[438];
    variables[863] = (variables[862]+fabs(variables[862]))/2.0;
    variables[868] = variables[863]*variables[327]*variables[119]*(variables[229]+variables[195])/variables[137];
    variables[845] = (1.0-variables[360]+variables[360]*variables[655])*(1.0-variables[375]+variables[375]*variables[654])*(1.0-variables[405]+variables[405]*variables[653])*(1.0-variables[390]+variables[390]*variables[652]);
    variables[846] = variables[541]*variables[845];
    variables[847] = (variables[846]-states[21])/variables[441];
    variables[848] = (variables[847]+fabs(variables[847]))/2.0;
    variables[853] = variables[848]*variables[330]*variables[122]*(variables[232]+variables[198])/variables[140];
    variables[830] = (1.0-variables[355]+variables[355]*variables[655])*(1.0-variables[370]+variables[370]*variables[654])*(1.0-variables[400]+variables[400]*variables[653])*(1.0-variables[385]+variables[385]*variables[652]);
    variables[831] = variables[538]*variables[830];
    variables[832] = (variables[831]-states[20])/variables[436];
    variables[833] = (variables[832]+fabs(variables[832]))/2.0;
    variables[838] = variables[833]*variables[325]*variables[117]*(variables[227]+variables[193])/variables[135];
    variables[815] = (1.0-variables[358]+variables[358]*variables[655])*(1.0-variables[373]+variables[373]*variables[654])*(1.0-variables[403]+variables[403]*variables[653])*(1.0-variables[388]+variables[388]*variables[652]);
    variables[816] = variables[535]*variables[815];
    variables[817] = (variables[816]-states[19])/variables[439];
    variables[818] = (variables[817]+fabs(variables[817]))/2.0;
    variables[823] = variables[818]*variables[328]*variables[120]*(variables[230]+variables[196])/variables[138];
    variables[800] = (1.0-variables[353]+variables[353]*variables[655])*(1.0-variables[368]+variables[368]*variables[654])*(1.0-variables[398]+variables[398]*variables[653])*(1.0-variables[383]+variables[383]*variables[652]);
    variables[801] = variables[532]*variables[800];
    variables[802] = (variables[801]-states[18])/variables[434];
    variables[803] = (variables[802]+fabs(variables[802]))/2.0;
    variables[808] = variables[803]*variables[323]*variables[115]*(variables[225]+variables[191])/variables[133];
    variables[702] = (1.0-variables[352]+variables[352]*variables[655])*(1.0-variables[367]+variables[367]*variables[654])*(1.0-variables[397]+variables[397]*variables[653])*(1.0-variables[382]+variables[382]*variables[652]);
    variables[703] = variables[314]*variables[702];
    variables[704] = (variables[703]-states[13])/variables[433];
    variables[705] = (variables[704]+fabs(variables[704]))/2.0;
    variables[710] = variables[705]*variables[322]*variables[114]*(variables[224]+variables[190])/variables[132];
    variables[687] = (1.0-variables[361]+variables[361]*variables[655])*(1.0-variables[376]+variables[376]*variables[654])*(1.0-variables[406]+variables[406]*variables[653])*(1.0-variables[391]+variables[391]*variables[652]);
    variables[688] = variables[315]*variables[687];
    variables[689] = (variables[688]-states[17])/variables[442];
    variables[690] = (variables[689]+fabs(variables[689]))/2.0;
    variables[695] = variables[690]*variables[331]*variables[123]*(variables[233]+variables[199])/variables[141];
    variables[666] = variables[695]+variables[710]+variables[808]+variables[823]+variables[838]+variables[853]+variables[868]+variables[883]+variables[898]+variables[913]+variables[928]+variables[943]+variables[958]+variables[973];
    variables[667] = variables[666];
    variables[681] = (variables[680]+variables[52])/((variables[680]+variables[667]+fabs(variables[680]-variables[667]))/2.0+variables[52]);
    variables[974] = variables[973]*variables[681]*variables[971];
    variables[975] = variables[972]+variables[974];
    variables[955] = states[7]/(variables[89]+states[7])*(variables[89]+variables[8])/variables[8];
    variables[949] = variables[318]*states[29];
    variables[957] = variables[949]*variables[110]*(variables[186]+variables[168]+variables[200])*variables[281]/variables[128]*variables[955];
    variables[956] = (variables[955]+1.0-fabs(variables[955]-1.0))/2.0;
    variables[959] = variables[958]*variables[681]*variables[956];
    variables[960] = variables[957]+variables[959];
    variables[940] = states[7]/(variables[100]+states[7])*(variables[100]+variables[19])/variables[19];
    variables[934] = variables[329]*states[28];
    variables[942] = variables[934]*variables[121]*(variables[197]+variables[179]+variables[211])*variables[292]/variables[139]*variables[940];
    variables[941] = (variables[940]+1.0-fabs(variables[940]-1.0))/2.0;
    variables[944] = variables[943]*variables[681]*variables[941];
    variables[945] = variables[942]+variables[944];
    variables[925] = states[7]/(variables[95]+states[7])*(variables[95]+variables[14])/variables[14];
    variables[919] = variables[324]*states[27];
    variables[927] = variables[919]*variables[116]*(variables[192]+variables[174]+variables[206])*variables[287]/variables[134]*variables[925];
    variables[926] = (variables[925]+1.0-fabs(variables[925]-1.0))/2.0;
    variables[929] = variables[928]*variables[681]*variables[926];
    variables[930] = variables[927]+variables[929];
    variables[910] = states[7]/(variables[90]+states[7])*(variables[90]+variables[9])/variables[9];
    variables[904] = variables[319]*states[26];
    variables[912] = variables[904]*variables[111]*(variables[187]+variables[169]+variables[201])*variables[282]/variables[129]*variables[910];
    variables[911] = (variables[910]+1.0-fabs(variables[910]-1.0))/2.0;
    variables[914] = variables[913]*variables[681]*variables[911];
    variables[915] = variables[912]+variables[914];
    variables[895] = states[7]/(variables[97]+states[7])*(variables[97]+variables[16])/variables[16];
    variables[889] = variables[326]*states[24];
    variables[897] = variables[889]*variables[118]*(variables[194]+variables[176]+variables[208])*variables[289]/variables[136]*variables[895];
    variables[896] = (variables[895]+1.0-fabs(variables[895]-1.0))/2.0;
    variables[899] = variables[898]*variables[681]*variables[896];
    variables[900] = variables[897]+variables[899];
    variables[880] = states[7]/(variables[92]+states[7])*(variables[92]+variables[11])/variables[11];
    variables[874] = variables[321]*states[23];
    variables[882] = variables[874]*variables[113]*(variables[189]+variables[171]+variables[203])*variables[284]/variables[131]*variables[880];
    variables[881] = (variables[880]+1.0-fabs(variables[880]-1.0))/2.0;
    variables[884] = variables[883]*variables[681]*variables[881];
    variables[885] = variables[882]+variables[884];
    variables[865] = states[7]/(variables[98]+states[7])*(variables[98]+variables[17])/variables[17];
    variables[859] = variables[327]*states[22];
    variables[867] = variables[859]*variables[119]*(variables[195]+variables[177]+variables[209])*variables[290]/variables[137]*variables[865];
    variables[866] = (variables[865]+1.0-fabs(variables[865]-1.0))/2.0;
    variables[869] = variables[868]*variables[681]*variables[866];
    variables[870] = variables[867]+variables[869];
    variables[850] = states[7]/(variables[101]+states[7])*(variables[101]+variables[20])/variables[20];
    variables[844] = variables[330]*states[21];
    variables[852] = variables[844]*variables[122]*(variables[198]+variables[180]+variables[212])*variables[293]/variables[140]*variables[850];
    variables[851] = (variables[850]+1.0-fabs(variables[850]-1.0))/2.0;
    variables[854] = variables[853]*variables[681]*variables[851];
    variables[855] = variables[852]+variables[854];
    variables[835] = states[7]/(variables[96]+states[7])*(variables[96]+variables[15])/variables[15];
    variables[829] = variables[325]*states[20];
    variables[837] = variables[829]*variables[117]*(variables[193]+variables[175]+variables[207])*variables[288]/variables[135]*variables[835];
    variables[836] = (variables[835]+1.0-fabs(variables[835]-1.0))/2.0;
    variables[839] = variables[838]*variables[681]*variables[836];
    variables[840] = variables[837]+variables[839];
    variables[820] = states[7]/(variables[99]+states[7])*(variables[99]+variables[18])/variables[18];
    variables[814] = variables[328]*states[19];
    variables[822] = variables[814]*variables[120]*(variables[196]+variables[178]+variables[210])*variables[291]/variables[138]*variables[820];
    variables[821] = (variables[820]+1.0-fabs(variables[820]-1.0))/2.0;
    variables[824] = variables[823]*variables[681]*variables[821];
    variables[825] = variables[822]+variables[824];
    variables[805] = states[7]/(variables[94]+states[7])*(variables[94]+variables[13])/variables[13];
    variables[799] = variables[323]*states[18];
    variables[807] = variables[799]*variables[115]*(variables[191]+variables[173]+variables[205])*variables[286]/variables[133]*variables[805];
    variables[806] = (variables[805]+1.0-fabs(variables[805]-1.0))/2.0;
    variables[809] = variables[808]*variables[681]*variables[806];
    variables[810] = variables[807]+variables[809];
    variables[707] = states[7]/(variables[93]+states[7])*(variables[93]+variables[12])/variables[12];
    variables[701] = variables[322]*states[13];
    variables[709] = variables[701]*variables[114]*(variables[190]+variables[172]+variables[204])*variables[285]/variables[132]*variables[707];
    variables[708] = (variables[707]+1.0-fabs(variables[707]-1.0))/2.0;
    variables[711] = variables[710]*variables[681]*variables[708];
    variables[712] = variables[709]+variables[711];
    variables[692] = states[7]/(variables[102]+states[7])*(variables[102]+variables[21])/variables[21];
    variables[686] = variables[331]*states[17];
    variables[694] = variables[686]*variables[123]*(variables[199]+variables[181]+variables[213])*variables[294]/variables[141]*variables[692];
    variables[693] = (variables[692]+1.0-fabs(variables[692]-1.0))/2.0;
    variables[696] = variables[695]*variables[681]*variables[693];
    variables[697] = variables[694]+variables[696];
    variables[668] = variables[697]+variables[712]+variables[810]+variables[825]+variables[840]+variables[855]+variables[870]+variables[885]+variables[900]+variables[915]+variables[930]+variables[945]+variables[960]+variables[975];
    variables[669] = variables[668];
    variables[683] = -variables[669];
    variables[624] = variables[683]+variables[663]+variables[735]+variables[780]+variables[787];
    variables[625] = variables[483]*variables[609]-variables[624];
    variables[626] = variables[483]*(variables[609]+variables[607]-variables[608]+variables[616]+variables[332]*variables[621])-variables[624];
    variables[778] = variables[775];
    variables[627] = variables[778];
    variables[789] = variables[786];
    variables[737] = variables[732];
    variables[665] = variables[662];
    variables[685] = variables[669];
    variables[628] = variables[685]+variables[665]+variables[737]+variables[520]+variables[789];
    variables[629] = variables[483]*(variables[607]*variables[272]+variables[608]*(2.0*variables[273]-2.0*variables[269]-variables[272])+variables[609]*variables[269]+variables[616]*(2.0*variables[269]+variables[271]-variables[272])+variables[617]*(variables[272]-variables[269]-variables[271])+variables[621]*((variables[332]+1.0)*variables[269]+variables[274]-variables[272]))+variables[628]*variables[269]+variables[627]*variables[272];
    variables[633] = variables[483]*variables[607];
    variables[634] = 3.0*variables[483]*variables[608];
    variables[635] = 3.0*variables[483]*variables[617];
    variables[636] = variables[634]-variables[633]-variables[635]+variables[483]*variables[616]+variables[483]*variables[621];
    variables[637] = 2.0*variables[483]*variables[616];
    variables[638] = variables[483]*variables[609];
    variables[639] = 3.0*variables[483]*states[14];
    variables[640] = variables[483]*variables[621];
    variables[641] = (variables[332]+1.0)*variables[483]*variables[621];
    variables[976] = variables[964]*variables[112]*(variables[222]+variables[188])/variables[130];
    variables[961] = variables[949]*variables[110]*(variables[220]+variables[186])/variables[128];
    variables[946] = variables[934]*variables[121]*(variables[231]+variables[197])/variables[139];
    variables[931] = variables[919]*variables[116]*(variables[226]+variables[192])/variables[134];
    variables[916] = variables[904]*variables[111]*(variables[221]+variables[187])/variables[129];
    variables[901] = variables[889]*variables[118]*(variables[228]+variables[194])/variables[136];
    variables[886] = variables[874]*variables[113]*(variables[223]+variables[189])/variables[131];
    variables[871] = variables[859]*variables[119]*(variables[229]+variables[195])/variables[137];
    variables[856] = variables[844]*variables[122]*(variables[232]+variables[198])/variables[140];
    variables[841] = variables[829]*variables[117]*(variables[227]+variables[193])/variables[135];
    variables[826] = variables[814]*variables[120]*(variables[230]+variables[196])/variables[138];
    variables[811] = variables[799]*variables[115]*(variables[225]+variables[191])/variables[133];
    variables[713] = variables[701]*variables[114]*(variables[224]+variables[190])/variables[132];
    variables[698] = variables[686]*variables[123]*(variables[233]+variables[199])/variables[141];
    variables[650] = variables[698]+variables[713]+variables[811]+variables[826]+variables[841]+variables[856]+variables[871]+variables[886]+variables[901]+variables[916]+variables[931]+variables[946]+variables[961]+variables[976];
    variables[651] = variables[650];
    variables[659] = variables[503]+variables[651];
    variables[660] = states[16]/variables[659];
    variables[661] = variables[639]/variables[659];
    variables[664] = variables[662];
    variables[670] = variables[694]+variables[709]+variables[807]+variables[822]+variables[837]+variables[852]+variables[867]+variables[882]+variables[897]+variables[912]+variables[927]+variables[942]+variables[957]+variables[972];
    variables[671] = variables[670];
    variables[672] = variables[696]+variables[711]+variables[809]+variables[824]+variables[839]+variables[854]+variables[869]+variables[884]+variables[899]+variables[914]+variables[929]+variables[944]+variables[959]+variables[974];
    variables[673] = variables[672];
    variables[674] = variables[698]+variables[713]+variables[811]+variables[826]+variables[841]+variables[856]+variables[871]+variables[886]+variables[901]+variables[916]+variables[931]+variables[946]+variables[961]+variables[976];
    variables[675] = variables[674];
    variables[977] = variables[563]*states[30];
    variables[962] = variables[560]*states[29];
    variables[947] = variables[557]*states[28];
    variables[932] = variables[554]*states[27];
    variables[917] = variables[552]*states[26];
    variables[902] = variables[549]*states[24];
    variables[887] = variables[546]*states[23];
    variables[872] = variables[543]*states[22];
    variables[857] = variables[540]*states[21];
    variables[842] = variables[537]*states[20];
    variables[827] = variables[534]*states[19];
    variables[812] = variables[531]*states[18];
    variables[714] = variables[508]*states[13];
    variables[699] = variables[506]*states[17];
    variables[676] = variables[699]+variables[714]+variables[812]+variables[827]+variables[842]+variables[857]+variables[872]+variables[887]+variables[902]+variables[917]+variables[932]+variables[947]+variables[962]+variables[977];
    variables[677] = variables[676];
    variables[682] = variables[677]/variables[505];
    variables[684] = variables[669];
    variables[691] = (variables[689]-fabs(variables[689]))/2.0;
    variables[700] = variables[699]/variables[505];
    variables[706] = (variables[704]-fabs(variables[704]))/2.0;
    variables[715] = variables[714]/variables[505];
    variables[720] = variables[510]*(states[1]-variables[593]);
    variables[721] = -variables[720]/variables[46]*1.0e-9;
    variables[722] = variables[148]*variables[151]/(2.0*variables[46])*log(variables[44]/states[5])*1000.0;
    variables[723] = variables[511]*(states[1]-variables[722]);
    variables[724] = -variables[723]/(2.0*variables[46])*1.0e-9;
    variables[725] = variables[512]*(states[1]-variables[602]);
    variables[726] = -variables[725]/variables[46]*1.0e-9;
    variables[733] = -variables[335]*variables[732];
    variables[734] = variables[733];
    variables[736] = variables[732];
    variables[738] = -(states[1]+variables[593]-2.0*variables[602]);
    variables[739] = variables[482]*variables[262]*states[19]*variables[738]/variables[46]*1.0e-9;
    variables[740] = -variables[739]*variables[46]*1000000000.0;
    variables[741] = -variables[739];
    variables[742] = 2.0*variables[739];
    variables[743] = variables[602]-variables[592];
    variables[744] = variables[482]*variables[261]*states[20]*variables[743]/variables[46]*1.0e-9;
    variables[745] = -variables[744];
    variables[746] = variables[744];
    variables[747] = variables[482]*variables[411]*states[21]*variables[265]*1.0e-6;
    variables[748] = variables[747]*(states[1]-variables[592]);
    variables[749] = -variables[748]/variables[46]*1.0e-9;
    variables[750] = variables[482]*variables[409]*states[22]*variables[344]*1.602176634e-10;
    variables[751] = variables[750]*(exp(variables[263]*states[1]*1.0e-3*variables[46]/(variables[148]*variables[151]))*pow(states[3], 3.0)*variables[43]-exp((variables[263]-1.0)*states[1]*1.0e-3*variables[46]/(variables[148]*variables[151]))*pow(variables[144], 3.0)*states[5]*variables[163])/((pow(variables[106], 3.0)+pow(variables[144], 3.0))*(variables[105]+variables[43])*(1.0+variables[108]*exp((variables[263]-1.0)*states[1]*1.0e-3*variables[46]/(variables[148]*variables[151]))));
    variables[752] = variables[751]/variables[46]*1.0e-9;
    variables[753] = -3.0*variables[751]/variables[46]*1.0e-9;
    variables[754] = variables[278]*states[23]*variables[482]*(variables[32]/(variables[80]+variables[32])-states[8]/(variables[80]+states[8]));
    variables[755] = states[12]*states[6]/variables[50];
    variables[756] = variables[279]*states[24]*variables[482]*(variables[513]/(variables[84]+variables[513])-variables[755]/(variables[84]+variables[755]));
    variables[757] = variables[756];
    variables[758] = -variables[754];
    variables[759] = -variables[756];
    variables[760] = variables[754];
    variables[761] = variables[756];
    variables[762] = variables[602]-states[1];
    variables[763] = states[25]*variables[245]*variables[762]/variables[342];
    variables[764] = states[26]*variables[763]*variables[336]/variables[127];
    variables[765] = -variables[764]*variables[46]*1000000000.0;
    variables[766] = variables[764]*variables[46]*variables[762]*0.001;
    variables[767] = variables[517]*variables[443]*variables[763]/variables[245]*(1.0+variables[338])/(states[26]+variables[338]);
    variables[768] = states[26]*variables[767];
    variables[769] = variables[768]/variables[517];
    variables[770] = variables[768]*variables[769];
    variables[771] = variables[317]*states[27];
    variables[772] = variables[298]*variables[771]/variables[126];
    variables[773] = states[10]/variables[218]*states[9]/variables[218]/(variables[31]/variables[218]*states[7]/variables[218]);
    variables[776] = variables[148]*variables[151]*(log(variables[518])-log(fabs(variables[773])+1.0e-300));
    variables[777] = -variables[775];
    variables[779] = variables[775];
    variables[781] = variables[775];
    variables[783] = -variables[238]-variables[148]*variables[151]*log(fabs(states[9]/variables[216]*states[11]/variables[216]/(states[7]/variables[216]))+1.0e-300);
    variables[785] = variables[148]*variables[151]*(log(fabs(variables[29]/variables[216])+1.0e-300)-log(fabs(states[8]/variables[216])+1.0e-300))+variables[337]*variables[783];
    variables[788] = variables[786];
    variables[790] = -variables[784];
    variables[791] = variables[784];
    variables[792] = variables[334]*variables[46]*(variables[602]-states[1])*0.001;
    variables[793] = variables[276]*states[30]*variables[482]*(variables[30]-states[8]*exp(-variables[792]/(variables[148]*variables[151])))/(variables[78]+variables[30]);
    variables[794] = variables[148]*variables[151]*(log(fabs(variables[30]/variables[217])+1.0e-300)-log(fabs(states[8]/variables[217])+1.0e-300))+variables[792];
    variables[795] = variables[334]*variables[793];
    variables[796] = -variables[46]*variables[795]*1000000000.0;
    variables[797] = -variables[793];
    variables[798] = variables[793];
    variables[804] = (variables[802]-fabs(variables[802]))/2.0;
    variables[813] = variables[812]/variables[505];
    variables[819] = (variables[817]-fabs(variables[817]))/2.0;
    variables[828] = variables[827]/variables[505];
    variables[834] = (variables[832]-fabs(variables[832]))/2.0;
    variables[843] = variables[842]/variables[505];
    variables[849] = (variables[847]-fabs(variables[847]))/2.0;
    variables[858] = variables[857]/variables[505];
    variables[864] = (variables[862]-fabs(variables[862]))/2.0;
    variables[873] = variables[872]/variables[505];
    variables[879] = (variables[877]-fabs(variables[877]))/2.0;
    variables[888] = variables[887]/variables[505];
    variables[894] = (variables[892]-fabs(variables[892]))/2.0;
    variables[903] = variables[902]/variables[505];
    variables[909] = (variables[907]-fabs(variables[907]))/2.0;
    variables[918] = variables[917]/variables[505];
    variables[924] = (variables[922]-fabs(variables[922]))/2.0;
    variables[933] = variables[932]/variables[505];
    variables[939] = (variables[937]-fabs(variables[937]))/2.0;
    variables[948] = variables[947]/variables[505];
    variables[954] = (variables[952]-fabs(variables[952]))/2.0;
    variables[963] = variables[962]/variables[505];
    variables[969] = (variables[967]-fabs(variables[967]))/2.0;
    variables[978] = variables[977]/variables[505];
    variables[591] = variables[720]+variables[723]+variables[725]+variables[731]+variables[740]+variables[748]+variables[751]+variables[765]+variables[796];
    variables[594] = variables[721]+variables[741]+variables[753];
    variables[595] = variables[749]+variables[745];
    variables[597] = variables[724]+variables[752];
    variables[600] = variables[726]+variables[734]+variables[742]+variables[746]+variables[764]+variables[795]+variables[757];
    variables[599] = variables[626];
    variables[632] = variables[684]+variables[664]+variables[736]+variables[781]+variables[788];
    variables[630] = variables[760]+variables[791]+variables[798];
    variables[631] = variables[761]+variables[522]+variables[527];
    variables[979] = variables[452]*states[31];
    variables[980] = pow((1.0-variables[979])/(1.0+variables[979]), 2.0);
    variables[981] = variables[453]*states[31];
    variables[982] = variables[456]*states[31]*pow(states[31]/variables[455], 8.0);
    variables[983] = variables[449]*((1.0-variables[454])*variables[981]+variables[982]);
    variables[984] = -log(states[32]/variables[451])/2.302585092994046;
}
