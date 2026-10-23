Set-Location "C:\won\UnityProject\AvengingSprit"
foreach ($k in @("dragon_blue")) {
    if (Test-Path "Projects/AVSR/_exchange/in/appear_$k.png") { "skip $k"; continue }
    for ($try = 0; $try -lt 4; $try++) {
        powershell -NoProfile -ExecutionPolicy Bypass -File "Projects/AVSR/_exchange/order_appear_$k.ps1" *> "Projects/AVSR/_exchange/log_appear_$k.txt"
        if (Test-Path "Projects/AVSR/_exchange/in/appear_$k.png") { break }
        Start-Sleep 300
    }
    if (Test-Path "Projects/AVSR/_exchange/in/appear_$k.png") { "ok $k" } else { "MISSING $k" }
}
