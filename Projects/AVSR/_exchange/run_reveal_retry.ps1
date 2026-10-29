Set-Location "C:\won\UnityProject\AvengingSprit"
for ($round = 0; $round -lt 6; $round++) {
    $left = @("reveal_burst","reveal_impact","reveal_plate") | Where-Object { -not (Test-Path "Projects/AVSR/_exchange/in/$_.png") }
    if ($left.Count -eq 0) { break }
    foreach ($k in $left) {
        powershell -NoProfile -ExecutionPolicy Bypass -File "Projects/AVSR/_exchange/order_$k.ps1" *> "Projects/AVSR/_exchange/log_$k.txt"
        if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "ok $k (round $round)" } else { "wait $k (round $round)"; break }
    }
    if ((@("reveal_burst","reveal_impact","reveal_plate") | Where-Object { -not (Test-Path "Projects/AVSR/_exchange/in/$_.png") }).Count -gt 0) { Start-Sleep 1800 }
}
@("reveal_burst","reveal_impact","reveal_plate") | ForEach-Object { if (Test-Path "Projects/AVSR/_exchange/in/$_.png") { "done $_" } else { "MISSING $_" } }
