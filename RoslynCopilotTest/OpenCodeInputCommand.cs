using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RoslynCopilotTest
{
    /// <summary>
    /// Comando para abrir la ventana de input manual de código
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class OpenCodeInputCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Establecer la UIApplication estática para uso global
                Application.CurrentUIApplication = commandData.Application;

                // Crear y mostrar la ventana de input de código
                var window = new CodeInputWindow(commandData);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error al abrir la ventana: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}