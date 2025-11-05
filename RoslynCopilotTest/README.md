# RoslynCopilotTest - Pruebas de Código Dinámico para Revit

Este proyecto es una prueba de concepto para ejecutar código C# dinámicamente dentro de Revit usando Roslyn (.NET Compiler Platform).

## Objetivo

Validar que es posible:
1. Compilar código C# en tiempo real dentro de un add-in de Revit
2. Ejecutar ese código con acceso completo a la API de Revit
3. Obtener resultados y manejar errores de forma robusta

## Estructura del Proyecto

```
RoslynCopilotTest/
├── RoslynCopilotTest.csproj    # Configuración del proyecto y referencias NuGet
├── RoslynCopilotTest.addin     # Manifest para que Revit reconozca el add-in
├── Application.cs              # Clase principal que crea la UI en Revit
├── RoslynTestCommand.cs        # Comando que ejecuta las pruebas de Roslyn
└── README.md                   # Este archivo
```

## Cómo Compilar

1. Abre el proyecto en Visual Studio (no VS Code)
2. Asegúrate de que las rutas a RevitAPI.dll en el .csproj sean correctas para tu versión
3. Compila el proyecto (Build Solution)

## Cómo Probar

1. Copia el archivo .addin a la carpeta de add-ins de Revit:
   `%APPDATA%\Autodesk\Revit\Addins\2024\` (ajusta la versión)
2. Copia el .dll compilado a la misma carpeta
3. Abre Revit
4. Busca la pestaña "Roslyn Test" en la ribbon
5. Haz clic en "Ejecutar Código Simple"

## Lo Que Hace la Prueba

El comando ejecuta código C# que:
- Cuenta los muros en el modelo activo
- Cuenta las puertas en el modelo activo  
- Genera un reporte con esta información
- Demuestra que Roslyn puede compilar y ejecutar código con acceso a la API de Revit

## Próximos Pasos

Si esta prueba funciona, se puede:
1. Crear una interfaz para ingresar código manualmente
2. Integrar con un LLM para generar código automáticamente
3. Añadir más contexto de Revit (selección, vistas, etc.)
4. Implementar funciones de seguridad y validación

## Notas Técnicas

- Usa .NET Framework 4.8 (requerido por Revit)
- Roslyn se ejecuta de forma asíncrona
- El código generado tiene acceso a variables globales (RevitDoc, UIDoc)
- Los errores de compilación se capturan y muestran al usuario