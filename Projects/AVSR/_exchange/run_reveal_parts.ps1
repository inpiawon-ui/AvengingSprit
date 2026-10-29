Set-Location "C:\won\UnityProject\AvengingSprit"
foreach ($k in @("reveal_stage","reveal_pillar","reveal_ring","reveal_burst","reveal_impact","reveal_plate")) {
    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "skip $k"; continue }
    for ($try = 0; $try -lt 2; $try++) {
        powershell -NoProfile -ExecutionPolicy Bypass -File "Projects/AVSR/_exchange/order_$k.ps1" *> "Projects/AVSR/_exchange/log_$k.txt"
        if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { break }
    }
    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "ok $k" } else { "MISSING $k" }
}
