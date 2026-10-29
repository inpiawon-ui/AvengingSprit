Set-Location "C:\won\UnityProject\AvengingSprit"
for ($i = 0; $i -lt 12; $i++) {
    if (Test-Path "Projects/AVSR/_exchange/in/reveal_plate.png") { break }
    Start-Sleep 600
    powershell -NoProfile -ExecutionPolicy Bypass -File "Projects/AVSR/_exchange/order_reveal_plate.ps1" *> "Projects/AVSR/_exchange/log_reveal_plate.txt"
    "try $i"
}
if (Test-Path "Projects/AVSR/_exchange/in/reveal_plate.png") { "ok reveal_plate" } else { "MISSING reveal_plate" }
