using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System;
using System.Windows.Forms;

namespace RoslynCopilotTest
{
    public class OpenWebViewCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // (Eliminado: lógica de catálogo online y CopilotBridge)
            return Result.Succeeded;
        }
    }
}
