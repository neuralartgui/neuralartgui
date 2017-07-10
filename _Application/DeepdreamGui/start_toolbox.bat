REM @echo OFF

REM docker-machine start default

FOR /f "tokens=*" %%i IN ('docker-machine.exe env --shell=cmd default') DO %%i

%*
