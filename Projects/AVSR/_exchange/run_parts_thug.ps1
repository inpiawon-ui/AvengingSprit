$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_th_spin.ps1 *> log_part_th_spin.txt
if (Test-Path in\part_th_spin.png) { "ok part_th_spin" } else { "FAIL part_th_spin" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_th_ring.ps1 *> log_part_th_ring.txt
if (Test-Path in\part_th_ring.png) { "ok part_th_ring" } else { "FAIL part_th_ring" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_th_shell.ps1 *> log_part_th_shell.txt
if (Test-Path in\part_th_shell.png) { "ok part_th_shell" } else { "FAIL part_th_shell" }
