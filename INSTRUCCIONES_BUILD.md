# Política: Solo Build (Sin Deploy Automático)

Importante
- No realizar deploy automático del add-in bajo ninguna circunstancia.
- Siempre que se termine una tarea, solo compilar y reportar estado.
- El deploy lo hará el usuario manualmente.

Cómo compilar (build) con .NET SDK
1) Asegúrate de tener .NET SDK instalado (se usa para invocar la build).
2) Ejecuta el build en modo Release para net48:

   dotnet build "c:\Users\geren\OneDrive\Escritorio\Proyecto Mars\RoslynCopilotTest\RoslynCopilotTest.csproj" /p:Configuration=Release

3) Resultado esperado:
   - Carpeta de salida: c:\Users\geren\OneDrive\Escritorio\Proyecto Mars\RoslynCopilotTest\bin\Release\net48
   - Ensamblado principal: CodeAssistantPro.dll
   - Dependencias: varias .dll (Microsoft.CodeAnalysis.*, Newtonsoft.Json.dll, etc.)

Verificación rápida post-build
- Confirmar exit code 0 del comando y presencia de CodeAssistantPro.dll en la carpeta de salida.

Qué NO hacer
- No realizar deploy automático (solo build y reporte de estado).
- No copiar archivos a %APPDATA% ni a C:\ProgramData desde estas instrucciones (el usuario hace el deploy manual).
- No modificar ni crear archivos .addin en el sistema (solo editar el del repo si se cambia la convención).
- No renombrar DLLs de salida.

Notas
- El proyecto está dirigido a .NET Framework 4.8 (TargetFramework net48) y usa Revit 2025 APIs como referencias.
- `dotnet build` es aceptable para compilar, pero el deploy debe ser manual.

---

# Deploy manual (convención de rutas relativa para el .addin)

Importante: Estas instrucciones son para el usuario que realiza el deploy manual. Desde estas tareas solo se compila.

Manifiesto soportado en el repo
- Archivo: `RoslynCopilotTest/BIMtegration Copilot.addin`
- Contenido clave:
   - `<Name>`: BIMtegration Copilot
   - `<Assembly>`: `BIMtegration Copilot\CodeAssistantPro.dll` (ruta relativa)
   - `<FullClassName>`: `RoslynCopilotTest.Application`

Estructura deseada en la carpeta Addins del usuario
- Carpeta destino típica: `%APPDATA%\Autodesk\Revit\Addins\2025`
- Estructura final:
   - `%APPDATA%\Autodesk\Revit\Addins\2025\BIMtegration Copilot.addin`
   - `%APPDATA%\Autodesk\Revit\Addins\2025\BIMtegration Copilot\CodeAssistantPro.dll`
   - `%APPDATA%\Autodesk\Revit\Addins\2025\BIMtegration Copilot\[todas las dependencias .dll]`

Pasos de deploy manual (usuario)
1) Compilar con el comando indicado arriba (Release/net48).
2) Copiar el archivo `BIMtegration Copilot.addin` a `%APPDATA%\Autodesk\Revit\Addins\2025`.
3) Crear (si no existe) la carpeta contigua `%APPDATA%\Autodesk\Revit\Addins\2025\BIMtegration Copilot`.
4) Copiar dentro de esa carpeta todo el contenido de `RoslynCopilotTest\bin\Release\net48` (incluidos `CodeAssistantPro.dll` y todas las dependencias .dll).
5) Cerrar y abrir Revit 2025 para que cargue el add-in.

Notas de diagnóstico
- Si aparece el mensaje "no se pudo inicializar... porque CodeAssistantPro.dll no existe", verificar que el `.addin` apunta a `BIMtegration Copilot\CodeAssistantPro.dll` y que esa ruta exista junto al `.addin`.
- Evitar tener múltiples `.addin` activos que apunten a DLLs distintos (limpiar `%APPDATA%` y `C:\ProgramData` si fuese necesario).
