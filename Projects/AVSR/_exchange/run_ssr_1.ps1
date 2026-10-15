$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_el_burst.ps1 *> log_part_el_burst.txt
if (Test-Path in\part_el_burst.png) { "ok part_el_burst" } else { "FAIL part_el_burst" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_dg_blast.ps1 *> log_part_dg_blast.txt
if (Test-Path in\part_dg_blast.png) { "ok part_dg_blast" } else { "FAIL part_dg_blast" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_md_dust.ps1 *> log_part_md_dust.txt
if (Test-Path in\part_md_dust.png) { "ok part_md_dust" } else { "FAIL part_md_dust" }
