# Food uptake at 2 Ga, food particles, sugar transporters, and extracellular space

Literature summary (2026-10-12) for the environment-field and level design. Confidence: [H] high, [M] medium,
[L] low. Several DOIs were written from memory and are marked "check".

## Engulf or absorb
- At 2.0 Ga the cell absorbs; it cannot engulf. Phagocytosis is eukaryotic, at or after eukaryogenesis: LECA ~1.87-1.68
  Ga (Parfrey 2011) or 1.8-1.0 Ga (Betts 2018); first eukaryote-grade fossils ~1.65-1.56 Ga (Zhu 2016; Miao 2024);
  stem-eukaryote biomarkers from ~1.6 Ga (Brocks 2023). [H]
- Prokaryote exceptions are modern and lineage-specific: *Ca.* Uab amorphum engulfs cells (Shiratori 2019);
  *Gemmata* takes up protein by an endocytosis-like route (Lonhienne 2010; its "compartments" are envelope
  invaginations, Boedeker 2017); the cultured Asgard archaeon makes protrusions but does not engulf (Imachi 2020). [H]
- Particle-attached bacteria digest outside the cell: surface or secreted glycosidases and proteases, then import of
  monomers and oligomers (Azam & Malfatti 2007; Arnosti 2011). Hydrolysis outpaces uptake on aggregates and much of the
  product leaks away as dissolved organic matter (Smith 1992). [H]
- A sphere of radius a captures a molecule released at distance r with probability a/r: product released at the
  surface is mostly kept, 10 µm away ~10%. Secreted enzymes pay off only on dense substrate or in clusters (Vetter 1998;
  Ebrahimi 2019); clearing the products speeds degradation >10x (Alcolombri 2021). [H physics, M field numbers]
- "Selfish" uptake: Bacteroidetes and Planctomycetes pull >600 Da oligosaccharides into the periplasm without loss, up
  to 26% of Atlantic cells (Reintjes 2017): the closest real thing to enveloping food, at the molecular scale. [H]

## Particle sizes and how fast they disappear
- Dissolved organic matter < 0.2-0.7 µm; colloids 1 nm-1 µm, 10^7-10^9 per mL (Koike 1990; Wells & Goldberg 1991);
  gels and microaggregates 3 µm to hundreds of µm (Alldredge 1993); marine snow > 0.5 mm (Alldredge & Silver 1988). At
  2 Ga: dead cells, cyanobacterial mucilage, mat fragments, no faecal pellets. [H, M for 2 Ga]
- A particle of free sugar is not physical: a glucose crystal dissolves in t = rho r^2/(2 D c_sat), ~1e-4 s at r =
  0.25 µm and ~3 s at 50 µm. Food packets are polymer (glycogen- or starch-like polysaccharide, protein, dead cells),
  lost at the rate enzymes allow. [H]
- Loss rates: aggregates ~0.1 per day (Iversen & Ploug 2010, check); model particles days still and ~10x faster in
  flow (Alcolombri 2021; Ebrahimi 2019). [M]
- A food packet for a 2 µm cell: a 0.5-2 µm polymer particle, about one dead cell, 0.05-0.5 pg C, gone in minutes to
  hours with cell-surface enzymes (faster with more enzyme, a smaller particle (rate ~ 1/r) or flow). [M/L, judgement]

## Sugar transporters
| type | glucose Km | cost per glucose | age |
|---|---|---|---|
| PTS (phosphotransferase, PtsG) | ~3-10 µM | 1 PEP, which also phosphorylates the sugar: a fermenter keeps its 2 ATP net | bacteria only; plausibly > 2 Ga [L/M] |
| ABC (binding protein) | ~0.1-1 µM (Mgl; Ferenci 1996) | ~2 ATP per cycle | all domains, LUCA-age [H/M] |
| H+ symport (MFS: LacY, GalP) | ~10 µM-1 mM | 1 H+ (~0.25-0.33 ATP) | all domains, ancient [H/M] |
| facilitated diffusion (Glf, *Z. mobilis*) | 4.1 mM | none | uniporters generic; Glf modern [M] |

Sources: Deutscher 2006; Davidson 2008; Pao 1998; Weisser 1995. Turnover ~10-1000 per s per transporter [M]. A
homolactic fermenter (2 ATP per glucose) cannot afford ABC import of single glucose: lactic acid bacteria use the PTS
for glucose and ABC for oligosaccharides. Lactate exported with H+ can itself make proton-motive force (Michels 1979,
check). [M]

## Diffusion-limited uptake
- J_max = 4 pi D a C (Berg & Purcell 1977): ~5x10^6 glucose per s per µM at a = 1 µm. [H]
- With N transporters of capture radius s ~ 1 nm, J = J_max N s/(N s + pi a): ~3,000 give half the limit, ~10^4 ~75%,
  covering 0.1-1% of the surface. [H]
- A 2 µm fermenter doubling in 1 h needs ~10^7 glucose per s: diffusion-limited below ~2 µM local glucose, and needs
  ~2x10^4 transporters at 500 per s. [M] At low glucose, affinity, staying on the particle and making food near the cell
  help; at high glucose, transporter number and speed.

## Extracellular space between cells
- Prokaryotes: biofilms and mats from 3.48 Ga (Noffke 2013) and stromatolites 3.43 Ga (Allwood 2006) [H]. The matrix
  (EPS, ~90% of biofilm dry mass; Flemming & Wingender 2010) slows diffusion to ~0.25-0.9 of water (Stewart 2003, check)
  and O2 reaches only ~50-100 µm into active biofilms (Stewart & Franklin 2008): a shared, unsealed space with gradients.
- Filamentous cyanobacteria: multicellularity ~2.5-2.4 Ga (Schirrmeister 2015) [M]; a continuous periplasm under one
  outer membrane, septal junctions between cells (Flores 2006, check; Flores & Herrero 2010; Mullineaux 2008) [H];
  heterocysts, fossil akinetes possibly ~2.1 Ga, secure 1.65-1.4 Ga (Tomitani 2006) [M/L].
- Eukaryotes: Francevillian 2.1 Ga macrofossils contested (El Albani 2010) [L]; multicellular fossils ~1.56 Ga (Zhu
  2016), *Qingshania* ~1.63 Ga (Miao 2024) [M]; *Bangiomorpha* 1.047 Ga (Gibson 2018) [H]; cadherins and integrins in
  holozoans (King 2008; Sebé-Pedrós 2010, check); collagen IV and basement membranes with animals (Fidler 2017) [H].
- A regulated interstitial fluid needs a sealing epithelium plus transport across it: sponges have one (Adams 2010;
  Leys & Riesgo 2012, check); animals ~0.8-0.65 Ga (dos Reis 2015, check), sponge biomarkers > 635 Ma (Love 2009, check).
- For the game: at level 4 (~1.8 Ga, prokaryote) a biofilm or filament colony is defensible: boundary cells face the
  sea, inner cells sit in a shared matrix (or shared periplasm), unsealed, with slowed diffusion and O2 falling inwards.
  A sealed, regulated interstitial fluid belongs to animals (level 7); adhesion and matrix proteins to level 6.

## References
Abreu 2007 IJSEM 10.1099/ijs.0.64857-0; Adams, Goss & Leys 2010 PLoS ONE 10.1371/journal.pone.0015040; Alcolombri 2021
Nat Geosci 10.1038/s41561-021-00817-x; Alldredge & Silver 1988 Prog Oceanogr 10.1016/0079-6611(88)90053-5; Alldredge,
Passow & Logan 1993 Deep-Sea Res I 10.1016/0967-0637(93)90129-Q; Allwood 2006 Nature 10.1038/nature04764; Arnosti 2011
Annu Rev Mar Sci 10.1146/annurev-marine-120709-142731; Azam & Malfatti 2007 Nat Rev Microbiol 10.1038/nrmicro1747;
Berg & Purcell 1977 Biophys J 10.1016/S0006-3495(77)85544-6; Betts 2018 Nat Ecol Evol 10.1038/s41559-018-0644-x
(check); Boedeker 2017 Nat Commun 10.1038/ncomms14853 (check); Brocks 2023 Nature 10.1038/s41586-023-06170-w; Burns,
Pittis & Kim 2018 Nat Ecol Evol 10.1038/s41559-018-0477-7; Davidson 2008 MMBR 10.1128/MMBR.00031-07; Deutscher 2006 MMBR
10.1128/MMBR.00024-06; dos Reis 2015 Curr Biol 10.1016/j.cub.2015.09.066 (check); Ebrahimi, Schwartzman & Cordero 2019
PNAS 10.1073/pnas.1908512116; El Albani 2010 Nature 10.1038/nature09166; Ferenci 1996 FEMS Microbiol Rev
10.1111/j.1574-6976.1996.tb00226.x; Fidler 2017 eLife 10.7554/eLife.24176; Flemming & Wingender 2010 Nat Rev Microbiol
10.1038/nrmicro2415; Flores 2006 Trends Microbiol 10.1016/j.tim.2006.08.007 (check); Flores & Herrero 2010 Nat Rev
Microbiol 10.1038/nrmicro2242; Gibson 2018 Geology 10.1130/G39829.1; Imachi 2020 Nature 10.1038/s41586-019-1916-6;
Iversen & Ploug 2010 Biogeosciences 10.5194/bg-7-2613-2010 (check); King 2008 Nature 10.1038/nature06617; Koike 1990
Nature 10.1038/345242a0; Leys & Riesgo 2012 J Exp Zool B 10.1002/jez.b.21442 (check); Lonhienne 2010 PNAS
10.1073/pnas.1001085107; Love 2009 Nature 10.1038/nature07673 (check); Miao 2024 Sci Adv 10.1126/sciadv.adk3208;
Michels 1979 FEMS Microbiol Lett 10.1111/j.1574-6968.1979.tb03258.x (check); Mullineaux 2008 EMBO J
10.1038/emboj.2008.18; Noffke 2013 Astrobiology 10.1089/ast.2013.1030; Pao, Paulsen & Saier 1998 MMBR
10.1128/MMBR.62.1.1-34.1998; Parfrey 2011 PNAS 10.1073/pnas.1110633108 (check); Reintjes 2017 ISME J
10.1038/ismej.2017.26; Schirrmeister, Gugger & Donoghue 2015 Palaeontology 10.1111/pala.12178; Sebé-Pedrós 2010 Mol Biol
Evol 10.1093/molbev/msq089 (check); Shiratori 2019 Nat Commun 10.1038/s41467-019-13499-2; Smith, Simon, Alldredge & Azam
1992 Nature 10.1038/359139a0; Stewart 2003 J Bacteriol 10.1128/JB.185.5.1485-1491.2003 (check); Stewart & Franklin 2008
Nat Rev Microbiol 10.1038/nrmicro1838; Tomitani 2006 PNAS 10.1073/pnas.0600999103; Weisser 1995 J Bacteriol
10.1128/jb.177.11.3351-3354.1995; Wells & Goldberg 1991 Nature 10.1038/353342a0; Zhu 2016 Nat Commun (no DOI given).
