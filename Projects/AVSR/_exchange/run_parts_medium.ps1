$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_md_wraith.ps1 *> log_part_md_wraith.txt
if (Test-Path in\part_md_wraith.png) { "ok part_md_wraith" } else { "FAIL part_md_wraith" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_md_pop.ps1 *> log_part_md_pop.txt
if (Test-Path in\part_md_pop.png) { "ok part_md_pop" } else { "FAIL part_md_pop" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_md_slash.ps1 *> log_part_md_slash.txt
if (Test-Path in\part_md_slash.png) { "ok part_md_slash" } else { "FAIL part_md_slash" }
