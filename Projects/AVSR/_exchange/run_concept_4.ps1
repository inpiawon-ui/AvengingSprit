$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_death.ps1 *> log_mock_active_death.txt
if (Test-Path in\mock_active_death.png) { "ok mock_active_death" } else { "FAIL mock_active_death" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_vampire.ps1 *> log_mock_active_vampire.txt
if (Test-Path in\mock_active_vampire.png) { "ok mock_active_vampire" } else { "FAIL mock_active_vampire" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_salamander.ps1 *> log_mock_active_salamander.txt
if (Test-Path in\mock_active_salamander.png) { "ok mock_active_salamander" } else { "FAIL mock_active_salamander" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_white_wizard.ps1 *> log_mock_active_white_wizard.txt
if (Test-Path in\mock_active_white_wizard.png) { "ok mock_active_white_wizard" } else { "FAIL mock_active_white_wizard" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_ninja_chain_v2.ps1 *> log_mock_skill_ninja_chain_v2.txt
if (Test-Path in\mock_skill_ninja_chain_v2.png) { "ok mock_skill_ninja_chain_v2" } else { "FAIL mock_skill_ninja_chain_v2" }
