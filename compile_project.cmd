@echo off
:: Print start time
echo Started at -------------------------- %time% --------------------------

:: Using utf-8 in CMD
chcp 65001 >nul 2>&1

:: -------------------------- BUILD TYPE PROMPT ---------------------------
:chooseBuildType
set "buildTypeInput="
set /p buildTypeInput=Enter build type ([P]ublish / [B]eta) [default: P]: 

if /i "%buildTypeInput%"=="" (
    set "buildType=publish"
) else if /i "%buildTypeInput%"=="p" (
    set "buildType=publish"
) else if /i "%buildTypeInput%"=="b" (
    set "buildType=beta"
) else (
    echo Invalid input. Please enter 'P' for publish or 'B' for beta.
    goto chooseBuildType
)

:: ------------------------------- VARIABLES -------------------------------
:: Please override everythings in <> """ not anything else """

:: Output folder name
set "output=output"

:: Compile folder name depends on build type
if /i "%buildType%"=="beta" (
    set "compileFolder=compile-beta\MixinApi"
) else (
    set "compileFolder=compile\MixinApi"
)

:: Solution name
set "sln=MixinApi.sln"

:: Build configuration name
set "buildConfigurationName=Compile"

:: Compile path
set "compileOutput=%cd%\%output%\%compileFolder%"

:: ------------------------ INFORMATION_VALIDATION -------------------------

echo:
echo Build type: %buildType%
echo Output folder: %output%
echo Compile folder: %compileFolder%
echo Solution name: %sln%
echo Build configuration name: %buildConfigurationName%
echo Compile output path: %compileOutput%
echo:
echo "If the above information is correct, press any key to continue..."
pause

:: ----------------------------- CREATE_FOLDERS ----------------------------

:: Create output folder if it doesn't exist
if not exist "%output%" (
    echo Creating %output% folder...
    mkdir "%output%"
)

:: Remove existing compile output folder if it exists
if exist "%compileOutput%" (
    echo Removing old folder: %compileOutput%
    rmdir /s /q "%compileOutput%"
)

:: Create new compile folder
echo Creating new folder: %compileOutput%
mkdir "%compileOutput%"

:: ------------------------------ GET_COMPILE ------------------------------

:: Restore packages
echo Restoring NuGet packages...
nuget restore "%sln%"

:: Compile project
echo Compiling project...
msbuild "%sln%" /t:Clean,Build /p:Configuration=%buildConfigurationName%;Platform=x86;OutputPath="%compileOutput%"

:: ---------------------------- DONE ----------------------------

echo:
echo Build finished at -------------------- %time% -------------------
pause
exit
