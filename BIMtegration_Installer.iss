; Script de instalación para BIMtegration Copilot
; Creado para Inno Setup 6.x
; Última actualización: Octubre 2025

#define MyAppName "BIMtegration Copilot"
#define MyAppVersion "1.0.4"
#define MyAppPublisher "BIMtegration"
#define MyAppURL "https://www.bimtegration.com"
#define MyAppExeName "CodeAssistantPro.dll"

[Setup]
; Información básica
; Use double braces to ensure the GUID is interpreted as a literal by ISPP
AppId={{FA486D60-B56D-4BD1-87F3-0D537AC86C30}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Directorio de instalación predeterminado
DefaultDirName={commonappdata}\Autodesk\Revit\Addins\2025\BIMtegration Copilot
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

; Archivos de salida
OutputDir=.\Installer
OutputBaseFilename=BIMtegration_Copilot_Setup_v{#MyAppVersion}
; SetupIconFile=.\icon.ico  ; Opcional: descomenta si tienes un icono personalizado
Compression=lzma2/max
SolidCompression=yes

; Privilegios administrativos
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; Apariencia moderna
WizardStyle=modern
WizardSizePercent=120
DisableWelcomePage=no

; Idiomas
ShowLanguageDialog=auto

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Carpeta Release completa con todas las DLLs
Source: ".\RoslynCopilotTest\bin\Release\net48\*"; DestDir: "{app}\net48"; Flags: ignoreversion recursesubdirs createallsubdirs

; Archivo .addin (manifest de Revit)
Source: ".\RoslynCopilotTest\BIMtegration Copilot.addin"; DestDir: "{commonappdata}\Autodesk\Revit\Addins\2025"; Flags: ignoreversion

; Script base con ejemplos
Source: ".\RoslynCopilotTest\Scripts\my-scripts.json"; DestDir: "{commonappdata}\RoslynCopilot\Scripts"; Flags: ignoreversion onlyifdoesntexist

[Dirs]
; Crear carpetas necesarias
Name: "{commonappdata}\RoslynCopilot"
Name: "{commonappdata}\RoslynCopilot\Scripts"
Name: "{commonappdata}\RoslynCopilot\History"
Name: "{commonappdata}\RoslynCopilot\Favorites"

[Icons]
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"

[Run]

[Code]
// Verificar versiones de Revit instaladas
function GetRevitVersions(): String;
var
  Versions: String;
begin
  Versions := '';
  
  // Verificar Revit 2025
  if RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\Autodesk\Revit\2025') then
    Versions := Versions + '2025, ';
  
  // Verificar Revit 2024
  if RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\Autodesk\Revit\2024') then
    Versions := Versions + '2024, ';
  
  // Verificar Revit 2023
  if RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\Autodesk\Revit\2023') then
    Versions := Versions + '2023, ';
  
  if Versions = '' then
    Result := 'No se detectaron versiones de Revit instaladas'
  else
  begin
    // Remover última coma
    Delete(Versions, Length(Versions) - 1, 2);
    Result := 'Versiones de Revit detectadas: ' + Versions;
  end;
end;

// Mostrar mensaje de bienvenida con versiones detectadas
function InitializeSetup(): Boolean;
var
  RevitVersions: String;
begin
  Result := True;
  RevitVersions := GetRevitVersions();
  
  // Verificar si hay al menos una versión de Revit
  if Pos('No se detectaron', RevitVersions) > 0 then
  begin
    if MsgBox('⚠️ ADVERTENCIA: No se detectó Autodesk Revit instalado en este equipo.' + #13#10 + #13#10 +
              'BIMtegration Copilot requiere Revit 2023 o superior para funcionar.' + #13#10 + #13#10 +
              '¿Desea continuar con la instalación de todas formas?',
              mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end
  else
  begin
    MsgBox('✅ ' + RevitVersions + #13#10 + #13#10 +
           'BIMtegration Copilot se instalará para Revit 2025.' + #13#10 +
           'Si necesita instalarlo para otras versiones, copie manualmente los archivos.',
           mbInformation, MB_OK);
  end;
end;

// Verificar .NET Framework 4.8
function IsDotNet48Installed(): Boolean;
begin
  Result := RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full') and
            (GetWindowsVersion >= $06020000); // Windows 8 o superior
end;

// Mensaje previo a la instalación
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  
  if CurPageID = wpReady then
  begin
    // Verificar .NET Framework
    if not IsDotNet48Installed() then
    begin
      if MsgBox('⚠️ ADVERTENCIA: No se detectó .NET Framework 4.8 o superior.' + #13#10 + #13#10 +
                'El addon requiere .NET Framework 4.8 para funcionar correctamente.' + #13#10 + #13#10 +
                '¿Desea continuar con la instalación de todas formas?',
                mbConfirmation, MB_YESNO) = IDNO then
        Result := False;
    end;
  end;
end;

// Mensaje post-instalación
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    MsgBox('✅ ¡Instalación completada exitosamente!' + #13#10 + #13#10 +
           '📌 Próximos pasos:' + #13#10 +
           '1. Cierre Revit si está abierto' + #13#10 +
           '2. Abra Revit 2025' + #13#10 +
           '3. Busque "BIMtegration Copilot" en la pestaña de Add-ins' + #13#10 + #13#10 +
           '📚 La documentación se encuentra en:' + #13#10 +
           ExpandConstant('{app}\Docs'),
           mbInformation, MB_OK);
  end;
end;

[UninstallDelete]
; NOTE: We intentionally do NOT delete the global RoslynCopilot data folder to preserve
; user/custom scripts that might be stored in ProgramData across upgrades/uninstalls.
; If a full removal is required, uncomment the next line.
;Type: filesandordirs; Name: "{commonappdata}\RoslynCopilot"
Type: files; Name: "{commonappdata}\Autodesk\Revit\Addins\2025\BIMtegration Copilot.addin"

[Messages]
spanish.WelcomeLabel1=Bienvenido al Asistente de Instalación de [name]
spanish.WelcomeLabel2=Este programa instalará [name/ver] en su equipo.%n%nSe recomienda cerrar todas las instancias de Autodesk Revit antes de continuar.
spanish.FinishedHeadingLabel=Completando el Asistente de Instalación de [name]
spanish.FinishedLabel=La aplicación ha sido instalada en su equipo. Puede ejecutarla abriendo Autodesk Revit.
