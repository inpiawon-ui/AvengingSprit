param([string[]]$Sheets)
# RE:BORN UI 재발주 · 시안을 한 장씩 차례로 돌린다. 이미 받은 것은 건너뛴다.
# ⚠ 이 실행기는 **한 줄로만** 돌린다 — 두 줄을 동시에 돌리면 겹치는 순간 429(짧은 시간 요청 몰림)가 난다(2026-10-22).
$ex = 'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange'
$empty = "$ex\ref\rework1021\empty.txt"
$all = @('rb_btn_primary','rb_btn_secondary','rb_btn_price','rb_btn_danger','rb_btn_max','rb_btn_chip','rb_btn_icon_round','rb_panels','rb_rows','rb_stat_tile','rb_host_frame','rb_badges','rb_card_option','rb_slot_item','rb_tab_bar','rb_controls','rb_bars','rb_pass_node','rb_summon_card','rb_misc','rb_result_panels','rb_hud_bars','rb_hud_hp','rb_hud_possess','rb_hud_skill','rb_hud_misc','rb_hidden_silhouette','rb_banner_11','rb_banner_12','rb_banner_13','rb_banner_14','rb_banner_15','rb_banner_16','rb_banner_17','rb_banner_18','rb_banner_19','rb_banner_20','rb_mock_summon_beam','rb_mock_card_flip','rb_mock_stamp_jackpot','rb_mock_chest_open','rb_mock_coin_burst','rb_mock_star_fill','rb_mock_awaken_6','rb_mock_awaken_10','rb_mock_upgrade_tick','rb_mock_reward_reveal','rb_mock_button_shine')
if ($Sheets) { $all = ($Sheets -join ',').Split(',') }
foreach ($s in $all) {
    if (Test-Path "$ex\in\$s.png") { continue }
    for ($try = 1; $try -le 5; $try++) {
        cmd /c "powershell -NoProfile -ExecutionPolicy Bypass -File `"$ex\order_$s.ps1`" < `"$empty`" > `"$ex\log_$s.txt`" 2>&1"
        if (Test-Path "$ex\in\$s.png") { break }
        Start-Sleep -Seconds 120
    }
    "[ORDER] $s $(if (Test-Path "$ex\in\$s.png") {'ok'} else {'fail'})"
}
