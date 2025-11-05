using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace RoslynCopilotTest
{
    public class FamilyInsertData
    {
        public string fileUrl { get; set; }
        public Parameters parameters { get; set; }
    }

    public class Parameters
    {
        public int Ancho { get; set; }
        public int Alto { get; set; }
    }

    public static class RevitApiHandler
    {
        public static void InsertFamily(FamilyInsertData data, Document doc)
        {
            // Aquí iría la lógica para descargar el archivo, cargar la familia y colocarla
            // Ejemplo simplificado:
            // 1. Descargar archivo .rfa desde data.fileUrl
            // 2. doc.LoadFamily(rutaLocal)
            // 3. doc.PromptForFamilyInstancePlacement()
            // 4. Modificar parámetros usando instance.LookupParameter(...)
        }
    }
}
