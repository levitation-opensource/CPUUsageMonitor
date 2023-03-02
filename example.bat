@echo off
:s
CPUUsageMonitor.exe -handlesTreshold=75000 -memoryCommitTresholdMB=1024 -programRegEx="YourProblematicExecutableName"
pskill.exe "YourProblematicExecutableName"
sleep 1
goto s
