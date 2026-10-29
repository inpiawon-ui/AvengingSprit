Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('mock_uiv5_home','mock_uiv5_summon','mock_uiv5_growth_skill','uikit_v5_1')
for ($i = 0; $i -lt $keys.Count; $i += 4) {
    $batch = $keys[$i..([Math]::Min($i + 3, $keys.Count - 1))]
    $running = @()
    foreach ($k in $batch) {
        $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'" -WindowStyle Hidden -PassThru
        $running += $p
        Write-Output "started $k"
        Start-Sleep 3
    }
    $running | Wait-Process
}
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
