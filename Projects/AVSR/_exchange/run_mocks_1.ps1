$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_commando_laser.ps1 *> log_mock_skill_commando_laser.txt
if (Test-Path in\mock_skill_commando_laser.png) { "ok commando_laser" } else { "FAIL commando_laser" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_hopper_smg.ps1 *> log_mock_skill_hopper_smg.txt
if (Test-Path in\mock_skill_hopper_smg.png) { "ok hopper_smg" } else { "FAIL hopper_smg" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_salamander.ps1 *> log_mock_skill_salamander.txt
if (Test-Path in\mock_skill_salamander.png) { "ok salamander" } else { "FAIL salamander" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_white_wizard.ps1 *> log_mock_skill_white_wizard.txt
if (Test-Path in\mock_skill_white_wizard.png) { "ok white_wizard" } else { "FAIL white_wizard" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_death.ps1 *> log_mock_skill_death.txt
if (Test-Path in\mock_skill_death.png) { "ok death" } else { "FAIL death" }
