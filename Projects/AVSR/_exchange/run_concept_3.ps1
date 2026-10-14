$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_medium.ps1 *> log_mock_active_medium.txt
if (Test-Path in\mock_active_medium.png) { "ok mock_active_medium" } else { "FAIL mock_active_medium" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_hopper_smg.ps1 *> log_mock_active_hopper_smg.txt
if (Test-Path in\mock_active_hopper_smg.png) { "ok mock_active_hopper_smg" } else { "FAIL mock_active_hopper_smg" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_commando_grenade.ps1 *> log_mock_active_commando_grenade.txt
if (Test-Path in\mock_active_commando_grenade.png) { "ok mock_active_commando_grenade" } else { "FAIL mock_active_commando_grenade" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_commando_laser.ps1 *> log_mock_active_commando_laser.txt
if (Test-Path in\mock_active_commando_laser.png) { "ok mock_active_commando_laser" } else { "FAIL mock_active_commando_laser" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_gangster_v2.ps1 *> log_mock_skill_gangster_v2.txt
if (Test-Path in\mock_skill_gangster_v2.png) { "ok mock_skill_gangster_v2" } else { "FAIL mock_skill_gangster_v2" }
