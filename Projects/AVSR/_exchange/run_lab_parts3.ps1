Set-Location "C:\won\UnityProject\AvengingSprit"
foreach ($k in @("lab_glass_shards2","lab_splash")) {
    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "skip $k"; continue }
    for ($try = 0; $try -lt 4; $try++) {
        powershell -NoProfile -ExecutionPolicy Bypass -File "Projects/AVSR/_exchange/order_$k.ps1" *> "Projects/AVSR/_exchange/log_$k.txt"
        if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { break }
        Start-Sleep 300
    }
    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "ok $k" } else { "MISSING $k" }
}
