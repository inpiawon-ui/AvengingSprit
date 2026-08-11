# AVENGING SPIRIT RE:BORN — README FIRST

Status: **FINAL LOCK — Document Sync v1.2**  
Date: 2026-08-11

## Source of Truth

1. **Developer Runtime v1.5** — Runtime JSON, Data Contract v1.5, Prefab Registry, Runtime Data, Level Design
2. **Host System v1.2** — Host identity and playstyle reference
3. **Core Combat Design v3.1 FINAL LOCK** — locked combat philosophy
4. **CH01–03 Experience Master v1.2** — room and experience intent
5. **Archive** — historical reference only; **DO NOT USE** for implementation

If any document conflicts with Developer Runtime v1.5, the Runtime wins. Do not tune values in the design workbooks.

## Implementation Locks

- Ghost Max HP 100; Tactical Possession **6 HP / 8 sec**; Death Possession 20; Ghost drain 2/sec.
- Tactical variants: A 8/8, B 6/8 default, C 5/10.
- Runtime enemy spawn source: **layout.enemySpawns only** — 104 records, 74 unique XY.
- Possessable enemies and candidates must resolve a valid HostID.
- CH1 Tag Buff pool is locked until CH1 clear.
- S01 H01→H11 is ALWAYS_BASE; BUF_T01 enhances it only.
- Exposure curve is CH1 6 → CH2 9 → CH3 12. Medium and Baseball first playable in CH3; Baseball first playable room CH3_N07.
- Bosses are NotPossessable; phase behavior uses Runtime bossPhases.
- DESIGN_EXPECTATION is pre-playtest intent. Measured data belongs only in PLAYTEST_RESULT.
- Growth currency vocabulary: Gold / Spirit Core / Host Memory / Gem.

## Files

- CORE_GDD_v1.2_FINAL_LOCK.xlsx
- HOST_SYSTEM_FINAL_LOCK_v1.2.xlsx
- CH01_03_VERTICAL_SLICE_MASTER_v1.2.xlsx
- DOCUMENT_SYNC_REPORT.xlsx
- DOCUMENT_CONFLICT_REPORT.xlsx

Final QA verdict: **PASS — 0 declared conflicts**.
