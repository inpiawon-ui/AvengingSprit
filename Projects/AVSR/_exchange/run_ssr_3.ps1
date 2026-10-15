$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_dg_dragon.ps1 *> log_part_dg_dragon.txt
if (Test-Path in\part_dg_dragon.png) { "ok part_dg_dragon" } else { "FAIL part_dg_dragon" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_md_rune.ps1 *> log_part_md_rune.txt
if (Test-Path in\part_md_rune.png) { "ok part_md_rune" } else { "FAIL part_md_rune" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_vp_orb.ps1 *> log_part_vp_orb.txt
if (Test-Path in\part_vp_orb.png) { "ok part_vp_orb" } else { "FAIL part_vp_orb" }
