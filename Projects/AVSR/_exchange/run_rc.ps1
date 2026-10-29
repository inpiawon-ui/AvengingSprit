Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('pixel_mote')
$max = 6
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_rc_$k.ps1' *> 'Projects/AVSR/_exchange/log_rc_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/rc_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
