$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_lz_charge.ps1 *> log_part_lz_charge.txt
if (Test-Path in\part_lz_charge.png) { "ok part_lz_charge" } else { "FAIL part_lz_charge" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_nj_slash.ps1 *> log_part_nj_slash.txt
if (Test-Path in\part_nj_slash.png) { "ok part_nj_slash" } else { "FAIL part_nj_slash" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_cm_lock.ps1 *> log_part_cm_lock.txt
if (Test-Path in\part_cm_lock.png) { "ok part_cm_lock" } else { "FAIL part_cm_lock" }
