$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_el_aura.ps1 *> log_part_el_aura.txt
if (Test-Path in\part_el_aura.png) { "ok part_el_aura" } else { "FAIL part_el_aura" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_dg_breath.ps1 *> log_part_dg_breath.txt
if (Test-Path in\part_dg_breath.png) { "ok part_dg_breath" } else { "FAIL part_dg_breath" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_md_slam.ps1 *> log_part_md_slam.txt
if (Test-Path in\part_md_slam.png) { "ok part_md_slam" } else { "FAIL part_md_slam" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_vp_burst.ps1 *> log_part_vp_burst.txt
if (Test-Path in\part_vp_burst.png) { "ok part_vp_burst" } else { "FAIL part_vp_burst" }
