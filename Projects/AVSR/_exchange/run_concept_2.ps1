$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_ninja_chain.ps1 *> log_mock_active_ninja_chain.txt
if (Test-Path in\mock_active_ninja_chain.png) { "ok mock_active_ninja_chain" } else { "FAIL mock_active_ninja_chain" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_amazon.ps1 *> log_mock_active_amazon.txt
if (Test-Path in\mock_active_amazon.png) { "ok mock_active_amazon" } else { "FAIL mock_active_amazon" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_guru.ps1 *> log_mock_active_guru.txt
if (Test-Path in\mock_active_guru.png) { "ok mock_active_guru" } else { "FAIL mock_active_guru" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_commando_missile.ps1 *> log_mock_active_commando_missile.txt
if (Test-Path in\mock_active_commando_missile.png) { "ok mock_active_commando_missile" } else { "FAIL mock_active_commando_missile" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_mock_active_amazon_elite.ps1 *> log_mock_active_amazon_elite.txt
if (Test-Path in\mock_active_amazon_elite.png) { "ok mock_active_amazon_elite" } else { "FAIL mock_active_amazon_elite" }
