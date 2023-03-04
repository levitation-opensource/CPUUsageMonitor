@echo off
:s
CPUUsageMonitor.exe -handlesThreshold=75000 -memoryCommitThresholdMB=1024 -programRegEx="YourProblematicExecutableName"
pskill.exe "YourProblematicExecutableName"
sleep 1
goto s
