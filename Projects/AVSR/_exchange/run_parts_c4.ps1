$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -NoProfile -ExecutionPolicy Bypass -File order_part_c4_bomb.ps1 *> log_part_c4_bomb.txt
if (Test-Path in\part_c4_bomb.png) { "ok part_c4_bomb" } else { "FAIL part_c4_bomb" }
