Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('chest_halo')
$running = @()
foreach ($k in $keys) {
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_c2_$k.ps1' *> 'Projects/AVSR/_exchange/log_c2_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/c2_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
