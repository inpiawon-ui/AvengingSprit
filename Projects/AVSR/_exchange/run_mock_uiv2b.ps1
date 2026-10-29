Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('mock_uiv2_c_home2','mock_uiv2_c_sortie','mock_uiv2_c_stats','mock_uiv2_c_star','mock_uiv2_c_ghost','mock_uiv2_c_shop','mock_uiv2_c_pass','mock_uiv2_c_modes','mock_uiv2_d_home2','mock_uiv2_d_sortie','mock_uiv2_d_stats','mock_uiv2_d_star','mock_uiv2_d_ghost','mock_uiv2_d_shop','mock_uiv2_d_pass','mock_uiv2_d_modes')
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
