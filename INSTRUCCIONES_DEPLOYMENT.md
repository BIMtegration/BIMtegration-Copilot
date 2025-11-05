# 🚀 INSTRUCCIONES DE DEPLOYMENT

**Sistema de Botones Premium - Guía de Implementación**  
**Versión:** 1.0  
**Fecha:** Noviembre 4, 2025  

---

## 📋 PRE-DEPLOYMENT CHECKLIST

Antes de hacer deploy a producción, verifica:

### ✅ Verificaciones Técnicas

- [ ] Todos los archivos compilados sin errores
- [ ] Solución compila completamente: `Build` → `Build Solution`
- [ ] No hay warnings críticos
- [ ] Código está en rama `main` o rama de desarrollo correcta
- [ ] Cambios están commitados en git
- [ ] Versión .addin está actualizada
- [ ] Versión de .NET Framework es 4.8

### ✅ Verificaciones Funcionales

- [ ] Prueba login con cuenta premium en ambiente de staging
- [ ] Verifica que botones se descargan automáticamente
- [ ] Verifica que caché funciona (segunda descarga es más rápida)
- [ ] Prueba retry en un botón (simula error)
- [ ] Prueba descarga manual de botón
- [ ] Verifica que scripts premium aparecen en Advanced tab
- [ ] Verifica que scripts se agrupan por empresa
- [ ] Prueba con usuario free (no debe mostrar botones)

### ✅ Verificaciones de Datos

- [ ] Google Sheets con metadatos de botones está actualizada
- [ ] URLs de Google Drive son públicas y válidas
- [ ] Archivos JSON en Google Drive están bien formados
- [ ] Categorías en JSON contienen "🔒 [Empresa]"
- [ ] Formato Google Sheets sigue: "nombre1,url1;nombre2,url2,company2"

### ✅ Verificaciones de Documentación

- [ ] INSTRUCCIONES_AI_SCRIPTS.md sección 11 es clara
- [ ] RESUMEN_SISTEMA_BOTONES_PREMIUM.md existe
- [ ] CHECKLIST_VALIDACION_BOTONES_PREMIUM.md existe
- [ ] Archivos están accesibles a usuarios finales

---

## 🔧 PASOS DE DEPLOYMENT

### Paso 1: Preparar Solución para Build

```powershell
# En Visual Studio o Terminal
cd "h:\Mi unidad\APPS\BIMTEGRACION\BIMtegration Copilot"

# Limpiar build previo
Remove-Item ".\RoslynCopilotTest\bin\Release" -Recurse -Force -ErrorAction SilentlyContinue

# Rebuild solución
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
  ".\Proyecto Mars.sln" `
  /p:Configuration=Release `
  /p:Platform=x64
```

### Paso 2: Validar Build Output

```powershell
# Verificar que el .addin se creó
Test-Path ".\RoslynCopilotTest\bin\Release\net48\BIMtegration Copilot.addin"
Test-Path ".\RoslynCopilotTest\bin\Release\net48\RoslynCopilotTest.dll"

# Ver tamaño de DLL (debe ser razonable)
Get-Item ".\RoslynCopilotTest\bin\Release\net48\RoslynCopilotTest.dll" | Select-Object Length
```

### Paso 3: Copiar Archivos a Locación de Instalación

```powershell
# Copiar DLL y dependencias a folder de Revit add-ins
$addinPath = "C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2025\BIMtegration\"
$sourcePath = ".\RoslynCopilotTest\bin\Release\net48\"

# Crear carpeta si no existe
if (-not (Test-Path $addinPath)) {
    New-Item $addinPath -ItemType Directory -Force
}

# Copiar archivos
Copy-Item "$sourcePath\RoslynCopilotTest.dll" $addinPath -Force
Copy-Item "$sourcePath\BIMtegration Copilot.addin" $addinPath -Force
Copy-Item "$sourcePath\*.dll" $addinPath -Force -Filter "*.dll"

# Verificar
Get-ChildItem $addinPath
```

### Paso 4: Actualizar Configuración de Usuario (si es necesario)

Si hay cambios en `BIMAuthService` endpoint:

1. Ve a `.\RoslynCopilotTest\Services\BIMAuthService.cs`
2. Verifica que `BaseUrl` apunta al servidor correcto
3. Si cambió: Actualiza todos los lugares donde se usa

### Paso 5: Verificación en Revit 2025

1. Abre Revit 2025
2. Ve a **Add-ins** → **Verify Loading Status**
3. Busca "BIMtegration Copilot" en la lista
4. Verifica que status es "Loaded" (no "Failed to Load")
5. Si hay error: Check **%AppData%/Autodesk/Revit/Addins/2025/BIMtegrationDebug.log**

6. Abre panel de Copilot: **Modify** → **BIMtegration Copilot** → **Show Script Panel**
7. Intenta login con cuenta premium
8. Verifica en **Debug** → **Output window** que logs `[Premium]` aparecen
9. Verifica que botones aparecen en pestaña Advanced

---

## 📦 DISTRIBUCIÓN A USUARIOS

### Opción A: Instalador Automatizado (Recomendado)

Si usar `Build_Installer.bat` y `BIMtegration_Installer.iss`:

```batch
# Actualizar versión en .iss si es necesario
# En BIMtegration_Installer.iss:
# [Setup]
# AppVersion=X.Y.Z

# Ejecutar build de instalador
.\Build_Installer.bat

# Resultado: BIMtegration_Setup.exe
```

Pasos para usuarios:
1. Descargan `BIMtegration_Setup.exe`
2. Ejecutan instalador
3. Se copia a `C:\Program Files\BIMtegration\` (u otra ruta)
4. Se registra add-in en Revit
5. Reinician Revit
6. Sistema de botones está funcional

### Opción B: Distribución Manual

1. Crear carpeta ZIP con contenido:
```
BIMtegration_v1.0.zip
├── RoslynCopilotTest.dll
├── BIMtegration Copilot.addin
├── (todas las dependencias .dll)
└── README.txt
```

2. Instruir usuarios:
   - Descomprimir a `%AppData%\Autodesk\Revit\Addins\2025\BIMtegration\`
   - Reiniciar Revit

### Opción C: Distribución en Red Compartida

1. Copiar archivos a `\\servidor\software\BIMtegration\v1.0\`
2. Crear script de instalación que:
   - Copia archivos de red local
   - Registra path en Revit

---

## 🔄 SINCRONIZACIÓN CON BACKEND

Antes de activar en producción, verificar con equipo backend:

### Verificaciones Backend

- [ ] Endpoint `/auth/login` retorna `buttons` en respuesta
- [ ] Formato de `buttons` es JSON válido con PremiumButtonInfo[]
- [ ] Google Sheets tiene credenciales de lectura correctas
- [ ] URLs en Google Sheets son públicas (compartidas)
- [ ] Archivos JSON en Google Drive son accesibles públicamente

### Testing Endpoint

```csharp
// En BIMAuthService.cs, probar manually:
using (var client = new HttpClient())
{
    var payload = new { email = "test@premium.com", password = "test123" };
    var json = JsonConvert.SerializeObject(payload);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    
    var response = await client.PostAsync(
        "https://tu-backend.com/auth/login",
        content
    );
    
    var result = JsonConvert.DeserializeObject<LoginResponse>(
        await response.Content.ReadAsStringAsync()
    );
    
    // Verificar que result.Buttons no es null
    Debug.WriteLine($"Botones recibidos: {result.Buttons?.Count ?? 0}");
}
```

---

## 🧪 TESTING EN PRODUCCIÓN

Después de deployment, testing plan:

### Week 1: Testing Interno

- [ ] Usuarios internos login con cuentas premium
- [ ] Verifican que botones se descargan
- [ ] Verifican que caché funciona
- [ ] Monitorean logs para errores `[Premium]`
- [ ] Reportan issues en equipo

### Week 2: Testing con Grupo Pilot

- [ ] 5-10 usuarios premium reales
- [ ] En ambiente real (no staging)
- [ ] Monitorear descarga, cache, ejecución
- [ ] Recolectar feedback

### Week 3+: Rollout Gradual

- [ ] 25% de usuarios premium
- [ ] 50% de usuarios premium
- [ ] 100% de usuarios premium

---

## 📊 MONITOREO POST-DEPLOYMENT

### KPIs a Monitorear

1. **Tasa de éxito de descarga:**
   - Target: > 95%
   - Monitor: Contar logs "✓ Descarga exitosa" vs "❌ Error"

2. **Tiempo promedio de descarga:**
   - Target: < 5 segundos (con caché < 1 segundo)
   - Monitor: Timestamp en logs

3. **Adopción de botones premium:**
   - Target: > 80% de usuarios premium usando al menos 1 botón
   - Monitor: Click en "Run" button

4. **Tasa de error de red:**
   - Target: < 2%
   - Monitor: Logs con "HttpRequestException" o "Timeout"

### Alertas Automáticas

Si alguno de estos ocurre, investigar:
- [ ] Más de 10 errores de descarga en una hora
- [ ] Más de 5 timeouts consecutivos
- [ ] Archivo de caché > 100 MB
- [ ] Manifest.json corrupto

---

## 🔧 TROUBLESHOOTING EN PRODUCCIÓN

### Si botones no aparecen

```powershell
# 1. Verificar que login retorna botones
# En Output window buscar: "[Premium] Iniciando descarga de X botones"

# 2. Verificar que caché se creó
$cachePath = "$env:APPDATA\RoslynCopilot\premium-buttons-cache\"
Test-Path $cachePath
Get-ChildItem $cachePath

# 3. Verificar manifest
Get-Content "$cachePath\manifest.json" | ConvertFrom-Json

# 4. Limpiar caché y reintentar
Remove-Item $cachePath -Recurse -Force
# Luego reiniciar Revit y hacer login de nuevo
```

### Si todos muestran error

```
Causas posibles:
1. URLs inválidas en Google Sheets
2. Archivos JSON en Google Drive no son públicos
3. Google Sheets no es accesible
4. Firewall bloqueando drive.usercontent.google.com

Acciones:
1. Verificar URLs: https://drive.usercontent.google.com/u/0/uc?id=...
2. Verificar Google Drive: Abrir URL en navegador
3. Verificar Google Sheets: Acceso público a lectura
4. Verificar firewall: Permitir drive.usercontent.google.com
```

### Si algunos usuarios tienen error, otros no

```
Causas posibles:
1. Diferentes versiones de DLL
2. Diferentes versiones de Revit
3. Diferentes permisos de acceso en Google Drive

Acciones:
1. Verificar que todos instalaron última versión
2. Verificar que Google Drive es accesible globalmente (no por dominio)
3. Limpiar caché en equipos problemáticos
```

---

## 🚨 ROLLBACK PLAN

Si algo falla críticamente:

### Opción 1: Rollback a Versión Anterior

```powershell
# Restaurar DLL anterior
$addinPath = "C:\Users\$env:USERNAME\AppData\Roaming\Autodesk\Revit\Addins\2025\BIMtegration\"
Copy-Item ".\backup\RoslynCopilotTest_v0.9.dll" "$addinPath\RoslynCopilotTest.dll" -Force

# Reiniciar Revit
```

### Opción 2: Desactivar Botones Premium Completamente

En `ScriptPanel.xaml.cs`, comentar:

```csharp
// Comentar la línea que llama DownloadPremiumButtonsAsync
// await DownloadPremiumButtonsAsync(premiumButtons);

// O hacer early return:
if (premiumButtons == null || premiumButtons.Count == 0)
    return; // No descargar nada
```

### Opción 3: Mantener Fallback Manual

Usuarios pueden seguir descargando botones manualmente:
- No automático, pero funcional
- Botón "💾 Download" sigue disponible
- Usuarios pueden importar JSON descargado

---

## 📞 SOPORTE Y ESCALATION

### Contactos

- **Equipo Técnico:** [correo del equipo]
- **Equipo Backend:** [correo backend]
- **Usuarios Premium:** [canal de soporte]

### Escalación

| Prioridad | Descripción | Tiempo de Respuesta |
|-----------|-------------|-------------------|
| P1 | 100% de usuarios no pueden descargar | < 1 hora |
| P2 | > 50% de usuarios tienen error | < 4 horas |
| P3 | < 50% de usuarios tienen error | < 1 día |
| P4 | Mejora de performance | < 1 semana |

---

## ✅ POST-DEPLOYMENT CHECKLIST

Después de 1 semana en producción:

- [ ] 0 errores críticos reportados
- [ ] > 95% tasa de éxito de descarga
- [ ] Tiempo promedio < 5 segundos
- [ ] > 80% adopción por usuarios premium
- [ ] Logs no muestran anomalías
- [ ] Usuarios reportan satisfacción
- [ ] Cache está dentro de límites (< 100 MB)

Si todo OK → Sistema está **STABLE EN PRODUCCIÓN** ✅

---

## 📝 DOCUMENTACIÓN FINAL PARA USUARIOS

Crear documento para distribución a usuarios:

```markdown
# BIMtegration Copilot - Botones Premium

## ¿Qué son?
Scripts personalizados disponibles automáticamente con suscripción premium.

## ¿Dónde están?
Pestaña **Advanced** → Sección **🔒 BOTONES PREMIUM**

## ¿Cómo usarlos?
1. Inicia sesión con cuenta premium
2. Los botones se descargan automáticamente
3. Haz click **▶️ Run** para ejecutar
4. O haz click **💾 Download** para guardar localmente

## ¿Qué pasa si veo error?
1. Haz click **🔄 Retry**
2. Si persiste, reinicia Revit
3. Si sigue, contacta soporte

## ¿Y después de expiración?
1. Descarga botones con **💾 Download** antes de expiración
2. Después, ve a Scripts → Import Selection
3. Carga el archivo JSON que descargaste
4. ¡Sigue disponible sin suscripción!

**¿Preguntas?** Contacta: [correo soporte]
```

---

**FIN DE INSTRUCCIONES DE DEPLOYMENT**

Para preguntas técnicas, revisar:
- RESUMEN_SISTEMA_BOTONES_PREMIUM.md (arquitectura)
- CHECKLIST_VALIDACION_BOTONES_PREMIUM.md (validación)
- Código fuente en `.cs` files (implementación)
