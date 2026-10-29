Set-Location "C:\won\UnityProject\AvengingSprit"
while (Get-Process -Id 147104,138564 -ErrorAction SilentlyContinue) { Start-Sleep 30 }
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_hopper.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Projects/AVSR/_exchange/run_charv2_ninja.ps1
