$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_hp_bind.ps1 *> log_part_hp_bind.txt
if (Test-Path in\part_hp_bind.png) { "ok part_hp_bind" } else { "FAIL part_hp_bind" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_sm_spit.ps1 *> log_part_sm_spit.txt
if (Test-Path in\part_sm_spit.png) { "ok part_sm_spit" } else { "FAIL part_sm_spit" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_db_strike.ps1 *> log_part_db_strike.txt
if (Test-Path in\part_db_strike.png) { "ok part_db_strike" } else { "FAIL part_db_strike" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_ww_burst.ps1 *> log_part_ww_burst.txt
if (Test-Path in\part_ww_burst.png) { "ok part_ww_burst" } else { "FAIL part_ww_burst" }
