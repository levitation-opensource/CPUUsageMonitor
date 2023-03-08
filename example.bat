@echo off
:s
CPUUsageMonitor.exe -handlesThreshold=75000 -memoryCommitThresholdMB=1024 -programRegEx="YourProblematicExecutableName"
pskill.exe "YourProblematicExecutableName"
ping -n 2 127.0.0.1
REM sleep 1
goto s
