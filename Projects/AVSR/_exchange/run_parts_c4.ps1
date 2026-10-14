$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_c4_disc.ps1 *> log_part_c4_disc.txt
if (Test-Path in\part_c4_disc.png) { "ok part_c4_disc" } else { "FAIL part_c4_disc" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_c4_bomb.ps1 *> log_part_c4_bomb.txt
if (Test-Path in\part_c4_bomb.png) { "ok part_c4_bomb" } else { "FAIL part_c4_bomb" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_c4_trail.ps1 *> log_part_c4_trail.txt
if (Test-Path in\part_c4_trail.png) { "ok part_c4_trail" } else { "FAIL part_c4_trail" }
