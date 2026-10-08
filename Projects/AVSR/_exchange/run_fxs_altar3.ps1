Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('altar_idle_fall')
$max = 5
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_fxs_$k.ps1' *> 'Projects/AVSR/_exchange/log_fxs_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/fxs_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
