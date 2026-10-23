Set-Location "C:\won\UnityProject\AvengingSprit"
$k = "lab_reveal_light_rainbow"
for ($try = 0; $try -lt 4; $try++) {
    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { break }
    powershell -NoProfile -ExecutionPolicy Bypass -File "Projects/AVSR/_exchange/order_$k.ps1" *> "Projects/AVSR/_exchange/log_$k.txt"
    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { break }
    Start-Sleep 300
}
if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "ok $k" } else { "MISSING $k" }
