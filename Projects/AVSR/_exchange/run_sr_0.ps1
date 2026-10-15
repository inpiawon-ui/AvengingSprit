$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_hs_launch.ps1 *> log_part_hs_launch.txt
if (Test-Path in\part_hs_launch.png) { "ok part_hs_launch" } else { "FAIL part_hs_launch" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_lz_beam.ps1 *> log_part_lz_beam.txt
if (Test-Path in\part_lz_beam.png) { "ok part_lz_beam" } else { "FAIL part_lz_beam" }
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_nc_slam.ps1 *> log_part_nc_slam.txt
if (Test-Path in\part_nc_slam.png) { "ok part_nc_slam" } else { "FAIL part_nc_slam" }
