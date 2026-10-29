Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('heal_burst','power_up','speed_up','vital_up','curse','card_absorb','heal_orb','card_rim_thin','goldpile_stages')
$max = 5
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_fxp_$k.ps1' *> 'Projects/AVSR/_exchange/log_fxp_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/fxp_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
