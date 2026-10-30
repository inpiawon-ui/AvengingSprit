$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -ExecutionPolicy Bypass -File order_mock1030_suppress_free1.ps1 *> log_mock1030_suppress_free1.txt
powershell -ExecutionPolicy Bypass -File order_mock1030_suppress_free2.ps1 *> log_mock1030_suppress_free2.txt
powershell -ExecutionPolicy Bypass -File order_mock1030_suppress_guide.ps1 *> log_mock1030_suppress_guide.txt
powershell -ExecutionPolicy Bypass -File order_mock1030_missile_free1.ps1 *> log_mock1030_missile_free1.txt
powershell -ExecutionPolicy Bypass -File order_mock1030_missile_free2.ps1 *> log_mock1030_missile_free2.txt
powershell -ExecutionPolicy Bypass -File order_mock1030_missile_guide.ps1 *> log_mock1030_missile_guide.txt
