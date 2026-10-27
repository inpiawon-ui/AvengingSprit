$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$jobs = @()
$jobs += Start-Job -ScriptBlock { & "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\order_mock_skill_buttons2_a.ps1" *> "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\log_mock_skill_buttons2_a.txt" }
$jobs += Start-Job -ScriptBlock { & "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\order_mock_skill_buttons2_b.ps1" *> "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\log_mock_skill_buttons2_b.txt" }
$jobs += Start-Job -ScriptBlock { & "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\order_mock_skill_buttons2_c.ps1" *> "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\log_mock_skill_buttons2_c.txt" }
$jobs += Start-Job -ScriptBlock { & "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\order_mock_skill_buttons2_d.ps1" *> "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\log_mock_skill_buttons2_d.txt" }
$jobs | Wait-Job | Out-Null
if (Test-Path "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\in\mock_skill_buttons2_a.png") { "ok mock_skill_buttons2_a" } else { "없음 mock_skill_buttons2_a" }
if (Test-Path "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\in\mock_skill_buttons2_b.png") { "ok mock_skill_buttons2_b" } else { "없음 mock_skill_buttons2_b" }
if (Test-Path "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\in\mock_skill_buttons2_c.png") { "ok mock_skill_buttons2_c" } else { "없음 mock_skill_buttons2_c" }
if (Test-Path "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\in\mock_skill_buttons2_d.png") { "ok mock_skill_buttons2_d" } else { "없음 mock_skill_buttons2_d" }
