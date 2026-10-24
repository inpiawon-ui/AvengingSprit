Set-Location "C:\won\UnityProject\AvengingSprit"
$env:OPENAI_API_KEY = $null
$topics = @('rbfx_chest_gold','rbfx_awaken10','rbfx_chest_silver','rbfx_chest_platinum','rbfx_awaken6','rbfx_newbody','rbfx_reward','rbfx_starfill','rbfx_upgrade','rbfx_button')
for ($i = 0; $i -lt $topics.Count; $i += 2) {
    $batch = $topics[$i..([Math]::Min($i + 1, $topics.Count - 1))]
    $procs = @()
    foreach ($t in $batch) {
        $procs += Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File',"Projects/AVSR/_exchange/run_mock_$t.ps1" -WindowStyle Hidden -PassThru
        Start-Sleep 5
    }
    $procs | Wait-Process
    Write-Output ("batch done: " + ($batch -join ', ') + " " + (Get-Date -Format HH:mm:ss))
}
Get-ChildItem Projects/AVSR/_exchange/in -Filter 'mock_rbfx_*.png' | Select-Object Name, Length