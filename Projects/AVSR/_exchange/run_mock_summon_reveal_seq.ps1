Set-Location "C:\won\UnityProject\AvengingSprit"
foreach ($k in @("mock_summon_reveal_free_peak","mock_summon_reveal_free_idle","mock_summon_reveal_guide_peak","mock_summon_reveal_guide_idle")) {
    for ($try = 0; $try -lt 2; $try++) {
        powershell -NoProfile -ExecutionPolicy Bypass -Command "& Projects/AVSR/_exchange/order_$k.ps1 *> Projects/AVSR/_exchange/log_$k.txt"
        if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { break }
    }
    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "ok $k" } else { "MISSING $k" }
}
