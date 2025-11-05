using System.Collections.Generic;

namespace RoslynCopilotTest
{
    public static class ScriptTemplates
    {
        public static List<ScriptTemplate> GetAvailableTemplates()
        {
            return new List<ScriptTemplate>
            {
                // MODELADO
                new ScriptTemplate
                {
                    Id = "create-wall-template",
                    Name = "Crear Muro",
                    Description = "Template para crear un muro básico",
                    Category = "Modelado",
                    Icon = "wall.png",
                    Code = @"// Crear un muro básico
using Transaction tx = new Transaction(doc, ""Crear Muro"");
tx.Start();

// Definir puntos de inicio y fin
XYZ startPoint = new XYZ(0, 0, 0);
XYZ endPoint = new XYZ(10, 0, 0);
Line wallLine = Line.CreateBound(startPoint, endPoint);

// Obtener nivel base
Level level = new FilteredElementCollector(doc)
    .OfClass(typeof(Level))
    .FirstOrDefault() as Level;

// Obtener tipo de muro por defecto
WallType wallType = new FilteredElementCollector(doc)
    .OfClass(typeof(WallType))
    .FirstOrDefault() as WallType;

// Crear el muro
Wall newWall = Wall.Create(doc, wallLine, wallType.Id, level.Id, 10.0, 0.0, false, false);

tx.Commit();
return $""✅ Muro creado con ID: {newWall.Id}"";",
                    Variables = new List<string> { "startPoint", "endPoint", "wallHeight", "wallType" }
                },

                new ScriptTemplate
                {
                    Id = "create-room-template",
                    Name = "Crear Habitación",
                    Description = "Template para crear una habitación",
                    Category = "Modelado",
                    Icon = "room.png",
                    Code = @"// Crear una habitación
using Transaction tx = new Transaction(doc, ""Crear Habitación"");
tx.Start();

// Obtener nivel para la habitación
Level level = new FilteredElementCollector(doc)
    .OfClass(typeof(Level))
    .FirstOrDefault() as Level;

// Crear punto UV en el espacio
UV roomPoint = new UV(5, 5); // Coordenadas del punto interior

// Crear la habitación
Room newRoom = doc.Create.NewRoom(level, roomPoint);

if (newRoom != null)
{
    // Establecer nombre de la habitación
    newRoom.Name = ""Nueva Habitación"";
    
    tx.Commit();
    return $""🏠 Habitación creada: {newRoom.Name} (ID: {newRoom.Id})"";
}
else
{
    tx.RollBack();
    return ""❌ No se pudo crear la habitación. Verifica que haya un espacio cerrado en esa ubicación."";
}",
                    Variables = new List<string> { "roomPoint", "roomName", "level" }
                },

                // ANÁLISIS
                new ScriptTemplate
                {
                    Id = "element-count-template",
                    Name = "Contar Elementos",
                    Description = "Template para contar elementos por categoría",
                    Category = "Análisis",
                    Icon = "count.png",
                    Code = @"// Contar elementos por categoría
BuiltInCategory categoryToCount = BuiltInCategory.OST_Walls; // Cambiar categoría aquí

int elementCount = new FilteredElementCollector(doc)
    .OfCategory(categoryToCount)
    .WhereElementIsNotElementType()
    .GetElementCount();

string categoryName = categoryToCount.ToString().Replace(""OST_"", """").Replace(""_"", "" "");
return $""📊 {categoryName}: {elementCount} elementos"";",
                    Variables = new List<string> { "categoryToCount", "categoryName" }
                },

                new ScriptTemplate
                {
                    Id = "parameter-analysis-template",
                    Name = "Análisis de Parámetros",
                    Description = "Template para analizar parámetros de elementos",
                    Category = "Análisis", 
                    Icon = "parameter.png",
                    Code = @"// Análisis de parámetros de elementos
var elements = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Walls)
    .WhereElementIsNotElementType()
    .ToList();

string parameterName = ""Length""; // Cambiar parámetro aquí
double totalValue = 0;
int validElements = 0;

foreach (Element element in elements)
{
    Parameter param = element.LookupParameter(parameterName);
    if (param != null && param.HasValue)
    {
        totalValue += param.AsDouble();
        validElements++;
    }
}

return $""📏 Análisis de '{parameterName}':\n"" +
       $""Elementos analizados: {validElements}\n"" +
       $""Valor total: {totalValue:F2}\n"" +
       $""Promedio: {(validElements > 0 ? totalValue / validElements : 0):F2}"";",
                    Variables = new List<string> { "parameterName", "categoryToAnalyze" }
                },

                // SELECCIÓN
                new ScriptTemplate
                {
                    Id = "filter-select-template",
                    Name = "Selección con Filtro",
                    Description = "Template para seleccionar elementos con filtros",
                    Category = "Selección",
                    Icon = "filter.png",
                    Code = @"// Seleccionar elementos con filtro personalizado
var elements = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Walls)
    .WhereElementIsNotElementType()
    .Where(e => {
        // Personalizar condición de filtro aquí
        Parameter lengthParam = e.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
        return lengthParam != null && lengthParam.AsDouble() > 10.0; // Muros > 10 pies
    })
    .ToList();

var elementIds = elements.Select(e => e.Id).ToList();
uidoc.Selection.SetElementIds(elementIds);

return $""🎯 Seleccionados {elementIds.Count} elementos que cumplen el criterio"";",
                    Variables = new List<string> { "filterCategory", "filterCondition", "minimumValue" }
                },

                // EXPORTACIÓN
                new ScriptTemplate
                {
                    Id = "export-data-template",
                    Name = "Exportar Datos",
                    Description = "Template para exportar datos a CSV",
                    Category = "Exportación",
                    Icon = "export.png",
                    Code = @"// Exportar datos de elementos a CSV
var elements = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Walls)
    .WhereElementIsNotElementType()
    .ToList();

string csvContent = ""ID,Nombre,Tipo,Longitud\n"";

foreach (Element element in elements)
{
    string id = element.Id.ToString();
    string name = element.Name ?? ""Sin nombre"";
    string type = element.GetTypeId() != ElementId.InvalidElementId ? 
                  doc.GetElement(element.GetTypeId()).Name : ""Sin tipo"";
    
    Parameter lengthParam = element.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
    string length = lengthParam?.AsDouble().ToString(""F2"") ?? ""0"";
    
    csvContent += $""{id},{name},{type},{length}\n"";
}

string filePath = $@""C:\temp\export_revit_{DateTime.Now:yyyyMMdd_HHmmss}.csv"";
System.IO.Directory.CreateDirectory(@""C:\temp"");
System.IO.File.WriteAllText(filePath, csvContent);

return $""📁 Datos exportados a: {filePath}\n"" +
       $""Elementos exportados: {elements.Count}"";",
                    Variables = new List<string> { "exportCategory", "outputPath", "csvColumns" }
                },

                // UTILIDADES
                new ScriptTemplate
                {
                    Id = "unit-conversion-template",
                    Name = "Conversión de Unidades",
                    Description = "Template para conversiones de unidades",
                    Category = "Utilidades",
                    Icon = "convert.png",
                    Code = @"// Conversión de unidades
double valueFeet = 10.0; // Valor en pies
double valueMeters = valueFeet * 0.3048; // Convertir a metros

double areaFeet2 = 100.0; // Área en pies cuadrados  
double areaMeters2 = areaFeet2 * 0.092903; // Convertir a metros cuadrados

double volumeFeet3 = 1000.0; // Volumen en pies cúbicos
double volumeMeters3 = volumeFeet3 * 0.028317; // Convertir a metros cúbicos

return $""🔄 CONVERSIONES DE UNIDADES:\n"" +
       $""Longitud: {valueFeet} ft = {valueMeters:F3} m\n"" +
       $""Área: {areaFeet2} ft² = {areaMeters2:F3} m²\n"" +
       $""Volumen: {volumeFeet3} ft³ = {volumeMeters3:F3} m³"";",
                    Variables = new List<string> { "inputValue", "fromUnit", "toUnit" }
                },

                new ScriptTemplate
                {
                    Id = "view-management-template",
                    Name = "Gestión de Vistas",
                    Description = "Template para crear y gestionar vistas",
                    Category = "Utilidades",
                    Icon = "view.png",
                    Code = @"// Gestión de vistas
using Transaction tx = new Transaction(doc, ""Gestionar Vistas"");
tx.Start();

// Obtener todas las vistas del proyecto
var views = new FilteredElementCollector(doc)
    .OfClass(typeof(View))
    .Cast<View>()
    .Where(v => !v.IsTemplate)
    .ToList();

// Obtener plantillas de vista
var viewTemplates = new FilteredElementCollector(doc)
    .OfClass(typeof(View))
    .Cast<View>()
    .Where(v => v.IsTemplate)
    .ToList();

string result = $""📋 GESTIÓN DE VISTAS:\n"" +
               $""Vistas totales: {views.Count}\n"" +
               $""Plantillas de vista: {viewTemplates.Count}\n\n"";

// Agrupar vistas por tipo
var viewsByType = views.GroupBy(v => v.ViewType);
foreach (var group in viewsByType)
{
    result += $""{group.Key}: {group.Count()} vistas\n"";
}

tx.Commit();
return result;",
                    Variables = new List<string> { "viewType", "templateName", "viewName" }
                }
            };
        }
    }

    public class ScriptTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Icon { get; set; }
        public string Code { get; set; }
        public List<string> Variables { get; set; } = new List<string>();
    }
}
