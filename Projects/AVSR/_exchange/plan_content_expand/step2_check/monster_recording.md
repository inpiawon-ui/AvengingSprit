# 2단계 몬스터 검증 녹화

통과 45 · 우려 3 · 미검 5 · 실패 0 — `Game.Editor.MonsterCheckRecorder`(1챕터 판 하나에서 몬스터를 차례로 세워 10초씩) · `python Projects/AVSR/Tools/monster_check_report.py`

내 몸 = 갱스터(무적 끔 · 체력 5000). 예고 · 자국 = 녹화 동안 동시에 떠 있던 최대 수. 원소 걸림 = 내 몸에 화상 · 냉기 · 독이 한 번이라도 걸렸나(독은 자국도 침).

| 몬스터 | 성질 | 원소 | 내 피해 | 예고 | 자국 | 판정 | 메모 | 영상 |
|---|---|---|---|---|---|---|---|---|
| skeleton | None |  | 0 | 0 | 0 | 우려 | 연사 몸(갱스터)에 맞을 때마다 예고(0.45초)가 끊겨 한 대도 못 침 — 예전 해골도 같다. 3단계 밸런스에서 본다 | `Projects/AVSR/_exchange/skillfx_now/clip_mon_skeleton.mp4` |
| skeleton_2 | DeathBurst4 |  | 11 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_skeleton_2.mp4` |
| skeleton_3 | Ambush |  | 41 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_skeleton_3.mp4` |
| skeleton_4 | Ambush |  | 49 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_skeleton_4.mp4` |
| skeleton_mage | Ring |  | 30 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_skeleton_mage.mp4` |
| skeleton_poison | None | Poison · 걸림=예 | 0 | 0 | 4 | 우려 | 독 자국은 깔린다(4). 몸으로 치는 독은 위와 같은 이유로 못 봄 | `Projects/AVSR/_exchange/skillfx_now/clip_mon_skeleton_poison.mp4` |
| bat | Dive |  | 52 | 1 | 3 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_bat.mp4` |
| bat_2 | Dive |  | 65 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_bat_2.mp4` |
| bat_bounce | BatBounce |  | 13 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_bat_bounce.mp4` |
| bat_poison | Dive | Poison · 걸림=예 | 72 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_bat_poison.mp4` |
| actor_enforcer | None |  | 84 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_actor_enforcer.mp4` |
| actor_enforcer_2 | Slam |  | 84 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_actor_enforcer_2.mp4` |
| actor_enforcer_3 | Slam |  | 135 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_actor_enforcer_3.mp4` |
| actor_enforcer_4 | Slam |  | 126 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_actor_enforcer_4.mp4` |
| gorilla_charger | Slam |  | 150 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_gorilla_charger.mp4` |
| gorilla_fire | Slam | Fire · 걸림=예 | 153 | 1 | 1 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_gorilla_fire.mp4` |
| scrapgunner | None |  | 8 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_scrapgunner.mp4` |
| scrapgunner_2 | GunnerBurst |  | 50 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_scrapgunner_2.mp4` |
| scrapgunner_3 | GunnerBurst |  | 50 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_scrapgunner_3.mp4` |
| scrapgunner_sniper | GunnerBurst |  | 41 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_scrapgunner_sniper.mp4` |
| scrapgunner_ice | GunnerBurst | Ice · 걸림=예 | 25 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_scrapgunner_ice.mp4` |
| roadwarden | None |  | 10 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_roadwarden.mp4` |
| roadwarden_2 | WardenBurst |  | 64 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_roadwarden_2.mp4` |
| roadwarden_3 | Spiral |  | 105 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_roadwarden_3.mp4` |
| roadwarden_fire | None | Fire · 걸림=예 | 78 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_roadwarden_fire.mp4` |
| coilwalker | None |  | 21 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_coilwalker.mp4` |
| coilwalker_2 | BounceShot |  | 42 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_coilwalker_2.mp4` |
| coilwalker_3 | SplitShot |  | 90 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_coilwalker_3.mp4` |
| coil_mortar | Mortar |  | 52 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_coil_mortar.mp4` |
| coilwalker_ice | Mortar | Ice · 걸림=예 | 52 | 1 | 1 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_coilwalker_ice.mp4` |
| obj_turret | None |  | 66 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_obj_turret.mp4` |
| obj_turret_2 | CrossAlt |  | 76 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_obj_turret_2.mp4` |
| obj_turret_3 | CrossStream |  | 42 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_obj_turret_3.mp4` |
| boar | Dive |  | 132 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_boar.mp4` |
| boar_2 | Dive |  | 190 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_boar_2.mp4` |
| mole | None |  | 50 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_mole.mp4` |
| mole_2 | MoleDouble |  | 87 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_mole_2.mp4` |
| mantis | None |  | 183 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_mantis.mp4` |
| mantis_2 | MantisDouble |  | 268 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_mantis_2.mp4` |
| armadillo | None |  | 38 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_armadillo.mp4` |
| armadillo_2 | ArmadilloRoll |  | 84 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_armadillo_2.mp4` |
| mushroom | None |  | 35 | 1 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_mushroom.mp4` |
| mushroom_2 | MushroomPool |  | 14 | 1 | 1 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_mushroom_2.mp4` |
| mummy | None |  | 36 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_mummy.mp4` |
| mummy_2 | MummyDouble |  | 80 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_mummy_2.mp4` |
| spider | None |  | 74 | 0 | 0 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_spider.mp4` |
| spider_2 | SpiderPool |  | 81 | 1 | 2 | 통과 |  | `Projects/AVSR/_exchange/skillfx_now/clip_mon_spider_2.mp4` |
| watcher_eye | Laser |  | 45 | 2 | 1 | 미검 | 행동은 됨 — 그림이 아직 없어 흰 네모로 섰다(그림 받은 뒤 다시 찍는다) | `Projects/AVSR/_exchange/skillfx_now/clip_mon_watcher_eye.mp4` |
| shadow_cat | Teleport |  | 20 | 1 | 0 | 미검 | 행동은 됨 — 그림이 아직 없어 흰 네모로 섰다(그림 받은 뒤 다시 찍는다) | `Projects/AVSR/_exchange/skillfx_now/clip_mon_shadow_cat.mp4` |
| saw_monkey | Boomerang |  | 228 | 0 | 0 | 우려 | 10초에 228 — 톱날이 갈 때 · 올 때 다 맞아 세다. 3단계 밸런스에서 줄인다 | `Projects/AVSR/_exchange/skillfx_now/clip_mon_saw_monkey.mp4` |
| mimic | Mimic |  | 55 | 0 | 0 | 미검 | 행동은 됨 — 그림이 아직 없어 흰 네모로 섰다(그림 받은 뒤 다시 찍는다) | `Projects/AVSR/_exchange/skillfx_now/clip_mon_mimic.mp4` |
| lantern | ShieldAlly |  | 0 | 0 | 0 | 미검 | 행동은 됨 — 그림이 아직 없어 흰 네모로 섰다(그림 받은 뒤 다시 찍는다) | `Projects/AVSR/_exchange/skillfx_now/clip_mon_lantern.mp4` |
| soul_hound | GhostHunt |  | — | 0 | 0 | 미검 | 몸을 버리자 유령이 곧바로 다시 빙의해 쫓을 시간이 없었다 — 몸이 없는 방에서 다시 잰다 | `Projects/AVSR/_exchange/skillfx_now/clip_mon_soul_hound.mp4` |
