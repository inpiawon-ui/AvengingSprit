$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_gs_beam.ps1 *> log_part_gs_beam.txt
if (Test-Path in\part_gs_beam.png) { "ok part_gs_beam" } else { "FAIL part_gs_beam" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_mg_wall.ps1 *> log_part_mg_wall.txt
if (Test-Path in\part_mg_wall.png) { "ok part_mg_wall" } else { "FAIL part_mg_wall" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_sm_geyser.ps1 *> log_part_sm_geyser.txt
if (Test-Path in\part_sm_geyser.png) { "ok part_sm_geyser" } else { "FAIL part_sm_geyser" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_gr_burst.ps1 *> log_part_gr_burst.txt
if (Test-Path in\part_gr_burst.png) { "ok part_gr_burst" } else { "FAIL part_gr_burst" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_sw_crystal.ps1 *> log_part_sw_crystal.txt
if (Test-Path in\part_sw_crystal.png) { "ok part_sw_crystal" } else { "FAIL part_sw_crystal" }
