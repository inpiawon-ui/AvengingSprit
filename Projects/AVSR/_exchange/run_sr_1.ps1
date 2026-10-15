$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_hs_trail.ps1 *> log_part_hs_trail.txt
if (Test-Path in\part_hs_trail.png) { "ok part_hs_trail" } else { "FAIL part_hs_trail" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_lz_hit.ps1 *> log_part_lz_hit.txt
if (Test-Path in\part_lz_hit.png) { "ok part_lz_hit" } else { "FAIL part_lz_hit" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_nc_hook.ps1 *> log_part_nc_hook.txt
if (Test-Path in\part_nc_hook.png) { "ok part_nc_hook" } else { "FAIL part_nc_hook" }
