/* The content of this file was generated using the C profile of libCellML 0.7.1. */

#include "model0d_core.h"

#include <math.h>
#include <stdlib.h>

const char VERSION[] = "0.8.0";
const char LIBCELLML_VERSION[] = "0.7.1";

const size_t STATE_COUNT = 42;
const size_t VARIABLE_COUNT = 1239;

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
    {"bio__D_phys", "dimensionless", "cell_0_module", STATE},
    {"bio__D_rad", "dimensionless", "cell_0_module", STATE},
    {"bio__D_ribo", "dimensionless", "cell_0_module", STATE},
    {"bio__D_energy", "dimensionless", "cell_0_module", STATE},
    {"bio__P_growth", "mole", "cell_0_module", STATE},
    {"ribosomes__n_ribo", "dimensionless", "cell_0_module", STATE},
    {"protein__N_aa", "dimensionless", "cell_0_module", STATE},
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
    {"ATP_dmg_mod_bio_cell_0", "millimolar", "parameters", CONSTANT},
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
    {"C_ATP_rest_mod_reg_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_G6P_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_G6P_rest_mod_reg_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_Pi_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_e_mod_glc_ABC_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_e_mod_glc_GalP_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_e_mod_glc_PTS_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_e_mod_membrane_face_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_init_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"C_glc_rest_mod_reg_cell_0", "millimolar", "parameters", CONSTANT},
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
    {"D_energy_init_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"D_phys_init_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"D_rad_init_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"D_ribo_init_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"F_cell_0", "C_per_mol", "parameters", CONSTANT},
    {"H_e_mod_membrane_face_cell_0", "millimolar", "parameters", CONSTANT},
    {"H_i_init_mod_H_cell_0", "millimolar", "parameters", CONSTANT},
    {"H_o_mod_H_cell_0", "millimolar", "parameters", CONSTANT},
    {"H_ref_mod_membrane_face_cell_0", "millimolar", "parameters", CONSTANT},
    {"H_rest_mod_reg_cell_0", "millimolar", "parameters", CONSTANT},
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
    {"K_Ca_dmg_mod_bio_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_D_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"K_G6P_lg_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_G6P_mx_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_G6P_reg_mod_reg_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_G6P_store_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_H_reg_mod_reg_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_K_dmg_mod_bio_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_Pi_PEP_mod_glc_PTS_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_Pi_gly_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_Pi_hyd_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_Pi_mob_mod_cytosol_cell_0", "millimolar", "parameters", CONSTANT},
    {"K_aa_mod_protein_cell_0", "dimensionless", "parameters", CONSTANT},
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
    {"L_rRNA_mod_ribosomes_cell_0", "dimensionless", "parameters", CONSTANT},
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
    {"N_A_mod_protein_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_A_mod_ribosomes_cell_0", "per_mol", "parameters", CONSTANT},
    {"N_aa_init_mod_protein_cell_0", "dimensionless", "parameters", CONSTANT},
    {"Nai_init_mod_Na_K_cell_0", "millimolar", "parameters", CONSTANT},
    {"Nao_mod_Na_K_cell_0", "millimolar", "parameters", CONSTANT},
    {"Nao_mod_i_NaCa_cell_0", "millimolar", "parameters", CONSTANT},
    {"P_init_mod_bio_cell_0", "mole", "parameters", CONSTANT},
    {"Q10_dmg_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"Q10_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
    {"Q_eps_mod_protein_cell_0", "per_s", "parameters", CONSTANT},
    {"Q_prot_max_mod_protein_cell_0", "per_s", "parameters", CONSTANT},
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
    {"aa_ribo_mod_ribosomes_cell_0", "dimensionless", "parameters", CONSTANT},
    {"alpha_NaCa_mod_i_NaCa_cell_0", "dimensionless", "parameters", CONSTANT},
    {"area_max_fraction_mod_protein_cell_0", "dimensionless", "parameters", CONSTANT},
    {"area_per_lipid_mod_bio_cell_0", "um2", "parameters", CONSTANT},
    {"beta_H_mod_H_cell_0", "millimolar", "parameters", CONSTANT},
    {"build_capacity_rest_mod_protein_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_aa_build_mod_ribosomes_cell_0", "dimensionless", "parameters", CONSTANT},
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
    {"c_nt_denovo_mod_ribosomes_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_nt_recycle_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"c_nt_recycle_mod_ribosomes_cell_0", "dimensionless", "parameters", CONSTANT},
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
    {"dose_rate_mod_bio_cell_0", "gray_per_s", "parameters", CONSTANT},
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
    {"k_Ca_dmg_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"k_K_dmg_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
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
    {"k_energy_dmg_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"k_lipid_dmg_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"k_lipid_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"k_qc_dmg_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"k_rad_dmg_mod_bio_cell_0", "per_gray", "parameters", CONSTANT},
    {"k_ribo_build_mod_ribosomes_cell_0", "per_s", "parameters", CONSTANT},
    {"k_ribo_mod_ribosomes_cell_0", "per_s", "parameters", CONSTANT},
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
    {"n_Ca_dmg_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_G6P_lg_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_H_mod_glc_GalP_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_H_mod_i_H_ATPase_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_H_rev_mod_i_flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_P_mod_glc_ABC_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_half_mod_i_flagellum_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_mRNA_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_reg_mod_reg_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_ribo_build_mod_ribosomes_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_ribo_init_mod_ribosomes_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_ribo_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"n_ribo_rest_mod_ribosomes_cell_0", "dimensionless", "parameters", CONSTANT},
    {"pmf_ref_mod_i_flagellum_cell_0", "milliV", "parameters", CONSTANT},
    {"r_H_ATPase_mod_i_H_ATPase_cell_0", "per_s", "parameters", CONSTANT},
    {"r_NaCa_mod_i_NaCa_cell_0", "per_s", "parameters", CONSTANT},
    {"r_dmg_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"r_dmg_phys_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"r_rep_max_mod_bio_cell_0", "per_s", "parameters", CONSTANT},
    {"reg_on_ATP_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
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
    {"reg_on_ATP_mod_protein_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_repair_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_ATP_upkeep_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
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
    {"reg_on_G6P_mod_protein_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_repair_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_G6P_upkeep_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
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
    {"reg_on_H_mod_protein_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_repair_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_H_upkeep_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_mod_cytosol_cell_0", "dimensionless", "parameters", CONSTANT},
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
    {"reg_on_glc_mod_protein_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_repair_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
    {"reg_on_glc_upkeep_mod_bio_cell_0", "dimensionless", "parameters", CONSTANT},
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
    {"tau_aa_cat_mod_protein_cell_0", "second", "parameters", CONSTANT},
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
    {"tau_prot_mod_pool_ABC_cell_0", "second", "parameters", CONSTANT},
    {"tau_prot_mod_pool_Flagellum_cell_0", "second", "parameters", CONSTANT},
    {"tau_prot_mod_pool_GalP_cell_0", "second", "parameters", CONSTANT},
    {"tau_prot_mod_pool_Glf_cell_0", "second", "parameters", CONSTANT},
    {"tau_prot_mod_pool_Glyc_cell_0", "second", "parameters", CONSTANT},
    {"tau_prot_mod_pool_H_ATPase_cell_0", "second", "parameters", CONSTANT},
    {"tau_prot_mod_pool_Hydrolase_cell_0", "second", "parameters", CONSTANT},
    {"tau_prot_mod_pool_KefC_cell_0", "second", "parameters", CONSTANT},
    {"tau_prot_mod_pool_LacT_cell_0", "second", "parameters", CONSTANT},
    {"tau_prot_mod_pool_NCX_cell_0", "second", "parameters", CONSTANT},
    {"tau_prot_mod_pool_NhaA_cell_0", "second", "parameters", CONSTANT},
    {"tau_prot_mod_pool_PTS_cell_0", "second", "parameters", CONSTANT},
    {"tau_prot_mod_pool_Trk_cell_0", "second", "parameters", CONSTANT},
    {"tau_prot_mod_pool_proteome_cell_0", "second", "parameters", CONSTANT},
    {"v_one_mod_i_flagellum_cell_0", "m_per_s", "parameters", CONSTANT},
    {"y_aa_cat_mod_protein_cell_0", "dimensionless", "parameters", CONSTANT},
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
    {"bio__R_dmg_rad", "per_s", "cell_0_module", COMPUTED_CONSTANT},
    {"bio__M_build_bio", "mole", "cell_0_module", COMPUTED_CONSTANT},
    {"ribosomes__c_ribo_build", "dimensionless", "cell_0_module", COMPUTED_CONSTANT},
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
    {"reg__f_H_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__f_glc_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__f_G6P_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__f_ATP_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"cytosol__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"bio__R_reg_upkeep", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__R_reg_repair", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__f_ATP_bio", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__x_unpaid_raw", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__x_unpaid", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__u_upkeep", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__J_RNA", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__J_lipid", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__f_Ca_dmg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__f_K_dmg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__f_energy_dmg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__R_dmg_lipid", "per_s", "cell_0_module", ALGEBRAIC},
    {"bio__R_dmg_phys", "per_s", "cell_0_module", ALGEBRAIC},
    {"bio__R_dmg_qc", "per_s", "cell_0_module", ALGEBRAIC},
    {"bio__R_dmg_ribo", "per_s", "cell_0_module", ALGEBRAIC},
    {"bio__R_dmg_energy", "per_s", "cell_0_module", ALGEBRAIC},
    {"bio__R_rep_phys", "per_s", "cell_0_module", ALGEBRAIC},
    {"bio__R_rep_rad", "per_s", "cell_0_module", ALGEBRAIC},
    {"bio__R_rep_ribo", "per_s", "cell_0_module", ALGEBRAIC},
    {"bio__R_rep_energy", "per_s", "cell_0_module", ALGEBRAIC},
    {"bio__R_repair", "per_s", "cell_0_module", ALGEBRAIC},
    {"bio__D", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__J_repair", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__x_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__throttle", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_bio_M_build_in__M_build_in", "mole", "cell_0_module", ALGEBRAIC},
    {"bio__M_build_protein", "mole", "cell_0_module", ALGEBRAIC},
    {"bio__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__J_growth", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__J_growth_used", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__M_daughter", "mole", "cell_0_module", ALGEBRAIC},
    {"bio__growth_progress", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"bio__glycogen_progress", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"ribosomes__J_ATP_ribo", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__J_ATP_bio", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__j_ATP_bio", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__j_ADP_bio", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"bio__j_Pi_bio", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"ribosomes__u_upkeep", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"ribosomes__x_ribo", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"ribosomes__R_ribo_loss", "per_s", "cell_0_module", ALGEBRAIC},
    {"ribosomes__R_ribo_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"ribosomes__J_ribo", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"ribosomes__J_ribo_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__R_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_J_demand_in__J_demand_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__J_demand_total", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_J_keep_in__J_keep_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__J_keep_total", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_Q_prot_demand_in__Q_prot_demand_in", "per_s", "cell_0_module", ALGEBRAIC},
    {"protein__Q_prot_demand_total", "per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_Q_aa_rel_in__Q_aa_rel_in", "per_s", "cell_0_module", ALGEBRAIC},
    {"protein__Q_aa_rel_total", "per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_Q_aa_use_in__Q_aa_use_in", "per_s", "cell_0_module", ALGEBRAIC},
    {"protein__Q_aa_use_total", "per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_J_prot_in__J_prot_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__J_prot_total", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_J_saved_in__J_saved_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__J_saved_total", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__s_prot", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"protein__phi_aa", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"protein__r_aa_cat", "per_s", "cell_0_module", ALGEBRAIC},
    {"protein__J_cat", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__J_recycled", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_J_ATP_in__J_ATP_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__J_ATP_total", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_J_build_in__J_build_in", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"protein__J_build_total", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_M_build_in__M_build_in", "mole", "cell_0_module", ALGEBRAIC},
    {"protein__M_build_protein", "mole", "cell_0_module", ALGEBRAIC},
    {"multiport_sum_mod_protein_A_protein_in__A_protein_in", "um2", "cell_0_module", ALGEBRAIC},
    {"protein__A_protein_total", "um2", "cell_0_module", ALGEBRAIC},
    {"protein__x_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"protein__throttle", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"protein__build_capacity", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"pool_proteome__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_proteome__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"pool_Glyc__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__J_build", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__J_ATP", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__M_build", "mole", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__A_protein", "um2", "cell_0_module", ALGEBRAIC},
    {"pool_Glyc__share", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__x_ATP_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__u_ATP_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__x_G6P_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__u_G6P_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__x_glc_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__u_glc_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__x_H_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"reg__u_H_reg", "dimensionless", "cell_0_module", ALGEBRAIC},
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
    {"pool_H_ATPase__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_H_ATPase__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"pool_NhaA__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NhaA__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"pool_KefC__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_KefC__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"pool_Trk__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Trk__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"pool_NCX__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_NCX__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"pool_Glf__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Glf__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"pool_LacT__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_LacT__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"pool_Flagellum__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Flagellum__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"pool_Hydrolase__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_Hydrolase__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"pool_PTS__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_PTS__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"pool_ABC__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_ABC__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    {"pool_GalP__f_ATP", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__f_build", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__m_kept", "dimensionless", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__r_deg", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__r_down", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__r_rep", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__J_keep", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__J_demand", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__r_build", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__Q_prot_demand", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__r_prot", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__J_prot", "mol_per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__Q_aa_rel", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__Q_aa_use", "per_s", "cell_0_module", ALGEBRAIC},
    {"pool_GalP__J_saved", "mol_per_s", "cell_0_module", ALGEBRAIC},
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
    variables[176] = -105.0;
    states[1] = variables[176];
    variables[115] = 200.0;
    states[2] = variables[115];
    variables[157] = 20.0;
    states[3] = variables[157];
    variables[24] = 7.991010113622174;
    states[4] = variables[24];
    variables[46] = 9.0e-05;
    states[5] = variables[46];
    variables[55] = 2.5118864315095822e-05;
    states[6] = variables[55];
    variables[27] = 9.6;
    states[7] = variables[27];
    variables[36] = 0.002;
    states[8] = variables[36];
    variables[26] = 0.56;
    states[9] = variables[26];
    variables[29] = 1.0;
    states[10] = variables[29];
    variables[31] = 10.0;
    states[11] = variables[31];
    variables[44] = 0.26990316647694707;
    states[12] = variables[44];
    variables[332] = 1.0;
    states[13] = variables[332];
    variables[38] = 0.0;
    states[14] = variables[38];
    variables[50] = 0.0;
    states[15] = variables[50];
    variables[51] = 0.0;
    states[16] = variables[51];
    variables[52] = 0.011111303111436668;
    states[17] = variables[52];
    variables[49] = 0.0;
    states[18] = variables[49];
    variables[160] = 0.0;
    states[19] = variables[160];
    variables[372] = 43982.2971502571;
    states[20] = variables[372];
    variables[156] = 0.0;
    states[21] = variables[156];
    variables[341] = 1.0;
    states[22] = variables[341];
    variables[333] = 1.0;
    states[23] = variables[333];
    variables[338] = 1.0;
    states[24] = variables[338];
    variables[335] = 1.0;
    states[25] = variables[335];
    variables[340] = 1.0;
    states[26] = variables[340];
    variables[337] = 1.0;
    states[27] = variables[337];
    variables[331] = 1.0;
    states[28] = variables[331];
    variables[336] = 1.0;
    states[29] = variables[336];
    variables[178] = 0.0;
    states[30] = variables[178];
    variables[329] = 1.0;
    states[31] = variables[329];
    variables[334] = 1.0;
    states[32] = variables[334];
    variables[339] = 0.0;
    states[33] = variables[339];
    variables[328] = 0.0;
    states[34] = variables[328];
    variables[330] = 0.0;
    states[35] = variables[330];
    variables[514] = 0.0;
    states[36] = variables[514];
    variables[512] = 5.011872336272725e-05;
    states[37] = variables[512];
    variables[510] = 0.1;
    states[38] = variables[510];
    variables[511] = 0.0;
    states[39] = variables[511];
    variables[513] = 0.003;
    states[40] = variables[513];
    states[41] = 0.0;
    variables[1] = -0.0558;
    variables[2] = 0.0133;
    variables[3] = 8.0e-15;
    variables[4] = 4.46e-16;
    variables[5] = 1.0;
    variables[6] = 4.0;
    variables[7] = 4.0;
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
    variables[23] = 9.6;
    variables[25] = 8.0;
    variables[28] = 9.6;
    variables[30] = 1.0;
    variables[32] = 0.1;
    variables[33] = 0.1;
    variables[34] = 0.1;
    variables[35] = 0.1;
    variables[37] = 0.002;
    variables[39] = 200.0;
    variables[40] = 0.0;
    variables[41] = 0.0;
    variables[42] = 0.0;
    variables[43] = 0.0;
    variables[45] = 3.0037809007103e-14;
    variables[47] = 10.28;
    variables[48] = 10.28;
    variables[53] = 96485.0;
    variables[54] = 5.011872336272725e-05;
    variables[56] = 5.011872336272725e-05;
    variables[57] = 0.0001;
    variables[58] = 2.5118864315095822e-05;
    variables[59] = 0.0;
    variables[60] = 1.0e-30;
    variables[61] = 9.06587001723412e-19;
    variables[62] = 9.06587001723412e-19;
    variables[63] = 0.1;
    variables[64] = 0.1;
    variables[65] = 0.5;
    variables[66] = 1.0;
    variables[67] = 0.1;
    variables[68] = 0.5;
    variables[69] = 1.0;
    variables[70] = 4.8;
    variables[71] = 1.0;
    variables[72] = 0.5;
    variables[73] = 0.1;
    variables[74] = 4.158318947097563;
    variables[75] = 50.0;
    variables[76] = 0.5;
    variables[77] = 50.0;
    variables[78] = 5.0237728630191644e-05;
    variables[79] = 100.0;
    variables[80] = 1.0;
    variables[81] = 1.0;
    variables[82] = 5.0;
    variables[83] = 5.0;
    variables[84] = 3783822.6340999305;
    variables[85] = 2.5118864315095822e-05;
    variables[86] = 0.1;
    variables[87] = 0.001;
    variables[88] = 0.0005;
    variables[89] = 0.1;
    variables[90] = 0.005;
    variables[91] = 4.1;
    variables[92] = 10.0;
    variables[93] = 1.0;
    variables[94] = 10.0;
    variables[95] = 4.0;
    variables[96] = 0.2;
    variables[97] = 0.1;
    variables[98] = 0.1;
    variables[99] = 0.1;
    variables[100] = 0.1;
    variables[101] = 0.1;
    variables[102] = 0.1;
    variables[103] = 0.1;
    variables[104] = 0.1;
    variables[105] = 0.1;
    variables[106] = 0.1;
    variables[107] = 0.1;
    variables[108] = 0.1;
    variables[109] = 0.1;
    variables[110] = 0.1;
    variables[111] = 0.1;
    variables[112] = 0.1;
    variables[113] = 0.1;
    variables[114] = 20.0;
    variables[116] = 1.38;
    variables[117] = 87.5;
    variables[118] = 10.21;
    variables[119] = 0.1;
    variables[120] = 1000.0;
    variables[121] = 1510.0;
    variables[122] = 10128240.0;
    variables[123] = 464.0;
    variables[124] = 473.0;
    variables[125] = 350.0;
    variables[126] = 4895.0;
    variables[127] = 700.0;
    variables[128] = 620.0;
    variables[129] = 551.0;
    variables[130] = 302.0;
    variables[131] = 388.0;
    variables[132] = 1306.0;
    variables[133] = 2798.0;
    variables[134] = 300.0;
    variables[135] = 4566.0;
    variables[136] = 4566.0;
    variables[137] = 6.02214076e+23;
    variables[138] = 6.02214076e+23;
    variables[139] = 6.02214076e+23;
    variables[140] = 6.02214076e+23;
    variables[141] = 6.02214076e+23;
    variables[142] = 6.02214076e+23;
    variables[143] = 6.02214076e+23;
    variables[144] = 6.02214076e+23;
    variables[145] = 6.02214076e+23;
    variables[146] = 6.02214076e+23;
    variables[147] = 6.02214076e+23;
    variables[148] = 6.02214076e+23;
    variables[149] = 6.02214076e+23;
    variables[150] = 6.02214076e+23;
    variables[151] = 6.02214076e+23;
    variables[152] = 6.02214076e+23;
    variables[153] = 6.02214076e+23;
    variables[154] = 6.02214076e+23;
    variables[155] = 6.02214076e+23;
    variables[158] = 469.0;
    variables[159] = 469.0;
    variables[161] = 2.0;
    variables[162] = 2.0;
    variables[163] = 1.0e-09;
    variables[164] = 80564.18321852955;
    variables[165] = 8.314;
    variables[166] = 1.0;
    variables[167] = 1.0;
    variables[168] = 293.15;
    variables[169] = 293.15;
    variables[170] = 293.15;
    variables[171] = 6.588125835191758;
    variables[172] = 0.08365908029758785;
    variables[173] = 28.05882448294951;
    variables[174] = 0.14428780266714164;
    variables[175] = 0.0072143901333570825;
    variables[177] = 0.05050073093349957;
    variables[179] = 0.0;
    variables[180] = 7500.0;
    variables[181] = 1.2;
    variables[182] = 0.15;
    variables[183] = 5.0e-07;
    variables[184] = 50.0;
    variables[185] = 9.02267970403271;
    variables[186] = 28.0;
    variables[187] = 1.0;
    variables[188] = 1.0;
    variables[189] = 1.0;
    variables[190] = 1.0;
    variables[191] = 1.0;
    variables[192] = 1.0;
    variables[193] = 1.0;
    variables[194] = 1.0;
    variables[195] = 1.0;
    variables[196] = 1.0;
    variables[197] = 1.0;
    variables[198] = 1.0;
    variables[199] = 1.0;
    variables[200] = 1.0;
    variables[201] = 100.0;
    variables[202] = 1.0;
    variables[203] = 50.0;
    variables[204] = 50.0;
    variables[205] = 2.0;
    variables[206] = 2.0;
    variables[207] = 4.0;
    variables[208] = 4.0;
    variables[209] = 4.0;
    variables[210] = 4.0;
    variables[211] = 4.0;
    variables[212] = 4.0;
    variables[213] = 4.0;
    variables[214] = 4.0;
    variables[215] = 4.0;
    variables[216] = 4.0;
    variables[217] = 4.0;
    variables[218] = 4.0;
    variables[219] = 4.0;
    variables[220] = 4.0;
    variables[221] = 0.5;
    variables[222] = 0.5;
    variables[223] = 0.5;
    variables[224] = 0.5;
    variables[225] = 0.5;
    variables[226] = 0.5;
    variables[227] = 0.5;
    variables[228] = 0.5;
    variables[229] = 0.5;
    variables[230] = 0.5;
    variables[231] = 0.5;
    variables[232] = 0.5;
    variables[233] = 0.5;
    variables[234] = 0.5;
    variables[235] = 1000.0;
    variables[236] = 1000.0;
    variables[237] = 1000.0;
    variables[238] = 1000.0;
    variables[239] = 1000.0;
    variables[240] = 1000.0;
    variables[241] = 24.0;
    variables[242] = 24.0;
    variables[243] = 24.0;
    variables[244] = 24.0;
    variables[245] = 24.0;
    variables[246] = 24.0;
    variables[247] = 24.0;
    variables[248] = 24.0;
    variables[249] = 24.0;
    variables[250] = 24.0;
    variables[251] = 24.0;
    variables[252] = 24.0;
    variables[253] = 24.0;
    variables[254] = 24.0;
    variables[255] = -196000.0;
    variables[256] = -16700.0;
    variables[257] = -16700.0;
    variables[258] = -30500.0;
    variables[259] = -30500.0;
    variables[260] = -30500.0;
    variables[261] = -55800.0;
    variables[262] = -42000.0;
    variables[263] = 10.0;
    variables[264] = 10.0;
    variables[265] = 0.0;
    variables[266] = 0.00108;
    variables[267] = 100.0;
    variables[268] = 25.0;
    variables[269] = 1700.0;
    variables[270] = 12.0;
    variables[271] = 12.0;
    variables[272] = 0.0;
    variables[273] = 100.0;
    variables[274] = 20.0;
    variables[275] = 20.0;
    variables[276] = 15.0;
    variables[277] = 12.0;
    variables[278] = 15.0;
    variables[279] = 12.0;
    variables[280] = 60.0;
    variables[281] = 0.0;
    variables[282] = 2.3686015649131477e-08;
    variables[283] = 1.2528761581650615e-08;
    variables[284] = 1.4452236359013608e-08;
    variables[285] = 0.35;
    variables[286] = 5.0;
    variables[287] = 50.0;
    variables[288] = 27.0;
    variables[289] = 9200000.0;
    variables[290] = 0.0;
    variables[291] = 20500.0;
    variables[292] = 2.0;
    variables[293] = 0.0;
    variables[294] = 23800.0;
    variables[295] = 63000.0;
    variables[296] = 0.0;
    variables[297] = 3.0933897205955483e-20;
    variables[298] = 7.733474301488869e-20;
    variables[299] = 3.0933897205955476e-19;
    variables[300] = 1.5466948602977742e-18;
    variables[301] = 4.32863408001425e-18;
    variables[302] = 0.0005555555555555556;
    variables[303] = 0.0002777777777777778;
    variables[304] = 100000.0;
    variables[305] = 2.777777777777778e-06;
    variables[306] = 2.777777777777778e-06;
    variables[307] = 2.777777777777778e-06;
    variables[308] = 2.777777777777778e-06;
    variables[309] = 2.777777777777778e-06;
    variables[310] = 2.777777777777778e-06;
    variables[311] = 2.777777777777778e-06;
    variables[312] = 2.777777777777778e-06;
    variables[313] = 2.777777777777778e-06;
    variables[314] = 2.777777777777778e-06;
    variables[315] = 2.777777777777778e-06;
    variables[316] = 2.777777777777778e-06;
    variables[317] = 2.777777777777778e-06;
    variables[318] = 2.777777777777778e-06;
    variables[319] = 0.0005555555555555556;
    variables[320] = 5.555555555555556e-06;
    variables[321] = 2.777777777777778e-06;
    variables[322] = 5.787037037037037e-06;
    variables[323] = 0.0033333333333333335;
    variables[324] = 0.0002777777777777778;
    variables[325] = 2.777777777777778e-06;
    variables[326] = 8000.0;
    variables[327] = 1000.0;
    variables[342] = 1.0;
    variables[343] = 1.0;
    variables[344] = 1.0;
    variables[345] = 0.0;
    variables[346] = 2729.797267782374;
    variables[347] = 11704.838040881496;
    variables[348] = 1.0;
    variables[349] = 11704.838040881496;
    variables[350] = 11704.838040881496;
    variables[351] = 1884955.592153876;
    variables[352] = 2850.255770975698;
    variables[353] = 2729.797267782374;
    variables[354] = 909.932422594125;
    variables[355] = 32757.567213388495;
    variables[356] = 2801.9209327573735;
    variables[357] = 682.4493169455936;
    variables[358] = 11704.838040881496;
    variables[359] = 90.9932422594125;
    variables[360] = 16964600.329384882;
    variables[361] = 2.0;
    variables[362] = 2.0;
    variables[363] = 4.0;
    variables[364] = 1.0;
    variables[365] = 3.3333333333333335;
    variables[366] = 1200.0;
    variables[367] = 2.0;
    variables[368] = 2.0;
    variables[369] = 8670.79572390783;
    variables[370] = 4.0;
    variables[371] = 8.0;
    variables[373] = 43982.2971502571;
    variables[374] = 43982.2971502571;
    variables[375] = 122.44926189223139;
    variables[376] = 100.0;
    variables[377] = 5000.0;
    variables[378] = 1.1574074074074074e-06;
    variables[379] = 0.0;
    variables[380] = 1.1574074074074073e-05;
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
    variables[407] = 0.0;
    variables[408] = 0.0;
    variables[409] = 0.0;
    variables[410] = 0.0;
    variables[411] = 0.0;
    variables[412] = 0.0;
    variables[413] = 0.0;
    variables[414] = 0.0;
    variables[415] = 0.0;
    variables[416] = 0.0;
    variables[417] = 0.0;
    variables[418] = 0.0;
    variables[419] = 0.0;
    variables[420] = 0.0;
    variables[421] = 0.0;
    variables[422] = 0.0;
    variables[423] = 0.0;
    variables[424] = 0.0;
    variables[425] = 0.0;
    variables[426] = 0.0;
    variables[427] = 0.0;
    variables[428] = 0.0;
    variables[429] = 0.0;
    variables[430] = 0.0;
    variables[431] = 0.0;
    variables[432] = 0.0;
    variables[433] = 0.0;
    variables[434] = 0.0;
    variables[435] = 0.0;
    variables[436] = 0.0;
    variables[437] = 0.0;
    variables[438] = 0.0;
    variables[439] = 0.0;
    variables[440] = 0.0;
    variables[441] = 0.0;
    variables[442] = 0.0;
    variables[443] = 0.0;
    variables[444] = 0.0;
    variables[445] = 0.0;
    variables[446] = 0.0;
    variables[447] = 0.0;
    variables[448] = 0.0;
    variables[449] = 0.0;
    variables[450] = 0.0;
    variables[451] = 0.0;
    variables[452] = 0.0;
    variables[453] = 0.0;
    variables[454] = 0.0;
    variables[455] = 0.0;
    variables[456] = 0.0;
    variables[457] = 1.0;
    variables[458] = 226.81614751349176;
    variables[459] = 222.96978330049507;
    variables[460] = 1.3806187541439416e-05;
    variables[461] = 0.0007772676368068118;
    variables[462] = 0.00016924908710165128;
    variables[463] = 0.0;
    variables[464] = 0.0;
    variables[465] = 0.049676875527902564;
    variables[466] = 0.10080717667266298;
    variables[467] = 0.01930936572472512;
    variables[468] = 0.006436455241575042;
    variables[469] = 0.17378429152252606;
    variables[470] = 0.011891721776026404;
    variables[471] = 0.00362050607338596;
    variables[472] = 0.0;
    variables[473] = 0.0019309365724725122;
    variables[474] = 0.0;
    variables[475] = 1.5;
    variables[476] = 1.5;
    variables[477] = 300.0;
    variables[478] = 1800.0;
    variables[479] = 0.3;
    variables[480] = 432.80851226668904;
    variables[481] = 432.80851226668904;
    variables[482] = 432.80851226668904;
    variables[483] = 432.80851226668904;
    variables[484] = 432.80851226668904;
    variables[485] = 432.80851226668904;
    variables[486] = 432.80851226668904;
    variables[487] = 432.80851226668904;
    variables[488] = 432.80851226668904;
    variables[489] = 432.80851226668904;
    variables[490] = 432.80851226668904;
    variables[491] = 432.80851226668904;
    variables[492] = 432.80851226668904;
    variables[493] = 432.80851226668904;
    variables[494] = 432.80851226668904;
    variables[495] = 432.80851226668904;
    variables[496] = 432.80851226668904;
    variables[497] = 432.80851226668904;
    variables[498] = 432.80851226668904;
    variables[499] = 432.80851226668904;
    variables[500] = 432.80851226668904;
    variables[501] = 432.80851226668904;
    variables[502] = 432.80851226668904;
    variables[503] = 432.80851226668904;
    variables[504] = 432.80851226668904;
    variables[505] = 432.80851226668904;
    variables[506] = 432.80851226668904;
    variables[507] = 432.80851226668904;
    variables[508] = 1.5e-05;
    variables[509] = 0.5;
    variables[515] = 1.4396817324298869e-18;
    variables[516] = 2599.594240021476;
    variables[517] = 1000.0;
    variables[518] = 9.719999999999999e-05;
    variables[519] = 1.0e-06;
    variables[520] = 0.5;
    variables[521] = 308.641975308642;
    variables[522] = 0.001;
    variables[523] = 0.0;
    variables[524] = 0.0;
    variables[525] = 0.0;
    variables[526] = 0.0;
    variables[527] = 0.0;
    variables[528] = 5.9e-10;
    variables[529] = 9.0e-10;
    variables[530] = 2.607594240021476e-06;
    variables[531] = 1.75e-09;
    variables[532] = 6.0e-13;
    variables[533] = 6.639528095680696e-07;
    variables[534] = 6.639528095680696e-07;
    variables[535] = 1.0;
    variables[536] = 0.1;
    variables[537] = 0.1;
    variables[538] = 0.0;
    variables[539] = 0.0;
    variables[540] = 5.011872336272725e-05;
    variables[541] = 5.011872336272725e-05;
    variables[542] = 0.003;
    variables[543] = 0.003;
    variables[544] = 1.0;
    variables[545] = 1.0;
    variables[582] = 0.0;
    variables[583] = 0.0;
    variables[584] = 0.0;
    variables[587] = 0.0;
    variables[588] = 0.0;
    variables[589] = 0.0;
    variables[590] = 0.0;
    variables[591] = 0.0;
    variables[592] = 0.0;
    variables[593] = 0.0;
    variables[594] = 0.0;
    variables[595] = 0.0;
    variables[596] = 0.0;
    variables[597] = 0.0;
    variables[598] = 0.0;
    variables[559] = 0.0;
    variables[560] = 0.0;
    variables[561] = 0.0;
    variables[563] = 0.0;
    variables[565] = 0.0;
    variables[547] = 0.0;
    variables[546] = 0.0;
    variables[635] = 0.0;
    variables[637] = 0.0;
    variables[639] = 0.0;
    variables[641] = 0.0;
    variables[634] = 0.0;
    variables[636] = 0.0;
    variables[638] = 0.0;
    variables[640] = 0.0;
}

void computeComputedConstants(double voi, double *states, double *rates, double *variables)
{
    variables[548] = 2.0*3.14159265358979*variables[167]*variables[292];
    variables[549] = 3.14159265358979*pow(variables[167], 2.0)*variables[292]*1.0e-18;
    variables[550] = variables[202]*variables[548]*0.01;
    variables[551] = -log(variables[56]/variables[235])/log(10.0);
    variables[552] = pow(variables[162], (variables[168]-variables[170])/variables[264]);
    variables[553] = exp(-variables[256]/(variables[165]*variables[168]));
    variables[554] = exp(-(variables[255]-2.0*variables[258]-variables[256])/(variables[165]*variables[168]));
    variables[555] = exp(-variables[258]/(variables[165]*variables[168]));
    variables[556] = exp(-(variables[262]-variables[256])/(variables[165]*variables[168]));
    variables[557] = exp(-(variables[256]+variables[258]-variables[262])/(variables[165]*variables[168]));
    variables[558] = exp(-variables[261]/(variables[165]*variables[168]));
    variables[567] = 2.0*variables[548]*variables[475]/variables[183];
    variables[568] = pow(variables[161], (variables[168]-variables[169])/variables[263]);
    variables[569] = variables[323]*variables[265];
    variables[570] = ((variables[369]*variables[120]+variables[373]*variables[135]+variables[289])*variables[203]+variables[567]*variables[201])/variables[137];
    variables[571] = variables[136]*variables[204]+variables[180]*variables[186];
    variables[572] = variables[548]*variables[476];
    variables[573] = variables[182]*variables[572];
    variables[574] = variables[360]*variables[281]*1.0e-6;
    variables[575] = variables[574]/variables[573];
    variables[576] = variables[351]*variables[272]*1.0e-6;
    variables[577] = variables[576]/variables[573];
    variables[578] = variables[548]*variables[462]*variables[288]*1.0e-6;
    variables[579] = variables[548]*variables[460]*variables[286]*1.0e-6;
    variables[580] = variables[548]*variables[282];
    variables[581] = variables[43]*variables[54]/variables[57];
    variables[585] = 6.0*3.14159265358979*variables[266]*variables[166]*1.0e-6;
    variables[586] = exp(-variables[257]/(variables[165]*variables[168]));
    variables[599] = variables[352]*variables[273]*1.0e-6;
    variables[600] = variables[466]*variables[573]/variables[599];
    variables[601] = variables[599]/variables[573];
    variables[602] = variables[357]*variables[278]*1.0e-6;
    variables[603] = variables[471]*variables[573]/variables[602];
    variables[604] = variables[602]/variables[573];
    variables[605] = variables[354]*variables[275]*1.0e-6;
    variables[606] = variables[468]*variables[573]/variables[605];
    variables[607] = variables[605]/variables[573];
    variables[608] = variables[359]*variables[280]*1.0e-6;
    variables[609] = variables[473]*variables[573]/variables[608];
    variables[610] = variables[608]/variables[573];
    variables[611] = variables[356]*variables[277]*1.0e-6;
    variables[612] = variables[470]*variables[573]/variables[611];
    variables[613] = variables[611]/variables[573];
    variables[614] = variables[350]*variables[271]*1.0e-6;
    variables[615] = variables[465]*variables[573]/variables[614];
    variables[616] = variables[614]/variables[573];
    variables[617] = variables[355]*variables[276]*1.0e-6;
    variables[618] = variables[469]*variables[573]/variables[617];
    variables[619] = variables[617]/variables[573];
    variables[620] = variables[348]*variables[269]*1.0e-6;
    variables[621] = variables[620]/variables[573];
    variables[622] = variables[353]*variables[274]*1.0e-6;
    variables[623] = variables[467]*variables[573]/variables[622];
    variables[624] = variables[622]/variables[573];
    variables[625] = variables[358]*variables[279]*1.0e-6;
    variables[626] = variables[472]*variables[573]/variables[625];
    variables[627] = variables[625]/variables[573];
    variables[628] = variables[347]*variables[268]*1.0e-6;
    variables[629] = variables[463]*variables[573]/variables[628];
    variables[630] = variables[628]/variables[573];
    variables[631] = variables[349]*variables[270]*1.0e-6;
    variables[632] = variables[464]*variables[573]/variables[631];
    variables[633] = variables[631]/variables[573];
    variables[562] = variables[582]+variables[591]+variables[596];
    variables[564] = variables[583]+variables[592]+variables[597];
    variables[566] = variables[584]+variables[593]+variables[598];
    variables[642] = variables[535]*variables[532]*variables[528]*variables[544]*variables[545]/(variables[533]*variables[545]+variables[534]*variables[544]);
    variables[643] = variables[642]*(variables[536]-variables[537]);
    variables[644] = variables[643];
    variables[645] = -variables[643];
    variables[646] = variables[535]*variables[532]*variables[529]*variables[544]*variables[545]/(variables[533]*variables[545]+variables[534]*variables[544]);
    variables[647] = variables[646]*(variables[538]-variables[539]);
    variables[648] = variables[647];
    variables[649] = -variables[647];
    variables[650] = variables[535]*variables[532]*variables[530]*variables[544]*variables[545]/(variables[533]*variables[545]+variables[534]*variables[544]);
    variables[651] = variables[650]*(variables[540]-variables[541]);
    variables[652] = variables[651];
    variables[653] = -variables[651];
    variables[654] = variables[535]*variables[532]*variables[531]*variables[544]*variables[545]/(variables[533]*variables[545]+variables[534]*variables[544]);
    variables[655] = variables[654]*(variables[542]-variables[543]);
    variables[656] = variables[655];
    variables[657] = -variables[655];
}

void computeRates(double voi, double *states, double *rates, double *variables)
{
    variables[658] = variables[1]*states[0]/(states[0]+variables[2]);
    rates[0] = (variables[547]+variables[546]+variables[4])/variables[3]+variables[658];
    variables[670] = variables[165]*variables[168]/variables[53]*log(variables[56]/states[6])*1000.0;
    variables[926] = variables[364]*variables[53]*(variables[670]-states[1])*0.001;
    variables[927] = variables[298]*states[35]*variables[548]*(variables[33]-states[8]*exp(-variables[926]/(variables[165]*variables[168])))/(variables[89]+variables[33]);
    variables[929] = variables[364]*variables[927];
    variables[930] = -variables[53]*variables[929]*1000000000.0;
    variables[896] = variables[670]-states[1];
    variables[897] = states[30]*variables[267]*variables[896]/variables[375];
    variables[898] = states[31]*variables[897]*variables[366]/variables[139];
    variables[899] = -variables[898]*variables[53]*1000000000.0;
    variables[661] = variables[165]*variables[168]/variables[53]*log(variables[158]/states[3])*1000.0;
    variables[872] = -(states[1]+variables[661]-2.0*variables[670]);
    variables[873] = variables[548]*variables[284]*states[24]*variables[872]/variables[53]*1.0e-9;
    variables[874] = -variables[873]*variables[53]*1000000000.0;
    variables[863] = variables[365]*variables[53]*(variables[670]-states[1])*0.001;
    variables[862] = -variables[260]-variables[165]*variables[168]*log(fabs(states[9]/variables[240]*states[11]/variables[240]/(states[7]/variables[240]))+1.0e-300);
    variables[864] = 1.0-exp(-(variables[862]-variables[863])/(variables[165]*variables[168]));
    variables[861] = variables[548]*variables[458]*states[23]*variables[376]*variables[365]*1.602176634e-10;
    variables[865] = variables[861]*states[7]/(variables[96]+states[7])*states[6]/(variables[85]+states[6])*variables[864];
    variables[859] = variables[580]*(states[1]-variables[670]);
    variables[856] = variables[165]*variables[168]/(2.0*variables[53])*log(variables[48]/states[5])*1000.0;
    variables[857] = variables[579]*(states[1]-variables[856]);
    variables[884] = variables[548]*variables[459]*states[27]*variables[377]*1.602176634e-10;
    variables[885] = variables[884]*(exp(variables[285]*states[1]*1.0e-3*variables[53]/(variables[165]*variables[168]))*pow(states[3], 3.0)*variables[47]-exp((variables[285]-1.0)*states[1]*1.0e-3*variables[53]/(variables[165]*variables[168]))*pow(variables[159], 3.0)*states[5]*variables[181])/((pow(variables[117], 3.0)+pow(variables[159], 3.0))*(variables[116]+variables[47])*(1.0+variables[119]*exp((variables[285]-1.0)*states[1]*1.0e-3*variables[53]/(variables[165]*variables[168]))));
    variables[660] = variables[165]*variables[168]/variables[53]*log(variables[118]/states[2])*1000.0;
    variables[881] = variables[548]*variables[461]*states[26]*variables[287]*1.0e-6;
    variables[882] = variables[881]*(states[1]-variables[660]);
    variables[854] = variables[578]*(states[1]-variables[661]);
    variables[659] = variables[854]+variables[857]+variables[859]+variables[865]+variables[874]+variables[882]+variables[885]+variables[899]+variables[930];
    rates[1] = 1000000.0*(variables[59]-variables[659])/variables[550];
    variables[875] = -variables[873];
    variables[887] = -3.0*variables[885]/variables[53]*1.0e-9;
    variables[855] = -variables[854]/variables[53]*1.0e-9;
    variables[662] = variables[855]+variables[875]+variables[887];
    rates[3] = variables[662]/variables[549];
    variables[877] = variables[670]-variables[660];
    variables[878] = variables[548]*variables[283]*states[25]*variables[877]/variables[53]*1.0e-9;
    variables[879] = -variables[878];
    variables[883] = -variables[882]/variables[53]*1.0e-9;
    variables[663] = variables[883]+variables[879];
    rates[2] = variables[663]/variables[549];
    variables[664] = variables[25]-states[4];
    rates[4] = variables[326]*variables[664]-variables[304]*states[5]*states[4];
    variables[858] = -variables[857]/(2.0*variables[53])*1.0e-9;
    variables[886] = variables[885]/variables[53]*1.0e-9;
    variables[665] = variables[858]+variables[886];
    rates[5] = variables[665]/variables[549]+variables[326]*variables[664]-variables[304]*states[5]*states[4];
    variables[889] = states[12]*states[6]/variables[57];
    variables[890] = variables[301]*states[29]*variables[548]*(variables[581]/(variables[95]+variables[581])-variables[889]/(variables[95]+variables[889]));
    variables[891] = variables[890];
    variables[880] = variables[878];
    variables[876] = 2.0*variables[873];
    variables[866] = variables[865]/(variables[365]*variables[53])*1.0e-9;
    variables[867] = -variables[365]*variables[866];
    variables[868] = variables[867];
    variables[860] = -variables[859]/variables[53]*1.0e-9;
    variables[668] = variables[860]+variables[868]+variables[876]+variables[880]+variables[898]+variables[929]+variables[891];
    variables[693] = pow(states[9]/variables[236], variables[361])*pow(states[11]/variables[236], variables[361]+1.0)/(states[10]/variables[236]*pow(states[7]/variables[236], variables[361]));
    variables[694] = variables[175]*variables[345]*variables[552]*states[10]/(variables[75]+states[10])*states[7]/(variables[69]+states[7])*(1.0-variables[693]/variables[558]+fabs(1.0-variables[693]/variables[558]))/2.0;
    variables[686] = states[14]/(variables[92]+states[14]);
    variables[689] = variables[177]*variables[474]*variables[552]*(states[10]*states[7]-variables[686]*states[9]*pow(states[11], 2.0)/(variables[236]*variables[556]))/((variables[77]+states[10])*(variables[71]+states[7]))/(1.0+pow(states[14]/variables[39], 8.0));
    variables[681] = variables[173]*states[13]*variables[552]*pow(fabs(states[10]), variables[363]-1.0)*(states[10]-pow(states[12], 2.0)*pow(states[7], 3.0)*variables[236]/(pow(states[9], 3.0)*pow(states[11], 2.0)*variables[554]))/(pow(variables[74], variables[363])+pow(fabs(states[10]), variables[363]))*states[9]/(variables[64]+states[9])*states[11]/(variables[81]+states[11]);
    variables[680] = variables[171]*states[13]*variables[552]*pow(states[9]/variables[64], 2.0)*pow(states[11]/variables[81], 2.0)*(states[8]*states[7]-states[10]*states[9]/variables[553])/(variables[86]*(variables[67]+states[7]))/((1.0+states[8]/variables[86])*pow(1.0+states[9]/variables[64], 2.0)*pow(1.0+states[11]/variables[81], 2.0)+pow(1.0+states[12]/variables[94], 2.0)*pow(1.0+states[7]/variables[66], 2.0)-1.0);
    variables[679] = states[9]/variables[236]*states[11]/variables[236]/(states[7]/variables[236]);
    variables[852] = variables[58]/states[6];
    variables[853] = (1.0-variables[852]+fabs(1.0-variables[852]))/(2.0*(1.0-variables[58]/variables[78]));
    variables[672] = 1.0/(1.0+pow(variables[853], variables[370]));
    variables[850] = (states[8]+fabs(states[8]))/(2.0*variables[37]);
    variables[851] = (1.0-variables[850]+fabs(1.0-variables[850]))/(2.0*(1.0-variables[87]/variables[37]));
    variables[673] = 1.0/(1.0+pow(variables[851], variables[370]));
    variables[848] = (states[10]+fabs(states[10]))/(2.0*variables[30]);
    variables[849] = (1.0-variables[848]+fabs(1.0-variables[848]))/(2.0*(1.0-variables[76]/variables[30]));
    variables[674] = 1.0/(1.0+pow(variables[849], variables[370]));
    variables[846] = (states[7]+fabs(states[7]))/(2.0*variables[28]);
    variables[847] = (1.0-variables[846]+fabs(1.0-variables[846]))/(2.0*(1.0-variables[70]/variables[28]));
    variables[675] = 1.0/(1.0+pow(variables[847], variables[370]));
    variables[676] = (1.0-variables[382]+variables[382]*variables[675])*(1.0-variables[401]+variables[401]*variables[674])*(1.0-variables[439]+variables[439]*variables[673])*(1.0-variables[420]+variables[420]*variables[672]);
    variables[682] = variables[172]*variables[676]*variables[552]*states[7]/variables[68]*(1.0-variables[679]/variables[555])/(1.0+states[7]/variables[68]+(1.0+states[9]/variables[65])*(1.0+states[11]/variables[82])-1.0);
    variables[916] = states[9]*states[11]/variables[237]*exp(variables[259]/(variables[165]*variables[168]));
    variables[918] = variables[297]*states[34]*variables[548]*(variables[32]*pow(states[7]/variables[98], variables[367])-states[8]*pow(variables[916]/variables[98], variables[367]))/((variables[88]+variables[32])*pow(1.0+states[7]/variables[98], variables[367]));
    variables[920] = variables[367]*variables[918];
    variables[921] = -variables[920];
    variables[908] = states[9]/(variables[63]+states[9])*states[11]/(variables[80]+states[11]);
    variables[909] = variables[299]*states[33]*variables[548]*variables[908]*(variables[34]*states[7]-states[10]*states[9]/variables[586])/((variables[99]+states[7])*(variables[90]+variables[34]));
    variables[914] = -variables[909];
    variables[869] = -variables[866];
    variables[717] = states[7]/(variables[97]+states[7])*(variables[97]+variables[8])/variables[8];
    variables[738] = (states[7]-variables[6])/(variables[8]-variables[6]);
    variables[739] = ((variables[738]+fabs(variables[738]))/2.0+1.0-fabs((variables[738]+fabs(variables[738]))/2.0-1.0))/2.0;
    variables[742] = (1.0-variables[381]+variables[381]*variables[675])*(1.0-variables[400]+variables[400]*variables[674])*(1.0-variables[438]+variables[438]*variables[673])*(1.0-variables[419]+variables[419]*variables[672]);
    variables[743] = variables[290]*variables[61]*variables[739]*variables[742];
    variables[744] = variables[743]*variables[717];
    variables[716] = (1.0-variables[398]+variables[398]*variables[675])*(1.0-variables[417]+variables[417]*variables[674])*(1.0-variables[455]+variables[455]*variables[673])*(1.0-variables[436]+variables[436]*variables[672]);
    variables[734] = variables[457]*variables[380]*states[18]/(variables[73]+states[18])*variables[717]*variables[716];
    variables[733] = variables[457]*variables[380]*states[17]/(variables[73]+states[17])*variables[717]*variables[716];
    variables[732] = variables[457]*variables[380]*states[16]/(variables[73]+states[16])*variables[717]*variables[716];
    variables[731] = variables[457]*variables[380]*states[15]/(variables[73]+states[15])*variables[717]*variables[716];
    variables[735] = variables[731]+variables[732]+variables[733]+variables[734];
    variables[737] = variables[735]*variables[45];
    variables[715] = (1.0-variables[399]+variables[399]*variables[675])*(1.0-variables[418]+variables[418]*variables[674])*(1.0-variables[456]+variables[456]*variables[673])*(1.0-variables[437]+variables[437]*variables[672]);
    variables[718] = variables[97]*(variables[8]-states[7])/((variables[97]+states[7])*variables[8])+variables[717]*(1.0-variables[715]);
    variables[719] = (variables[718]+fabs(variables[718]))/2.0;
    variables[720] = 1.0-variables[719];
    variables[722] = variables[567]*variables[201]*variables[321]/variables[137]*variables[720];
    variables[721] = variables[369]*variables[120]*variables[205]*log(2.0)/variables[477]/variables[137]*variables[720];
    variables[753] = 1.0-variables[719];
    variables[756] = variables[324]*(variables[374]-states[20]+fabs(variables[374]-states[20]))/2.0*pow(variables[753], variables[371]);
    variables[758] = variables[756]*variables[571]/variables[155];
    variables[757] = states[20]*variables[136]*variables[206]*variables[325]/variables[155]*variables[753];
    variables[748] = variables[757]+variables[758];
    variables[749] = variables[721]+variables[748]+variables[722]+variables[737]+variables[744];
    variables[750] = -variables[749];
    variables[776] = (states[21]+fabs(states[21]))/2.0/variables[478];
    variables[777] = variables[509]*variables[776]/variables[154];
    variables[1213] = states[7]/(variables[102]+states[7])*(variables[102]+variables[11])/variables[11];
    variables[1214] = (variables[1213]+1.0-fabs(variables[1213]-1.0))/2.0;
    variables[1216] = variables[307]*states[35]*variables[1214];
    variables[1209] = (1.0-variables[385]+variables[385]*variables[675])*(1.0-variables[404]+variables[404]*variables[674])*(1.0-variables[442]+variables[442]*variables[673])*(1.0-variables[423]+variables[423]*variables[672]);
    variables[1210] = variables[632]*variables[1209];
    variables[1215] = (states[35]+variables[1210]-fabs(states[35]-variables[1210]))/2.0;
    variables[759] = (1.0-variables[397]+variables[397]*variables[675])*(1.0-variables[416]+variables[416]*variables[674])*(1.0-variables[454]+variables[454]*variables[673])*(1.0-variables[435]+variables[435]*variables[672]);
    variables[1217] = -variables[307]*variables[1214]*((states[35]-variables[1210]+fabs(states[35]-variables[1210]))/2.0+(1.0-variables[759])*variables[1215]);
    variables[1218] = variables[1216]+variables[1217];
    variables[1219] = variables[349]*variables[123]*((variables[209]+variables[223])*variables[1218]+variables[189]*variables[1216])/variables[142];
    variables[1109] = (1.0-variables[384]+variables[384]*variables[675])*(1.0-variables[403]+variables[403]*variables[674])*(1.0-variables[441]+variables[441]*variables[673])*(1.0-variables[422]+variables[422]*variables[672]);
    variables[1110] = variables[342]*variables[1109];
    variables[1122] = variables[348]*variables[122]*(states[31]-variables[1110]+fabs(states[31]-variables[1110]))/2.0/variables[495];
    variables[1222] = variables[349]*variables[123]*(states[35]-variables[1210]+fabs(states[35]-variables[1210]))/2.0/variables[496];
    variables[1184] = (1.0-variables[383]+variables[383]*variables[675])*(1.0-variables[402]+variables[402]*variables[674])*(1.0-variables[440]+variables[440]*variables[673])*(1.0-variables[421]+variables[421]*variables[672]);
    variables[1185] = variables[629]*variables[1184];
    variables[1197] = variables[347]*variables[121]*(states[34]-variables[1185]+fabs(states[34]-variables[1185]))/2.0/variables[494];
    variables[1159] = (1.0-variables[394]+variables[394]*variables[675])*(1.0-variables[413]+variables[413]*variables[674])*(1.0-variables[451]+variables[451]*variables[673])*(1.0-variables[432]+variables[432]*variables[672]);
    variables[1160] = variables[626]*variables[1159];
    variables[1172] = variables[358]*variables[132]*(states[33]-variables[1160]+fabs(states[33]-variables[1160]))/2.0/variables[505];
    variables[1134] = (1.0-variables[389]+variables[389]*variables[675])*(1.0-variables[408]+variables[408]*variables[674])*(1.0-variables[446]+variables[446]*variables[673])*(1.0-variables[427]+variables[427]*variables[672]);
    variables[1135] = variables[623]*variables[1134];
    variables[1147] = variables[353]*variables[127]*(states[32]-variables[1135]+fabs(states[32]-variables[1135]))/2.0/variables[500];
    variables[1084] = (1.0-variables[391]+variables[391]*variables[675])*(1.0-variables[410]+variables[410]*variables[674])*(1.0-variables[448]+variables[448]*variables[673])*(1.0-variables[429]+variables[429]*variables[672]);
    variables[1085] = variables[618]*variables[1084];
    variables[1097] = variables[355]*variables[129]*(states[29]-variables[1085]+fabs(states[29]-variables[1085]))/2.0/variables[502];
    variables[1059] = (1.0-variables[386]+variables[386]*variables[675])*(1.0-variables[405]+variables[405]*variables[674])*(1.0-variables[443]+variables[443]*variables[673])*(1.0-variables[424]+variables[424]*variables[672]);
    variables[1060] = variables[615]*variables[1059];
    variables[1072] = variables[350]*variables[124]*(states[28]-variables[1060]+fabs(states[28]-variables[1060]))/2.0/variables[497];
    variables[1034] = (1.0-variables[392]+variables[392]*variables[675])*(1.0-variables[411]+variables[411]*variables[674])*(1.0-variables[449]+variables[449]*variables[673])*(1.0-variables[430]+variables[430]*variables[672]);
    variables[1035] = variables[612]*variables[1034];
    variables[1047] = variables[356]*variables[130]*(states[27]-variables[1035]+fabs(states[27]-variables[1035]))/2.0/variables[503];
    variables[1009] = (1.0-variables[395]+variables[395]*variables[675])*(1.0-variables[414]+variables[414]*variables[674])*(1.0-variables[452]+variables[452]*variables[673])*(1.0-variables[433]+variables[433]*variables[672]);
    variables[1010] = variables[609]*variables[1009];
    variables[1022] = variables[359]*variables[133]*(states[26]-variables[1010]+fabs(states[26]-variables[1010]))/2.0/variables[506];
    variables[984] = (1.0-variables[390]+variables[390]*variables[675])*(1.0-variables[409]+variables[409]*variables[674])*(1.0-variables[447]+variables[447]*variables[673])*(1.0-variables[428]+variables[428]*variables[672]);
    variables[985] = variables[606]*variables[984];
    variables[997] = variables[354]*variables[128]*(states[25]-variables[985]+fabs(states[25]-variables[985]))/2.0/variables[501];
    variables[959] = (1.0-variables[393]+variables[393]*variables[675])*(1.0-variables[412]+variables[412]*variables[674])*(1.0-variables[450]+variables[450]*variables[673])*(1.0-variables[431]+variables[431]*variables[672]);
    variables[960] = variables[603]*variables[959];
    variables[972] = variables[357]*variables[131]*(states[24]-variables[960]+fabs(states[24]-variables[960]))/2.0/variables[504];
    variables[934] = (1.0-variables[388]+variables[388]*variables[675])*(1.0-variables[407]+variables[407]*variables[674])*(1.0-variables[445]+variables[445]*variables[673])*(1.0-variables[426]+variables[426]*variables[672]);
    variables[935] = variables[600]*variables[934];
    variables[947] = variables[352]*variables[126]*(states[23]-variables[935]+fabs(states[23]-variables[935]))/2.0/variables[499];
    variables[822] = (1.0-variables[387]+variables[387]*variables[675])*(1.0-variables[406]+variables[406]*variables[674])*(1.0-variables[444]+variables[444]*variables[673])*(1.0-variables[425]+variables[425]*variables[672]);
    variables[823] = variables[343]*variables[822];
    variables[835] = variables[351]*variables[125]*(states[13]-variables[823]+fabs(states[13]-variables[823]))/2.0/variables[498];
    variables[797] = (1.0-variables[396]+variables[396]*variables[675])*(1.0-variables[415]+variables[415]*variables[674])*(1.0-variables[453]+variables[453]*variables[673])*(1.0-variables[434]+variables[434]*variables[672]);
    variables[798] = variables[344]*variables[797];
    variables[810] = variables[360]*variables[134]*(states[22]-variables[798]+fabs(states[22]-variables[798]))/2.0/variables[507];
    variables[764] = variables[810]+variables[835]+variables[947]+variables[972]+variables[997]+variables[1022]+variables[1047]+variables[1072]+variables[1097]+variables[1122]+variables[1147]+variables[1172]+variables[1197]+variables[1222];
    variables[765] = variables[764];
    variables[774] = (variables[164]+variables[163])/((variables[164]+variables[765]+fabs(variables[164]-variables[765]))/2.0+variables[163]);
    variables[1223] = (states[35]-variables[1210]+fabs(states[35]-variables[1210]))/2.0/variables[496]*variables[774]*variables[1214];
    variables[1224] = variables[189]*variables[349]*variables[123]*variables[1223]/variables[142];
    variables[775] = (states[21]+fabs(states[21]))/2.0/(variables[84]+(states[21]+fabs(states[21]))/2.0);
    variables[1211] = (variables[1210]-states[35])/variables[482];
    variables[1212] = (variables[1211]+fabs(variables[1211]))/2.0;
    variables[787] = (states[7]-variables[7])/(variables[23]-variables[7]);
    variables[788] = ((variables[787]+fabs(variables[787]))/2.0+1.0-fabs((variables[787]+fabs(variables[787]))/2.0-1.0))/2.0;
    variables[754] = states[20]/variables[374];
    variables[789] = variables[185]*variables[754];
    variables[790] = variables[789]*variables[62]*variables[788]*variables[759];
    variables[1111] = (variables[1110]-states[31])/variables[481];
    variables[1112] = (variables[1111]+fabs(variables[1111]))/2.0;
    variables[1120] = variables[1112]*variables[348]*variables[122]*(variables[242]+variables[208])/variables[141];
    variables[1220] = variables[1212]*variables[349]*variables[123]*(variables[243]+variables[209])/variables[142];
    variables[1186] = (variables[1185]-states[34])/variables[480];
    variables[1187] = (variables[1186]+fabs(variables[1186]))/2.0;
    variables[1195] = variables[1187]*variables[347]*variables[121]*(variables[241]+variables[207])/variables[140];
    variables[1161] = (variables[1160]-states[33])/variables[491];
    variables[1162] = (variables[1161]+fabs(variables[1161]))/2.0;
    variables[1170] = variables[1162]*variables[358]*variables[132]*(variables[252]+variables[218])/variables[151];
    variables[1136] = (variables[1135]-states[32])/variables[486];
    variables[1137] = (variables[1136]+fabs(variables[1136]))/2.0;
    variables[1145] = variables[1137]*variables[353]*variables[127]*(variables[247]+variables[213])/variables[146];
    variables[1086] = (variables[1085]-states[29])/variables[488];
    variables[1087] = (variables[1086]+fabs(variables[1086]))/2.0;
    variables[1095] = variables[1087]*variables[355]*variables[129]*(variables[249]+variables[215])/variables[148];
    variables[1061] = (variables[1060]-states[28])/variables[483];
    variables[1062] = (variables[1061]+fabs(variables[1061]))/2.0;
    variables[1070] = variables[1062]*variables[350]*variables[124]*(variables[244]+variables[210])/variables[143];
    variables[1036] = (variables[1035]-states[27])/variables[489];
    variables[1037] = (variables[1036]+fabs(variables[1036]))/2.0;
    variables[1045] = variables[1037]*variables[356]*variables[130]*(variables[250]+variables[216])/variables[149];
    variables[1011] = (variables[1010]-states[26])/variables[492];
    variables[1012] = (variables[1011]+fabs(variables[1011]))/2.0;
    variables[1020] = variables[1012]*variables[359]*variables[133]*(variables[253]+variables[219])/variables[152];
    variables[986] = (variables[985]-states[25])/variables[487];
    variables[987] = (variables[986]+fabs(variables[986]))/2.0;
    variables[995] = variables[987]*variables[354]*variables[128]*(variables[248]+variables[214])/variables[147];
    variables[961] = (variables[960]-states[24])/variables[490];
    variables[962] = (variables[961]+fabs(variables[961]))/2.0;
    variables[970] = variables[962]*variables[357]*variables[131]*(variables[251]+variables[217])/variables[150];
    variables[936] = (variables[935]-states[23])/variables[485];
    variables[937] = (variables[936]+fabs(variables[936]))/2.0;
    variables[945] = variables[937]*variables[352]*variables[126]*(variables[246]+variables[212])/variables[145];
    variables[824] = (variables[823]-states[13])/variables[484];
    variables[825] = (variables[824]+fabs(variables[824]))/2.0;
    variables[833] = variables[825]*variables[351]*variables[125]*(variables[245]+variables[211])/variables[144];
    variables[799] = (variables[798]-states[22])/variables[493];
    variables[800] = (variables[799]+fabs(variables[799]))/2.0;
    variables[808] = variables[800]*variables[360]*variables[134]*(variables[254]+variables[220])/variables[153];
    variables[760] = variables[808]+variables[833]+variables[945]+variables[970]+variables[995]+variables[1020]+variables[1045]+variables[1070]+variables[1095]+variables[1120]+variables[1145]+variables[1170]+variables[1195]+variables[1220];
    variables[761] = variables[760];
    variables[791] = (variables[790]+variables[60])/((variables[790]+variables[761]+fabs(variables[790]-variables[761]))/2.0+variables[60]);
    variables[1221] = variables[1212]*variables[791]*variables[1214];
    variables[1226] = variables[349]*variables[123]*variables[775]*variables[1221];
    variables[1227] = (variables[243]-variables[223])*variables[1226]/variables[142];
    variables[1228] = variables[1221]*variables[349]*variables[123]*(variables[243]+variables[209])/variables[142]-variables[1227];
    variables[1229] = variables[1219]+variables[1228]+variables[1224];
    variables[1188] = states[7]/(variables[100]+states[7])*(variables[100]+variables[9])/variables[9];
    variables[1189] = (variables[1188]+1.0-fabs(variables[1188]-1.0))/2.0;
    variables[1191] = variables[305]*states[34]*variables[1189];
    variables[1190] = (states[34]+variables[1185]-fabs(states[34]-variables[1185]))/2.0;
    variables[1192] = -variables[305]*variables[1189]*((states[34]-variables[1185]+fabs(states[34]-variables[1185]))/2.0+(1.0-variables[759])*variables[1190]);
    variables[1193] = variables[1191]+variables[1192];
    variables[1194] = variables[347]*variables[121]*((variables[207]+variables[221])*variables[1193]+variables[187]*variables[1191])/variables[140];
    variables[1198] = (states[34]-variables[1185]+fabs(states[34]-variables[1185]))/2.0/variables[494]*variables[774]*variables[1189];
    variables[1199] = variables[187]*variables[347]*variables[121]*variables[1198]/variables[140];
    variables[1196] = variables[1187]*variables[791]*variables[1189];
    variables[1201] = variables[347]*variables[121]*variables[775]*variables[1196];
    variables[1202] = (variables[241]-variables[221])*variables[1201]/variables[140];
    variables[1203] = variables[1196]*variables[347]*variables[121]*(variables[241]+variables[207])/variables[140]-variables[1202];
    variables[1204] = variables[1194]+variables[1203]+variables[1199];
    variables[1163] = states[7]/(variables[111]+states[7])*(variables[111]+variables[20])/variables[20];
    variables[1164] = (variables[1163]+1.0-fabs(variables[1163]-1.0))/2.0;
    variables[1166] = variables[316]*states[33]*variables[1164];
    variables[1165] = (states[33]+variables[1160]-fabs(states[33]-variables[1160]))/2.0;
    variables[1167] = -variables[316]*variables[1164]*((states[33]-variables[1160]+fabs(states[33]-variables[1160]))/2.0+(1.0-variables[759])*variables[1165]);
    variables[1168] = variables[1166]+variables[1167];
    variables[1169] = variables[358]*variables[132]*((variables[218]+variables[232])*variables[1168]+variables[198]*variables[1166])/variables[151];
    variables[1173] = (states[33]-variables[1160]+fabs(states[33]-variables[1160]))/2.0/variables[505]*variables[774]*variables[1164];
    variables[1174] = variables[198]*variables[358]*variables[132]*variables[1173]/variables[151];
    variables[1171] = variables[1162]*variables[791]*variables[1164];
    variables[1176] = variables[358]*variables[132]*variables[775]*variables[1171];
    variables[1177] = (variables[252]-variables[232])*variables[1176]/variables[151];
    variables[1178] = variables[1171]*variables[358]*variables[132]*(variables[252]+variables[218])/variables[151]-variables[1177];
    variables[1179] = variables[1169]+variables[1178]+variables[1174];
    variables[1138] = states[7]/(variables[106]+states[7])*(variables[106]+variables[15])/variables[15];
    variables[1139] = (variables[1138]+1.0-fabs(variables[1138]-1.0))/2.0;
    variables[1141] = variables[311]*states[32]*variables[1139];
    variables[1140] = (states[32]+variables[1135]-fabs(states[32]-variables[1135]))/2.0;
    variables[1142] = -variables[311]*variables[1139]*((states[32]-variables[1135]+fabs(states[32]-variables[1135]))/2.0+(1.0-variables[759])*variables[1140]);
    variables[1143] = variables[1141]+variables[1142];
    variables[1144] = variables[353]*variables[127]*((variables[213]+variables[227])*variables[1143]+variables[193]*variables[1141])/variables[146];
    variables[1148] = (states[32]-variables[1135]+fabs(states[32]-variables[1135]))/2.0/variables[500]*variables[774]*variables[1139];
    variables[1149] = variables[193]*variables[353]*variables[127]*variables[1148]/variables[146];
    variables[1146] = variables[1137]*variables[791]*variables[1139];
    variables[1151] = variables[353]*variables[127]*variables[775]*variables[1146];
    variables[1152] = (variables[247]-variables[227])*variables[1151]/variables[146];
    variables[1153] = variables[1146]*variables[353]*variables[127]*(variables[247]+variables[213])/variables[146]-variables[1152];
    variables[1154] = variables[1144]+variables[1153]+variables[1149];
    variables[1113] = states[7]/(variables[101]+states[7])*(variables[101]+variables[10])/variables[10];
    variables[1114] = (variables[1113]+1.0-fabs(variables[1113]-1.0))/2.0;
    variables[1116] = variables[306]*states[31]*variables[1114];
    variables[1115] = (states[31]+variables[1110]-fabs(states[31]-variables[1110]))/2.0;
    variables[1117] = -variables[306]*variables[1114]*((states[31]-variables[1110]+fabs(states[31]-variables[1110]))/2.0+(1.0-variables[759])*variables[1115]);
    variables[1118] = variables[1116]+variables[1117];
    variables[1119] = variables[348]*variables[122]*((variables[208]+variables[222])*variables[1118]+variables[188]*variables[1116])/variables[141];
    variables[1123] = (states[31]-variables[1110]+fabs(states[31]-variables[1110]))/2.0/variables[495]*variables[774]*variables[1114];
    variables[1124] = variables[188]*variables[348]*variables[122]*variables[1123]/variables[141];
    variables[1121] = variables[1112]*variables[791]*variables[1114];
    variables[1126] = variables[348]*variables[122]*variables[775]*variables[1121];
    variables[1127] = (variables[242]-variables[222])*variables[1126]/variables[141];
    variables[1128] = variables[1121]*variables[348]*variables[122]*(variables[242]+variables[208])/variables[141]-variables[1127];
    variables[1129] = variables[1119]+variables[1128]+variables[1124];
    variables[1088] = states[7]/(variables[108]+states[7])*(variables[108]+variables[17])/variables[17];
    variables[1089] = (variables[1088]+1.0-fabs(variables[1088]-1.0))/2.0;
    variables[1091] = variables[313]*states[29]*variables[1089];
    variables[1090] = (states[29]+variables[1085]-fabs(states[29]-variables[1085]))/2.0;
    variables[1092] = -variables[313]*variables[1089]*((states[29]-variables[1085]+fabs(states[29]-variables[1085]))/2.0+(1.0-variables[759])*variables[1090]);
    variables[1093] = variables[1091]+variables[1092];
    variables[1094] = variables[355]*variables[129]*((variables[215]+variables[229])*variables[1093]+variables[195]*variables[1091])/variables[148];
    variables[1098] = (states[29]-variables[1085]+fabs(states[29]-variables[1085]))/2.0/variables[502]*variables[774]*variables[1089];
    variables[1099] = variables[195]*variables[355]*variables[129]*variables[1098]/variables[148];
    variables[1096] = variables[1087]*variables[791]*variables[1089];
    variables[1101] = variables[355]*variables[129]*variables[775]*variables[1096];
    variables[1102] = (variables[249]-variables[229])*variables[1101]/variables[148];
    variables[1103] = variables[1096]*variables[355]*variables[129]*(variables[249]+variables[215])/variables[148]-variables[1102];
    variables[1104] = variables[1094]+variables[1103]+variables[1099];
    variables[1063] = states[7]/(variables[103]+states[7])*(variables[103]+variables[12])/variables[12];
    variables[1064] = (variables[1063]+1.0-fabs(variables[1063]-1.0))/2.0;
    variables[1066] = variables[308]*states[28]*variables[1064];
    variables[1065] = (states[28]+variables[1060]-fabs(states[28]-variables[1060]))/2.0;
    variables[1067] = -variables[308]*variables[1064]*((states[28]-variables[1060]+fabs(states[28]-variables[1060]))/2.0+(1.0-variables[759])*variables[1065]);
    variables[1068] = variables[1066]+variables[1067];
    variables[1069] = variables[350]*variables[124]*((variables[210]+variables[224])*variables[1068]+variables[190]*variables[1066])/variables[143];
    variables[1073] = (states[28]-variables[1060]+fabs(states[28]-variables[1060]))/2.0/variables[497]*variables[774]*variables[1064];
    variables[1074] = variables[190]*variables[350]*variables[124]*variables[1073]/variables[143];
    variables[1071] = variables[1062]*variables[791]*variables[1064];
    variables[1076] = variables[350]*variables[124]*variables[775]*variables[1071];
    variables[1077] = (variables[244]-variables[224])*variables[1076]/variables[143];
    variables[1078] = variables[1071]*variables[350]*variables[124]*(variables[244]+variables[210])/variables[143]-variables[1077];
    variables[1079] = variables[1069]+variables[1078]+variables[1074];
    variables[1038] = states[7]/(variables[109]+states[7])*(variables[109]+variables[18])/variables[18];
    variables[1039] = (variables[1038]+1.0-fabs(variables[1038]-1.0))/2.0;
    variables[1041] = variables[314]*states[27]*variables[1039];
    variables[1040] = (states[27]+variables[1035]-fabs(states[27]-variables[1035]))/2.0;
    variables[1042] = -variables[314]*variables[1039]*((states[27]-variables[1035]+fabs(states[27]-variables[1035]))/2.0+(1.0-variables[759])*variables[1040]);
    variables[1043] = variables[1041]+variables[1042];
    variables[1044] = variables[356]*variables[130]*((variables[216]+variables[230])*variables[1043]+variables[196]*variables[1041])/variables[149];
    variables[1048] = (states[27]-variables[1035]+fabs(states[27]-variables[1035]))/2.0/variables[503]*variables[774]*variables[1039];
    variables[1049] = variables[196]*variables[356]*variables[130]*variables[1048]/variables[149];
    variables[1046] = variables[1037]*variables[791]*variables[1039];
    variables[1051] = variables[356]*variables[130]*variables[775]*variables[1046];
    variables[1052] = (variables[250]-variables[230])*variables[1051]/variables[149];
    variables[1053] = variables[1046]*variables[356]*variables[130]*(variables[250]+variables[216])/variables[149]-variables[1052];
    variables[1054] = variables[1044]+variables[1053]+variables[1049];
    variables[1013] = states[7]/(variables[112]+states[7])*(variables[112]+variables[21])/variables[21];
    variables[1014] = (variables[1013]+1.0-fabs(variables[1013]-1.0))/2.0;
    variables[1016] = variables[317]*states[26]*variables[1014];
    variables[1015] = (states[26]+variables[1010]-fabs(states[26]-variables[1010]))/2.0;
    variables[1017] = -variables[317]*variables[1014]*((states[26]-variables[1010]+fabs(states[26]-variables[1010]))/2.0+(1.0-variables[759])*variables[1015]);
    variables[1018] = variables[1016]+variables[1017];
    variables[1019] = variables[359]*variables[133]*((variables[219]+variables[233])*variables[1018]+variables[199]*variables[1016])/variables[152];
    variables[1023] = (states[26]-variables[1010]+fabs(states[26]-variables[1010]))/2.0/variables[506]*variables[774]*variables[1014];
    variables[1024] = variables[199]*variables[359]*variables[133]*variables[1023]/variables[152];
    variables[1021] = variables[1012]*variables[791]*variables[1014];
    variables[1026] = variables[359]*variables[133]*variables[775]*variables[1021];
    variables[1027] = (variables[253]-variables[233])*variables[1026]/variables[152];
    variables[1028] = variables[1021]*variables[359]*variables[133]*(variables[253]+variables[219])/variables[152]-variables[1027];
    variables[1029] = variables[1019]+variables[1028]+variables[1024];
    variables[988] = states[7]/(variables[107]+states[7])*(variables[107]+variables[16])/variables[16];
    variables[989] = (variables[988]+1.0-fabs(variables[988]-1.0))/2.0;
    variables[991] = variables[312]*states[25]*variables[989];
    variables[990] = (states[25]+variables[985]-fabs(states[25]-variables[985]))/2.0;
    variables[992] = -variables[312]*variables[989]*((states[25]-variables[985]+fabs(states[25]-variables[985]))/2.0+(1.0-variables[759])*variables[990]);
    variables[993] = variables[991]+variables[992];
    variables[994] = variables[354]*variables[128]*((variables[214]+variables[228])*variables[993]+variables[194]*variables[991])/variables[147];
    variables[998] = (states[25]-variables[985]+fabs(states[25]-variables[985]))/2.0/variables[501]*variables[774]*variables[989];
    variables[999] = variables[194]*variables[354]*variables[128]*variables[998]/variables[147];
    variables[996] = variables[987]*variables[791]*variables[989];
    variables[1001] = variables[354]*variables[128]*variables[775]*variables[996];
    variables[1002] = (variables[248]-variables[228])*variables[1001]/variables[147];
    variables[1003] = variables[996]*variables[354]*variables[128]*(variables[248]+variables[214])/variables[147]-variables[1002];
    variables[1004] = variables[994]+variables[1003]+variables[999];
    variables[963] = states[7]/(variables[110]+states[7])*(variables[110]+variables[19])/variables[19];
    variables[964] = (variables[963]+1.0-fabs(variables[963]-1.0))/2.0;
    variables[966] = variables[315]*states[24]*variables[964];
    variables[965] = (states[24]+variables[960]-fabs(states[24]-variables[960]))/2.0;
    variables[967] = -variables[315]*variables[964]*((states[24]-variables[960]+fabs(states[24]-variables[960]))/2.0+(1.0-variables[759])*variables[965]);
    variables[968] = variables[966]+variables[967];
    variables[969] = variables[357]*variables[131]*((variables[217]+variables[231])*variables[968]+variables[197]*variables[966])/variables[150];
    variables[973] = (states[24]-variables[960]+fabs(states[24]-variables[960]))/2.0/variables[504]*variables[774]*variables[964];
    variables[974] = variables[197]*variables[357]*variables[131]*variables[973]/variables[150];
    variables[971] = variables[962]*variables[791]*variables[964];
    variables[976] = variables[357]*variables[131]*variables[775]*variables[971];
    variables[977] = (variables[251]-variables[231])*variables[976]/variables[150];
    variables[978] = variables[971]*variables[357]*variables[131]*(variables[251]+variables[217])/variables[150]-variables[977];
    variables[979] = variables[969]+variables[978]+variables[974];
    variables[938] = states[7]/(variables[105]+states[7])*(variables[105]+variables[14])/variables[14];
    variables[939] = (variables[938]+1.0-fabs(variables[938]-1.0))/2.0;
    variables[941] = variables[310]*states[23]*variables[939];
    variables[940] = (states[23]+variables[935]-fabs(states[23]-variables[935]))/2.0;
    variables[942] = -variables[310]*variables[939]*((states[23]-variables[935]+fabs(states[23]-variables[935]))/2.0+(1.0-variables[759])*variables[940]);
    variables[943] = variables[941]+variables[942];
    variables[944] = variables[352]*variables[126]*((variables[212]+variables[226])*variables[943]+variables[192]*variables[941])/variables[145];
    variables[948] = (states[23]-variables[935]+fabs(states[23]-variables[935]))/2.0/variables[499]*variables[774]*variables[939];
    variables[949] = variables[192]*variables[352]*variables[126]*variables[948]/variables[145];
    variables[946] = variables[937]*variables[791]*variables[939];
    variables[951] = variables[352]*variables[126]*variables[775]*variables[946];
    variables[952] = (variables[246]-variables[226])*variables[951]/variables[145];
    variables[953] = variables[946]*variables[352]*variables[126]*(variables[246]+variables[212])/variables[145]-variables[952];
    variables[954] = variables[944]+variables[953]+variables[949];
    variables[826] = states[7]/(variables[104]+states[7])*(variables[104]+variables[13])/variables[13];
    variables[827] = (variables[826]+1.0-fabs(variables[826]-1.0))/2.0;
    variables[829] = variables[309]*states[13]*variables[827];
    variables[828] = (states[13]+variables[823]-fabs(states[13]-variables[823]))/2.0;
    variables[830] = -variables[309]*variables[827]*((states[13]-variables[823]+fabs(states[13]-variables[823]))/2.0+(1.0-variables[759])*variables[828]);
    variables[831] = variables[829]+variables[830];
    variables[832] = variables[351]*variables[125]*((variables[211]+variables[225])*variables[831]+variables[191]*variables[829])/variables[144];
    variables[836] = (states[13]-variables[823]+fabs(states[13]-variables[823]))/2.0/variables[498]*variables[774]*variables[827];
    variables[837] = variables[191]*variables[351]*variables[125]*variables[836]/variables[144];
    variables[834] = variables[825]*variables[791]*variables[827];
    variables[839] = variables[351]*variables[125]*variables[775]*variables[834];
    variables[840] = (variables[245]-variables[225])*variables[839]/variables[144];
    variables[841] = variables[834]*variables[351]*variables[125]*(variables[245]+variables[211])/variables[144]-variables[840];
    variables[842] = variables[832]+variables[841]+variables[837];
    variables[801] = states[7]/(variables[113]+states[7])*(variables[113]+variables[22])/variables[22];
    variables[802] = (variables[801]+1.0-fabs(variables[801]-1.0))/2.0;
    variables[804] = variables[318]*states[22]*variables[802];
    variables[803] = (states[22]+variables[798]-fabs(states[22]-variables[798]))/2.0;
    variables[805] = -variables[318]*variables[802]*((states[22]-variables[798]+fabs(states[22]-variables[798]))/2.0+(1.0-variables[759])*variables[803]);
    variables[806] = variables[804]+variables[805];
    variables[807] = variables[360]*variables[134]*((variables[220]+variables[234])*variables[806]+variables[200]*variables[804])/variables[153];
    variables[811] = (states[22]-variables[798]+fabs(states[22]-variables[798]))/2.0/variables[507]*variables[774]*variables[802];
    variables[812] = variables[200]*variables[360]*variables[134]*variables[811]/variables[153];
    variables[809] = variables[800]*variables[791]*variables[802];
    variables[814] = variables[360]*variables[134]*variables[775]*variables[809];
    variables[815] = (variables[254]-variables[234])*variables[814]/variables[153];
    variables[816] = variables[809]*variables[360]*variables[134]*(variables[254]+variables[220])/variables[153]-variables[815];
    variables[817] = variables[807]+variables[816]+variables[812];
    variables[779] = variables[817]+variables[842]+variables[954]+variables[979]+variables[1004]+variables[1029]+variables[1054]+variables[1079]+variables[1104]+variables[1129]+variables[1154]+variables[1179]+variables[1204]+variables[1229];
    variables[780] = variables[779]-variables[777];
    variables[793] = -variables[780];
    variables[697] = variables[793]+variables[750]+variables[869]+variables[914]+variables[921];
    variables[699] = variables[549]*(variables[682]+variables[680]-variables[681]+variables[689]+variables[361]*variables[694])-variables[697];
    variables[667] = variables[699];
    rates[6] = (variables[668]+variables[667])/variables[549]/(1.0+variables[184]/(log(10.0)*states[6]));
    variables[932] = variables[927];
    variables[925] = variables[918];
    variables[888] = variables[300]*states[28]*variables[548]*(variables[35]/(variables[91]+variables[35])-states[8]/(variables[91]+states[8]));
    variables[894] = variables[888];
    variables[703] = variables[894]+variables[925]+variables[932];
    rates[8] = (variables[703]+variables[559])/variables[549]-variables[680];
    variables[690] = variables[174]*variables[552]*states[14]/(variables[92]+states[14])*(states[11]-states[10]/variables[557])/(variables[83]+states[11])*variables[93]/(variables[93]+states[7]);
    variables[912] = variables[909];
    variables[700] = variables[912];
    rates[10] = variables[700]/variables[549]+variables[680]+variables[690]-variables[681]-variables[689]-variables[694];
    variables[895] = variables[890];
    variables[704] = variables[895]+variables[590]+variables[595];
    rates[12] = (variables[704]+variables[560])/variables[549]+2.0*variables[681];
    rates[7] = (variables[562]+variables[561]+variables[697])/variables[549]-variables[680]+3.0*variables[681]-variables[682]-variables[689]-variables[361]*variables[694];
    variables[922] = variables[920];
    variables[915] = variables[909];
    variables[870] = variables[866];
    variables[751] = variables[749];
    variables[794] = variables[780];
    variables[705] = variables[794]+variables[751]+variables[870]+variables[915]+variables[922];
    rates[9] = (variables[564]+variables[563]+variables[705])/variables[549]+variables[680]-3.0*variables[681]+variables[682]+variables[689]+variables[361]*variables[694];
    variables[923] = variables[920];
    variables[871] = variables[866];
    variables[752] = variables[749];
    variables[795] = variables[780];
    variables[701] = variables[795]+variables[752]+variables[871]+variables[588]+variables[923];
    rates[11] = (variables[566]+variables[565]+variables[701])/variables[549]-2.0*variables[681]+variables[682]+2.0*variables[689]-variables[690]+(variables[361]+1.0)*variables[694];
    rates[14] = variables[689]-variables[690];
    variables[726] = variables[320]*variables[719];
    variables[727] = variables[379]+variables[726];
    rates[15] = variables[727]-variables[731];
    rates[16] = variables[569]-variables[732];
    variables[724] = pow((1.0-states[2]/variables[79]+fabs(1.0-states[2]/variables[79]))/2.0, 2.0);
    variables[723] = pow(states[5], variables[362])/(pow(variables[72], variables[362])+pow(states[5], variables[362]));
    variables[728] = variables[322]*(1.0-variables[676]);
    variables[729] = variables[378]*variables[568]+variables[302]*variables[723]+variables[303]*variables[724]+variables[728];
    rates[17] = variables[729]-variables[733];
    variables[725] = pow((1.0-states[7]/variables[5]+fabs(1.0-states[7]/variables[5]))/2.0, 2.0);
    variables[730] = variables[319]*variables[725];
    rates[18] = variables[730]-variables[734];
    rates[19] = variables[744];
    variables[755] = variables[325]*states[20]*variables[719];
    rates[20] = variables[756]-variables[755];
    variables[768] = variables[814]+variables[839]+variables[951]+variables[976]+variables[1001]+variables[1026]+variables[1051]+variables[1076]+variables[1101]+variables[1126]+variables[1151]+variables[1176]+variables[1201]+variables[1226];
    variables[1225] = variables[349]*variables[123]*(variables[1223]-variables[1217]);
    variables[1200] = variables[347]*variables[121]*(variables[1198]-variables[1192]);
    variables[1175] = variables[358]*variables[132]*(variables[1173]-variables[1167]);
    variables[1150] = variables[353]*variables[127]*(variables[1148]-variables[1142]);
    variables[1125] = variables[348]*variables[122]*(variables[1123]-variables[1117]);
    variables[1100] = variables[355]*variables[129]*(variables[1098]-variables[1092]);
    variables[1075] = variables[350]*variables[124]*(variables[1073]-variables[1067]);
    variables[1050] = variables[356]*variables[130]*(variables[1048]-variables[1042]);
    variables[1025] = variables[359]*variables[133]*(variables[1023]-variables[1017]);
    variables[1000] = variables[354]*variables[128]*(variables[998]-variables[992]);
    variables[975] = variables[357]*variables[131]*(variables[973]-variables[967]);
    variables[950] = variables[352]*variables[126]*(variables[948]-variables[942]);
    variables[838] = variables[351]*variables[125]*(variables[836]-variables[830]);
    variables[813] = variables[360]*variables[134]*(variables[811]-variables[805]);
    variables[766] = variables[813]+variables[838]+variables[950]+variables[975]+variables[1000]+variables[1025]+variables[1050]+variables[1075]+variables[1100]+variables[1125]+variables[1150]+variables[1175]+variables[1200]+variables[1225];
    rates[21] = variables[766]-variables[768]-variables[776];
    rates[22] = variables[809]+variables[805]-variables[811];
    rates[13] = variables[834]+variables[830]-variables[836];
    rates[30] = (variables[179]-states[30])/variables[479];
    rates[23] = variables[946]+variables[942]-variables[948];
    rates[24] = variables[971]+variables[967]-variables[973];
    rates[25] = variables[996]+variables[992]-variables[998];
    rates[26] = variables[1021]+variables[1017]-variables[1023];
    rates[27] = variables[1046]+variables[1042]-variables[1048];
    rates[28] = variables[1071]+variables[1067]-variables[1073];
    rates[29] = variables[1096]+variables[1092]-variables[1098];
    rates[31] = variables[1121]+variables[1117]-variables[1123];
    rates[32] = variables[1146]+variables[1142]-variables[1148];
    rates[33] = variables[1171]+variables[1167]-variables[1173];
    rates[34] = variables[1196]+variables[1192]-variables[1198];
    rates[35] = variables[1221]+variables[1217]-variables[1223];
    variables[1235] = variables[519]*states[36];
    rates[38] = (variables[635]+variables[634]+variables[523])/variables[515]+variables[520]*variables[1235];
    rates[39] = (variables[637]+variables[636]+variables[524])/variables[515];
    rates[37] = (variables[639]+variables[638]+variables[525])/(variables[515]*(1.0+variables[516]));
    rates[40] = (variables[641]+variables[640]+variables[526])/variables[515];
    variables[1236] = variables[522]*states[36]*pow(states[36]/variables[521], 8.0);
    rates[36] = variables[527]/variables[515]-variables[1235]-variables[1236];
    variables[1237] = variables[515]*((1.0-variables[520])*variables[1235]+variables[1236]);
    rates[41] = variables[1237];
}

void computeVariables(double voi, double *states, double *rates, double *variables)
{
    variables[658] = variables[1]*states[0]/(states[0]+variables[2]);
    variables[660] = variables[165]*variables[168]/variables[53]*log(variables[118]/states[2])*1000.0;
    variables[661] = variables[165]*variables[168]/variables[53]*log(variables[158]/states[3])*1000.0;
    variables[664] = variables[25]-states[4];
    variables[666] = states[5]+variables[664];
    variables[669] = -log(states[6]/variables[235])/log(10.0);
    variables[670] = variables[165]*variables[168]/variables[53]*log(variables[56]/states[6])*1000.0;
    variables[671] = states[6]-variables[184]*variables[669];
    variables[852] = variables[58]/states[6];
    variables[853] = (1.0-variables[852]+fabs(1.0-variables[852]))/(2.0*(1.0-variables[58]/variables[78]));
    variables[672] = 1.0/(1.0+pow(variables[853], variables[370]));
    variables[850] = (states[8]+fabs(states[8]))/(2.0*variables[37]);
    variables[851] = (1.0-variables[850]+fabs(1.0-variables[850]))/(2.0*(1.0-variables[87]/variables[37]));
    variables[673] = 1.0/(1.0+pow(variables[851], variables[370]));
    variables[848] = (states[10]+fabs(states[10]))/(2.0*variables[30]);
    variables[849] = (1.0-variables[848]+fabs(1.0-variables[848]))/(2.0*(1.0-variables[76]/variables[30]));
    variables[674] = 1.0/(1.0+pow(variables[849], variables[370]));
    variables[846] = (states[7]+fabs(states[7]))/(2.0*variables[28]);
    variables[847] = (1.0-variables[846]+fabs(1.0-variables[846]))/(2.0*(1.0-variables[70]/variables[28]));
    variables[675] = 1.0/(1.0+pow(variables[847], variables[370]));
    variables[676] = (1.0-variables[382]+variables[382]*variables[675])*(1.0-variables[401]+variables[401]*variables[674])*(1.0-variables[439]+variables[439]*variables[673])*(1.0-variables[420]+variables[420]*variables[672]);
    variables[677] = states[10]/variables[236]*states[9]/variables[236]/(states[8]/variables[236]*states[7]/variables[236]);
    variables[678] = pow(states[12]/variables[236], 2.0)*pow(states[7]/variables[236], 3.0)/(states[10]/variables[236]*pow(states[9]/variables[236], 3.0)*pow(states[11]/variables[236], 2.0));
    variables[679] = states[9]/variables[236]*states[11]/variables[236]/(states[7]/variables[236]);
    variables[680] = variables[171]*states[13]*variables[552]*pow(states[9]/variables[64], 2.0)*pow(states[11]/variables[81], 2.0)*(states[8]*states[7]-states[10]*states[9]/variables[553])/(variables[86]*(variables[67]+states[7]))/((1.0+states[8]/variables[86])*pow(1.0+states[9]/variables[64], 2.0)*pow(1.0+states[11]/variables[81], 2.0)+pow(1.0+states[12]/variables[94], 2.0)*pow(1.0+states[7]/variables[66], 2.0)-1.0);
    variables[681] = variables[173]*states[13]*variables[552]*pow(fabs(states[10]), variables[363]-1.0)*(states[10]-pow(states[12], 2.0)*pow(states[7], 3.0)*variables[236]/(pow(states[9], 3.0)*pow(states[11], 2.0)*variables[554]))/(pow(variables[74], variables[363])+pow(fabs(states[10]), variables[363]))*states[9]/(variables[64]+states[9])*states[11]/(variables[81]+states[11]);
    variables[682] = variables[172]*variables[676]*variables[552]*states[7]/variables[68]*(1.0-variables[679]/variables[555])/(1.0+states[7]/variables[68]+(1.0+states[9]/variables[65])*(1.0+states[11]/variables[82])-1.0);
    variables[683] = variables[165]*variables[168]*(log(variables[553])-log(fabs(variables[677])+1.0e-300));
    variables[684] = variables[165]*variables[168]*(log(variables[554])-log(fabs(variables[678])+1.0e-300));
    variables[685] = variables[165]*variables[168]*(log(variables[555])-log(fabs(variables[679])+1.0e-300));
    variables[686] = states[14]/(variables[92]+states[14]);
    variables[687] = variables[686]*states[9]/variables[236]*pow(states[11]/variables[236], 2.0)/(states[10]/variables[236]*states[7]/variables[236]);
    variables[688] = states[10]/variables[236]/(states[11]/variables[236]);
    variables[689] = variables[177]*variables[474]*variables[552]*(states[10]*states[7]-variables[686]*states[9]*pow(states[11], 2.0)/(variables[236]*variables[556]))/((variables[77]+states[10])*(variables[71]+states[7]))/(1.0+pow(states[14]/variables[39], 8.0));
    variables[690] = variables[174]*variables[552]*states[14]/(variables[92]+states[14])*(states[11]-states[10]/variables[557])/(variables[83]+states[11])*variables[93]/(variables[93]+states[7]);
    variables[691] = variables[165]*variables[168]*(log(variables[556])-log(fabs(variables[687])+1.0e-300));
    variables[692] = variables[165]*variables[168]*(log(variables[557])-log(fabs(variables[688])+1.0e-300));
    variables[693] = pow(states[9]/variables[236], variables[361])*pow(states[11]/variables[236], variables[361]+1.0)/(states[10]/variables[236]*pow(states[7]/variables[236], variables[361]));
    variables[694] = variables[175]*variables[345]*variables[552]*states[10]/(variables[75]+states[10])*states[7]/(variables[69]+states[7])*(1.0-variables[693]/variables[558]+fabs(1.0-variables[693]/variables[558]))/2.0;
    variables[695] = variables[165]*variables[168]*(log(variables[558])-log(fabs(variables[693])+1.0e-300));
    variables[696] = variables[549]*(variables[680]*variables[683]+variables[681]*variables[684]+variables[682]*variables[685]+variables[689]*variables[691]+variables[690]*variables[692]+variables[694]*variables[695]);
    variables[916] = states[9]*states[11]/variables[237]*exp(variables[259]/(variables[165]*variables[168]));
    variables[918] = variables[297]*states[34]*variables[548]*(variables[32]*pow(states[7]/variables[98], variables[367])-states[8]*pow(variables[916]/variables[98], variables[367]))/((variables[88]+variables[32])*pow(1.0+states[7]/variables[98], variables[367]));
    variables[920] = variables[367]*variables[918];
    variables[921] = -variables[920];
    variables[908] = states[9]/(variables[63]+states[9])*states[11]/(variables[80]+states[11]);
    variables[909] = variables[299]*states[33]*variables[548]*variables[908]*(variables[34]*states[7]-states[10]*states[9]/variables[586])/((variables[99]+states[7])*(variables[90]+variables[34]));
    variables[914] = -variables[909];
    variables[863] = variables[365]*variables[53]*(variables[670]-states[1])*0.001;
    variables[862] = -variables[260]-variables[165]*variables[168]*log(fabs(states[9]/variables[240]*states[11]/variables[240]/(states[7]/variables[240]))+1.0e-300);
    variables[864] = 1.0-exp(-(variables[862]-variables[863])/(variables[165]*variables[168]));
    variables[861] = variables[548]*variables[458]*states[23]*variables[376]*variables[365]*1.602176634e-10;
    variables[865] = variables[861]*states[7]/(variables[96]+states[7])*states[6]/(variables[85]+states[6])*variables[864];
    variables[866] = variables[865]/(variables[365]*variables[53])*1.0e-9;
    variables[869] = -variables[866];
    variables[717] = states[7]/(variables[97]+states[7])*(variables[97]+variables[8])/variables[8];
    variables[738] = (states[7]-variables[6])/(variables[8]-variables[6]);
    variables[739] = ((variables[738]+fabs(variables[738]))/2.0+1.0-fabs((variables[738]+fabs(variables[738]))/2.0-1.0))/2.0;
    variables[742] = (1.0-variables[381]+variables[381]*variables[675])*(1.0-variables[400]+variables[400]*variables[674])*(1.0-variables[438]+variables[438]*variables[673])*(1.0-variables[419]+variables[419]*variables[672]);
    variables[743] = variables[290]*variables[61]*variables[739]*variables[742];
    variables[744] = variables[743]*variables[717];
    variables[716] = (1.0-variables[398]+variables[398]*variables[675])*(1.0-variables[417]+variables[417]*variables[674])*(1.0-variables[455]+variables[455]*variables[673])*(1.0-variables[436]+variables[436]*variables[672]);
    variables[734] = variables[457]*variables[380]*states[18]/(variables[73]+states[18])*variables[717]*variables[716];
    variables[733] = variables[457]*variables[380]*states[17]/(variables[73]+states[17])*variables[717]*variables[716];
    variables[732] = variables[457]*variables[380]*states[16]/(variables[73]+states[16])*variables[717]*variables[716];
    variables[731] = variables[457]*variables[380]*states[15]/(variables[73]+states[15])*variables[717]*variables[716];
    variables[735] = variables[731]+variables[732]+variables[733]+variables[734];
    variables[737] = variables[735]*variables[45];
    variables[715] = (1.0-variables[399]+variables[399]*variables[675])*(1.0-variables[418]+variables[418]*variables[674])*(1.0-variables[456]+variables[456]*variables[673])*(1.0-variables[437]+variables[437]*variables[672]);
    variables[718] = variables[97]*(variables[8]-states[7])/((variables[97]+states[7])*variables[8])+variables[717]*(1.0-variables[715]);
    variables[719] = (variables[718]+fabs(variables[718]))/2.0;
    variables[720] = 1.0-variables[719];
    variables[722] = variables[567]*variables[201]*variables[321]/variables[137]*variables[720];
    variables[721] = variables[369]*variables[120]*variables[205]*log(2.0)/variables[477]/variables[137]*variables[720];
    variables[753] = 1.0-variables[719];
    variables[756] = variables[324]*(variables[374]-states[20]+fabs(variables[374]-states[20]))/2.0*pow(variables[753], variables[371]);
    variables[758] = variables[756]*variables[571]/variables[155];
    variables[757] = states[20]*variables[136]*variables[206]*variables[325]/variables[155]*variables[753];
    variables[748] = variables[757]+variables[758];
    variables[749] = variables[721]+variables[748]+variables[722]+variables[737]+variables[744];
    variables[750] = -variables[749];
    variables[776] = (states[21]+fabs(states[21]))/2.0/variables[478];
    variables[777] = variables[509]*variables[776]/variables[154];
    variables[1213] = states[7]/(variables[102]+states[7])*(variables[102]+variables[11])/variables[11];
    variables[1214] = (variables[1213]+1.0-fabs(variables[1213]-1.0))/2.0;
    variables[1216] = variables[307]*states[35]*variables[1214];
    variables[1209] = (1.0-variables[385]+variables[385]*variables[675])*(1.0-variables[404]+variables[404]*variables[674])*(1.0-variables[442]+variables[442]*variables[673])*(1.0-variables[423]+variables[423]*variables[672]);
    variables[1210] = variables[632]*variables[1209];
    variables[1215] = (states[35]+variables[1210]-fabs(states[35]-variables[1210]))/2.0;
    variables[759] = (1.0-variables[397]+variables[397]*variables[675])*(1.0-variables[416]+variables[416]*variables[674])*(1.0-variables[454]+variables[454]*variables[673])*(1.0-variables[435]+variables[435]*variables[672]);
    variables[1217] = -variables[307]*variables[1214]*((states[35]-variables[1210]+fabs(states[35]-variables[1210]))/2.0+(1.0-variables[759])*variables[1215]);
    variables[1218] = variables[1216]+variables[1217];
    variables[1219] = variables[349]*variables[123]*((variables[209]+variables[223])*variables[1218]+variables[189]*variables[1216])/variables[142];
    variables[1109] = (1.0-variables[384]+variables[384]*variables[675])*(1.0-variables[403]+variables[403]*variables[674])*(1.0-variables[441]+variables[441]*variables[673])*(1.0-variables[422]+variables[422]*variables[672]);
    variables[1110] = variables[342]*variables[1109];
    variables[1122] = variables[348]*variables[122]*(states[31]-variables[1110]+fabs(states[31]-variables[1110]))/2.0/variables[495];
    variables[1222] = variables[349]*variables[123]*(states[35]-variables[1210]+fabs(states[35]-variables[1210]))/2.0/variables[496];
    variables[1184] = (1.0-variables[383]+variables[383]*variables[675])*(1.0-variables[402]+variables[402]*variables[674])*(1.0-variables[440]+variables[440]*variables[673])*(1.0-variables[421]+variables[421]*variables[672]);
    variables[1185] = variables[629]*variables[1184];
    variables[1197] = variables[347]*variables[121]*(states[34]-variables[1185]+fabs(states[34]-variables[1185]))/2.0/variables[494];
    variables[1159] = (1.0-variables[394]+variables[394]*variables[675])*(1.0-variables[413]+variables[413]*variables[674])*(1.0-variables[451]+variables[451]*variables[673])*(1.0-variables[432]+variables[432]*variables[672]);
    variables[1160] = variables[626]*variables[1159];
    variables[1172] = variables[358]*variables[132]*(states[33]-variables[1160]+fabs(states[33]-variables[1160]))/2.0/variables[505];
    variables[1134] = (1.0-variables[389]+variables[389]*variables[675])*(1.0-variables[408]+variables[408]*variables[674])*(1.0-variables[446]+variables[446]*variables[673])*(1.0-variables[427]+variables[427]*variables[672]);
    variables[1135] = variables[623]*variables[1134];
    variables[1147] = variables[353]*variables[127]*(states[32]-variables[1135]+fabs(states[32]-variables[1135]))/2.0/variables[500];
    variables[1084] = (1.0-variables[391]+variables[391]*variables[675])*(1.0-variables[410]+variables[410]*variables[674])*(1.0-variables[448]+variables[448]*variables[673])*(1.0-variables[429]+variables[429]*variables[672]);
    variables[1085] = variables[618]*variables[1084];
    variables[1097] = variables[355]*variables[129]*(states[29]-variables[1085]+fabs(states[29]-variables[1085]))/2.0/variables[502];
    variables[1059] = (1.0-variables[386]+variables[386]*variables[675])*(1.0-variables[405]+variables[405]*variables[674])*(1.0-variables[443]+variables[443]*variables[673])*(1.0-variables[424]+variables[424]*variables[672]);
    variables[1060] = variables[615]*variables[1059];
    variables[1072] = variables[350]*variables[124]*(states[28]-variables[1060]+fabs(states[28]-variables[1060]))/2.0/variables[497];
    variables[1034] = (1.0-variables[392]+variables[392]*variables[675])*(1.0-variables[411]+variables[411]*variables[674])*(1.0-variables[449]+variables[449]*variables[673])*(1.0-variables[430]+variables[430]*variables[672]);
    variables[1035] = variables[612]*variables[1034];
    variables[1047] = variables[356]*variables[130]*(states[27]-variables[1035]+fabs(states[27]-variables[1035]))/2.0/variables[503];
    variables[1009] = (1.0-variables[395]+variables[395]*variables[675])*(1.0-variables[414]+variables[414]*variables[674])*(1.0-variables[452]+variables[452]*variables[673])*(1.0-variables[433]+variables[433]*variables[672]);
    variables[1010] = variables[609]*variables[1009];
    variables[1022] = variables[359]*variables[133]*(states[26]-variables[1010]+fabs(states[26]-variables[1010]))/2.0/variables[506];
    variables[984] = (1.0-variables[390]+variables[390]*variables[675])*(1.0-variables[409]+variables[409]*variables[674])*(1.0-variables[447]+variables[447]*variables[673])*(1.0-variables[428]+variables[428]*variables[672]);
    variables[985] = variables[606]*variables[984];
    variables[997] = variables[354]*variables[128]*(states[25]-variables[985]+fabs(states[25]-variables[985]))/2.0/variables[501];
    variables[959] = (1.0-variables[393]+variables[393]*variables[675])*(1.0-variables[412]+variables[412]*variables[674])*(1.0-variables[450]+variables[450]*variables[673])*(1.0-variables[431]+variables[431]*variables[672]);
    variables[960] = variables[603]*variables[959];
    variables[972] = variables[357]*variables[131]*(states[24]-variables[960]+fabs(states[24]-variables[960]))/2.0/variables[504];
    variables[934] = (1.0-variables[388]+variables[388]*variables[675])*(1.0-variables[407]+variables[407]*variables[674])*(1.0-variables[445]+variables[445]*variables[673])*(1.0-variables[426]+variables[426]*variables[672]);
    variables[935] = variables[600]*variables[934];
    variables[947] = variables[352]*variables[126]*(states[23]-variables[935]+fabs(states[23]-variables[935]))/2.0/variables[499];
    variables[822] = (1.0-variables[387]+variables[387]*variables[675])*(1.0-variables[406]+variables[406]*variables[674])*(1.0-variables[444]+variables[444]*variables[673])*(1.0-variables[425]+variables[425]*variables[672]);
    variables[823] = variables[343]*variables[822];
    variables[835] = variables[351]*variables[125]*(states[13]-variables[823]+fabs(states[13]-variables[823]))/2.0/variables[498];
    variables[797] = (1.0-variables[396]+variables[396]*variables[675])*(1.0-variables[415]+variables[415]*variables[674])*(1.0-variables[453]+variables[453]*variables[673])*(1.0-variables[434]+variables[434]*variables[672]);
    variables[798] = variables[344]*variables[797];
    variables[810] = variables[360]*variables[134]*(states[22]-variables[798]+fabs(states[22]-variables[798]))/2.0/variables[507];
    variables[764] = variables[810]+variables[835]+variables[947]+variables[972]+variables[997]+variables[1022]+variables[1047]+variables[1072]+variables[1097]+variables[1122]+variables[1147]+variables[1172]+variables[1197]+variables[1222];
    variables[765] = variables[764];
    variables[774] = (variables[164]+variables[163])/((variables[164]+variables[765]+fabs(variables[164]-variables[765]))/2.0+variables[163]);
    variables[1223] = (states[35]-variables[1210]+fabs(states[35]-variables[1210]))/2.0/variables[496]*variables[774]*variables[1214];
    variables[1224] = variables[189]*variables[349]*variables[123]*variables[1223]/variables[142];
    variables[775] = (states[21]+fabs(states[21]))/2.0/(variables[84]+(states[21]+fabs(states[21]))/2.0);
    variables[1211] = (variables[1210]-states[35])/variables[482];
    variables[1212] = (variables[1211]+fabs(variables[1211]))/2.0;
    variables[787] = (states[7]-variables[7])/(variables[23]-variables[7]);
    variables[788] = ((variables[787]+fabs(variables[787]))/2.0+1.0-fabs((variables[787]+fabs(variables[787]))/2.0-1.0))/2.0;
    variables[754] = states[20]/variables[374];
    variables[789] = variables[185]*variables[754];
    variables[790] = variables[789]*variables[62]*variables[788]*variables[759];
    variables[1111] = (variables[1110]-states[31])/variables[481];
    variables[1112] = (variables[1111]+fabs(variables[1111]))/2.0;
    variables[1120] = variables[1112]*variables[348]*variables[122]*(variables[242]+variables[208])/variables[141];
    variables[1220] = variables[1212]*variables[349]*variables[123]*(variables[243]+variables[209])/variables[142];
    variables[1186] = (variables[1185]-states[34])/variables[480];
    variables[1187] = (variables[1186]+fabs(variables[1186]))/2.0;
    variables[1195] = variables[1187]*variables[347]*variables[121]*(variables[241]+variables[207])/variables[140];
    variables[1161] = (variables[1160]-states[33])/variables[491];
    variables[1162] = (variables[1161]+fabs(variables[1161]))/2.0;
    variables[1170] = variables[1162]*variables[358]*variables[132]*(variables[252]+variables[218])/variables[151];
    variables[1136] = (variables[1135]-states[32])/variables[486];
    variables[1137] = (variables[1136]+fabs(variables[1136]))/2.0;
    variables[1145] = variables[1137]*variables[353]*variables[127]*(variables[247]+variables[213])/variables[146];
    variables[1086] = (variables[1085]-states[29])/variables[488];
    variables[1087] = (variables[1086]+fabs(variables[1086]))/2.0;
    variables[1095] = variables[1087]*variables[355]*variables[129]*(variables[249]+variables[215])/variables[148];
    variables[1061] = (variables[1060]-states[28])/variables[483];
    variables[1062] = (variables[1061]+fabs(variables[1061]))/2.0;
    variables[1070] = variables[1062]*variables[350]*variables[124]*(variables[244]+variables[210])/variables[143];
    variables[1036] = (variables[1035]-states[27])/variables[489];
    variables[1037] = (variables[1036]+fabs(variables[1036]))/2.0;
    variables[1045] = variables[1037]*variables[356]*variables[130]*(variables[250]+variables[216])/variables[149];
    variables[1011] = (variables[1010]-states[26])/variables[492];
    variables[1012] = (variables[1011]+fabs(variables[1011]))/2.0;
    variables[1020] = variables[1012]*variables[359]*variables[133]*(variables[253]+variables[219])/variables[152];
    variables[986] = (variables[985]-states[25])/variables[487];
    variables[987] = (variables[986]+fabs(variables[986]))/2.0;
    variables[995] = variables[987]*variables[354]*variables[128]*(variables[248]+variables[214])/variables[147];
    variables[961] = (variables[960]-states[24])/variables[490];
    variables[962] = (variables[961]+fabs(variables[961]))/2.0;
    variables[970] = variables[962]*variables[357]*variables[131]*(variables[251]+variables[217])/variables[150];
    variables[936] = (variables[935]-states[23])/variables[485];
    variables[937] = (variables[936]+fabs(variables[936]))/2.0;
    variables[945] = variables[937]*variables[352]*variables[126]*(variables[246]+variables[212])/variables[145];
    variables[824] = (variables[823]-states[13])/variables[484];
    variables[825] = (variables[824]+fabs(variables[824]))/2.0;
    variables[833] = variables[825]*variables[351]*variables[125]*(variables[245]+variables[211])/variables[144];
    variables[799] = (variables[798]-states[22])/variables[493];
    variables[800] = (variables[799]+fabs(variables[799]))/2.0;
    variables[808] = variables[800]*variables[360]*variables[134]*(variables[254]+variables[220])/variables[153];
    variables[760] = variables[808]+variables[833]+variables[945]+variables[970]+variables[995]+variables[1020]+variables[1045]+variables[1070]+variables[1095]+variables[1120]+variables[1145]+variables[1170]+variables[1195]+variables[1220];
    variables[761] = variables[760];
    variables[791] = (variables[790]+variables[60])/((variables[790]+variables[761]+fabs(variables[790]-variables[761]))/2.0+variables[60]);
    variables[1221] = variables[1212]*variables[791]*variables[1214];
    variables[1226] = variables[349]*variables[123]*variables[775]*variables[1221];
    variables[1227] = (variables[243]-variables[223])*variables[1226]/variables[142];
    variables[1228] = variables[1221]*variables[349]*variables[123]*(variables[243]+variables[209])/variables[142]-variables[1227];
    variables[1229] = variables[1219]+variables[1228]+variables[1224];
    variables[1188] = states[7]/(variables[100]+states[7])*(variables[100]+variables[9])/variables[9];
    variables[1189] = (variables[1188]+1.0-fabs(variables[1188]-1.0))/2.0;
    variables[1191] = variables[305]*states[34]*variables[1189];
    variables[1190] = (states[34]+variables[1185]-fabs(states[34]-variables[1185]))/2.0;
    variables[1192] = -variables[305]*variables[1189]*((states[34]-variables[1185]+fabs(states[34]-variables[1185]))/2.0+(1.0-variables[759])*variables[1190]);
    variables[1193] = variables[1191]+variables[1192];
    variables[1194] = variables[347]*variables[121]*((variables[207]+variables[221])*variables[1193]+variables[187]*variables[1191])/variables[140];
    variables[1198] = (states[34]-variables[1185]+fabs(states[34]-variables[1185]))/2.0/variables[494]*variables[774]*variables[1189];
    variables[1199] = variables[187]*variables[347]*variables[121]*variables[1198]/variables[140];
    variables[1196] = variables[1187]*variables[791]*variables[1189];
    variables[1201] = variables[347]*variables[121]*variables[775]*variables[1196];
    variables[1202] = (variables[241]-variables[221])*variables[1201]/variables[140];
    variables[1203] = variables[1196]*variables[347]*variables[121]*(variables[241]+variables[207])/variables[140]-variables[1202];
    variables[1204] = variables[1194]+variables[1203]+variables[1199];
    variables[1163] = states[7]/(variables[111]+states[7])*(variables[111]+variables[20])/variables[20];
    variables[1164] = (variables[1163]+1.0-fabs(variables[1163]-1.0))/2.0;
    variables[1166] = variables[316]*states[33]*variables[1164];
    variables[1165] = (states[33]+variables[1160]-fabs(states[33]-variables[1160]))/2.0;
    variables[1167] = -variables[316]*variables[1164]*((states[33]-variables[1160]+fabs(states[33]-variables[1160]))/2.0+(1.0-variables[759])*variables[1165]);
    variables[1168] = variables[1166]+variables[1167];
    variables[1169] = variables[358]*variables[132]*((variables[218]+variables[232])*variables[1168]+variables[198]*variables[1166])/variables[151];
    variables[1173] = (states[33]-variables[1160]+fabs(states[33]-variables[1160]))/2.0/variables[505]*variables[774]*variables[1164];
    variables[1174] = variables[198]*variables[358]*variables[132]*variables[1173]/variables[151];
    variables[1171] = variables[1162]*variables[791]*variables[1164];
    variables[1176] = variables[358]*variables[132]*variables[775]*variables[1171];
    variables[1177] = (variables[252]-variables[232])*variables[1176]/variables[151];
    variables[1178] = variables[1171]*variables[358]*variables[132]*(variables[252]+variables[218])/variables[151]-variables[1177];
    variables[1179] = variables[1169]+variables[1178]+variables[1174];
    variables[1138] = states[7]/(variables[106]+states[7])*(variables[106]+variables[15])/variables[15];
    variables[1139] = (variables[1138]+1.0-fabs(variables[1138]-1.0))/2.0;
    variables[1141] = variables[311]*states[32]*variables[1139];
    variables[1140] = (states[32]+variables[1135]-fabs(states[32]-variables[1135]))/2.0;
    variables[1142] = -variables[311]*variables[1139]*((states[32]-variables[1135]+fabs(states[32]-variables[1135]))/2.0+(1.0-variables[759])*variables[1140]);
    variables[1143] = variables[1141]+variables[1142];
    variables[1144] = variables[353]*variables[127]*((variables[213]+variables[227])*variables[1143]+variables[193]*variables[1141])/variables[146];
    variables[1148] = (states[32]-variables[1135]+fabs(states[32]-variables[1135]))/2.0/variables[500]*variables[774]*variables[1139];
    variables[1149] = variables[193]*variables[353]*variables[127]*variables[1148]/variables[146];
    variables[1146] = variables[1137]*variables[791]*variables[1139];
    variables[1151] = variables[353]*variables[127]*variables[775]*variables[1146];
    variables[1152] = (variables[247]-variables[227])*variables[1151]/variables[146];
    variables[1153] = variables[1146]*variables[353]*variables[127]*(variables[247]+variables[213])/variables[146]-variables[1152];
    variables[1154] = variables[1144]+variables[1153]+variables[1149];
    variables[1113] = states[7]/(variables[101]+states[7])*(variables[101]+variables[10])/variables[10];
    variables[1114] = (variables[1113]+1.0-fabs(variables[1113]-1.0))/2.0;
    variables[1116] = variables[306]*states[31]*variables[1114];
    variables[1115] = (states[31]+variables[1110]-fabs(states[31]-variables[1110]))/2.0;
    variables[1117] = -variables[306]*variables[1114]*((states[31]-variables[1110]+fabs(states[31]-variables[1110]))/2.0+(1.0-variables[759])*variables[1115]);
    variables[1118] = variables[1116]+variables[1117];
    variables[1119] = variables[348]*variables[122]*((variables[208]+variables[222])*variables[1118]+variables[188]*variables[1116])/variables[141];
    variables[1123] = (states[31]-variables[1110]+fabs(states[31]-variables[1110]))/2.0/variables[495]*variables[774]*variables[1114];
    variables[1124] = variables[188]*variables[348]*variables[122]*variables[1123]/variables[141];
    variables[1121] = variables[1112]*variables[791]*variables[1114];
    variables[1126] = variables[348]*variables[122]*variables[775]*variables[1121];
    variables[1127] = (variables[242]-variables[222])*variables[1126]/variables[141];
    variables[1128] = variables[1121]*variables[348]*variables[122]*(variables[242]+variables[208])/variables[141]-variables[1127];
    variables[1129] = variables[1119]+variables[1128]+variables[1124];
    variables[1088] = states[7]/(variables[108]+states[7])*(variables[108]+variables[17])/variables[17];
    variables[1089] = (variables[1088]+1.0-fabs(variables[1088]-1.0))/2.0;
    variables[1091] = variables[313]*states[29]*variables[1089];
    variables[1090] = (states[29]+variables[1085]-fabs(states[29]-variables[1085]))/2.0;
    variables[1092] = -variables[313]*variables[1089]*((states[29]-variables[1085]+fabs(states[29]-variables[1085]))/2.0+(1.0-variables[759])*variables[1090]);
    variables[1093] = variables[1091]+variables[1092];
    variables[1094] = variables[355]*variables[129]*((variables[215]+variables[229])*variables[1093]+variables[195]*variables[1091])/variables[148];
    variables[1098] = (states[29]-variables[1085]+fabs(states[29]-variables[1085]))/2.0/variables[502]*variables[774]*variables[1089];
    variables[1099] = variables[195]*variables[355]*variables[129]*variables[1098]/variables[148];
    variables[1096] = variables[1087]*variables[791]*variables[1089];
    variables[1101] = variables[355]*variables[129]*variables[775]*variables[1096];
    variables[1102] = (variables[249]-variables[229])*variables[1101]/variables[148];
    variables[1103] = variables[1096]*variables[355]*variables[129]*(variables[249]+variables[215])/variables[148]-variables[1102];
    variables[1104] = variables[1094]+variables[1103]+variables[1099];
    variables[1063] = states[7]/(variables[103]+states[7])*(variables[103]+variables[12])/variables[12];
    variables[1064] = (variables[1063]+1.0-fabs(variables[1063]-1.0))/2.0;
    variables[1066] = variables[308]*states[28]*variables[1064];
    variables[1065] = (states[28]+variables[1060]-fabs(states[28]-variables[1060]))/2.0;
    variables[1067] = -variables[308]*variables[1064]*((states[28]-variables[1060]+fabs(states[28]-variables[1060]))/2.0+(1.0-variables[759])*variables[1065]);
    variables[1068] = variables[1066]+variables[1067];
    variables[1069] = variables[350]*variables[124]*((variables[210]+variables[224])*variables[1068]+variables[190]*variables[1066])/variables[143];
    variables[1073] = (states[28]-variables[1060]+fabs(states[28]-variables[1060]))/2.0/variables[497]*variables[774]*variables[1064];
    variables[1074] = variables[190]*variables[350]*variables[124]*variables[1073]/variables[143];
    variables[1071] = variables[1062]*variables[791]*variables[1064];
    variables[1076] = variables[350]*variables[124]*variables[775]*variables[1071];
    variables[1077] = (variables[244]-variables[224])*variables[1076]/variables[143];
    variables[1078] = variables[1071]*variables[350]*variables[124]*(variables[244]+variables[210])/variables[143]-variables[1077];
    variables[1079] = variables[1069]+variables[1078]+variables[1074];
    variables[1038] = states[7]/(variables[109]+states[7])*(variables[109]+variables[18])/variables[18];
    variables[1039] = (variables[1038]+1.0-fabs(variables[1038]-1.0))/2.0;
    variables[1041] = variables[314]*states[27]*variables[1039];
    variables[1040] = (states[27]+variables[1035]-fabs(states[27]-variables[1035]))/2.0;
    variables[1042] = -variables[314]*variables[1039]*((states[27]-variables[1035]+fabs(states[27]-variables[1035]))/2.0+(1.0-variables[759])*variables[1040]);
    variables[1043] = variables[1041]+variables[1042];
    variables[1044] = variables[356]*variables[130]*((variables[216]+variables[230])*variables[1043]+variables[196]*variables[1041])/variables[149];
    variables[1048] = (states[27]-variables[1035]+fabs(states[27]-variables[1035]))/2.0/variables[503]*variables[774]*variables[1039];
    variables[1049] = variables[196]*variables[356]*variables[130]*variables[1048]/variables[149];
    variables[1046] = variables[1037]*variables[791]*variables[1039];
    variables[1051] = variables[356]*variables[130]*variables[775]*variables[1046];
    variables[1052] = (variables[250]-variables[230])*variables[1051]/variables[149];
    variables[1053] = variables[1046]*variables[356]*variables[130]*(variables[250]+variables[216])/variables[149]-variables[1052];
    variables[1054] = variables[1044]+variables[1053]+variables[1049];
    variables[1013] = states[7]/(variables[112]+states[7])*(variables[112]+variables[21])/variables[21];
    variables[1014] = (variables[1013]+1.0-fabs(variables[1013]-1.0))/2.0;
    variables[1016] = variables[317]*states[26]*variables[1014];
    variables[1015] = (states[26]+variables[1010]-fabs(states[26]-variables[1010]))/2.0;
    variables[1017] = -variables[317]*variables[1014]*((states[26]-variables[1010]+fabs(states[26]-variables[1010]))/2.0+(1.0-variables[759])*variables[1015]);
    variables[1018] = variables[1016]+variables[1017];
    variables[1019] = variables[359]*variables[133]*((variables[219]+variables[233])*variables[1018]+variables[199]*variables[1016])/variables[152];
    variables[1023] = (states[26]-variables[1010]+fabs(states[26]-variables[1010]))/2.0/variables[506]*variables[774]*variables[1014];
    variables[1024] = variables[199]*variables[359]*variables[133]*variables[1023]/variables[152];
    variables[1021] = variables[1012]*variables[791]*variables[1014];
    variables[1026] = variables[359]*variables[133]*variables[775]*variables[1021];
    variables[1027] = (variables[253]-variables[233])*variables[1026]/variables[152];
    variables[1028] = variables[1021]*variables[359]*variables[133]*(variables[253]+variables[219])/variables[152]-variables[1027];
    variables[1029] = variables[1019]+variables[1028]+variables[1024];
    variables[988] = states[7]/(variables[107]+states[7])*(variables[107]+variables[16])/variables[16];
    variables[989] = (variables[988]+1.0-fabs(variables[988]-1.0))/2.0;
    variables[991] = variables[312]*states[25]*variables[989];
    variables[990] = (states[25]+variables[985]-fabs(states[25]-variables[985]))/2.0;
    variables[992] = -variables[312]*variables[989]*((states[25]-variables[985]+fabs(states[25]-variables[985]))/2.0+(1.0-variables[759])*variables[990]);
    variables[993] = variables[991]+variables[992];
    variables[994] = variables[354]*variables[128]*((variables[214]+variables[228])*variables[993]+variables[194]*variables[991])/variables[147];
    variables[998] = (states[25]-variables[985]+fabs(states[25]-variables[985]))/2.0/variables[501]*variables[774]*variables[989];
    variables[999] = variables[194]*variables[354]*variables[128]*variables[998]/variables[147];
    variables[996] = variables[987]*variables[791]*variables[989];
    variables[1001] = variables[354]*variables[128]*variables[775]*variables[996];
    variables[1002] = (variables[248]-variables[228])*variables[1001]/variables[147];
    variables[1003] = variables[996]*variables[354]*variables[128]*(variables[248]+variables[214])/variables[147]-variables[1002];
    variables[1004] = variables[994]+variables[1003]+variables[999];
    variables[963] = states[7]/(variables[110]+states[7])*(variables[110]+variables[19])/variables[19];
    variables[964] = (variables[963]+1.0-fabs(variables[963]-1.0))/2.0;
    variables[966] = variables[315]*states[24]*variables[964];
    variables[965] = (states[24]+variables[960]-fabs(states[24]-variables[960]))/2.0;
    variables[967] = -variables[315]*variables[964]*((states[24]-variables[960]+fabs(states[24]-variables[960]))/2.0+(1.0-variables[759])*variables[965]);
    variables[968] = variables[966]+variables[967];
    variables[969] = variables[357]*variables[131]*((variables[217]+variables[231])*variables[968]+variables[197]*variables[966])/variables[150];
    variables[973] = (states[24]-variables[960]+fabs(states[24]-variables[960]))/2.0/variables[504]*variables[774]*variables[964];
    variables[974] = variables[197]*variables[357]*variables[131]*variables[973]/variables[150];
    variables[971] = variables[962]*variables[791]*variables[964];
    variables[976] = variables[357]*variables[131]*variables[775]*variables[971];
    variables[977] = (variables[251]-variables[231])*variables[976]/variables[150];
    variables[978] = variables[971]*variables[357]*variables[131]*(variables[251]+variables[217])/variables[150]-variables[977];
    variables[979] = variables[969]+variables[978]+variables[974];
    variables[938] = states[7]/(variables[105]+states[7])*(variables[105]+variables[14])/variables[14];
    variables[939] = (variables[938]+1.0-fabs(variables[938]-1.0))/2.0;
    variables[941] = variables[310]*states[23]*variables[939];
    variables[940] = (states[23]+variables[935]-fabs(states[23]-variables[935]))/2.0;
    variables[942] = -variables[310]*variables[939]*((states[23]-variables[935]+fabs(states[23]-variables[935]))/2.0+(1.0-variables[759])*variables[940]);
    variables[943] = variables[941]+variables[942];
    variables[944] = variables[352]*variables[126]*((variables[212]+variables[226])*variables[943]+variables[192]*variables[941])/variables[145];
    variables[948] = (states[23]-variables[935]+fabs(states[23]-variables[935]))/2.0/variables[499]*variables[774]*variables[939];
    variables[949] = variables[192]*variables[352]*variables[126]*variables[948]/variables[145];
    variables[946] = variables[937]*variables[791]*variables[939];
    variables[951] = variables[352]*variables[126]*variables[775]*variables[946];
    variables[952] = (variables[246]-variables[226])*variables[951]/variables[145];
    variables[953] = variables[946]*variables[352]*variables[126]*(variables[246]+variables[212])/variables[145]-variables[952];
    variables[954] = variables[944]+variables[953]+variables[949];
    variables[826] = states[7]/(variables[104]+states[7])*(variables[104]+variables[13])/variables[13];
    variables[827] = (variables[826]+1.0-fabs(variables[826]-1.0))/2.0;
    variables[829] = variables[309]*states[13]*variables[827];
    variables[828] = (states[13]+variables[823]-fabs(states[13]-variables[823]))/2.0;
    variables[830] = -variables[309]*variables[827]*((states[13]-variables[823]+fabs(states[13]-variables[823]))/2.0+(1.0-variables[759])*variables[828]);
    variables[831] = variables[829]+variables[830];
    variables[832] = variables[351]*variables[125]*((variables[211]+variables[225])*variables[831]+variables[191]*variables[829])/variables[144];
    variables[836] = (states[13]-variables[823]+fabs(states[13]-variables[823]))/2.0/variables[498]*variables[774]*variables[827];
    variables[837] = variables[191]*variables[351]*variables[125]*variables[836]/variables[144];
    variables[834] = variables[825]*variables[791]*variables[827];
    variables[839] = variables[351]*variables[125]*variables[775]*variables[834];
    variables[840] = (variables[245]-variables[225])*variables[839]/variables[144];
    variables[841] = variables[834]*variables[351]*variables[125]*(variables[245]+variables[211])/variables[144]-variables[840];
    variables[842] = variables[832]+variables[841]+variables[837];
    variables[801] = states[7]/(variables[113]+states[7])*(variables[113]+variables[22])/variables[22];
    variables[802] = (variables[801]+1.0-fabs(variables[801]-1.0))/2.0;
    variables[804] = variables[318]*states[22]*variables[802];
    variables[803] = (states[22]+variables[798]-fabs(states[22]-variables[798]))/2.0;
    variables[805] = -variables[318]*variables[802]*((states[22]-variables[798]+fabs(states[22]-variables[798]))/2.0+(1.0-variables[759])*variables[803]);
    variables[806] = variables[804]+variables[805];
    variables[807] = variables[360]*variables[134]*((variables[220]+variables[234])*variables[806]+variables[200]*variables[804])/variables[153];
    variables[811] = (states[22]-variables[798]+fabs(states[22]-variables[798]))/2.0/variables[507]*variables[774]*variables[802];
    variables[812] = variables[200]*variables[360]*variables[134]*variables[811]/variables[153];
    variables[809] = variables[800]*variables[791]*variables[802];
    variables[814] = variables[360]*variables[134]*variables[775]*variables[809];
    variables[815] = (variables[254]-variables[234])*variables[814]/variables[153];
    variables[816] = variables[809]*variables[360]*variables[134]*(variables[254]+variables[220])/variables[153]-variables[815];
    variables[817] = variables[807]+variables[816]+variables[812];
    variables[779] = variables[817]+variables[842]+variables[954]+variables[979]+variables[1004]+variables[1029]+variables[1054]+variables[1079]+variables[1104]+variables[1129]+variables[1154]+variables[1179]+variables[1204]+variables[1229];
    variables[780] = variables[779]-variables[777];
    variables[793] = -variables[780];
    variables[697] = variables[793]+variables[750]+variables[869]+variables[914]+variables[921];
    variables[698] = variables[549]*variables[682]-variables[697];
    variables[699] = variables[549]*(variables[682]+variables[680]-variables[681]+variables[689]+variables[361]*variables[694])-variables[697];
    variables[912] = variables[909];
    variables[700] = variables[912];
    variables[923] = variables[920];
    variables[871] = variables[866];
    variables[752] = variables[749];
    variables[795] = variables[780];
    variables[701] = variables[795]+variables[752]+variables[871]+variables[588]+variables[923];
    variables[702] = variables[549]*(variables[680]*variables[294]+variables[681]*(2.0*variables[295]-2.0*variables[291]-variables[294])+variables[682]*variables[291]+variables[689]*(2.0*variables[291]+variables[293]-variables[294])+variables[690]*(variables[294]-variables[291]-variables[293])+variables[694]*((variables[361]+1.0)*variables[291]+variables[296]-variables[294]))+variables[701]*variables[291]+variables[700]*variables[294];
    variables[706] = variables[549]*variables[680];
    variables[707] = 3.0*variables[549]*variables[681];
    variables[708] = 3.0*variables[549]*variables[690];
    variables[709] = variables[707]-variables[706]-variables[708]+variables[549]*variables[689]+variables[549]*variables[694];
    variables[710] = 2.0*variables[549]*variables[689];
    variables[711] = variables[549]*variables[682];
    variables[712] = 3.0*variables[549]*states[14];
    variables[713] = variables[549]*variables[694];
    variables[714] = (variables[361]+1.0)*variables[549]*variables[694];
    variables[723] = pow(states[5], variables[362])/(pow(variables[72], variables[362])+pow(states[5], variables[362]));
    variables[724] = pow((1.0-states[2]/variables[79]+fabs(1.0-states[2]/variables[79]))/2.0, 2.0);
    variables[725] = pow((1.0-states[7]/variables[5]+fabs(1.0-states[7]/variables[5]))/2.0, 2.0);
    variables[726] = variables[320]*variables[719];
    variables[727] = variables[379]+variables[726];
    variables[728] = variables[322]*(1.0-variables[676]);
    variables[729] = variables[378]*variables[568]+variables[302]*variables[723]+variables[303]*variables[724]+variables[728];
    variables[730] = variables[319]*variables[725];
    variables[736] = states[15]+states[16]+states[17]+states[18];
    variables[1208] = variables[349]*states[35];
    variables[1230] = variables[1208]*variables[123]*(variables[243]+variables[209])/variables[142];
    variables[1183] = variables[347]*states[34];
    variables[1205] = variables[1183]*variables[121]*(variables[241]+variables[207])/variables[140];
    variables[1158] = variables[358]*states[33];
    variables[1180] = variables[1158]*variables[132]*(variables[252]+variables[218])/variables[151];
    variables[1133] = variables[353]*states[32];
    variables[1155] = variables[1133]*variables[127]*(variables[247]+variables[213])/variables[146];
    variables[1108] = variables[348]*states[31];
    variables[1130] = variables[1108]*variables[122]*(variables[242]+variables[208])/variables[141];
    variables[1083] = variables[355]*states[29];
    variables[1105] = variables[1083]*variables[129]*(variables[249]+variables[215])/variables[148];
    variables[1058] = variables[350]*states[28];
    variables[1080] = variables[1058]*variables[124]*(variables[244]+variables[210])/variables[143];
    variables[1033] = variables[356]*states[27];
    variables[1055] = variables[1033]*variables[130]*(variables[250]+variables[216])/variables[149];
    variables[1008] = variables[359]*states[26];
    variables[1030] = variables[1008]*variables[133]*(variables[253]+variables[219])/variables[152];
    variables[983] = variables[354]*states[25];
    variables[1005] = variables[983]*variables[128]*(variables[248]+variables[214])/variables[147];
    variables[958] = variables[357]*states[24];
    variables[980] = variables[958]*variables[131]*(variables[251]+variables[217])/variables[150];
    variables[933] = variables[352]*states[23];
    variables[955] = variables[933]*variables[126]*(variables[246]+variables[212])/variables[145];
    variables[821] = variables[351]*states[13];
    variables[843] = variables[821]*variables[125]*(variables[245]+variables[211])/variables[144];
    variables[796] = variables[360]*states[22];
    variables[818] = variables[796]*variables[134]*(variables[254]+variables[220])/variables[153];
    variables[740] = variables[818]+variables[843]+variables[955]+variables[980]+variables[1005]+variables[1030]+variables[1055]+variables[1080]+variables[1105]+variables[1130]+variables[1155]+variables[1180]+variables[1205]+variables[1230];
    variables[741] = variables[740];
    variables[745] = variables[570]+variables[741];
    variables[746] = states[19]/variables[745];
    variables[747] = variables[712]/variables[745];
    variables[751] = variables[749];
    variables[755] = variables[325]*states[20]*variables[719];
    variables[762] = variables[807]+variables[832]+variables[944]+variables[969]+variables[994]+variables[1019]+variables[1044]+variables[1069]+variables[1094]+variables[1119]+variables[1144]+variables[1169]+variables[1194]+variables[1219];
    variables[763] = variables[762];
    variables[1225] = variables[349]*variables[123]*(variables[1223]-variables[1217]);
    variables[1200] = variables[347]*variables[121]*(variables[1198]-variables[1192]);
    variables[1175] = variables[358]*variables[132]*(variables[1173]-variables[1167]);
    variables[1150] = variables[353]*variables[127]*(variables[1148]-variables[1142]);
    variables[1125] = variables[348]*variables[122]*(variables[1123]-variables[1117]);
    variables[1100] = variables[355]*variables[129]*(variables[1098]-variables[1092]);
    variables[1075] = variables[350]*variables[124]*(variables[1073]-variables[1067]);
    variables[1050] = variables[356]*variables[130]*(variables[1048]-variables[1042]);
    variables[1025] = variables[359]*variables[133]*(variables[1023]-variables[1017]);
    variables[1000] = variables[354]*variables[128]*(variables[998]-variables[992]);
    variables[975] = variables[357]*variables[131]*(variables[973]-variables[967]);
    variables[950] = variables[352]*variables[126]*(variables[948]-variables[942]);
    variables[838] = variables[351]*variables[125]*(variables[836]-variables[830]);
    variables[813] = variables[360]*variables[134]*(variables[811]-variables[805]);
    variables[766] = variables[813]+variables[838]+variables[950]+variables[975]+variables[1000]+variables[1025]+variables[1050]+variables[1075]+variables[1100]+variables[1125]+variables[1150]+variables[1175]+variables[1200]+variables[1225];
    variables[767] = variables[766];
    variables[768] = variables[814]+variables[839]+variables[951]+variables[976]+variables[1001]+variables[1026]+variables[1051]+variables[1076]+variables[1101]+variables[1126]+variables[1151]+variables[1176]+variables[1201]+variables[1226];
    variables[769] = variables[768];
    variables[770] = variables[812]+variables[837]+variables[949]+variables[974]+variables[999]+variables[1024]+variables[1049]+variables[1074]+variables[1099]+variables[1124]+variables[1149]+variables[1174]+variables[1199]+variables[1224];
    variables[771] = variables[770];
    variables[772] = variables[815]+variables[840]+variables[952]+variables[977]+variables[1002]+variables[1027]+variables[1052]+variables[1077]+variables[1102]+variables[1127]+variables[1152]+variables[1177]+variables[1202]+variables[1227];
    variables[773] = variables[772];
    variables[778] = variables[772]+variables[777];
    variables[781] = variables[816]+variables[841]+variables[953]+variables[978]+variables[1003]+variables[1028]+variables[1053]+variables[1078]+variables[1103]+variables[1128]+variables[1153]+variables[1178]+variables[1203]+variables[1228];
    variables[782] = variables[781];
    variables[783] = variables[818]+variables[843]+variables[955]+variables[980]+variables[1005]+variables[1030]+variables[1055]+variables[1080]+variables[1105]+variables[1130]+variables[1155]+variables[1180]+variables[1205]+variables[1230];
    variables[784] = variables[783];
    variables[1231] = variables[631]*states[35];
    variables[1206] = variables[628]*states[34];
    variables[1181] = variables[625]*states[33];
    variables[1156] = variables[622]*states[32];
    variables[1131] = variables[620]*states[31];
    variables[1106] = variables[617]*states[29];
    variables[1081] = variables[614]*states[28];
    variables[1056] = variables[611]*states[27];
    variables[1031] = variables[608]*states[26];
    variables[1006] = variables[605]*states[25];
    variables[981] = variables[602]*states[24];
    variables[956] = variables[599]*states[23];
    variables[844] = variables[576]*states[13];
    variables[819] = variables[574]*states[22];
    variables[785] = variables[819]+variables[844]+variables[956]+variables[981]+variables[1006]+variables[1031]+variables[1056]+variables[1081]+variables[1106]+variables[1131]+variables[1156]+variables[1181]+variables[1206]+variables[1231];
    variables[786] = variables[785];
    variables[792] = variables[786]/variables[573];
    variables[794] = variables[780];
    variables[820] = variables[819]/variables[573];
    variables[845] = variables[844]/variables[573];
    variables[854] = variables[578]*(states[1]-variables[661]);
    variables[855] = -variables[854]/variables[53]*1.0e-9;
    variables[856] = variables[165]*variables[168]/(2.0*variables[53])*log(variables[48]/states[5])*1000.0;
    variables[857] = variables[579]*(states[1]-variables[856]);
    variables[858] = -variables[857]/(2.0*variables[53])*1.0e-9;
    variables[859] = variables[580]*(states[1]-variables[670]);
    variables[860] = -variables[859]/variables[53]*1.0e-9;
    variables[867] = -variables[365]*variables[866];
    variables[868] = variables[867];
    variables[870] = variables[866];
    variables[872] = -(states[1]+variables[661]-2.0*variables[670]);
    variables[873] = variables[548]*variables[284]*states[24]*variables[872]/variables[53]*1.0e-9;
    variables[874] = -variables[873]*variables[53]*1000000000.0;
    variables[875] = -variables[873];
    variables[876] = 2.0*variables[873];
    variables[877] = variables[670]-variables[660];
    variables[878] = variables[548]*variables[283]*states[25]*variables[877]/variables[53]*1.0e-9;
    variables[879] = -variables[878];
    variables[880] = variables[878];
    variables[881] = variables[548]*variables[461]*states[26]*variables[287]*1.0e-6;
    variables[882] = variables[881]*(states[1]-variables[660]);
    variables[883] = -variables[882]/variables[53]*1.0e-9;
    variables[884] = variables[548]*variables[459]*states[27]*variables[377]*1.602176634e-10;
    variables[885] = variables[884]*(exp(variables[285]*states[1]*1.0e-3*variables[53]/(variables[165]*variables[168]))*pow(states[3], 3.0)*variables[47]-exp((variables[285]-1.0)*states[1]*1.0e-3*variables[53]/(variables[165]*variables[168]))*pow(variables[159], 3.0)*states[5]*variables[181])/((pow(variables[117], 3.0)+pow(variables[159], 3.0))*(variables[116]+variables[47])*(1.0+variables[119]*exp((variables[285]-1.0)*states[1]*1.0e-3*variables[53]/(variables[165]*variables[168]))));
    variables[886] = variables[885]/variables[53]*1.0e-9;
    variables[887] = -3.0*variables[885]/variables[53]*1.0e-9;
    variables[888] = variables[300]*states[28]*variables[548]*(variables[35]/(variables[91]+variables[35])-states[8]/(variables[91]+states[8]));
    variables[889] = states[12]*states[6]/variables[57];
    variables[890] = variables[301]*states[29]*variables[548]*(variables[581]/(variables[95]+variables[581])-variables[889]/(variables[95]+variables[889]));
    variables[891] = variables[890];
    variables[892] = -variables[888];
    variables[893] = -variables[890];
    variables[894] = variables[888];
    variables[895] = variables[890];
    variables[896] = variables[670]-states[1];
    variables[897] = states[30]*variables[267]*variables[896]/variables[375];
    variables[898] = states[31]*variables[897]*variables[366]/variables[139];
    variables[899] = -variables[898]*variables[53]*1000000000.0;
    variables[900] = variables[898]*variables[53]*variables[896]*0.001;
    variables[901] = variables[585]*variables[508]*variables[897]/variables[267]*(1.0+variables[368])/(states[31]+variables[368]);
    variables[902] = states[31]*variables[901];
    variables[903] = variables[902]/variables[585];
    variables[904] = variables[902]*variables[903];
    variables[905] = variables[346]*states[32];
    variables[906] = variables[327]*variables[905]/variables[138];
    variables[907] = states[10]/variables[239]*states[9]/variables[239]/(variables[34]/variables[239]*states[7]/variables[239]);
    variables[910] = variables[165]*variables[168]*(log(variables[586])-log(fabs(variables[907])+1.0e-300));
    variables[911] = -variables[909];
    variables[913] = variables[909];
    variables[915] = variables[909];
    variables[917] = -variables[259]-variables[165]*variables[168]*log(fabs(states[9]/variables[237]*states[11]/variables[237]/(states[7]/variables[237]))+1.0e-300);
    variables[919] = variables[165]*variables[168]*(log(fabs(variables[32]/variables[237])+1.0e-300)-log(fabs(states[8]/variables[237])+1.0e-300))+variables[367]*variables[917];
    variables[922] = variables[920];
    variables[924] = -variables[918];
    variables[925] = variables[918];
    variables[926] = variables[364]*variables[53]*(variables[670]-states[1])*0.001;
    variables[927] = variables[298]*states[35]*variables[548]*(variables[33]-states[8]*exp(-variables[926]/(variables[165]*variables[168])))/(variables[89]+variables[33]);
    variables[928] = variables[165]*variables[168]*(log(fabs(variables[33]/variables[238])+1.0e-300)-log(fabs(states[8]/variables[238])+1.0e-300))+variables[926];
    variables[929] = variables[364]*variables[927];
    variables[930] = -variables[53]*variables[929]*1000000000.0;
    variables[931] = -variables[927];
    variables[932] = variables[927];
    variables[957] = variables[956]/variables[573];
    variables[982] = variables[981]/variables[573];
    variables[1007] = variables[1006]/variables[573];
    variables[1032] = variables[1031]/variables[573];
    variables[1057] = variables[1056]/variables[573];
    variables[1082] = variables[1081]/variables[573];
    variables[1107] = variables[1106]/variables[573];
    variables[1132] = variables[1131]/variables[573];
    variables[1157] = variables[1156]/variables[573];
    variables[1182] = variables[1181]/variables[573];
    variables[1207] = variables[1206]/variables[573];
    variables[1232] = variables[1231]/variables[573];
    variables[659] = variables[854]+variables[857]+variables[859]+variables[865]+variables[874]+variables[882]+variables[885]+variables[899]+variables[930];
    variables[662] = variables[855]+variables[875]+variables[887];
    variables[663] = variables[883]+variables[879];
    variables[665] = variables[858]+variables[886];
    variables[668] = variables[860]+variables[868]+variables[876]+variables[880]+variables[898]+variables[929]+variables[891];
    variables[667] = variables[699];
    variables[705] = variables[794]+variables[751]+variables[870]+variables[915]+variables[922];
    variables[703] = variables[894]+variables[925]+variables[932];
    variables[704] = variables[895]+variables[590]+variables[595];
    variables[1233] = variables[518]*states[36];
    variables[1234] = pow((1.0-variables[1233])/(1.0+variables[1233]), 2.0);
    variables[1235] = variables[519]*states[36];
    variables[1236] = variables[522]*states[36]*pow(states[36]/variables[521], 8.0);
    variables[1237] = variables[515]*((1.0-variables[520])*variables[1235]+variables[1236]);
    variables[1238] = -log(states[37]/variables[517])/2.302585092994046;
}
