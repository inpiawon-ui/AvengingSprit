$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange"
powershell -ExecutionPolicy Bypass -File order_mock1030_infuse_free1.ps1 *> log_mock1030_infuse_free1.txt
powershell -ExecutionPolicy Bypass -File order_mock1030_infuse_free2.ps1 *> log_mock1030_infuse_free2.txt
powershell -ExecutionPolicy Bypass -File order_mock1030_infuse_guide.ps1 *> log_mock1030_infuse_guide.txt
powershell -ExecutionPolicy Bypass -File order_mock1030_ritual_free1.ps1 *> log_mock1030_ritual_free1.txt
powershell -ExecutionPolicy Bypass -File order_mock1030_ritual_free2.ps1 *> log_mock1030_ritual_free2.txt
powershell -ExecutionPolicy Bypass -File order_mock1030_ritual_guide.ps1 *> log_mock1030_ritual_guide.txt
