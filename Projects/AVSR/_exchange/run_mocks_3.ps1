$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_commando_missile.ps1 *> log_mock_skill_commando_missile.txt
if (Test-Path in\mock_skill_commando_missile.png) { "ok commando_missile" } else { "FAIL commando_missile" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_ninja_chain.ps1 *> log_mock_skill_ninja_chain.txt
if (Test-Path in\mock_skill_ninja_chain.png) { "ok ninja_chain" } else { "FAIL ninja_chain" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_dragon_blue.ps1 *> log_mock_skill_dragon_blue.txt
if (Test-Path in\mock_skill_dragon_blue.png) { "ok dragon_blue" } else { "FAIL dragon_blue" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_vampire.ps1 *> log_mock_skill_vampire.txt
if (Test-Path in\mock_skill_vampire.png) { "ok vampire" } else { "FAIL vampire" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_medium_v3.ps1 *> log_mock_skill_medium_v3.txt
if (Test-Path in\mock_skill_medium_v3.png) { "ok medium_v3" } else { "FAIL medium_v3" }
