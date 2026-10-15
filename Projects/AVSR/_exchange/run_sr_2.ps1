$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_hs_shield.ps1 *> log_part_hs_shield.txt
if (Test-Path in\part_hs_shield.png) { "ok part_hs_shield" } else { "FAIL part_hs_shield" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_nj_smoke.ps1 *> log_part_nj_smoke.txt
if (Test-Path in\part_nj_smoke.png) { "ok part_nj_smoke" } else { "FAIL part_nj_smoke" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_rb_drop.ps1 *> log_part_rb_drop.txt
if (Test-Path in\part_rb_drop.png) { "ok part_rb_drop" } else { "FAIL part_rb_drop" }
