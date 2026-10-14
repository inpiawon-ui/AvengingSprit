$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_hopper.ps1 *> log_mock_active_hopper.txt
if (Test-Path in\mock_active_hopper.png) { "ok mock_active_hopper" } else { "FAIL mock_active_hopper" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_ninja.ps1 *> log_mock_active_ninja.txt
if (Test-Path in\mock_active_ninja.png) { "ok mock_active_ninja" } else { "FAIL mock_active_ninja" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_baseball.ps1 *> log_mock_active_baseball.txt
if (Test-Path in\mock_active_baseball.png) { "ok mock_active_baseball" } else { "FAIL mock_active_baseball" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_thug.ps1 *> log_mock_active_thug.txt
if (Test-Path in\mock_active_thug.png) { "ok mock_active_thug" } else { "FAIL mock_active_thug" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_commando_mg.ps1 *> log_mock_active_commando_mg.txt
if (Test-Path in\mock_active_commando_mg.png) { "ok mock_active_commando_mg" } else { "FAIL mock_active_commando_mg" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_amazon_elite_v2.ps1 *> log_mock_skill_amazon_elite_v2.txt
if (Test-Path in\mock_skill_amazon_elite_v2.png) { "ok mock_skill_amazon_elite_v2" } else { "FAIL mock_skill_amazon_elite_v2" }
