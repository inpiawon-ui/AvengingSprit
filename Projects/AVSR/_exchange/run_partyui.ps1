$env:OPENAI_API_KEY = $null
$d = "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
$jobs = @()
$jobs += Start-Job -ScriptBlock { param($d) powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_partyui_A_free.ps1" *> "$d\log_partyui_A_free.txt" } -ArgumentList $d
$jobs += Start-Job -ScriptBlock { param($d) powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_partyui_B_trait.ps1" *> "$d\log_partyui_B_trait.txt" } -ArgumentList $d
$jobs += Start-Job -ScriptBlock { param($d) powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_partyui_C_band.ps1" *> "$d\log_partyui_C_band.txt" } -ArgumentList $d
$jobs += Start-Job -ScriptBlock { param($d) powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_partyui_choice.ps1" *> "$d\log_partyui_choice.txt" } -ArgumentList $d
$jobs += Start-Job -ScriptBlock { param($d) powershell -NoProfile -ExecutionPolicy Bypass -File "$d\order_partyui_death.ps1" *> "$d\log_partyui_death.txt" } -ArgumentList $d
$jobs | Wait-Job | Out-Null
"다 끝남"
