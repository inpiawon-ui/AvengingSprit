$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_commando_mg.ps1 *> log_mock_skill_commando_mg.txt
if (Test-Path in\mock_skill_commando_mg.png) { "ok commando_mg" } else { "FAIL commando_mg" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_ninja.ps1 *> log_mock_skill_ninja.txt
if (Test-Path in\mock_skill_ninja.png) { "ok ninja" } else { "FAIL ninja" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_dragoon.ps1 *> log_mock_skill_dragoon.txt
if (Test-Path in\mock_skill_dragoon.png) { "ok dragoon" } else { "FAIL dragoon" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_snowwoman.ps1 *> log_mock_skill_snowwoman.txt
if (Test-Path in\mock_skill_snowwoman.png) { "ok snowwoman" } else { "FAIL snowwoman" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_commando_grenade_v2.ps1 *> log_mock_skill_commando_grenade_v2.txt
if (Test-Path in\mock_skill_commando_grenade_v2.png) { "ok commando_grenade_v2" } else { "FAIL commando_grenade_v2" }
