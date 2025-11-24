@echo off
echo ========================================
echo   BIMtegration Copilot - Build Installer
echo ========================================
echo.

:: Cambiar al directorio del script
cd /d "%~dp0"

:: Verificar que Inno Setup esté instalado
set INNO_PATH="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist %INNO_PATH% (
    echo [ERROR] No se encontro Inno Setup en: %INNO_PATH%
    echo.
    echo Por favor, instala Inno Setup 6 desde:
    echo https://jrsoftware.org/isdl.php
    echo.
    pause
    exit /b 1
)

:: Verificar que el proyecto esté compilado en Release
if not exist "RoslynCopilotTest\bin\Release\net48\CodeAssistantPro.dll" (
    echo [ERROR] No se encontro CodeAssistantPro.dll en Release
    echo.
    echo Compilando proyecto en Release mode...
    dotnet build -c Release
    if errorlevel 1 (
        echo [ERROR] Fallo la compilacion del proyecto
        pause
        exit /b 1
    )
)

:: Verificar archivos críticos
echo Verificando archivos necesarios...
echo.

set "MISSING_FILES="

if not exist "RoslynCopilotTest\BIMtegration Copilot.addin" (
    echo [X] FALTA: BIMtegration Copilot.addin
    set "MISSING_FILES=1"
) else (
    echo [OK] BIMtegration Copilot.addin
)

if not exist "RoslynCopilotTest\Scripts\my-scripts.json" (
    echo [X] FALTA: my-scripts.json
    set "MISSING_FILES=1"
) else (
    echo [OK] my-scripts.json
)

if not exist "INSTRUCCIONES_AI_SCRIPTS.md" (
    echo [!] ADVERTENCIA: INSTRUCCIONES_AI_SCRIPTS.md no encontrado
)

if defined MISSING_FILES (
    echo.
    echo [ERROR] Faltan archivos criticos. No se puede crear el instalador.
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Compilando instalador con Inno Setup
echo ========================================
echo.

:: Compilar con Inno Setup
%INNO_PATH% "BIMtegration_Installer.iss"

if errorlevel 1 (
    echo.
    echo [ERROR] Fallo la compilacion del instalador
    pause
    exit /b 1
)

echo.
echo ========================================
echo   [EXITO] Instalador creado correctamente
echo ========================================
echo.

:: Mostrar ubicación del instalador
if exist "Installer\BIMtegration_Copilot_Setup_v1.0.4.exe" (
    echo Instalador generado en:
    echo %cd%\Installer\BIMtegration_Copilot_Setup_v1.0.4.exe
    echo.
    
    :: Obtener tamaño del archivo
    for %%A in ("Installer\BIMtegration_Copilot_Setup_v1.0.4.exe") do (
        set "SIZE=%%~zA"
    )
    
    :: Convertir bytes a MB
    set /a SIZE_MB=%SIZE% / 1048576
    echo Tamano: %SIZE_MB% MB
    echo.
    
    :: Preguntar si desea abrir la carpeta
    set /p OPEN="Desea abrir la carpeta del instalador? (S/N): "
    if /i "%OPEN%"=="S" (
        explorer "Installer"
    )
) else (
    echo [ADVERTENCIA] No se encontro el instalador en la ubicacion esperada
)

echo.
pause
