# 🔍 Instrucciones de Debugging - Botones Premium

## ¿Qué se agregó?

Se ha implementado un **sistema de logging a archivo** para diagnosticar por qué los botones premium no se están cargando correctamente después del login.

### Archivos modificados:
1. **BIMLoginWindow.cs** - Registra cuando el login ocurre y cuántos botones premium se reciben
2. **ScriptPanel.xaml.cs** - Registra el inicio y fin de la descarga de botones
3. **PremiumButtonsCacheManager.cs** - Registra detalles de descarga y caché

---

## 📋 Pasos para Probar

### 1. Compilar la nueva versión

```powershell
cd "h:\Mi unidad\APPS\BIMTEGRACION\BIMtegration Copilot"
dotnet build "Proyecto Mars.sln" -c Release
```

✅ **Resultado esperado**: 0 errores, 42 advertencias

---

### 2. Copiar archivos compilados a la carpeta de add-ins de Revit

```powershell
# Ubicación de salida
$source = "h:\Mi unidad\APPS\BIMTEGRACION\BIMtegration Copilot\RoslynCopilotTest\bin\Release\net48\"

# Ubicación de Revit Add-ins (2025)
$target = "C:\ProgramData\Autodesk\Revit\Addins\2025\"

# Copiar
Copy-Item "$source\CodeAssistantPro.dll" $target -Force
Copy-Item "$source\CodeAssistantPro.pdb" $target -Force

# Copiar también las dependencias (opcional pero recomendado)
Copy-Item "$source\*.dll" $target -Force
```

---

### 3. Reiniciar Revit

Cierra Revit completamente y vuelve a abrirlo para que cargue la nueva versión.

---

### 4. Hacer Login con tu cuenta BIMtegration

1. Abre el panel de RoslynCopilot
2. Haz clic en el botón de Login (Tab "Advanced")
3. Introduce tus credenciales

> **IMPORTANTE**: ¿Utilizaste una cuenta PREMIUM o FREE?
> - **FREE**: Los botones premium no aparecerán (es comportamiento esperado)
> - **PREMIUM**: Deberían aparecer botones premium

---

### 5. Revisar el archivo de log

El sistema ahora genera un archivo de log en:

```
C:\Users\[TuUsuario]\AppData\Roaming\RoslynCopilot\premium-buttons-debug.log
```

**Cómo abrir:**
```powershell
# En PowerShell:
code "$env:APPDATA\RoslynCopilot\premium-buttons-debug.log"

# O directamente:
notepad "$env:APPDATA\RoslynCopilot\premium-buttons-debug.log"
```

---

## 📊 Qué buscar en el Log

### ✅ Caso exitoso (PREMIUM account):

```
[HH:MM:SS.mmm] [BIMLoginWindow] ✅ Login exitoso - Usuario: tu_usuario
[HH:MM:SS.mmm] [BIMLoginWindow] Plan: PREMIUM
[HH:MM:SS.mmm] [BIMLoginWindow] Botones premium recibidos: 5
[HH:MM:SS.mmm]   - Script 1 (Empresa: MiEmpresa)
[HH:MM:SS.mmm]   - Script 2 (Empresa: MiEmpresa)
[HH:MM:SS.mmm] [ScriptPanel.DownloadPremiumButtonsAsync] Iniciando descarga. Botones recibidos: 5
[HH:MM:SS.mmm] [DownloadPremiumButtonsAsync] ✅ COMPLETADO: 5 exitosos, 0 con error, 5 scripts totales
```

### ⚠️ Caso FREE account:

```
[HH:MM:SS.mmm] [BIMLoginWindow] ✅ Login exitoso - Usuario: tu_usuario
[HH:MM:SS.mmm] [BIMLoginWindow] Plan: FREE
[HH:MM:SS.mmm] [BIMLoginWindow] Botones premium recibidos: 0
[HH:MM:SS.mmm] [ScriptPanel.DownloadPremiumButtonsAsync] ⚠️ Sin botones premium para descargar
```

### ❌ Errores comunes:

```
[HH:MM:SS.mmm] [BIMLoginWindow] ❌ Login falló: Credenciales inválidas

[HH:MM:SS.mmm] [DownloadPremiumButtonsAsync] ❌ Error crítico: HttpRequestException
```

---

## 🔧 Interpretación de Resultados

### Escenario 1: Log muestra "Botones premium recibidos: 0"
**Causa probable**: El backend no está retornando botones
**Solución**: 
- Verificar que tu cuenta tiene plan PREMIUM
- Confirmar que hay scripts en Google Sheets configurados

### Escenario 2: Log muestra "Botones premium recibidos: 5" pero el UI muestra "No premium scripts available"
**Causa probable**: Problema en el UI o caché
**Solución**:
- Limpiar caché: Borrar `%APPDATA%\RoslynCopilot\premium-buttons-cache\`
- Reiniciar Revit
- Hacer login nuevamente

### Escenario 3: No aparece el log o está vacío
**Causa probable**: `DownloadPremiumButtonsAsync()` no se está llamando
**Solución**:
- Verificar que el login fue exitoso
- Buscar en el log si dice "✅ Login exitoso"
- Si no aparece, revisar consola de Revit para errores

---

## 💡 Próximos pasos de debugging

Si aún así no funcionan los botones premium:

1. **Envía el archivo de log completo**
2. **Incluye**:
   - Tipo de cuenta (PREMIUM/FREE)
   - Número de scripts configurados en el backend
   - Cualquier error que veas en Revit

---

## 🚀 Archivos compilados

Después de ejecutar `dotnet build`, encontrarás:

```
RoslynCopilotTest\bin\Release\net48\
├── CodeAssistantPro.dll (main)
├── CodeAssistantPro.pdb (symbols)
├── *.dll (dependencies)
└── ...
```

**Todos estos archivos deben copiarse a la carpeta de Revit Add-ins.**

---

## 📝 Notas técnicas

- El log se reinicia cada vez que Revit se abre (se borra el archivo anterior)
- Los logs se escriben en tiempo real conforme ocurren
- No afecta el rendimiento de la aplicación
- El archivo de log es seguro borrar si ocupa mucho espacio

