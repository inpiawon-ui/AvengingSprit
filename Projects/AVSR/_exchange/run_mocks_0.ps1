$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_gangster.ps1 *> log_mock_skill_gangster.txt
if (Test-Path in\mock_skill_gangster.png) { "ok gangster" } else { "FAIL gangster" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_hopper.ps1 *> log_mock_skill_hopper.txt
if (Test-Path in\mock_skill_hopper.png) { "ok hopper" } else { "FAIL hopper" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_robot.ps1 *> log_mock_skill_robot.txt
if (Test-Path in\mock_skill_robot.png) { "ok robot" } else { "FAIL robot" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_guru.ps1 *> log_mock_skill_guru.txt
if (Test-Path in\mock_skill_guru.png) { "ok guru" } else { "FAIL guru" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_amazon_elite.ps1 *> log_mock_skill_amazon_elite.txt
if (Test-Path in\mock_skill_amazon_elite.png) { "ok amazon_elite" } else { "FAIL amazon_elite" }
