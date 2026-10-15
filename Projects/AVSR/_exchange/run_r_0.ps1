$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_gs_lock.ps1 *> log_part_gs_lock.txt
if (Test-Path in\part_gs_lock.png) { "ok part_gs_lock" } else { "FAIL part_gs_lock" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_hp_slash.ps1 *> log_part_hp_slash.txt
if (Test-Path in\part_hp_slash.png) { "ok part_hp_slash" } else { "FAIL part_hp_slash" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_sm_pool.ps1 *> log_part_sm_pool.txt
if (Test-Path in\part_sm_pool.png) { "ok part_sm_pool" } else { "FAIL part_sm_pool" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_gr_mandala.ps1 *> log_part_gr_mandala.txt
if (Test-Path in\part_gr_mandala.png) { "ok part_gr_mandala" } else { "FAIL part_gr_mandala" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_sw_circle.ps1 *> log_part_sw_circle.txt
if (Test-Path in\part_sw_circle.png) { "ok part_sw_circle" } else { "FAIL part_sw_circle" }
