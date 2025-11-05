# 📦 Instalador BIMtegration Copilot

## 🎯 Contenido del Instalador

El instalador `.exe` incluye todo lo necesario para usar BIMtegration Copilot en Revit:

### ✅ Archivos Incluidos

1. **Addon Completo** (`net48` folder):
   - `CodeAssistantPro.dll` (addon principal)
   - Todas las dependencias (Roslyn, Newtonsoft.Json, EPPlus, etc.)
   - Archivos de localización (es/, en/, etc.)

2. **Manifest de Revit**:
   - `BIMtegration Copilot.addin` → instalado en `%AppData%\Autodesk\Revit\Addins\2025\`

3. **Scripts de Ejemplo** (my-scripts.json):
   - 17 scripts pre-configurados listos para usar
   - Ejemplos de HTTP requests, selección de elementos, exportación, etc.
   - Instalados en `%AppData%\RoslynCopilot\Scripts\`

4. **Documentación**:
   - `INSTRUCCIONES_AI_SCRIPTS.md` → guía para generar scripts con IA
   - `INSTRUCCIONES_AUTH_BIMTEGRATION.md` → sistema de autenticación
   - `INSTRUCCIONES_BUILD.md` → compilación del proyecto
   - `README.md` → información general

---

## 🛠️ Cómo Compilar el Instalador

### Requisitos Previos

1. ✅ **Inno Setup 6.x** instalado
2. ✅ Proyecto compilado en **Release** mode
3. ✅ Archivo `BIMtegration_Installer.iss` en la raíz del proyecto

### Opción 1: Compilar con Script Batch (Fácil)

1. Ejecutar `Build_Installer.bat`
2. El instalador se generará en la carpeta `Installer\`

### Opción 2: Compilar Manualmente

1. Abrir **Inno Setup Compiler**
2. Abrir el archivo `BIMtegration_Installer.iss`
3. Presionar **F9** o hacer clic en **Build → Compile**
4. El instalador se generará en la carpeta `Installer\`

### Opción 3: Compilar desde Terminal

```powershell
# Ruta típica de Inno Setup
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "BIMtegration_Installer.iss"
```

---

## 📋 Verificación Pre-Build

Antes de compilar el instalador, verifica que existan estos archivos:

```
✅ RoslynCopilotTest\bin\Release\net48\CodeAssistantPro.dll
✅ RoslynCopilotTest\bin\Release\net48\*.dll (todas las dependencias)
✅ RoslynCopilotTest\BIMtegration Copilot.addin
✅ RoslynCopilotTest\Scripts\my-scripts.json
✅ INSTRUCCIONES_AUTH_BIMTEGRATION.md
✅ INSTRUCCIONES_AI_SCRIPTS.md
✅ INSTRUCCIONES_BUILD.md
✅ README.md
```

### Script de Verificación

```powershell
# Verificar archivos necesarios
$files = @(
    "RoslynCopilotTest\bin\Release\net48\CodeAssistantPro.dll",
    "RoslynCopilotTest\BIMtegration Copilot.addin",
    "RoslynCopilotTest\Scripts\my-scripts.json",
    "INSTRUCCIONES_AUTH_BIMTEGRATION.md"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "✅ $file" -ForegroundColor Green
    } else {
        Write-Host "❌ FALTA: $file" -ForegroundColor Red
    }
}
```

---

## 🚀 Proceso de Instalación (Usuario Final)

### Lo que hace el instalador:

1. **Verifica versiones de Revit instaladas** (2023, 2024, 2025)
2. **Verifica .NET Framework 4.8** (requerido)
3. **Copia archivos del addon** a:
   ```
   C:\ProgramData\Autodesk\Revit\Addins\2025\BIMtegration Copilot\
   ```

4. **Instala el manifest** en:
   ```
   C:\ProgramData\Autodesk\Revit\Addins\2025\BIMtegration Copilot.addin
   ```

5. **Copia scripts de ejemplo** a:
   ```
   C:\ProgramData\RoslynCopilot\Scripts\
   ```

6. **Crea carpetas de trabajo**:
   - `%AppData%\RoslynCopilot\Scripts\` → scripts personalizados
   - `%AppData%\RoslynCopilot\History\` → historial de ejecución
   - `%AppData%\RoslynCopilot\Favorites\` → scripts favoritos

### Mensaje Post-Instalación

El usuario verá:
```
✅ ¡Instalación completada exitosamente!

📌 Próximos pasos:
1. Cierre Revit si está abierto
2. Abra Revit 2025
3. Busque "BIMtegration Copilot" en la pestaña de Add-ins

📚 La documentación se encuentra en:
C:\ProgramData\Autodesk\Revit\Addins\2025\BIMtegration Copilot\Docs
```

---

## 📁 Estructura Post-Instalación

```
C:\ProgramData\Autodesk\Revit\Addins\2025\
├── BIMtegration Copilot\
│   ├── net48\
│   │   ├── CodeAssistantPro.dll
│   │   ├── Newtonsoft.Json.dll
│   │   ├── EPPlus.dll
│   │   ├── Microsoft.CodeAnalysis.*.dll
│   │   └── ... (todas las dependencias)
│   └── Docs\
│       ├── INSTRUCCIONES_AI_SCRIPTS.md
│       ├── INSTRUCCIONES_AUTH_BIMTEGRATION.md
│       ├── INSTRUCCIONES_BUILD.md
│       └── README.md
├── BIMtegration Copilot.addin

C:\ProgramData\RoslynCopilot\
├── Scripts\
│   └── my-scripts.json (scripts de ejemplo)
├── History\
└── Favorites\
```

---

## 🔧 Personalización del Instalador

### Cambiar Versión

Editar en `BIMtegration_Installer.iss`:
```pascal
#define MyAppVersion "1.0.0"  // <- Cambiar aquí
```

### Agregar Icono Personalizado

1. Crear archivo `icon.ico` en la raíz del proyecto
2. El instalador lo usará automáticamente

### Soportar Múltiples Versiones de Revit

Editar la sección `[Files]` para agregar más versiones:
```pascal
; Revit 2024
Source: ".\RoslynCopilotTest\BIMtegration Copilot.addin"; 
DestDir: "{commonappdata}\Autodesk\Revit\Addins\2024"; 
Flags: ignoreversion

; Revit 2023
Source: ".\RoslynCopilotTest\BIMtegration Copilot.addin"; 
DestDir: "{commonappdata}\Autodesk\Revit\Addins\2023"; 
Flags: ignoreversion
```

---

## 🐛 Troubleshooting

### Error: "Cannot find file CodeAssistantPro.dll"

**Solución**: Compilar el proyecto en Release mode primero:
```powershell
dotnet build -c Release
```

### Error: "Cannot find Inno Setup"

**Solución**: Instalar Inno Setup desde https://jrsoftware.org/isdl.php

### Instalador muy grande (>200MB)

**Solución**: Normal. El instalador incluye:
- 35+ DLLs del addon
- Roslyn compiler (~50MB)
- EPPlus libraries
- Documentación

### El addon no aparece en Revit

**Causas posibles**:
1. Revit estaba abierto durante la instalación → **Cerrar y reabrir Revit**
2. .NET Framework 4.8 no instalado → **Instalar .NET 4.8**
3. Addon bloqueado por Windows → **Desbloquear DLLs** (clic derecho → Properties → Unblock)

---

## 📊 Especificaciones del Instalador

| Característica | Valor |
|----------------|-------|
| **Tamaño aproximado** | ~150-200 MB |
| **Compresión** | LZMA2/Max (mejor ratio) |
| **Requiere admin** | Sí (escribe en ProgramData) |
| **Idiomas** | Español, Inglés |
| **Versiones de Revit soportadas** | 2023, 2024, 2025 |
| **Sistema operativo** | Windows 8 o superior |
| **.NET Framework** | 4.8 o superior |

---

## 📝 Notas Importantes

1. **Siempre compilar en Release**: El instalador busca archivos en `bin\Release\net48\`
2. **Verificar dependencias**: Todas las DLLs deben estar en la carpeta Release
3. **Probar antes de distribuir**: Instalar en una máquina limpia para verificar
4. **Documentación actualizada**: Incluir siempre la última versión de los .md files

---

## 🔄 Actualizaciones

Para crear un instalador de actualización:

1. Cambiar `#define MyAppVersion` en el .iss
2. El instalador detectará la versión anterior y la sobrescribirá
3. Los scripts y configuraciones del usuario se preservarán (flag `onlyifdoesntexist`).

Nota: A partir de esta versión, el instalador no borra la carpeta global de ejemplos (`C:\ProgramData\RoslynCopilot\`) durante la desinstalación para evitar pérdida de scripts. Además, al primer inicio del addon, si el usuario no tiene una copia en `%AppData%`, la aplicación copiará automáticamente los ejemplos globales desde `C:\ProgramData\RoslynCopilot\Scripts\my-scripts.json` a la ruta de usuario para que cada usuario pueda personalizar su propia copia.

---

## 📞 Soporte

Para problemas con el instalador o la instalación, revisar:
- `INSTRUCCIONES_BUILD.md` → compilación del proyecto
- `INSTRUCCIONES_AUTH_BIMTEGRATION.md` → autenticación
- Logs de Inno Setup en `%TEMP%\Setup Log YYYY-MM-DD #XXX.txt`

---

**Última actualización:** Octubre 2025  
**Versión del instalador:** 1.0.0
