// Archivo renombrado: antes KatalogExternalEventHandler.cs
// Clase principal para ExternalEvent seguro
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System;

namespace RoslynCopilotTest
{
    public class GenericExternalEventHandler : IExternalEventHandler
    {
        public Action<UIApplication> ActionToExecute { get; set; }

        public void Execute(UIApplication app)
        {
            var doc = app?.ActiveUIDocument?.Document;
            if (doc == null)
            {
                ActionToExecute?.Invoke(app);
                return;
            }

            using (var tx = new Transaction(doc, "Script Copilot"))
            {
                tx.Start();
                ActionToExecute?.Invoke(app);
                tx.Commit();
            }
        }

        public string GetName() => "GenericExternalEventHandler";
    }
}
