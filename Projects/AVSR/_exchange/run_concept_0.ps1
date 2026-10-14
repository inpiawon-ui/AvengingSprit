$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_gangster.ps1 *> log_mock_active_gangster.txt
if (Test-Path in\mock_active_gangster.png) { "ok mock_active_gangster" } else { "FAIL mock_active_gangster" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_robot.ps1 *> log_mock_active_robot.txt
if (Test-Path in\mock_active_robot.png) { "ok mock_active_robot" } else { "FAIL mock_active_robot" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_dragoon.ps1 *> log_mock_active_dragoon.txt
if (Test-Path in\mock_active_dragoon.png) { "ok mock_active_dragoon" } else { "FAIL mock_active_dragoon" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_snowwoman.ps1 *> log_mock_active_snowwoman.txt
if (Test-Path in\mock_active_snowwoman.png) { "ok mock_active_snowwoman" } else { "FAIL mock_active_snowwoman" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_dragon_blue.ps1 *> log_mock_active_dragon_blue.txt
if (Test-Path in\mock_active_dragon_blue.png) { "ok mock_active_dragon_blue" } else { "FAIL mock_active_dragon_blue" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_skill_robot_v2.ps1 *> log_mock_skill_robot_v2.txt
if (Test-Path in\mock_skill_robot_v2.png) { "ok mock_skill_robot_v2" } else { "FAIL mock_skill_robot_v2" }
