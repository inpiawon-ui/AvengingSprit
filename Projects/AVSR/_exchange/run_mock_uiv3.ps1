Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('mock_uiv3_home','mock_uiv3_sortie','mock_uiv3_growth_skill','mock_uiv3_growth_stats','mock_uiv3_growth_star','mock_uiv3_pass','mock_uiv3_tower','mock_uiv3_night','mock_uiv3_summon')
for ($i = 0; $i -lt $keys.Count; $i += 5) {
    $batch = $keys[$i..([Math]::Min($i + 4, $keys.Count - 1))]
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
