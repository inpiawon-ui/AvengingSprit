$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_sl_vortex.ps1 *> log_part_sl_vortex.txt
if (Test-Path in\part_sl_vortex.png) { "ok part_sl_vortex" } else { "FAIL part_sl_vortex" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_sl_swing.ps1 *> log_part_sl_swing.txt
if (Test-Path in\part_sl_swing.png) { "ok part_sl_swing" } else { "FAIL part_sl_swing" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_sl_ball.ps1 *> log_part_sl_ball.txt
if (Test-Path in\part_sl_ball.png) { "ok part_sl_ball" } else { "FAIL part_sl_ball" }
