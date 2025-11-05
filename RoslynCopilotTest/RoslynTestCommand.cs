using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace RoslynCopilotTest
{
    /// <summary>
    /// Comando para probar la ejecución de código C# usando Roslyn
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class RoslynTestCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Establecer la UIApplication estática para uso global
                Application.CurrentUIApplication = commandData.Application;

                // Ejecutar async real con mejor manejo
                var result = ExecuteAsyncScript(commandData).GetAwaiter().GetResult();
                
                // Mostrar el resultado al usuario
                TaskDialog.Show("Resultado de Async Script", 
                    $"✅ ¡Script async ejecutado correctamente!\n\n" +
                    $"Resultado: {result}\n\n" +
                    $"Ahora puedes usar await real en tus scripts dinámicos.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Capturar errores y mostrar detalles
                string errorDetails = ex.InnerException?.Message ?? ex.Message;
                
                TaskDialog.Show("Error en Async Script", 
                    $"❌ Error al ejecutar el código async:\n\n" +
                    $"{errorDetails}\n\n" +
                    $"Revisa la sintaxis y las referencias necesarias.");

                message = errorDetails;
                return Result.Failed;
            }
        }

        /// <summary>
        /// Ejecuta script async dinámico con soporte completo para await
        /// </summary>
        private async Task<object> ExecuteAsyncScript(ExternalCommandData commandData)
        {
            // SCRIPT DE PRUEBA SIMPLE - El script real va en el editor dinámico
            string codeToExecute = @"
                // Prueba de async/await real
                using System.Net.Http;
                
                using (var client = new HttpClient()) {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    // Test simple async
                    var response = await client.GetStringAsync(""https://httpbin.org/json"");
                    
                    return $""✅ Async funcionando! Respuesta: {response.Substring(0, 50)}..."";
                }
            ";



            // Configurar las referencias para script async avanzado
            var scriptOptions = ScriptOptions.Default
                .AddReferences(
                    typeof(Document).Assembly,           // RevitAPI.dll
                    typeof(UIDocument).Assembly,         // RevitAPIUI.dll
                    typeof(System.Linq.Enumerable).Assembly,  // System.Core.dll para LINQ
                    typeof(HttpClient).Assembly,         // System.Net.Http.dll
                    typeof(Newtonsoft.Json.JsonConvert).Assembly  // Newtonsoft.Json.dll
                )
                .WithImports(
                    "System", 
                    "System.Linq",
                    "System.Collections.Generic",
                    "System.Threading.Tasks",
                    "Autodesk.Revit.DB", 
                    "Autodesk.Revit.UI"
                );

            // ¡MAGIA ASYNC REAL! Compilar y ejecutar código con await verdadero
            object returnValue = await CSharpScript.EvaluateAsync(codeToExecute, scriptOptions);
            return returnValue;
        }
    }

    // Eliminada la clase ScriptGlobals: ahora los scripts usan solo 'doc' y 'uidoc' como variables globales
}