$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_el_crack.ps1 *> log_part_el_crack.txt
if (Test-Path in\part_el_crack.png) { "ok part_el_crack" } else { "FAIL part_el_crack" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_dg_rise.ps1 *> log_part_dg_rise.txt
if (Test-Path in\part_dg_rise.png) { "ok part_dg_rise" } else { "FAIL part_dg_rise" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_vp_claw.ps1 *> log_part_vp_claw.txt
if (Test-Path in\part_vp_claw.png) { "ok part_vp_claw" } else { "FAIL part_vp_claw" }
