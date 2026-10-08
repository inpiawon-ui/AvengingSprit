Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('clear')
$max = 5
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_fxstory_$k.ps1' *> 'Projects/AVSR/_exchange/log_fxstory_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = Get-ChildItem "Projects/AVSR/_exchange/in/mock_fxstory_$($k)_v*.png" -ErrorAction SilentlyContinue; if ($f) { "ok $k" } else { "MISSING $k" } }
