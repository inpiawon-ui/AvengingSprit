$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_gs_burst.ps1 *> log_part_gs_burst.txt
if (Test-Path in\part_gs_burst.png) { "ok part_gs_burst" } else { "FAIL part_gs_burst" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_mg_shatter.ps1 *> log_part_mg_shatter.txt
if (Test-Path in\part_mg_shatter.png) { "ok part_mg_shatter" } else { "FAIL part_mg_shatter" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_db_orb.ps1 *> log_part_db_orb.txt
if (Test-Path in\part_db_orb.png) { "ok part_db_orb" } else { "FAIL part_db_orb" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_ww_orb.ps1 *> log_part_ww_orb.txt
if (Test-Path in\part_ww_orb.png) { "ok part_ww_orb" } else { "FAIL part_ww_orb" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_sw_shatter.ps1 *> log_part_sw_shatter.txt
if (Test-Path in\part_sw_shatter.png) { "ok part_sw_shatter" } else { "FAIL part_sw_shatter" }
