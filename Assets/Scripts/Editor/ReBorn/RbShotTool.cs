// RE:BORN UI 리뉴얼(2026-10-23) — 새 화면을 플레이에서 해상도 · 언어 · 상태별로 찍는다(실제 데이터).
// 결과: Projects/AVSR/_exchange/ref/ui_reborn_1023/play/{이름}.png · 끝나면 플레이를 끄고 언어를 한국어(0)로 되돌린다.
// ⚠ EditorApplication.update 안에서는 예외를 흘리지 않는다(LobbyShotTool 머리말 — 에디터가 멎은 적이 있다).
using System.IO;
using Game.Module.Common;
using Game.Module.Lobby;
using Game.User;
using GameFramework.Core.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor.ReBorn
{
    [InitializeOnLoad]
    internal static class RbShotTool
    {
        private const string Key = "avsr.rbshot";
        private const string OutDir = "Projects/AVSR/_exchange/ref/ui_reborn_1023/play";

        /// <summary>상태 이름 — battle · host · growth:{hostKey}(비우면 가진 몸 중 첫째).</summary>
        private static readonly (int w, int h, Language lang, string state, string name)[] Shots =
        {
            (720, 1280, Language.Korean, "battle", "lobby_16x9"),
            (1080, 2400, Language.Korean, "battle", "lobby_b_20x9"),
            (768, 1024, Language.Korean, "battle", "lobby_b_4x3"),
            (768, 1024, Language.Korean, "shop:best", "shop_best_4x3"),
            (768, 1024, Language.Korean, "shop:today", "shop_today_4x3"),
            (768, 1024, Language.Korean, "shop:chest", "shop_chest_4x3"),
            (768, 1024, Language.Korean, "shop:currency", "shop_currency_4x3"),
            (768, 1024, Language.Korean, "summon", "summon_4x3"),
            (768, 1024, Language.Korean, "challenge", "challenge_4x3"),
            (720, 1280, Language.Japanese, "battle", "lobby_ja"),
            (720, 1280, Language.English, "battle", "lobby_en"),
            (720, 1280, Language.Korean, "battle:4@0.05", "lobby_pick_early"),
            (720, 1280, Language.Korean, "battle:5@0.3", "lobby_pick_mid"),
            (720, 1280, Language.Korean, "battle:1", "lobby_ghost_16x9"),
            (720, 1280, Language.Korean, "battle:2", "lobby_host_16x9"),
            (1080, 2400, Language.Korean, "battle:2", "lobby_host_20x9"),
            (720, 1280, Language.Korean, "battle:paren", "lobby_paren_16x9"),
            (720, 1280, Language.Japanese, "battle:paren", "lobby_paren_ja"),
            (720, 1280, Language.English, "battle:paren", "lobby_paren_en"),
            (720, 1280, Language.Korean, "rail:Ghost", "ghost_16x9"),
            (1080, 2400, Language.Korean, "rail:Ghost", "ghost_20x9"),
            (720, 1280, Language.Korean, "rail:Missions", "missions_16x9"),
            (720, 1280, Language.Korean, "rail:Pass", "pass_16x9"),
            (720, 1280, Language.Korean, "rail:Mail", "mail_16x9"),
            (720, 1280, Language.Korean, "rail:Daily", "daily_16x9"),
            (720, 1280, Language.Korean, "rail:Settings", "settings_16x9"),
            (720, 1280, Language.Korean, "rail:Language", "language_16x9"),
            (720, 1280, Language.Korean, "rail:Profile", "profile_16x9"),
            (1080, 2400, Language.Korean, "rail:Profile", "profile_20x9"),
            (768, 1024, Language.Korean, "rail:Profile", "profile_4x3"),
            (720, 1280, Language.English, "rail:Profile", "profile_en"),
            (720, 1280, Language.Japanese, "rail:ProfileTitle", "profile_title_ja"),
            (720, 1280, Language.English, "rail:Pass", "pass_en"),
            (720, 1280, Language.Japanese, "rail:Missions", "missions_ja"),
            (720, 1280, Language.Korean, "challenge", "challenge_16x9"),
            (720, 1280, Language.Korean, "challenge:tower", "tower_16x9"),
            (720, 1280, Language.Korean, "challenge:night", "night_16x9"),
            (720, 1280, Language.Korean, "challenge:night:d1", "night_d1_16x9"),
            (720, 1280, Language.Korean, "challenge:night:d2", "night_d2_16x9"),
            (720, 1280, Language.Korean, "challenge:night:d3", "night_d3_16x9"),
            (720, 1280, Language.Korean, "challenge:night:d4", "night_d4_16x9"),
            (720, 1280, Language.Korean, "challenge:night:d5", "night_d5_16x9"),
            (720, 1280, Language.Korean, "challenge:night:d6", "night_d6_16x9"),
            (1080, 2400, Language.Korean, "challenge:night", "night_20x9"),
            (768, 1024, Language.Korean, "challenge:night", "night_4x3"),
            (1080, 2400, Language.Korean, "challenge:tower", "tower_20x9"),
            (768, 1024, Language.Korean, "challenge:tower", "tower_4x3"),
            (1080, 2400, Language.Korean, "challenge", "challenge_20x9"),
            (720, 1280, Language.English, "challenge:tower", "tower_en"),
            (720, 1280, Language.Japanese, "challenge:night", "night_ja"),
            (720, 1280, Language.Korean, "shop:best", "shop_best_16x9"),
            (720, 1280, Language.Korean, "shop:today", "shop_today_16x9"),
            (720, 1280, Language.Korean, "shop:chest", "shop_chest_16x9"),
            (720, 1280, Language.Korean, "shop:currency", "shop_currency_16x9"),
            (1080, 2400, Language.Korean, "shop:best", "shop_best_20x9"),
            (720, 1280, Language.Japanese, "shop:currency", "shop_currency_ja"),
            (720, 1280, Language.Korean, "summon", "summon_16x9"),
            (1080, 2400, Language.Korean, "summon", "summon_20x9"),
            (720, 1280, Language.English, "summon", "summon_en"),
            (720, 1280, Language.Korean, "summon:result10", "summon_result10_16x9"),
            (720, 1280, Language.Korean, "summon:result1", "summon_result1_16x9"),
            (720, 1280, Language.Korean, "summon:rates", "summon_rates_16x9"),
            (720, 1280, Language.Korean, "summon:records", "summon_records_16x9"),
            (720, 1280, Language.Korean, "host", "host_16x9"),
            (1080, 2400, Language.Korean, "host", "host_20x9"),
            (768, 1024, Language.Korean, "host", "host_4x3"),
            (720, 1280, Language.Japanese, "host", "host_ja"),
            (720, 1280, Language.English, "host", "host_en"),
            (720, 1280, Language.Korean, "host:f1", "hostchip_force_ko"),
            (720, 1280, Language.Japanese, "host:f2", "hostchip_blade_ja"),
            (720, 1280, Language.English, "host:f2", "hostchip_blade_en"),
            (720, 1280, Language.ChineseTraditional, "host:f3", "hostchip_magic_zht"),
            (720, 1280, Language.ChineseSimplified, "host:f1", "hostchip_force_zhs"),
            (768, 1024, Language.English, "host:f2", "hostchip_blade_en_4x3"),
            (720, 1280, Language.Korean, "growth:low", "growth_low_16x9"),
            (720, 1280, Language.Korean, "growth:scroll_low", "growth_low_16x9_bottom"),
            (720, 1280, Language.Korean, "host:low", "host_low_16x9"),
            (720, 1280, Language.Korean, "growth:", "growth_16x9"),
            (720, 1280, Language.Korean, "growth:poor", "growth_poor_16x9"),
            // 발밑 그림자 — 발 자리가 칸 가운데에서 먼 몸들(갱스터 42% · 폭력배 42% · 드라군 59% · 샐러맨더 55%)과 가운데인 몸
            (720, 1280, Language.Korean, "growth:k=gangster", "growthshadow_gangster"),
            (720, 1280, Language.Korean, "growth:k=thug", "growthshadow_thug"),
            (720, 1280, Language.Korean, "growth:k=dragoon", "growthshadow_dragoon"),
            (720, 1280, Language.Korean, "growth:k=salamander", "growthshadow_salamander"),
            (720, 1280, Language.Korean, "growth:k=amazon", "growthshadow_amazon"),
            (720, 1280, Language.Korean, "growth:k=commando_mg", "growthshadow_commando_mg"),
            // 강화 연출(노란 레벨업) 낱장 — 0.6초에 틀고 0.04초 간격(GIF 로 묶는다)
            (720, 1280, Language.Korean, "growth:tick@0.60", "growthtick_00"),
            (720, 1280, Language.Korean, "growth:tick@0.64", "growthtick_01"),
            (720, 1280, Language.Korean, "growth:tick@0.68", "growthtick_02"),
            (720, 1280, Language.Korean, "growth:tick@0.72", "growthtick_03"),
            (720, 1280, Language.Korean, "growth:tick@0.76", "growthtick_04"),
            (720, 1280, Language.Korean, "growth:tick@0.80", "growthtick_05"),
            (720, 1280, Language.Korean, "growth:tick@0.84", "growthtick_06"),
            (720, 1280, Language.Korean, "growth:tick@0.88", "growthtick_07"),
            (720, 1280, Language.Korean, "growth:tick@0.94", "growthtick_08"),
            (720, 1280, Language.Korean, "growth:tick@1.02", "growthtick_09"),
            (720, 1280, Language.Korean, "growth:tick@1.12", "growthtick_10"),
            (720, 1280, Language.Korean, "growth:scroll", "growth_16x9_bottom"),
            (1080, 2400, Language.Korean, "growth:", "growth_20x9"),
            (768, 1024, Language.Korean, "growth:", "growth_4x3"),
            (720, 1280, Language.Japanese, "growth:", "growth_ja"),
            (720, 1280, Language.English, "growth:", "growth_en"),
            (720, 1280, Language.ChineseSimplified, "growth:", "growth_zhs"),
            (720, 1280, Language.Korean, "growth:ask_low", "growthpop_ask_16x9"),
            (720, 1280, Language.Korean, "growth:done_low", "growthpop_done_16x9"),
            (720, 1280, Language.Korean, "growth:skill", "growthpop_skill_16x9"),
            (1080, 2400, Language.Korean, "growth:ask_low", "growthpop_ask_20x9"),
            (768, 1024, Language.Korean, "growth:skill", "growthpop_skill_4x3"),
            (720, 1280, Language.English, "growth:ask_low", "growthpop_ask_en"),
            (720, 1280, Language.Japanese, "growth:done_low", "growthpop_done_ja"),
            (720, 1280, Language.English, "growth:skill", "growthpop_skill_en"),
            (720, 1280, Language.Korean, "random:ask", "random_ask_16x9"),
            (720, 1280, Language.Korean, "random:reveal", "random_reveal_16x9"),
            (720, 1280, Language.Korean, "random:hidden", "random_hidden_16x9"),
            (1080, 2400, Language.Korean, "random:reveal", "random_reveal_20x9"),
            (768, 1024, Language.Korean, "random:ask", "random_ask_4x3"),
            (720, 1280, Language.English, "random:reveal", "random_reveal_en"),
            (720, 1280, Language.Japanese, "random:hidden", "random_hidden_ja"),
            (720, 1280, Language.Korean, "chapters", "chapters_16x9"),
            (1080, 2400, Language.Korean, "chapters", "chapters_20x9"),
            (768, 1024, Language.Korean, "chapters", "chapters_4x3"),
            (720, 1280, Language.English, "chapters", "chapters_en"),
            (720, 1280, Language.Japanese, "chapters", "chapters_ja"),
            (720, 1280, Language.Korean, "rail:PassOptions", "notice_passopt_16x9"),
            (720, 1280, Language.English, "rail:PassOptions", "notice_passopt_en"),
            (720, 1280, Language.Korean, "notice:reward", "notice_reward_16x9"),
            (720, 1280, Language.Korean, "notice:reward4", "notice_reward4_16x9"),
            (720, 1280, Language.Korean, "notice:stamina", "notice_stamina_16x9"),
            (720, 1280, Language.Korean, "notice:gem", "notice_gem_16x9"),
            (720, 1280, Language.Korean, "notice:gold", "notice_gold_16x9"),
            (720, 1280, Language.Korean, "notice:confirm", "notice_confirm_16x9"),
            (768, 1024, Language.Korean, "notice:gold", "notice_gold_4x3"),
            (1080, 2400, Language.Korean, "notice:reward", "notice_reward_20x9"),
            (720, 1280, Language.English, "notice:gold", "notice_gold_en"),
            (720, 1280, Language.Japanese, "notice:stamina", "notice_stamina_ja"),
            (720, 1280, Language.English, "notice:reward4", "notice_reward4_en"),
            (720, 1280, Language.Korean, "host:lock_low", "hostlock_locked_16x9"),
            (720, 1280, Language.Korean, "host:lockready_low", "hostlock_ready_16x9"),
            (1080, 2400, Language.Korean, "host:lock_low", "hostlock_locked_20x9"),
            (768, 1024, Language.Korean, "host:lock_low", "hostlock_locked_4x3"),
            (720, 1280, Language.English, "host:lock_low", "hostlock_locked_en"),
            (720, 1280, Language.Korean, "host:reveal", "hostlock_new_16x9"),
            (720, 1280, Language.Korean, "host:awaken6", "hostlock_awaken6_16x9"),
            (720, 1280, Language.Korean, "host:awaken10", "hostlock_awaken10_16x9"),
            (1080, 2400, Language.Korean, "host:reveal", "hostlock_new_20x9"),
            (768, 1024, Language.Korean, "host:reveal", "hostlock_new_4x3"),
            (720, 1280, Language.Japanese, "host:reveal", "hostlock_new_ja"),
            (720, 1280, Language.Korean, "growth:sources_low", "growthsrc_sources_16x9"),
            (720, 1280, Language.Korean, "growth:sources_essence_low", "growthsrc_essence_16x9"),
            (768, 1024, Language.Korean, "growth:sources_low", "growthsrc_sources_4x3"),
            (720, 1280, Language.English, "growth:sources_low", "growthsrc_sources_en"),
            (720, 1280, Language.Japanese, "growth:sources_essence_low", "growthsrc_essence_ja"),
            // 남은 화면(2026-10-24) — 안내 · 상자 열기
            (720, 1280, Language.Korean, "guide", "extra_guide_16x9"),
            (720, 1280, Language.Korean, "guide:2", "extra_guide_p2_16x9"),
            (1080, 2400, Language.Korean, "guide", "extra_guide_20x9"),
            (768, 1024, Language.Korean, "guide", "extra_guide_4x3"),
            (720, 1280, Language.English, "guide:2", "extra_guide_p2_en"),
            (720, 1280, Language.Japanese, "guide", "extra_guide_ja"),
            (720, 1280, Language.Korean, "chest:gold", "extra_chest_closed_16x9"),
            (720, 1280, Language.Korean, "chest:gold:open", "extra_chest_open_16x9"),
            (720, 1280, Language.Korean, "chest:platinum:open", "extra_chest_open4_16x9"),
            (1080, 2400, Language.Korean, "chest:gold:open", "extra_chest_open_20x9"),
            (768, 1024, Language.Korean, "chest:silver", "extra_chest_closed_4x3"),
            (720, 1280, Language.English, "chest:gold:open", "extra_chest_open_en"),
            // 아웃게임 연출(2026-10-24) — 「@초」 뒤에 찍는다(움직임을 장면별로 → GIF). 같은 연출은 장마다 다시 튼다
            (720, 1280, Language.Korean, "fx:newbody@0.12", "fx_newbody_1"),
            (720, 1280, Language.Korean, "fx:newbody@0.35", "fx_newbody_2"),
            (720, 1280, Language.Korean, "fx:newbody@0.7", "fx_newbody_3"),
            (720, 1280, Language.Korean, "fx:newbody@2", "fx_newbody_4"),
            (720, 1280, Language.Korean, "fx:awaken6@0.12", "fx_awaken6_1"),
            (720, 1280, Language.Korean, "fx:awaken6@0.35", "fx_awaken6_2"),
            (720, 1280, Language.Korean, "fx:awaken6@0.7", "fx_awaken6_3"),
            (720, 1280, Language.Korean, "fx:awaken6@2", "fx_awaken6_4"),
            (720, 1280, Language.Korean, "fx:awaken10@0.12", "fx_awaken10_1"),
            (720, 1280, Language.Korean, "fx:awaken10@0.35", "fx_awaken10_2"),
            (720, 1280, Language.Korean, "fx:awaken10@0.7", "fx_awaken10_3"),
            (720, 1280, Language.Korean, "fx:awaken10@2", "fx_awaken10_4"),
            (720, 1280, Language.Korean, "fx:star@0.08", "fx_star_1"),
            (720, 1280, Language.Korean, "fx:star@0.2", "fx_star_2"),
            (720, 1280, Language.Korean, "fx:star@0.5", "fx_star_3"),
            (720, 1280, Language.Korean, "fx:upgrade@0.06", "fx_upgrade_1"),
            (720, 1280, Language.Korean, "fx:upgrade@0.15", "fx_upgrade_2"),
            (720, 1280, Language.Korean, "fx:upgrade@0.4", "fx_upgrade_3"),
            (720, 1280, Language.Korean, "fx:shine@0.2", "fx_shine_1"),
            (720, 1280, Language.Korean, "fx:shine@0.3", "fx_shine_2"),
            (720, 1280, Language.Korean, "fx:shine@0.4", "fx_shine_3"),
            (720, 1280, Language.Korean, "fx:chest_silver@0.4", "fx_chestsilver_0"),
            (720, 1280, Language.Korean, "fx:chest_silver:open@0.32", "fx_chestsilver_1"),
            (720, 1280, Language.Korean, "fx:chest_silver:open@0.5", "fx_chestsilver_2"),
            (720, 1280, Language.Korean, "fx:chest_gold@0.4", "fx_chestgold_0"),
            (720, 1280, Language.Korean, "fx:chest_gold:open@0.32", "fx_chestgold_1"),
            (720, 1280, Language.Korean, "fx:chest_gold:open@0.5", "fx_chestgold_2"),
            (720, 1280, Language.Korean, "fx:chest_gold:open@0.75", "fx_chestgold_3"),
            (720, 1280, Language.Korean, "fx:chest_gold:open@1.0", "fx_chestgold_4"),
            (720, 1280, Language.Korean, "fx:chest_gold:open@1.5", "fx_chestgold_5"),
            (720, 1280, Language.Korean, "fx:chest_gold:open@1.8", "fx_chestgold_6"),
            (720, 1280, Language.Korean, "fx:chest_platinum@0.4", "fx_chestplatinum_0"),
            (720, 1280, Language.Korean, "fx:chest_platinum:open@0.32", "fx_chestplatinum_1"),
            (720, 1280, Language.Korean, "fx:chest_platinum:open@0.5", "fx_chestplatinum_2"),
            (720, 1280, Language.Korean, "fx:chest_platinum:open@0.85", "fx_chestplatinum_3"),
            (720, 1280, Language.Korean, "fx:reward@0.1", "fx_reward_1"),
            (720, 1280, Language.Korean, "fx:reward@0.3", "fx_reward_2"),
            (720, 1280, Language.Korean, "fx:reward@0.75", "fx_reward_3"),
            (720, 1280, Language.Korean, "fx:reward@1.0", "fx_reward_4"),
            (720, 1280, Language.Korean, "fx:reward@1.6", "fx_reward_5"),
            // PD 결정 반영(2026-10-25) — 성급 강화 성공 전체 화면 · 강화 「전투력 +n」 띠
            (720, 1280, Language.Korean, "growth:done_low", "now_stardone_16x9"),
            (1080, 2400, Language.Korean, "growth:done_low", "now_stardone_20x9"),
            (720, 1280, Language.English, "growth:done_low", "now_stardone_en"),
            (720, 1280, Language.Korean, "fx:powerup@0.3", "now_powerup_16x9"),
        };

        /// <summary>인게임 — 로비에서 출전을 눌러 판에 들어간 뒤 판을 거의 세우고(timeScale) 찍는다. levelup 은 3택1 을 바로 연다.</summary>
        private static readonly (int w, int h, Language lang, string state, string name)[] InGameShots =
        {
            (720, 1280, Language.Korean, "ingame", "fight_16x9"),
            (1080, 2400, Language.Korean, "ingame", "fight_20x9"),
            (768, 1024, Language.Korean, "ingame", "fight_4x3"),
            (1200, 1920, Language.Korean, "ingame", "fight_16x10"),   // 16:10 태블릿(PD 2026-10-27 「Lv 게이지랑 UI 가 화면을 삐져나간다」)
            (720, 1280, Language.Korean, "ingame:levelup", "levelup_16x9"),
            (720, 1280, Language.Japanese, "ingame:levelup", "levelup_ja"),
            (720, 1280, Language.English, "ingame:levelup", "levelup_en"),
            (1080, 2400, Language.Korean, "ingame:levelup", "levelup_20x9"),
            (768, 1024, Language.Korean, "ingame:levelup", "levelup_4x3"),
            (1080, 2400, Language.Korean, "ingame:result_fail", "result_fail_20x9"),
            (768, 1024, Language.Korean, "ingame:result_clear", "result_clear_4x3"),
            (720, 1280, Language.English, "ingame:result_fail", "result_fail_en"),
            (1080, 2400, Language.Korean, "ingame:result_clear6", "result_hosts6_20x9"),
            (720, 1280, Language.Korean, "ingame:result_clear6", "result_hosts6_16x9"),
            (720, 1280, Language.Korean, "ingame:shop", "shop_16x9"),
            (720, 1280, Language.Japanese, "ingame:shop", "shop_ja"),
            (720, 1280, Language.Korean, "ingame:event", "event_16x9"),
            (720, 1280, Language.English, "ingame:event", "event_en"),
            // 「내준다 / 받는다」 두 줄 판(PD 2026-10-27) — 대가 · 보상 종류마다 한 장
            // 4챕터 방 전체 기믹 「번개 + 물웅덩이」 — 1번 방에서 바로 켜고 3.2초를 0.07초마다 녹화(storm_rec/) · 끝에 한 장(PD 2026-10-27)
            (720, 1280, Language.Korean, "ingame:storm@3.5", "storm_16x9"),
            // 방 전체 기믹 시안 바탕 — SessionState .chapter 로 챕터를 정하고 찍는다(이름 끝 숫자는 사람이 붙이는 것, 2026-10-27)
            (720, 1280, Language.Korean, "ingame", "hazard_base"),
            // 2 · 3 · 5챕터 방 전체 기믹 — SessionState .chapter 로 그 챕터 무대에서 켜고 녹화(hazard_rec/) · PD 2026-10-27 「추천대로 가자」
            (720, 1280, Language.Korean, "ingame:hazard:bomb_target@3.5", "hazard_bomb_16x9"),
            (720, 1280, Language.Korean, "ingame:hazard:neon_tracker@3.5", "hazard_neon_16x9"),
            (720, 1280, Language.Korean, "ingame:hazard:gas_band@4.5", "hazard_gas_16x9"),
            (720, 1280, Language.Korean, "ingame:event:EV_CH2_06", "event_altar_16x9"),
            (720, 1280, Language.Korean, "ingame:event:EV_CH2_06:accept", "event_altar_accept_16x9"),
            (720, 1280, Language.Korean, "ingame:event:EV_CH1_03", "event_safe_16x9"),
            (720, 1280, Language.Korean, "ingame:event:EV_DEAL_01", "event_pact_16x9"),
            (720, 1280, Language.Korean, "ingame:event:EV_CH3_02", "event_duel_16x9"),
            (720, 1280, Language.Korean, "ingame:event:EV_CH2_04", "event_cross_16x9"),
            (720, 1280, Language.Korean, "ingame:event:EV_CH1_02", "event_supply_16x9"),
            (1080, 2400, Language.Korean, "ingame:event:EV_CH2_06", "event_altar_20x9"),
            (768, 1024, Language.Korean, "ingame:event:EV_CH2_06", "event_altar_4x3"),
            (720, 1280, Language.English, "ingame:event:EV_DEAL_01", "event_pact_en"),
            (720, 1280, Language.Japanese, "ingame:event:EV_CH2_04", "event_cross_ja"),
            (720, 1280, Language.Korean, "ingame:shrine", "shrine_16x9"),
            (1080, 2400, Language.Korean, "ingame:shrine", "shrine_20x9"),
            (720, 1280, Language.Korean, "ingame:result_clear", "result_clear_16x9"),
            (720, 1280, Language.Korean, "ingame:result_fail", "result_fail_16x9"),
            (1080, 2400, Language.Korean, "ingame:result_clear", "result_clear_20x9"),
            (720, 1280, Language.Japanese, "ingame:result_clear", "result_clear_ja"),
            (768, 1024, Language.English, "ingame:result_fail", "result_fail_4x3"),
            (720, 1280, Language.Korean, "ingame:tower_clear", "moderesult_tower_clear_16x9"),
            (720, 1280, Language.Korean, "ingame:tower_fail", "moderesult_tower_fail_16x9"),
            (720, 1280, Language.Korean, "ingame:night", "moderesult_night_16x9"),
            (720, 1280, Language.English, "ingame:night", "moderesult_night_en"),
            (720, 1280, Language.Japanese, "ingame:tower_clear", "moderesult_tower_clear_ja"),
            (1080, 2400, Language.Korean, "ingame:tower_clear", "moderesult_tower_clear_20x9"),
            (720, 1280, Language.Korean, "ingame:pause", "pause_16x9"),
            // 카드가 찬 일시정지(등급 넷 섞어 5장 · 3장은 Lv 2) — PD 2026-10-27 「카드 찬 모습까지 확인」
            (720, 1280, Language.Korean, "ingame:pause_cards", "pause_cards_16x9"),
            (1080, 2400, Language.Korean, "ingame:pause_cards", "pause_cards_20x9"),
            (768, 1024, Language.Korean, "ingame:pause_cards", "pause_cards_4x3"),
            (720, 1280, Language.Korean, "ingame:abandon", "abandon_16x9"),
            (720, 1280, Language.Japanese, "ingame:pause", "pause_ja"),
            // 남은 화면(2026-10-24) — 보스 경고 · 다음 방 · 밤 몸 고르기 · 첫 판 안내
            (720, 1280, Language.Korean, "ingame:bosswarn", "extra_bosswarn_16x9"),
            (1080, 2400, Language.Korean, "ingame:bosswarn", "extra_bosswarn_20x9"),
            (720, 1280, Language.English, "ingame:bosswarn", "extra_bosswarn_en"),
            (720, 1280, Language.Korean, "ingame:nextroom", "extra_nextroom_16x9"),
            (768, 1024, Language.Korean, "ingame:nextroom", "extra_nextroom_4x3"),
            (720, 1280, Language.Japanese, "ingame:nextroom", "extra_nextroom_ja"),
            (720, 1280, Language.Korean, "ingame:nightpick", "extra_nightpick_16x9"),
            (720, 1280, Language.Korean, "ingame:nightpick_sel", "extra_nightpick_sel_16x9"),
            (1080, 2400, Language.Korean, "ingame:nightpick_sel", "extra_nightpick_sel_20x9"),
            (720, 1280, Language.English, "ingame:nightpick_sel", "extra_nightpick_sel_en"),
            (720, 1280, Language.Korean, "ingame:guide", "extra_ingameguide_16x9"),
            (768, 1024, Language.Korean, "ingame:guide", "extra_ingameguide_4x3"),
            (720, 1280, Language.Japanese, "ingame:guide", "extra_ingameguide_ja"),
            // 모드 판 표시는 맨 끝 — 위 가운데 칸 · 띠를 바꿔 두므로 뒤 장면에 남는다
            (720, 1280, Language.Korean, "ingame:towerhud", "modehud_tower_16x9"),
            (720, 1280, Language.Korean, "ingame:nighthud", "modehud_night_16x9"),
            (1080, 2400, Language.Korean, "ingame:towerhud", "modehud_tower_20x9"),
            (768, 1024, Language.Korean, "ingame:nighthud", "modehud_night_4x3"),
            // 몸 없음(유령으로 출전 — SessionState .ghost 를 켜고 거르개 now_ghost)
            (720, 1280, Language.Korean, "ingame", "now_ghost_16x9"),
            (720, 1280, Language.Korean, "ingame:danger", "now_ghost_danger_16x9"),
            // PD 결정 반영(2026-10-25) — 쉼터 골라서 확정 · 상인 4칸
            (720, 1280, Language.Korean, "ingame:shrine", "now_shrine_16x9"),
            (720, 1280, Language.Korean, "ingame:shop", "now_shop_16x9"),
            (1080, 2400, Language.Korean, "ingame:shop", "now_shop_20x9"),
            (720, 1280, Language.English, "ingame:shop", "now_shop_en"),
        };

        // 이름 앞머리 거르개(SessionState Key+".only") — 새 화면만 찍을 때 목록 전체를 돌지 않게
        private static (int w, int h, Language lang, string state, string name)[] Current
        {
            get
            {
                var all = SessionState.GetInt(Key + ".set", 0) == 1 ? InGameShots : Shots;
                string only = SessionState.GetString(Key + ".only", string.Empty);
                if (string.IsNullOrEmpty(only)) return all;
                var heads = only.Split(',');   // 쉼표로 여러 앞머리
                return System.Array.FindAll(all, s => System.Array.Exists(heads, h => s.name.StartsWith(h.Trim())));
            }
        }

        private static int _wait;
        private static int _loadingSeen;
        private static int _prepared = -1;
        private static float _shotAt;
        private static bool _sortie;

        static RbShotTool() { EditorApplication.update += Tick; }

        [MenuItem("Tools/Game/RE:BORN 화면/플레이 캡처 — 인게임")]
        private static void BeginInGame()
        {
            SessionState.SetInt(Key + ".set", 1);
            _sortie = false;
            Start();
        }

        [MenuItem("Tools/Game/RE:BORN 화면/플레이 캡처")]
        private static void Begin()
        {
            SessionState.SetInt(Key + ".set", 0);
            Start();
        }

        private static void Start()
        {
            SessionState.SetInt(Key, 1);
            SessionState.SetBool(Key + ".tShot", false);
            SessionState.SetBool(Key + ".tWait", false);
            SessionState.SetBool(Key + ".lShot", false);
            _prepared = -1;
            HoldGuides(true);
            RebornUiSwitch.Enabled = true;
            if (!EditorApplication.isPlaying)
            {
                const string boot = "Assets/Scenes/BootScene.unity";
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != boot
                    && UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(boot);
                EditorApplication.EnterPlaymode();
            }
        }

        [MenuItem("Tools/Game/RE:BORN 화면/플레이 캡처 멈춤")]
        private static void Cancel()
        {
            SessionState.SetInt(Key, 0);
            HoldGuides(false);
        }

        // 첫 실행 안내(RbGuide.ShowOnce)가 다른 장면을 덮지 않게 찍는 동안만 「본 것」으로 둔다 —
        // 끝나면 우리가 켠 표시만 지운다(PD 가 처음 볼 안내를 캡처가 써 버리지 않게)
        private static readonly string[] FirstRunGuides = { "lobby", "ingame", "cards", "merchant", "boss" };

        private static void HoldGuides(bool on)
        {
            if (on)
            {
                if (SessionState.GetString(Key + ".guides", string.Empty).Length > 0) return;   // 앞 캡처가 아직 쥐고 있다
                var held = new System.Collections.Generic.List<string>();
                foreach (var id in FirstRunGuides)
                {
                    if (Game.Module.Common.UI.RbGuide.HasSeen(id)) continue;
                    held.Add(id);
                    PlayerPrefs.SetInt("guide.seen." + id, 1);
                }
                SessionState.SetString(Key + ".guides", string.Join(",", held));
            }
            else
            {
                foreach (var id in SessionState.GetString(Key + ".guides", string.Empty).Split(','))
                    if (id.Length > 0) PlayerPrefs.DeleteKey("guide.seen." + id);
                SessionState.SetString(Key + ".guides", string.Empty);
            }
            PlayerPrefs.Save();
        }

        private static float s_tickAt;

        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0);
            if (step <= 0 || !EditorApplication.isPlaying) return;
            if (s_tickAt > 0f && Time.realtimeSinceStartup >= s_tickAt)
            {
                s_tickAt = 0f;
                var grow = GameObject.Find("GrwStat0");
                var lobbyUi = Object.FindAnyObjectByType<RbLobbyUI>();
                if (grow != null && lobbyUi != null && lobbyUi.Fx != null)
                    lobbyUi.Fx.UpgradeTick((RectTransform)grow.transform, grow.transform.Find("GrwStatValue") as RectTransform);
            }
            if (_wait > 0) { _wait--; return; }
            try { Step(step); }
            catch (System.Exception e)
            {
                SessionState.SetInt(Key, 0);
                HoldGuides(false);
                Debug.LogError($"[RbShot] 중단 — {e.GetType().Name}: {e.Message}");
            }
        }

        private static void Step(int step)
        {
            var shots = Current;
            bool ingame = SessionState.GetInt(Key + ".set", 0) == 1;   // 거르개를 쓰면 목록이 새 배열이라 참조로 비교하면 안 된다
            var lobby = Object.FindAnyObjectByType<RbLobbyUI>();
            var battleUi = ingame ? Object.FindAnyObjectByType<Game.Module.InGame.InGameMainUI>() : null;
            if (ingame && battleUi == null)
            {
                // 아직 로비 — 출전을 한 번 누르고 판이 설 때까지 기다린다
                if (lobby != null && !_sortie && CoreModule.TryGet<IPlayerDataService>(out var p0) && p0.IsReady)
                {
                    lobby.SelectTab("Battle");
                    // SessionState .ghost — 몸 없이(유령으로) 출전해 「몸 없음」 화면을 찍는다
                    if (SessionState.GetBool(Key + ".ghost", false)) { lobby.StartRunDirect(1, Game.Character.HostEntry.GhostKey); _sortie = true; }
                    else if (SessionState.GetInt(Key + ".chapter", 0) > 0)
                    {
                        // SessionState .chapter — 그 챕터로 바로 출전(방 전체 기믹 시안 바탕 · 무대 배경 찍기). 가진 몸 첫째로
                        string key = null;
                        foreach (var e in p0.PlayableHosts)
                            if (e != null && !e.IsGhost && !PlayerDataService.IsHiddenHost(e.HostKey)) { key = e.HostKey; break; }
                        int ch = SessionState.GetInt(Key + ".chapter", 0);
                        // 아직 안 연 챕터면 고른 챕터가 열린 데까지로 깎인다 — 캡처하는 동안만 메모리에서 연다(세이브는 캡처 전 백업 · 뒤에 되돌린다)
                        const System.Reflection.BindingFlags DF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                        var data = p0.GetType().GetField("_data", DF)?.GetValue(p0);
                        var cleared = data?.GetType().GetField("clearedChapter");
                        if (cleared != null && (int)cleared.GetValue(data) < ch - 1) cleared.SetValue(data, ch - 1);
                        lobby.StartRunDirect(ch, key);
                        _sortie = true;
                    }
                    else _sortie = Click("SortieButton");
                    _wait = 120;
                    return;
                }
                // 랜덤 출전은 공개 쪽이 먼저 뜬다 — 「공개하기」 · 「전투 시작」 을 차례로 누른다
                if (Click("SkipButton") || Click("TouchArea") || Click("StartButton") || Click("RndGo")) _wait = 30; else _wait = 20;
                return;
            }
            if (ingame && (GameObject.Find("BootCover") != null || battleUi.GetComponent<Game.Module.InGame.BattleDirector>() == null))
            { _wait = 30; return; }
            if (!ingame && (lobby == null || GameObject.Find("[LoadingView]") != null))
            {
                if (PreShot()) return;
                if (Click("SkipButton") || Click("TouchArea") || Click("StartButton")) _wait = 30; else _wait = 10;
                return;
            }
            if (!CoreModule.TryGet<IPlayerDataService>(out var player) || !player.IsReady) { _wait = 30; return; }

            int shot = step - 1;
            if (shot >= shots.Length)
            {
                SessionState.SetInt(Key, 0);
                Time.timeScale = 1f;
                LowMode(false);
                HoldGuides(false);
                PlayerPrefs.SetInt("game.language", 0);   // PD 에디터 언어는 한국어(메모리 restore-language-after-recording)
                PlayerPrefs.Save();
                Debug.Log($"[RbShot] 끝 → {OutDir}");
                EditorApplication.ExitPlaymode();
                return;
            }
            var s = shots[shot];
            if (Screen.width != s.w || Screen.height != s.h) { SetSize(s.w, s.h); _wait = 150; return; }
            if (_prepared < shot)
            {
                _prepared = shot;
                if (CoreModule.TryGet<ILanguageService>(out var lang)) lang.SetLanguage(s.lang);
                if (ingame) ShowInGame(battleUi, s.state);
                else Show(lobby, player, s.state);
                _wait = s.state.StartsWith("ingame:") ? 300 : 90;   // 창 여는 연출이 끝날 때까지
                // 「@초」 — 연출을 튼 뒤 그 시각에 찍는다(프레임 수가 아니라 실제 시간)
                int at = s.state.IndexOf('@');
                if (at > 0 && float.TryParse(s.state.Substring(at + 1), System.Globalization.NumberStyles.Float,
                                             System.Globalization.CultureInfo.InvariantCulture, out float sec))
                {
                    _wait = 0;
                    _shotAt = Time.realtimeSinceStartup + sec;
                }
                return;
            }
            if (Time.realtimeSinceStartup < _shotAt) return;
            if (ingame && battleUi != null)
            {
                // 그리기 순서 기록 — 창보다 HUD 가 위에 그려진 적이 있다(제단 16:9)
                var sb = new System.Text.StringBuilder($"[RbShot] 순서 {s.name}:");
                for (int i = 0; i < battleUi.transform.childCount; i++)
                {
                    var c = battleUi.transform.GetChild(i);
                    if (!c.gameObject.activeSelf) continue;
                    var cv = c.GetComponent<Canvas>();
                    sb.Append($" {i}:{c.name}({(cv != null ? cv.sortingOrder.ToString() : "-")})");
                }
                Debug.Log(sb.ToString());
            }
            Directory.CreateDirectory(OutDir);
            ScreenCapture.CaptureScreenshot(Path.Combine(OutDir, s.name + ".png"));
            Debug.Log($"[RbShot] {s.name} {Screen.width}x{Screen.height}");
            SessionState.SetInt(Key, step + 1);
            _wait = 45;
        }

        /// <summary>
        /// 로비 앞 화면(타이틀 · 로딩)은 지나가며 한 번씩 찍는다 — 거르개가 「title」일 때만.
        /// 크기는 SessionState `.preW` · `.preH`(기본 720 × 1280), 이름 꼬리는 `.preTag`(기본 16x9).
        /// </summary>
        private static bool PreShot()
        {
            if (SessionState.GetString(Key + ".only", string.Empty) != "title") return false;
            int w = SessionState.GetInt(Key + ".preW", 720), h = SessionState.GetInt(Key + ".preH", 1280);
            string tag = SessionState.GetString(Key + ".preTag", "16x9");
            if (Screen.width != w || Screen.height != h) { SetSize(w, h); _wait = 150; return true; }
            var title = Object.FindAnyObjectByType<Game.Module.Title.TitleMainUI>();
            bool covered = GameObject.Find("[LoadingView]") != null;   // 부트 → 타이틀 전환도 가림막을 쓴다 — 걷힌 뒤에 찍는다
            if (title != null && !SessionState.GetBool(Key + ".tShot", false))
            {
                if (covered) { _wait = 10; return true; }
                if (!SessionState.GetBool(Key + ".tWait", false)) { SessionState.SetBool(Key + ".tWait", true); _wait = 120; return true; }
                Capture($"title_{tag}");
                SessionState.SetBool(Key + ".tShot", true);
                _wait = 45;
                return true;
            }
            var loading = GameObject.Find("RbLoadingView(Clone)");
            if (loading != null && SessionState.GetBool(Key + ".tShot", false) && !SessionState.GetBool(Key + ".lShot", false))
            {
                if (_loadingSeen++ < 40) { _wait = 0; return true; }   // 막대가 조금 찬 뒤에 찍는다
                Capture($"loading_{tag}");
                SessionState.SetBool(Key + ".lShot", true);
                return true;
            }
            return false;
        }

        private static void Capture(string name)
        {
            Directory.CreateDirectory(OutDir);
            ScreenCapture.CaptureScreenshot(Path.Combine(OutDir, name + ".png"));
            Debug.Log($"[RbShot] {name} {Screen.width}x{Screen.height}");
        }

        // 「모든 호스트 열어 둠」(GameConfig._openAllHosts)이면 성급이 늘 10으로 읽힌다 — 보통 상태를 찍는 동안만 끈다
        private static readonly System.Reflection.FieldInfo OpenAll =
            typeof(PlayerDataService).GetField("s_openAllHosts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        private static object s_savedOpenAll;

        private static void LowMode(bool on)
        {
            if (OpenAll == null) return;
            if (on && s_savedOpenAll == null) { s_savedOpenAll = OpenAll.GetValue(null); OpenAll.SetValue(null, false); }
            else if (!on && s_savedOpenAll != null) { OpenAll.SetValue(null, s_savedOpenAll); s_savedOpenAll = null; }
        }

        private static void Show(RbLobbyUI lobby, IPlayerDataService player, string state)
        {
            LowMode(state.EndsWith("low"));
            foreach (var t in lobby.GetComponentsInChildren<Transform>(true))
                if (t.name is "RbGuide" or "RbChestOpen" or "PageReveal" or "GrwStarDone" or "RbNotReward") t.gameObject.SetActive(false);
            if (state == "battle") { lobby.SelectTab("Battle"); return; }
            if (state.StartsWith("battle:"))
            {
                // battle:{n} — 출전 줄 n번째 카드를 고른다(1 유령 · 2 오늘의 몸) · battle:paren — 괄호 이름 카드까지 넘겨 고른다
                lobby.SelectTab("Battle");
                string arg = state.Substring(7).Split('@')[0];   // 「battle:3@0.05」 — 누른 직후를 찍는다
                if (arg != "paren") { Click("SortieCard" + arg); return; }
                foreach (var t in lobby.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                {
                    if (t.name != "CardName" || !t.text.Contains("\n(") && !t.text.Contains("\n（")) continue;
                    var card = (RectTransform)t.transform.parent;
                    if (card.parent is RectTransform content) content.anchoredPosition = new Vector2(-(card.anchoredPosition.x - 40f), content.anchoredPosition.y);
                    Click(card.name);
                    return;
                }
                return;
            }
            if (state.StartsWith("fx:")) { ShowFx(lobby, player, state.Substring(3, state.IndexOf('@') - 3)); return; }
            if (state.StartsWith("guide"))
            {
                // 로비 첫 안내(출전 · 성장 · 소환) — guide:2 는 「다음」 한 번
                lobby.SelectTab("Battle");
                lobby.ShowGuide("sortie", "growth", "summon");
                if (state == "guide:2") Click("RbGuideNext");
                return;
            }
            if (state.StartsWith("chest:"))
            {
                // 상자 열기 — 박자 1(닫힌 상자) · :open 이면 박자 2(받은 것). 값은 예시(저장하지 않는다)
                lobby.SelectTab("Shop");
                var parts = state.Split(':');
                var chest = lobby.GetComponentInChildren<RbChestOpen>(true);
                var icons = typeof(RbNotice).GetField("_icons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(lobby.Notice) as Sprite[];
                if (chest == null) return;
                var lines = new System.Collections.Generic.List<(string, string, int)> { ("gold", null, 1200) };
                int hosts = parts[1] == "platinum" ? 3 : 1;
                foreach (var e in player.PlayableHosts)
                {
                    if (hosts == 0) break;
                    if (e == null || e.IsGhost || PlayerDataService.IsHiddenHost(e.HostKey)) continue;
                    lines.Add(("shard", e.HostKey, 3 + hosts));
                    hosts--;
                }
                chest.Show(parts[1], lines, icons ?? System.Array.Empty<Sprite>());
                if (parts.Length > 2) Click("RbChestOpenTap");
                return;
            }
            if (state.StartsWith("chapters")) { LowMode(state.EndsWith("low")); lobby.SelectTab("Chapters"); return; }
            if (state.StartsWith("random:"))
            {
                // 랜덤 공개 — 박자 1(?) · 박자 2(가진 몸 첫째 · 히든). 「전투 시작」 은 누르지 않는다
                lobby.SelectTab("Battle");
                var rnd = Object.FindAnyObjectByType<RbRandomPage>(FindObjectsInactive.Include);
                if (rnd == null) return;
                string pick = PlayerDataService.HiddenTrialHostKey;
                if (!state.EndsWith("hidden"))
                    foreach (var e in player.PlayableHosts)
                        if (e != null && !e.IsGhost && player.IsHostOwned(e) && !PlayerDataService.IsHiddenHost(e.HostKey)) { pick = e.HostKey; break; }
                rnd.Show(player, pick, null);
                if (!state.EndsWith("ask")) typeof(RbRandomPage).GetMethod("Reveal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(rnd, null);
                return;
            }
            if (state.StartsWith("notice:"))
            {
                // 알림 창 — 값은 시안 예시(보상 432 골드 · 조각 3 · 유령 경험치 30 / 골드 필요 200 보유 40 / 입문 패키지 ₩5,900)
                lobby.SelectTab("Battle");
                var n = lobby.Notice;
                string what = state.Substring(7);
                if (what == "reward")
                    n.ShowReward(new System.Collections.Generic.List<(string, string, int)> { ("gold", null, 432), ("shard", null, 3), ("passxp", null, 30) });
                else if (what == "reward4")
                    n.ShowReward(new System.Collections.Generic.List<(string, string, int)> { ("gem", null, 300), ("ticket", null, 10), ("gold", null, 5000), ("chest", "gold", 1) });
                else if (what == "stamina" && CoreModule.TryGet<Game.Module.Common.Store.IStaminaService>(out var st)) n.ShowStamina(st, player, true, null);
                else if (what == "gem") n.ShowGemShort();
                else if (what == "gold") n.ShowGoldShort(200, 40, Localize.FromTable("ui.rb.notice.gold_keep", "선택한 호스트는 그대로 유지됩니다."),
                                                       Localize.FromTable("ui.rb.lobby.gold_short_random", "랜덤으로 출전 · 골드 없음"), null);
                else if (what == "confirm") n.ShowConfirm("rb_icon_currency_gem_96", "입문 패키지 I 구매", "₩5,900", Localize.FromTable("ui.rb.notice.store_pay", "결제는 스토어 계정으로 진행됩니다"), null);
                return;
            }
            if (state.StartsWith("rail:"))
            {
                lobby.SelectTab("Battle");
                string what = state.Substring(5);
                if (what == "Language")
                {
                    lobby.OpenRail("Settings");
                    var misc = Object.FindAnyObjectByType<RbMiscPages>();
                    foreach (var t in misc.GetComponentsInChildren<Transform>(true)) if (t.name == "RbLanguage") t.gameObject.SetActive(true);
                    typeof(RbMiscPages).GetMethod("FillLanguage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(misc, null);
                }
                else if (what == "ProfileTitle")
                {
                    lobby.OpenRail("Profile");
                    foreach (var b in Object.FindAnyObjectByType<RbMiscPages>().GetComponentsInChildren<UnityEngine.UI.Button>(true))
                        if (b.name == "ProfTabTitle") b.onClick.Invoke();
                }
                else if (what == "PassOptions")
                {
                    lobby.OpenRail("Pass");
                    typeof(RbPassPage).GetMethod("BuyPremium", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(Object.FindAnyObjectByType<RbPassPage>(), null);
                }
                else lobby.OpenRail(what);
                return;
            }
            if (state.StartsWith("challenge"))
            {
                lobby.SelectTab("Challenge");
                var chl = Object.FindAnyObjectByType<RbChallengePage>();
                if (chl == null || state == "challenge") return;
                const System.Reflection.BindingFlags F3 = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                typeof(RbChallengePage).GetMethod("Show", F3)?.Invoke(chl, new object[] { state.Contains("tower") ? "ChlTower" : "ChlNight" });
                // 「challenge:night:d3」 — 그 요일(0 월 … 6 일)로 바꿔 찍는다(배너 몸 줄이 요일마다 바뀌는지)
                int dAt = state.IndexOf(":d", System.StringComparison.Ordinal);
                if (dAt > 0 && int.TryParse(state.Substring(dAt + 2), out int dd))
                {
                    typeof(RbChallengePage).GetField("_day", F3)?.SetValue(chl, dd);
                    typeof(RbChallengePage).GetMethod("FillNight", F3)?.Invoke(chl, null);
                }
                return;
            }
            if (state.StartsWith("shop:"))
            {
                lobby.SelectTab("Shop");
                var shop = Object.FindAnyObjectByType<RbShopPage>();
                if (shop == null) return;
                const System.Reflection.BindingFlags F2 = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                typeof(RbShopPage).GetField("_tab", F2)?.SetValue(shop, state.Substring(5));
                typeof(RbShopPage).GetMethod("Refresh", F2)?.Invoke(shop, null);
                return;
            }
            if (state.StartsWith("summon"))
            {
                lobby.SelectTab("Summon");
                var page = Object.FindAnyObjectByType<RbSummonPage>();
                if (page == null) return;
                const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                if (state == "summon:rates" || state == "summon:records")
                    typeof(RbSummonPage).GetMethod("ShowSub", F)?.Invoke(page, new object[] { state == "summon:rates" ? "SumRates" : "SumRecords" });
                else if (state.StartsWith("summon:result"))
                {
                    // 값을 내지 않고 결과 쪽만 — 가진 몸 열 개로 시안 같은 결과를 꾸민다(저장하지 않는다)
                    var keys = new System.Collections.Generic.List<string>();
                    foreach (var e in player.PlayableHosts)
                        if (e != null && !e.IsGhost && !PlayerDataService.IsHiddenHost(e.HostKey)) keys.Add(e.HostKey);
                    int n = state.EndsWith("1") ? 1 : 10;
                    var pulls = new System.Collections.Generic.List<Game.Module.Common.Store.GachaPull>();
                    for (int i = 0; i < n && keys.Count > 0; i++)
                        pulls.Add(new Game.Module.Common.Store.GachaPull(keys[i % keys.Count], 1 + i % 3, i == 0 && state.EndsWith("jackpot"), false, i == 0, 0));
                    typeof(RbSummonPage).GetMethod("ShowResult", F)?.Invoke(page, new object[] { pulls, n });
                }
                return;
            }
            lobby.SelectTab("Host");
            string key = null;
            foreach (var e in player.PlayableHosts)
                if (e != null && !e.IsGhost && player.IsHostOwned(e) && !PlayerDataService.IsHiddenHost(e.HostKey)) { key = e.HostKey; break; }
            if (key == null) return;
            // 미보유(조각 12 · 다 모음) · 새 몸 · 6 · 10성 각성 보여주기 — 미보유는 메모리에서만 성급 0 으로 만든다
            if (state.StartsWith("host:lock"))
            {
                SeedHost(player, key, 0, 12);
                if (state.StartsWith("host:lockready")) SeedHost(player, key, 0, player.MasteryCost(key));
                lobby.OpenGrowth(key);
                return;
            }
            // 거르기 칩 고름(host:f1 파워 · f2 무기 · f3 마법) — 칩 아이콘 색 · 언어별 넘침 확인
            if (state.Length == 7 && state.StartsWith("host:f") && GameObject.Find($"ColFilter{state[6]}") is GameObject chip
                && chip.TryGetComponent<Button>(out var chipBtn)) { chipBtn.onClick.Invoke(); return; }
            if (state.StartsWith("host:reveal")) { lobby.ShowReveal(key, 0); return; }
            if (state.StartsWith("host:awaken")) { lobby.ShowReveal(key, state.Contains("10") ? 10 : 6); return; }
            if (!state.StartsWith("growth:")) return;
            // 「k=키」 — 그 몸으로 연다(발밑 그림자 자리를 몸마다 보려고)
            int kAt = state.IndexOf("k=", System.StringComparison.Ordinal);
            if (kAt > 0) key = state.Substring(kAt + 2).Split('@', ',')[0];
            if (state.EndsWith("low")) SeedLow(player, key);
            // 「poor」 — 골드 90(메모리에서만): 값 60 칸은 금색 · 120 칸은 빨강으로 같이 찍힌다
            if (state.Contains("poor"))
            {
                var data = player.GetType().GetField("_data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(player);
                data?.GetType().GetField("gold")?.SetValue(data, 90);
            }
            lobby.OpenGrowth(key);
            if (state.Contains("scroll") && GameObject.Find("GrwBody") is GameObject body && body.TryGetComponent<ScrollRect>(out var sr))
                sr.verticalNormalizedPosition = 0f;
            // 창 — 성급 강화 확인(2 → 3 · 조각 20 · 골드 1,200 예시값) · 성급 올랐어요 · 스킬 정보(필살기)
            var growthPage = Object.FindAnyObjectByType<RbGrowthPage>();
            const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            // 「tick」 — 창이 다 열린 0.6초 뒤 공격력 칸에 강화 연출을 튼다(「@초」 로 그 사이를 나눠 찍는다)
            if (state.Contains("tick")) s_tickAt = Time.realtimeSinceStartup + 0.6f;
            if (state.Contains("ask")) typeof(RbGrowthPage).GetMethod("ShowStarAsk", Private)?.Invoke(growthPage, new object[] { 2, 20, 1200 });
            if (state.Contains("done")) typeof(RbGrowthPage).GetMethod("ShowStarDone", Private)?.Invoke(growthPage, null);
            if (state.Contains("skill")) typeof(RbGrowthPage).GetMethod("ShowSkill", Private)?.Invoke(growthPage, new object[] { 0 });
            if (state.Contains("sources")) typeof(RbGrowthPage).GetMethod("ShowSources", Private)?.Invoke(growthPage, null);
            if (state.Contains("essence")) typeof(RbGrowthPage).GetMethod("ShowEssence", Private)?.Invoke(growthPage, null);
        }

        /// <summary>아웃게임 연출을 처음부터 다시 튼다 — 값은 저장하지 않는다(강화 · 상자는 연출만 부른다).</summary>
        private static void ShowFx(RbLobbyUI lobby, IPlayerDataService player, string what)
        {
            var fx = lobby.Fx;
            if (fx == null) return;
            string key = null;
            foreach (var e in player.PlayableHosts)
                if (e != null && !e.IsGhost && player.IsHostOwned(e) && !PlayerDataService.IsHiddenHost(e.HostKey)) { key = e.HostKey; break; }
            const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            switch (what)
            {
                case "newbody": lobby.SelectTab("Host"); lobby.ShowReveal(key, 0); break;
                case "awaken6": lobby.SelectTab("Host"); lobby.ShowReveal(key, 6); break;
                case "awaken10": lobby.SelectTab("Host"); lobby.ShowReveal(key, 10); break;
                case "star":
                {
                    lobby.SelectTab("Host");
                    lobby.OpenGrowth(key);
                    var page = Object.FindAnyObjectByType<RbGrowthPage>();
                    typeof(RbGrowthPage).GetMethod("ShowStarDone", Private)?.Invoke(page, null);
                    break;
                }
                case "upgrade":
                {
                    lobby.SelectTab("Host");
                    lobby.OpenGrowth(key);
                    if (GameObject.Find("GrwStat0") is GameObject tile)
                        fx.UpgradeTick((RectTransform)tile.transform, tile.transform.Find("GrwStatValue") as RectTransform
                                       ?? FindChild(tile.transform, "GrwStatValue"));
                    break;
                }
                case "powerup":
                {
                    lobby.SelectTab("Host");
                    lobby.OpenGrowth(key);
                    var page = Object.FindAnyObjectByType<RbGrowthPage>();
                    typeof(RbGrowthPage).GetMethod("PowerUpAsync", Private)?.Invoke(page, new object[] { 12 });
                    break;
                }
                case "shine":
                    lobby.SelectTab("Battle");
                    if (GameObject.Find("SortieButton") is GameObject b) { fx.Stop((RectTransform)b.transform); fx.ButtonShine((RectTransform)b.transform); }
                    break;
                case "reward":
                    lobby.SelectTab("Battle");
                    lobby.Notice.ShowReward(new System.Collections.Generic.List<(string, string, int)> { ("gold", null, 432), ("shard", null, 3), ("passxp", null, 30) });
                    break;
                default:
                    if (what.StartsWith("chest_"))
                    {
                        // chest_gold · chest_gold:open — 상자 열기 쪽을 직접(상점 구매는 하지 않는다)
                        lobby.SelectTab("Shop");
                        var parts = what.Substring(6).Split(':');
                        var chest = lobby.GetComponentInChildren<RbChestOpen>(true);
                        var icons = typeof(RbNotice).GetField("_icons", Private)?.GetValue(lobby.Notice) as Sprite[];
                        var lines = new System.Collections.Generic.List<(string, string, int)> { ("gold", null, 1200) };
                        if (key != null) lines.Add(("shard", key, 4));
                        chest.Show(parts[0], lines, icons ?? System.Array.Empty<Sprite>());
                        if (parts.Length > 1) Click("RbChestOpenTap");
                    }
                    break;
            }
        }

        private static RectTransform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return (RectTransform)t;
            return null;
        }

        /// <summary>
        /// 시험 세이브는 모든 몸이 10성이라 보통 상태(성급 2 · 조각 12/20)를 볼 수 없다 — 메모리에서만 바꾼다.
        /// 저장하지 않는다(캡처 전 세이브 백업 · 끝나면 되돌림).
        /// </summary>
        private static void SeedLow(IPlayerDataService player, string key) => SeedHost(player, key, 2, 12);

        private static void SeedHost(IPlayerDataService player, string key, int rank, int shardCount)
        {
            var t = player.GetType();
            var data = t.GetField("_data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(player);
            var index = t.GetMethod("IndexOfHost", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (data == null || index == null) return;
            int i = (int)index.Invoke(player, new object[] { key });
            if (i < 0) return;
            var dt = data.GetType();
            if (dt.GetField("hostMastery")?.GetValue(data) is int[] mastery && i < mastery.Length) mastery[i] = rank;
            if (dt.GetField("hostShards")?.GetValue(data) is int[] shards && i < shards.Length) shards[i] = shardCount;
        }

        /// <summary>
        /// 정해 둔 이벤트를 띄운다 — 나머지를 「이 판에서 봤다」에 넣어 뽑기가 그것만 고르게 한다.
        /// 부서진 제단은 벼릴 카드가 있어야 보여 줄 것이 있다 — 보스 사냥 Lv2 · 공격 강화 Lv1 을 먼저 쥐여 준다(시험 판에서만, 저장 안 함).
        /// </summary>
        private static void ForceEvent(Game.Module.InGame.BattleDirector battle, string id)
        {
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var bt = typeof(Game.Module.InGame.BattleDirector);
            var table = bt.GetField("_eventTable", F)?.GetValue(battle) as Game.Module.InGame.EventTable;
            var used = bt.GetField("_eventsUsed", F)?.GetValue(battle) as System.Collections.Generic.ICollection<string>;
            if (table == null || used == null) return;
            used.Clear();
            foreach (var e in table.Entries) if (e != null && e.EventId != id) used.Add(e.EventId);
            if (id == "EV_CH2_06"
                && bt.GetField("_buffs", F)?.GetValue(battle) is Game.Module.InGame.RunBuffs buffs
                && bt.GetField("_buffTable", F)?.GetValue(battle) is Game.Character.BuffTable cards)
            {
                foreach (var c in cards.Entries)
                {
                    if (c == null) continue;
                    if (c.BuffKey == "c025") while (buffs.LevelOf("c025") < 2) buffs.Apply(c);
                    if (c.BuffKey == "c011" && buffs.LevelOf("c011") < 1) buffs.Apply(c);
                }
            }
            bt.GetMethod("OfferEvent", F)?.Invoke(battle, null);
        }

        // ── 번개 녹화 ──
        private static float _stormRecUntil, _stormRecNext;
        private static int _stormRecFrame;
        private const string StormRecDir = "Projects/AVSR/_exchange/ref/ui_reborn_1023/play/storm_rec";

        /// <summary>
        /// 시험 전용 — 1번 방이라 원래는 안 켜지는 번개를 바로 켠다(웅덩이 4개 · 0.4초 뒤 첫 낙뢰 · 3초마다).
        /// 게임 코드의 조건(챕터 3분의 1 뒤 방)은 그대로 두고 여기서만 켠다.
        /// </summary>
        private static void StartStorm(Game.Module.InGame.BattleDirector battle)
        {
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var t = typeof(Game.Module.InGame.BattleDirector);
            t.GetMethod("ClearStorm", F)?.Invoke(battle, null);
            t.GetMethod("CacheStormSprites", F)?.Invoke(battle, null);
            t.GetField("_stormOn", F)?.SetValue(battle, true);
            t.GetField("_stormEvery", F)?.SetValue(battle, 3f);
            t.GetField("_stormTimer", F)?.SetValue(battle, 0.4f);
            var place = t.GetMethod("TryPlacePuddle", F);
            for (int i = 0; i < 40; i++)
            {
                if (t.GetField("_puddles", F)?.GetValue(battle) is System.Collections.ICollection c && c.Count >= 4) break;
                place?.Invoke(battle, null);
            }
            BeginRec(StormRecDir, 3.2f);
        }

        private static string _recDir = StormRecDir;

        /// <summary>0.07초마다 화면을 <paramref name="dir"/> 에 찍는다(지난 녹화는 지운다).</summary>
        private static void BeginRec(string dir, float seconds)
        {
            _recDir = dir;
            if (Directory.Exists(dir)) foreach (var f in Directory.GetFiles(dir, "*.png")) File.Delete(f);
            Directory.CreateDirectory(dir);
            _stormRecFrame = 0;
            _stormRecNext = Time.realtimeSinceStartup;
            _stormRecUntil = Time.realtimeSinceStartup + seconds;
            EditorApplication.update -= TickStormRec;
            EditorApplication.update += TickStormRec;
        }

        private static void TickStormRec()
        {
            if (!EditorApplication.isPlaying || Time.realtimeSinceStartup > _stormRecUntil) { EditorApplication.update -= TickStormRec; return; }
            if (Time.realtimeSinceStartup < _stormRecNext) return;
            _stormRecNext += 0.07f;
            ScreenCapture.CaptureScreenshot(Path.Combine(_recDir, $"f{_stormRecFrame++:000}.png"));
        }

        /// <summary>난이도 확인 — 지금 몸 체력 · 방 적 한 대 피해 · 「몇 대에 죽나」를 콘솔 한 줄로(PD 2026-10-27 난이도 기획안).</summary>
        private static void LogHits(Game.Module.InGame.InGameMainUI ui)
        {
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var battle = ui.GetComponent<Game.Module.InGame.BattleDirector>();
            var t = typeof(Game.Module.InGame.BattleDirector);
            var host = t.GetField("_host", F)?.GetValue(battle) as Game.Module.InGame.Unit;
            if (host == null || t.GetField("_enemies", F)?.GetValue(battle) is not System.Collections.IList enemies) return;
            var sb = new System.Text.StringBuilder($"[캡처] 난이도 — 챕터 {t.GetField("_runChapter", F)?.GetValue(battle)} · 몸 {host.Key} 체력 {host.HpMax} · 적:");
            float sum = 0f; int n = 0;
            foreach (var o in enemies)
            {
                if (o is not Game.Module.InGame.Unit u || !u.IsAlive || u.IsHostBody) continue;
                sb.Append($" {u.Key} {u.Atk}{(u.IsElite ? "(엘리트)" : "")}");
                sum += u.Atk; n++;
            }
            if (n > 0) sb.Append($" · 평균 한 대 {sum / n:0.0} → {host.HpMax / (sum / n):0.0}대에 죽음");
            Debug.Log(sb.ToString());
        }

        /// <summary>받아들이고 카드가 어떻게 바뀌었는지 콘솔에 — 부서진 제단(B안) 규칙 확인용. 화면은 창을 닫고 연출이 도는 모습이 찍힌다.</summary>
        private static void AcceptAndLog(Game.Module.InGame.BattleDirector battle, Game.Module.InGame.InGameMainUI ui)
        {
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var buffs = typeof(Game.Module.InGame.BattleDirector).GetField("_buffs", F)?.GetValue(battle) as Game.Module.InGame.RunBuffs;
            string Cards()
            {
                var sb = new System.Text.StringBuilder();
                if (buffs != null) foreach (var kv in buffs.Levels) sb.Append($"{kv.Key} Lv{kv.Value} · ");
                return sb.ToString();
            }
            string before = Cards();
            var bus = GameFramework.Core.Base.CoreModule.Get<GameFramework.Core.Module.EventBus.IEventBus>();
            System.IDisposable token = null;
            token = bus.Subscribe<Game.Module.Events.EventResolvedEvent>(e =>
            {
                Debug.Log($"[캡처] 제단 수락 — 전: {before} 후: {Cards()} 결과: {e.ResultLine}");
                token?.Dispose();
            });
            ui.GetType().GetMethod("Resolve", F)?.Invoke(ui, new object[] { true });
        }

        /// <summary>판을 거의 세운다(유령이 몇 초 만에 죽어 화면이 바뀐다 — 메모리 freeze-run-before-ingame-check). levelup 은 3택1 을 다시 연다(언어가 바뀌면 카드 글도 다시).</summary>
        private static void ShowInGame(Game.Module.InGame.InGameMainUI ui, string state)
        {
            Time.timeScale = 0.05f;
            Game.Module.Common.Modes.RunModeContext.Clear();   // 모드 판 캡처가 켜 둔 것을 되돌린다
            // 앞 장의 창은 닫는다 — 같은 화면에 겹쳐 찍히지 않게
            foreach (var t in ui.GetComponentsInChildren<Transform>(true))
                if (t.name is "BuffChoicePanel" or "ShopPanel" or "EventPanel" or "ShrinePanel" or "RbResultPanel" or "RbPausePanel" or "RbModeBar" or "RbModeBanner"
                    or "RbBossWarn" or "RbNextRoom" or "RbNightPick" or "RbGuide") t.gameObject.SetActive(false);
            if (state == "ingame") { LogHits(ui); return; }
            // 창이 열리는 연출은 게임 시간으로 돈다 — 0.05 배면 덜 열린 채 찍힌다(제단 첫 캡처). 창이 떠 있으면 판은 멈춰 있다
            Time.timeScale = 1f;
            var battle = ui.GetComponent<Game.Module.InGame.BattleDirector>();
            const System.Reflection.BindingFlags F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var bt = typeof(Game.Module.InGame.BattleDirector);
            if (state.StartsWith("ingame:hazard:"))
            {
                // 1번 방이라 원래는 안 켜진다 — 여기서만 켠다(3초마다 · 곧바로 첫 판). 녹화는 번개와 같은 폴더 규칙
                string key = state.Substring("ingame:hazard:".Length).Split('@')[0];
                const System.Reflection.BindingFlags HF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var bt2 = typeof(Game.Module.InGame.BattleDirector);
                bt2.GetMethod("ClearRoomHazards", HF)?.Invoke(battle, null);
                bt2.GetMethod("StartRoomHazard", HF)?.Invoke(battle, new object[] { key, 3f, 0.02f }   /* 1번 방은 적이 금방 다 죽어 문이 열린다 — 그 전에 첫 판을 건다 */);
                BeginRec(StormRecDir.Replace("storm_rec", "hazard_rec/" + key), key == "gas_band" ? 4.2f : 3.2f);
                return;
            }
            if (state.StartsWith("ingame:storm"))
            {
                StartStorm(battle);
                return;
            }
            if (state.StartsWith("ingame:event:"))
            {
                string id = state.Substring("ingame:event:".Length);
                bool accept = id.EndsWith(":accept");
                if (accept) id = id.Substring(0, id.Length - ":accept".Length);
                ForceEvent(battle, id);
                if (accept) AcceptAndLog(battle, ui);
                return;
            }
            switch (state)
            {
                case "ingame:levelup":
                    bt.GetField("_awaitingBuff", F)?.SetValue(battle, false);
                    bt.GetMethod("OfferBuff", F)?.Invoke(battle, new object[] { Game.Character.CardRarity.Common });
                    break;
                case "ingame:shop": bt.GetMethod("OpenShop", F)?.Invoke(battle, null); break;
                case "ingame:event":
                    // 이 판에서 본 이벤트는 다시 안 나온다 — 장마다 비운다
                    var used = bt.GetField("_eventsUsed", F)?.GetValue(battle);
                    used?.GetType().GetMethod("Clear")?.Invoke(used, null);
                    bt.GetMethod("OfferEvent", F)?.Invoke(battle, null);
                    break;
                case "ingame:shrine": bt.GetMethod("OpenShrine", F)?.Invoke(battle, null); break;
                case "ingame:danger":
                {
                    // 몸 없이 유령 체력을 20% 로 내린다 — 붉은 테두리가 켜진다(시험 판에서만, 저장하지 않는다)
                    var hpField = bt.GetField("_ghostHp", F);
                    var maxProp = bt.GetProperty("GhostHpMax", F | System.Reflection.BindingFlags.Public);
                    if (hpField != null && maxProp != null)
                    {
                        hpField.SetValue(battle, Mathf.Max(1, (int)maxProp.GetValue(battle) / 5));
                        bt.GetMethod("PublishHp", F)?.Invoke(battle, null);
                    }
                    Time.timeScale = 0.05f;   // 체력이 계속 깎여 0 이 되지 않게 판을 거의 세운다
                    break;
                }
                case "ingame:bosswarn":
                {
                    // 보스 띠 — 3.2초 뒤 걷히므로 걷는 순서를 끊고(아이디 +1) 다 보이게 둔다
                    var t = typeof(Game.Module.InGame.InGameMainUI);
                    t.GetMethod("OnRbBoss", F)?.Invoke(ui, new object[] { new Game.Module.Events.BossHpChangedEvent { AppearedBossKey = "crusher", BossName = Localize.Get("boss.crusher.name") } });
                    var idField = t.GetField("_rbBossWarnId", F);
                    if (idField != null) idField.SetValue(ui, (int)idField.GetValue(ui) + 1);
                    foreach (var tr in ui.GetComponentsInChildren<Transform>(true))
                        if (tr.name == "RbBossWarn" && tr.TryGetComponent<CanvasGroup>(out var g)) g.alpha = 1f;
                    break;
                }
                case "ingame:nextroom":
                    typeof(Game.Module.InGame.InGameMainUI).GetMethod("OpenNextRoom", F)?.Invoke(ui, new object[] { Game.Module.Events.RoomKind.Elite, false });
                    break;
                case "ingame:nightpick":
                case "ingame:nightpick_sel":
                {
                    // 몸 세 개 — 가진 몸 앞에서부터(시안은 무작위 셋)
                    var keys = new System.Collections.Generic.List<string>();
                    if (CoreModule.TryGet<IPlayerDataService>(out var p))
                        foreach (var e in p.PlayableHosts)
                            if (keys.Count < 3 && e != null && !e.IsGhost && !PlayerDataService.IsHiddenHost(e.HostKey)) keys.Add(e.HostKey);
                    var t = typeof(Game.Module.InGame.InGameMainUI);
                    t.GetMethod("OpenNightPick", F)?.Invoke(ui, new object[] { keys.ToArray() });
                    if (state.EndsWith("sel")) Click("RbNightPickCard1");
                    break;
                }
                case "ingame:guide":
                    ui.GetComponentInChildren<Game.Module.Common.UI.RbGuide>(true)?.Show(true, "possess", "stand_fire", "ghost_hp", "lock", "release", "rooms");
                    break;
                case "ingame:pause_cards":
                    {
                        // 시험 전용 — 판에 카드를 몇 장 붙인다(등급마다 앞에서 하나씩 + 일반 하나, 앞 셋은 두 번 = Lv 2)
                        const System.Reflection.BindingFlags CF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                        var bt3 = typeof(Game.Module.InGame.BattleDirector);
                        var table = bt3.GetField("_buffTable", CF)?.GetValue(battle) as Game.Character.BuffTable;
                        var buffs = bt3.GetField("_buffs", CF)?.GetValue(battle) as Game.Module.InGame.RunBuffs;
                        if (table != null && buffs != null)
                        {
                            var picked = new System.Collections.Generic.List<Game.Character.BuffEntry>();
                            foreach (Game.Character.CardRarity r in System.Enum.GetValues(typeof(Game.Character.CardRarity)))
                                foreach (var e in table.Entries) if (e != null && e.Rarity == r && !picked.Contains(e)) { picked.Add(e); break; }
                            foreach (var e in table.Entries) if (e != null && !picked.Contains(e) && picked.Count < 5) picked.Add(e);
                            for (int i = 0; i < picked.Count; i++) { buffs.Apply(picked[i]); if (i < 3) buffs.Apply(picked[i]); }
                        }
                        ui.GetComponentInChildren<Game.Module.InGame.RbPausePanel>(true)?.Open(ui);
                        break;
                    }
                case "ingame:pause":
                case "ingame:abandon":
                    var pause = ui.GetComponentInChildren<Game.Module.InGame.RbPausePanel>(true);
                    pause?.Open(ui);
                    if (state.EndsWith("abandon") && pause != null)
                        foreach (var t in pause.GetComponentsInChildren<Transform>(true)) if (t.name == "RbAbandon") t.gameObject.SetActive(true);
                    break;
                case "ingame:result_clear":
                case "ingame:result_clear6":
                case "ingame:result_fail":
                {
                    // 결과 화면만 띄운다(판을 끝내지 않는다 — 저장 · 보상 지급이 일어나지 않게). 숫자는 시안 예시 ·
                    // 조각은 여러 몸(몸 카드 줄 A안) — 4종, 「clear6」 은 6종(옆으로 밀기)
                    var faces = typeof(Game.Module.InGame.InGameMainUI).GetField("_faceAtlas", F)?.GetValue(ui) as UnityEngine.U2D.SpriteAtlas;
                    bool clear = !state.EndsWith("fail");
                    bool six = state.EndsWith("6");
                    ui.GetComponentInChildren<Game.Module.InGame.RbRunResult>(true)?.Show(new Game.Module.Events.StageFinishedEvent
                    {
                        IsCleared = clear, FinishedChapter = 1, RewardGold = clear ? 432 : 96, RewardGhostExp = 30,
                        ShardHostKeys = six ? new[] { "amazon", "gangster", "ninja", "medium", "guru", "robot" } : new[] { "amazon", "gangster", "ninja", "medium" },
                        ShardCounts = six ? new[] { 3, 2, 2, 1, 1, 1 } : new[] { 3, 2, 2, 1 },
                        ReachedRoom = clear ? 16 : 7, ChapterRooms = 16, Kills = clear ? 128 : 54, PlaySeconds = clear ? 522 : 231,
                        LongestHostKey = "amazon",
                    }, faces);
                    break;
                }
                case "ingame:towerhud":
                case "ingame:nighthud":
                {
                    // 모드 판 모양만 — 판은 스토리 그대로 두고 모드 표시만 켠다(시안 예시값). 띠는 걷히지 않게 막는다
                    var t = typeof(Game.Module.InGame.InGameMainUI);
                    bool tower = state == "ingame:towerhud";
                    if (tower) Game.Module.Common.Modes.RunModeContext.BeginTower(2, 0);
                    else Game.Module.Common.Modes.RunModeContext.BeginNight(3, 0);
                    t.GetMethod("ApplyModeHud", F)?.Invoke(ui, null);
                    if (tower)
                    {
                        t.GetMethod("OnTowerFloorEntered", F)?.Invoke(ui, new object[] { new Game.Module.Events.TowerFloorEnteredEvent { EnteredFloor = 2, IsLeaveLocked = true, FloorNameKey = "tower.floor.2.name" } });
                        t.GetMethod("OnTowerEnemiesLeft", F)?.Invoke(ui, new object[] { new Game.Module.Events.TowerEnemiesLeftEvent { RemainingEnemies = 12 } });
                    }
                    else
                    {
                        t.GetMethod("OnNightState", F)?.Invoke(ui, new object[] { new Game.Module.Events.NightStateEvent { ElapsedSeconds = 522, KillCount = 128, WaveAffinityKind = 3, NextEventKind = 2, NextEventSeconds = 80 } });
                        t.GetMethod("ShowModeBanner", F)?.Invoke(ui, new object[] { null, Localize.Format("ui.night.banner.boss", "망령왕"), Localize.Get("ui.night.banner.boss_sub") });
                    }
                    var idField = t.GetField("_modeBannerId", F);
                    if (idField != null) idField.SetValue(ui, (int)idField.GetValue(ui) + 1);
                    foreach (var tr in ui.GetComponentsInChildren<Transform>(true))
                        if (tr.name == "RbModeBanner" && tr.TryGetComponent<CanvasGroup>(out var g)) g.alpha = 1f;
                    break;
                }
                case "ingame:tower_clear":
                case "ingame:tower_fail":
                case "ingame:night":
                    // 탑 · 밤 결과 판만 띄운다(판을 끝내지 않는다). 숫자는 시안 예시(2F · 남은 입장 2 · 128 처치)
                    var modeFaces = typeof(Game.Module.InGame.InGameMainUI).GetField("_faceAtlas", F)?.GetValue(ui) as UnityEngine.U2D.SpriteAtlas;
                    var rb = ui.GetComponentInChildren<Game.Module.InGame.RbRunResult>(true);
                    if (rb == null) break;
                    if (state == "ingame:night")
                        rb.ShowNight(new Game.Module.Events.NightFinishedEvent
                        {
                            IsCleared = true, NightDayIndex = 3, FinalKills = 128, PreviousBest = 96, IsNewBest = true,
                            SurvivedSeconds = 522, ReachedLevel = 12, ShardHostKeys = new[] { "amazon" }, ShardCounts = new[] { 3 }, EntriesLeft = 2,
                        }, modeFaces);
                    else
                    {
                        bool towerClear = state.EndsWith("clear");
                        rb.ShowTower(new Game.Module.Events.TowerFinishedEvent
                        {
                            IsFloorCleared = towerClear, PlayedFloor = 2, NextFloor = 3, RewardGold = 500, EntriesLeft = 2,
                            ShardHostKeys = new[] { "amazon" }, ShardCounts = new[] { 3 }, SuggestHostKeys = new[] { "gangster" },
                            PossessCount = 3, LostCount = 1, ElapsedSeconds = 162,
                        }, modeFaces);
                    }
                    break;
            }
        }

        private static bool Click(string name)
        {
            var go = GameObject.Find(name);
            if (go == null || !go.activeInHierarchy) return false;
            var b = go.GetComponent<Button>();
            if (b == null || !b.isActiveAndEnabled) return false;
            b.onClick.Invoke();
            return true;
        }

        /// <summary>게임 뷰를 고정 해상도로 맞춘다(LobbyShotTool 과 같은 리플렉션).</summary>
        private static void SetSize(int w, int h)
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = single.GetProperty("instance").GetValue(null);
            var group = sizesType.GetProperty("currentGroup").GetValue(instance);
            var gt = group.GetType();
            int total = (int)gt.GetMethod("GetTotalCount").Invoke(group, null);
            var getSize = gt.GetMethod("GetGameViewSize");
            int found = -1;
            for (int i = 0; i < total; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                var st = size.GetType();
                if ((int)st.GetProperty("width").GetValue(size) == w && (int)st.GetProperty("height").GetValue(size) == h) { found = i; break; }
            }
            if (found < 0)
            {
                var sizeType = asm.GetType("UnityEditor.GameViewSize");
                var kindType = asm.GetType("UnityEditor.GameViewSizeType");
                var ctor = sizeType.GetConstructor(new[] { kindType, typeof(int), typeof(int), typeof(string) });
                var made = ctor.Invoke(new object[] { System.Enum.Parse(kindType, "FixedResolution"), w, h, $"fit {w}x{h}" });
                gt.GetMethod("AddCustomSize").Invoke(group, new[] { made });
                found = total;
            }
            var gameViewType = asm.GetType("UnityEditor.GameView");
            var win = EditorWindow.GetWindow(gameViewType, false, null, false);
            gameViewType.GetMethod("SizeSelectionCallback").Invoke(win, new object[] { found, null });
        }
    }
}
