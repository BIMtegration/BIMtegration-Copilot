# ✅ Sistema de Logging para Debugging - Premium Buttons

## 📝 Resumen de Cambios

Se ha implementado un **sistema de logging a archivo** para diagnosticar problemas con los botones premium. El sistema registra cada paso del proceso de login y descarga de botones.

---

## 🔧 Archivos Modificados

### 1. **BIMLoginWindow.cs**
- ✅ Agrega método `LogToFile()` para escribir logs
- ✅ Registra usuario y plan tras login exitoso
- ✅ Registra cantidad de botones premium recibidos
- ✅ Lista cada botón descargado con su empresa
- ✅ Registra errores de login

**Logs generados:**
```
[BIMLoginWindow] ✅ Login exitoso - Usuario: {usuario}
[BIMLoginWindow] Plan: {PREMIUM/FREE}
[BIMLoginWindow] Botones premium recibidos: {cantidad}
  - {nombre botón} (Empresa: {empresa})
```

---

### 2. **ScriptPanel.xaml.cs**
- ✅ Agrega método `LogPremium()` para escribir logs
- ✅ Registra inicio de descarga de botones
- ✅ Registra cada botón que se va descargando
- ✅ Registra resumen final (exitosos/errores)
- ✅ Registra cualquier excepción

**Logs generados:**
```
[ScriptPanel.DownloadPremiumButtonsAsync] Iniciando descarga. Botones recibidos: {N}
[ScriptPanel.DownloadPremiumButtonsAsync] Inicializando estado de descarga para {N} botones
  - Descargando: {nombre} (ID: {id})
[DownloadPremiumButtonsAsync] ✅ COMPLETADO: {X} exitosos, {Y} con error, {Z} scripts totales
```

---

### 3. **PremiumButtonsCacheManager.cs**
- ✅ Agrega método `LogToFile()` estático para escribir logs
- ✅ Registra inicio de descarga detallada
- ✅ Registra resumen de descargas exitosas/errores
- ✅ Registra cualquier excepción crítica

**Logs generados:**
```
[PremiumButtonsCacheManager.DownloadPremiumButtonsWithDetailsAsync] Iniciando descarga de {N} botones premium
[PremiumButtonsCacheManager.DownloadPremiumButtonsWithDetailsAsync] ✅ Descarga completada: {X} exitosas, {Y} con error
[PremiumButtonsCacheManager.DownloadPremiumButtonsWithDetailsAsync] ❌ Error crítico: {tipo error}
```

---

## 📍 Ubicación del Archivo de Log

**Ruta**: `C:\Users\[TuUsuario]\AppData\Roaming\RoslynCopilot\premium-buttons-debug.log`

**Comando para abrir:**
```powershell
code "$env:APPDATA\RoslynCopilot\premium-buttons-debug.log"
```

**Nota**: El log se **reemplaza en cada inicio** de Revit (se limpia la sesión anterior)

---

## 🚀 Cómo Usar

### Opción 1: Script automático (RECOMENDADO)

```powershell
# Ejecutar desde PowerShell en la carpeta del proyecto
.\Deploy-PremiumButtons.ps1
```

Esto hará automáticamente:
1. ✅ Compilar en Release
2. ✅ Copiar archivos a Revit Add-ins
3. ✅ Mostrar instrucciones

---

### Opción 2: Manual

```powershell
# 1. Compilar
cd "h:\Mi unidad\APPS\BIMTEGRACION\BIMtegration Copilot"
dotnet build "Proyecto Mars.sln" -c Release

# 2. Copiar archivos (cuando Revit esté cerrado)
$source = ".\RoslynCopilotTest\bin\Release\net48\"
$target = "C:\ProgramData\Autodesk\Revit\Addins\2025\"
Copy-Item "$source\*.dll" $target -Force
Copy-Item "$source\*.pdb" $target -Force

# 3. Abrir Revit
```

---

## 🔍 Interpretación del Log

### Caso 1: ✅ PREMIUM Account con Botones

```
[14:23:45.123] [BIMLoginWindow] ✅ Login exitoso - Usuario: juan@empresa.com
[14:23:45.145] [BIMLoginWindow] Plan: PREMIUM
[14:23:45.156] [BIMLoginWindow] Botones premium recibidos: 3
[14:23:45.167]   - Script de Muros (Empresa: MiEmpresa)
[14:23:45.178]   - Script de Puertas (Empresa: MiEmpresa)
[14:23:45.189]   - Script de Ventanas (Empresa: MiEmpresa)
[14:23:45.200] [ScriptPanel.DownloadPremiumButtonsAsync] Iniciando descarga. Botones recibidos: 3
[14:23:45.300] [DownloadPremiumButtonsAsync] ✅ COMPLETADO: 3 exitosos, 0 con error, 3 scripts totales
```

**Resultado**: ✅ Los botones deben aparecer en el panel

---

### Caso 2: 🆓 FREE Account

```
[14:23:45.123] [BIMLoginWindow] ✅ Login exitoso - Usuario: juan@empresa.com
[14:23:45.145] [BIMLoginWindow] Plan: FREE
[14:23:45.156] [BIMLoginWindow] Botones premium recibidos: 0
[14:23:45.200] [ScriptPanel.DownloadPremiumButtonsAsync] ⚠️ Sin botones premium para descargar
```

**Resultado**: ✅ Comportamiento esperado (FREE account no tiene botones)

---

### Caso 3: ❌ Error en Login

```
[14:23:45.123] [BIMLoginWindow] ❌ Login falló: Credenciales inválidas
```

**Acción**: Verificar usuario/contraseña

---

### Caso 4: ❌ Error en Descarga

```
[14:23:45.200] [DownloadPremiumButtonsAsync] ❌ COMPLETADO: 0 exitosos, 3 con error, 0 scripts totales
[14:23:45.210] [DownloadPremiumButtonsAsync] ❌ Error crítico: HttpRequestException - Unable to connect to server
```

**Causa probable**: Problema de conexión o URL de descarga inválida

---

## 🐛 Troubleshooting

### "El log está vacío o no aparecen mensajes de login"
1. ❌ Revisa que el archivo existe: `$env:APPDATA\RoslynCopilot\premium-buttons-debug.log`
2. ✅ Intenta hacer login nuevamente
3. ✅ Si aún está vacío, `DownloadPremiumButtonsAsync()` no se está llamando

---

### "Plan: FREE pero espero PREMIUM"
1. Verificar en el backend que la cuenta tiene plan PREMIUM
2. Confirmar que está logueado con la cuenta correcta
3. Revisar Google Sheets si hay scripts configurados

---

### "Botones recibidos: 3 pero el UI muestra 0"
1. Limpiar caché: `Remove-Item "$env:APPDATA\RoslynCopilot\premium-buttons-cache\" -Recurse -Force`
2. Reiniciar Revit
3. Hacer login nuevamente
4. Revisar el nuevo log

---

## 📊 Estadísticas

| Métrica | Valor |
|---------|-------|
| **Archivos modificados** | 3 |
| **Métodos de logging agregados** | 3 |
| **Lineas de logging agregadas** | ~15 |
| **Errores en compilación** | 0 |
| **Warnings (no críticos)** | 42 |
| **Tiempo de compilación** | ~3.35s |

---

## ✨ Beneficios

- ✅ **Debugging sin VS2022**: Puedes ver exactamente qué ocurre sin necesidad de debugger
- ✅ **Diagnostico rapido**: Identifica rápidamente si el problema es en backend, login o UI
- ✅ **Cero impacto en rendimiento**: Logging asincrónico, no bloquea la aplicación
- ✅ **Fácil de desactivar**: Si necesitas, puedes remover los logs rápidamente
- ✅ **Archivo persistente**: Los logs se guardan en disco para análisis posterior

---

## 🎯 Próximo Paso

Ejecuta el script de deployment y proporciona el contenido del log para que podamos diagnosticar el problema específico.

