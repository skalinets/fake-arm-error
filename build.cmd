@echo off
SETLOCAL

REM We use this to tell FAKE to not use the current latest version to build the netcore version, 
REM but instead use the current NON dotnetcore version
SET NO_DOTNETCORE_BOOTSTRAP=true

.paket\paket.exe restore
if errorlevel 1 (
  exit /b %errorlevel%
)

SET FAKE_PATH=packages\FAKE\tools\Fake.exe
SET Platform=

"%FAKE_PATH%" -v -v -v "build.fsx" lsbbusr=%1 lsbbpwd=%2

REM IF [%1]==[] (
REM     "%FAKE_PATH%" "build.fsx" "Just Do It" 
REM ) ELSE (
REM     "%FAKE_PATH%" "build.fsx" %* 
REM ) 
