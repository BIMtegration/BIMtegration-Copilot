using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace RoslynCopilotTest.Services
{
    public class GitHubAIService
    {
        private readonly HttpClient _httpClient;
        private readonly SecureTokenStorage _tokenStorage;
        private const string GITHUB_MODELS_API_BASE = "https://models.inference.ai.azure.com";

        public GitHubAIService()
        {
            _httpClient = new HttpClient();
            _tokenStorage = new SecureTokenStorage();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "RoslynCopilotTest/1.0");
        }

        /// <summary>
        /// Genera código usando GitHub AI específico para Revit (con fallback inteligente)
        /// </summary>
        public async Task<string> GenerateRevitCodeAsync(string prompt, string context = "")
        {
            try
            {
                // Detectar si es una conversación normal o solicitud de código
                if (IsConversationalMessage(prompt))
                {
                    return GenerateConversationalResponse(prompt);
                }

                // Primero intentar con AI local inteligente
                var intelligentResponse = GenerateIntelligentRevitCode(prompt, context);
                if (!string.IsNullOrEmpty(intelligentResponse))
                {
                    return intelligentResponse;
                }

                var tokenData = SecureTokenStorage.LoadToken();
                if (tokenData?.AccessToken == null)
                {
                    // Si no hay token, usar generación local mejorada
                    return GenerateBasicRevitCode(prompt, context);
                }

                var token = tokenData.AccessToken;
                var revitPrompt = BuildRevitSpecificPrompt(prompt, context);

                // Intentar con diferentes endpoints de GitHub
                var endpoints = new[]
                {
                    "https://api.githubcopilot.com/chat/completions",
                    "https://copilot-proxy.githubusercontent.com/v1/chat/completions",
                    "https://api.github.com/models/completions"
                };

                Exception lastException = null;

                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        var result = await TryGenerateWithEndpoint(endpoint, token, revitPrompt);
                        if (result != null)
                            return result;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        continue;
                    }
                }

                // Si todos los endpoints fallan, usar generación local básica
                return GenerateBasicRevitCode(prompt, context);
            }
            catch (Exception)
            {
                // Fallback a generación básica
                return GenerateBasicRevitCode(prompt, context);
            }
        }

        private async Task<string> TryGenerateWithEndpoint(string endpoint, string token, string prompt)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "RoslynCopilotTest/1.0");

            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = GetRevitSystemPrompt() },
                    new { role = "user", content = prompt }
                },
                model = "gpt-4",
                temperature = 0.3,
                max_tokens = 1500,
                stream = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var aiResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);
                return ExtractCodeFromResponse(aiResponse);
            }

            return null;
        }

        /// <summary>
        /// Generación inteligente local que entiende mejor las solicitudes en español
        /// </summary>
        private string GenerateIntelligentRevitCode(string prompt, string context)
        {
            var lowerPrompt = prompt.ToLower();

            // Análisis más sofisticado de patrones
            if (lowerPrompt.Contains("muro") && (lowerPrompt.Contains("doble") || lowerPrompt.Contains("dos") || lowerPrompt.Contains("nivel")))
            {
                return GenerateDoubleWallTemplate();
            }
            
            if (lowerPrompt.Contains("crear") && lowerPrompt.Contains("muro"))
            {
                return GenerateCreateWallTemplate();
            }

            if (lowerPrompt.Contains("área") || lowerPrompt.Contains("area"))
            {
                return GenerateAreaCalculationTemplate();
            }

            if (lowerPrompt.Contains("puerta") || lowerPrompt.Contains("door"))
            {
                return GenerateDoorTemplate();
            }

            if (lowerPrompt.Contains("ventana") || lowerPrompt.Contains("window"))
            {
                return GenerateWindowTemplate();
            }

            if (lowerPrompt.Contains("seleccionar") || lowerPrompt.Contains("filtrar"))
            {
                return GenerateSelectionTemplate();
            }

            return null; // Si no reconoce el patrón, retorna null para usar fallback
        }

        /// <summary>
        /// Genera código básico de Revit como fallback
        /// </summary>
        private string GenerateBasicRevitCode(string prompt, string context)
        {
            var templates = new Dictionary<string, string>
            {
                ["elementos"] = GenerateElementCollectionTemplate(),
                ["muros"] = GenerateWallTemplate(),
                ["crear muro"] = GenerateCreateWallTemplate(),
                ["muro doble"] = GenerateDoubleWallTemplate(),
                ["doble nivel"] = GenerateDoubleWallTemplate(),
                ["filtrar"] = GenerateFilterTemplate(),
                ["contar"] = GenerateCountTemplate(),
                ["propiedades"] = GeneratePropertiesTemplate(),
                ["transacción"] = GenerateTransactionTemplate()
            };

            var lowerPrompt = prompt.ToLower();
            
            foreach (var template in templates)
            {
                if (lowerPrompt.Contains(template.Key))
                {
                    return template.Value;
                }
            }

            return GenerateGenericTemplate(prompt);
        }

        /// <summary>
        /// Detecta si el mensaje es conversacional (saludo, pregunta general) vs solicitud de código
        /// </summary>
        private bool IsConversationalMessage(string prompt)
        {
            var lowerPrompt = prompt.ToLower().Trim();
            
            // Saludos y mensajes sociales
            var conversationalPatterns = new[]
            {
                "hola", "hello", "hi", "buenas", "buenos días", "buenas tardes", "buenas noches",
                "¿cómo estás?", "como estas", "how are you", "¿qué tal?", "que tal",
                "gracias", "thank you", "thanks", "de nada", "you're welcome",
                "adiós", "bye", "hasta luego", "nos vemos", "chau", "goodbye",
                "¿quién eres?", "quien eres", "who are you", "¿qué puedes hacer?", "que puedes hacer",
                "ayuda", "help", "¿cómo funciona?", "como funciona", "how does this work",
                "¿qué día", "que dia", "fecha", "today", "hoy", "¿qué hora", "que hora",
                "¿cuándo", "cuando", "when", "tiempo", "time", "date"
            };

            // Si el mensaje es muy corto y contiene patrones conversacionales
            if (lowerPrompt.Length < 50)
            {
                foreach (var pattern in conversationalPatterns)
                {
                    if (lowerPrompt.Contains(pattern))
                        return true;
                }
            }

            // Palabras clave que indican solicitud de código
            var codeKeywords = new[]
            {
                "crear", "create", "generar", "generate", "hacer", "make",
                "muro", "wall", "puerta", "door", "ventana", "window",
                "área", "area", "elemento", "element", "filtrar", "filter",
                "seleccionar", "select", "contar", "count", "modificar", "modify",
                "código", "code", "script", "función", "function"
            };

            foreach (var keyword in codeKeywords)
            {
                if (lowerPrompt.Contains(keyword))
                    return false; // Es solicitud de código
            }

            return lowerPrompt.Length < 80; // Mensajes cortos sin palabras clave son conversacionales
        }

        /// <summary>
        /// Genera respuestas conversacionales amigables
        /// </summary>
        private string GenerateConversationalResponse(string prompt)
        {
            var lowerPrompt = prompt.ToLower().Trim();

            // Saludos
            if (lowerPrompt.Contains("hola") || lowerPrompt.Contains("hello") || lowerPrompt.Contains("hi"))
            {
                return "¡Hola! 👋 Soy tu asistente de Revit. Puedo ayudarte a generar código para Revit API. \n\n" +
                       "Ejemplos de lo que puedo hacer:\n" +
                       "• \"Crear un muro de doble nivel\"\n" +
                       "• \"Seleccionar todas las puertas\"\n" +
                       "• \"Calcular áreas de habitaciones\"\n" +
                       "• \"Cambiar material de ventanas\"\n\n" +
                       "¿En qué te puedo ayudar hoy?";
            }

            if (lowerPrompt.Contains("¿cómo estás?") || lowerPrompt.Contains("como estas") || lowerPrompt.Contains("how are you"))
            {
                return "¡Muy bien, gracias por preguntar! 😊 Estoy aquí listo para ayudarte con cualquier tarea de Revit API. " +
                       "¿Tienes algún proyecto en mente?";
            }

            if (lowerPrompt.Contains("¿qué puedes hacer?") || lowerPrompt.Contains("que puedes hacer") || 
                lowerPrompt.Contains("¿quién eres?") || lowerPrompt.Contains("quien eres"))
            {
                return "Soy tu asistente especializado en Revit API! 🏗️ Puedo generar código C# para:\n\n" +
                       "📐 **Modelado**: Crear muros, puertas, ventanas\n" +
                       "📊 **Análisis**: Calcular áreas, contar elementos\n" +
                       "🔍 **Selección**: Filtrar elementos por categorías\n" +
                       "⚙️ **Automatización**: Modificar propiedades, aplicar materiales\n\n" +
                       "Solo dime qué necesitas y yo genero el código para ti.";
            }

            if (lowerPrompt.Contains("gracias") || lowerPrompt.Contains("thank you"))
            {
                return "¡De nada! 😊 Me alegra poder ayudarte. Si tienes más preguntas sobre Revit API, no dudes en preguntarme.";
            }

            if (lowerPrompt.Contains("ayuda") || lowerPrompt.Contains("help"))
            {
                return "¡Por supuesto! Te puedo ayudar con código de Revit API. \n\n" +
                       "**Formato sugerido**: Describe lo que quieres hacer de forma natural, por ejemplo:\n" +
                       "• \"Necesito crear 5 muros en forma de rectángulo\"\n" +
                       "• \"Seleccionar todas las puertas del nivel 1\"\n" +
                       "• \"Cambiar material de ventanas a vidrio azul\"\n\n" +
                       "¿Qué proyecto tienes en mente?";
            }

            if (lowerPrompt.Contains("día") || lowerPrompt.Contains("fecha") || lowerPrompt.Contains("hoy") || 
                lowerPrompt.Contains("today") || lowerPrompt.Contains("date"))
            {
                var today = DateTime.Now;
                return $"Hoy es {today.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"))} 📅\n\n" +
                       "¿Hay algún proyecto de Revit en el que te pueda ayudar hoy?";
            }

            if (lowerPrompt.Contains("hora") || lowerPrompt.Contains("time"))
            {
                var now = DateTime.Now;
                return $"Son las {now.ToString("HH:mm")} ⏰\n\n" +
                       "¿En qué puedo ayudarte con Revit API?";
            }

            // Respuesta genérica para otros mensajes conversacionales
            return "¡Hola! Estoy aquí para ayudarte con Revit API. 🔧\n\n" +
                   "Puedes pedirme que genere código para crear elementos, modificar propiedades, " +
                   "hacer cálculos o automatizar tareas en Revit.\n\n" +
                   "¿Hay algo específico en lo que te pueda ayudar?";
        }

        #region Template Generators

        private string GenerateElementCollectionTemplate()
        {
            return @"// Obtener todos los elementos de una categoría
var elements = new FilteredElementCollector(doc)
    .OfClass(typeof(Element))
    .WhereElementIsNotElementType()
    .ToElements();

TaskDialog.Show(""Resultado"", $""Se encontraron {elements.Count} elementos"");";
        }

        private string GenerateWallTemplate()
        {
            return @"// Obtener todos los muros del documento
var walls = new FilteredElementCollector(doc)
    .OfClass(typeof(Wall))
    .WhereElementIsNotElementType()
    .Cast<Wall>()
    .ToList();

TaskDialog.Show(""Muros"", $""Se encontraron {walls.Count} muros en el documento"");";
        }

        private string GenerateCreateWallTemplate()
        {
            return @"// Crear un muro básico
using (Transaction trans = new Transaction(doc, ""Crear Muro""))
{
    trans.Start();
    
    try
    {
        // Definir puntos de inicio y fin del muro
        XYZ startPoint = new XYZ(0, 0, 0);
        XYZ endPoint = new XYZ(10, 0, 0);  // Muro de 10 pies de largo
        Line wallLine = Line.CreateBound(startPoint, endPoint);
        
        // Obtener tipo de muro básico
        var wallType = new FilteredElementCollector(doc)
            .OfClass(typeof(WallType))
            .FirstOrDefault() as WallType;
            
        if (wallType != null)
        {
            // Crear el muro
            Wall newWall = Wall.Create(doc, wallLine, wallType.Id, Level.Create(doc, 0).Id, 10.0, 0, false, false);
            
            trans.Commit();
            TaskDialog.Show(""Éxito"", ""Muro creado exitosamente"");
        }
        else
        {
            trans.RollBack();
            TaskDialog.Show(""Error"", ""No se encontró tipo de muro disponible"");
        }
    }
    catch (Exception ex)
    {
        trans.RollBack();
        TaskDialog.Show(""Error"", $""Error al crear muro: {ex.Message}"");
    }
}";
        }

        private string GenerateDoubleWallTemplate()
        {
            return @"// Crear muro de doble nivel/altura
using (Transaction trans = new Transaction(doc, ""Crear Muro Doble Nivel""))
{
    trans.Start();
    
    try
    {
        // Obtener el nivel activo
        Level activeLevel = doc.ActiveView.GenLevel ?? 
                           new FilteredElementCollector(doc)
                               .OfClass(typeof(Level))
                               .Cast<Level>()
                               .OrderBy(l => l.Elevation)
                               .FirstOrDefault();
        
        if (activeLevel == null)
        {
            TaskDialog.Show(""Error"", ""No se encontró nivel activo"");
            return;
        }
        
        // Obtener tipo de muro
        WallType wallType = new FilteredElementCollector(doc)
            .OfClass(typeof(WallType))
            .Cast<WallType>()
            .FirstOrDefault();
            
        if (wallType == null)
        {
            TaskDialog.Show(""Error"", ""No se encontró tipo de muro"");
            return;
        }
        
        // Definir línea base del muro
        XYZ startPoint = new XYZ(0, 0, 0);
        XYZ endPoint = new XYZ(20, 0, 0);  // Muro de 20 pies
        Line wallLine = Line.CreateBound(startPoint, endPoint);
        
        // Crear primer muro (nivel inferior) - altura normal
        Wall wall1 = Wall.Create(doc, wallLine, wallType.Id, activeLevel.Id, 12.0, 0, false, false);
        
        // Crear segundo muro (nivel superior) - más alto
        // Desplazar hacia arriba para efecto de doble nivel
        XYZ offset = new XYZ(0, 2, 0);  // Desplazar 2 pies hacia el norte
        Line wallLine2 = Line.CreateBound(startPoint + offset, endPoint + offset);
        Wall wall2 = Wall.Create(doc, wallLine2, wallType.Id, activeLevel.Id, 16.0, 0, false, false);
        
        trans.Commit();
        TaskDialog.Show(""Éxito"", ""Muro de doble nivel creado exitosamente\\n\\nSe crearon 2 muros:\\n- Muro principal: 12 pies de altura\\n- Muro secundario: 16 pies de altura, desplazado 2 pies"");
    }
    catch (Exception ex)
    {
        trans.RollBack();
        TaskDialog.Show(""Error"", $""Error al crear muro doble nivel: {ex.Message}"");
    }
}";
        }

        private string GenerateFilterTemplate()
        {
            return @"// Filtrar elementos por categoría específica
var filter = new ElementCategoryFilter(BuiltInCategory.OST_Walls);
var filteredElements = new FilteredElementCollector(doc)
    .WherePasses(filter)
    .WhereElementIsNotElementType()
    .ToElements();

TaskDialog.Show(""Filtrado"", $""Se encontraron {filteredElements.Count} elementos filtrados"");";
        }

        private string GenerateCountTemplate()
        {
            return @"// Contar elementos por categoría
var wallCount = new FilteredElementCollector(doc)
    .OfClass(typeof(Wall))
    .WhereElementIsNotElementType()
    .GetElementCount();

var doorCount = new FilteredElementCollector(doc)
    .OfClass(typeof(FamilyInstance))
    .OfCategory(BuiltInCategory.OST_Doors)
    .WhereElementIsNotElementType()
    .GetElementCount();

TaskDialog.Show(""Conteo"", $""Muros: {wallCount}, Puertas: {doorCount}"");";
        }

        private string GeneratePropertiesTemplate()
        {
            return @"// Obtener propiedades de elementos
var walls = new FilteredElementCollector(doc)
    .OfClass(typeof(Wall))
    .WhereElementIsNotElementType()
    .Cast<Wall>()
    .Take(5); // Solo los primeros 5

var info = string.Empty;
foreach (var wall in walls)
{
    var wallType = wall.WallType;
    var height = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsValueString() ?? ""N/A"";
    info += $""Muro ID: {wall.Id}, Tipo: {wallType.Name}, Altura: {height}\n"";
}

TaskDialog.Show(""Propiedades"", info);";
        }

        private string GenerateTransactionTemplate()
        {
            return @"// Realizar cambios en el documento con transacción
using (Transaction trans = new Transaction(doc, ""Modificar elementos""))
{
    trans.Start();
    
    try
    {
        // Aquí va tu código de modificación
        // Ejemplo: cambiar un parámetro
        var walls = new FilteredElementCollector(doc)
            .OfClass(typeof(Wall))
            .WhereElementIsNotElementType()
            .Cast<Wall>()
            .Take(1);
            
        foreach (var wall in walls)
        {
            // Ejemplo de modificación
            var param = wall.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            if (param != null && !param.IsReadOnly)
            {
                param.Set(""Modificado por script"");
            }
        }
        
        trans.Commit();
        TaskDialog.Show(""Éxito"", ""Transacción completada correctamente"");
    }
    catch (Exception ex)
    {
        trans.RollBack();
        TaskDialog.Show(""Error"", $""Error en la transacción: {ex.Message}"");
    }
}";
        }

        private string GenerateGenericTemplate(string prompt)
        {
            return $@"// Código generado para: {prompt}
try
{{
    // Obtener elementos del documento
    var elements = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .ToElements();
    
    // Procesar elementos aquí
    var count = elements.Count;
    
    // Mostrar resultado
    TaskDialog.Show(""Resultado"", $""Procesados {{count}} elementos para: {prompt}"");
}}
catch (Exception ex)
{{
    TaskDialog.Show(""Error"", $""Error al procesar: {{ex.Message}}"");
}}";
        }

        #endregion

        /// <summary>
        /// Construye un prompt específico para Revit
        /// </summary>
        private string BuildRevitSpecificPrompt(string userPrompt, string context)
        {
            var promptBuilder = new StringBuilder();
            
            promptBuilder.AppendLine("Genera código C# para un script de Revit 2025 que:");
            promptBuilder.AppendLine($"- {userPrompt}");
            
            if (!string.IsNullOrEmpty(context))
            {
                promptBuilder.AppendLine($"- Contexto adicional: {context}");
            }

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Requisitos técnicos:");
            promptBuilder.AppendLine("- Usar Autodesk.Revit.DB y Autodesk.Revit.UI");
            promptBuilder.AppendLine("- Compatible con .NET Framework 4.8");
            promptBuilder.AppendLine("- Incluir manejo de errores con try-catch");
            promptBuilder.AppendLine("- Usar TaskDialog para mostrar resultados");
            promptBuilder.AppendLine("- El código debe ser ejecutable directamente en el script panel");
            promptBuilder.AppendLine("- Incluir comentarios explicativos en español");

            return promptBuilder.ToString();
        }

        /// <summary>
        /// Prompt del sistema para GitHub AI especializado en Revit
        /// </summary>
        private string GetRevitSystemPrompt()
        {
            return @"Eres un experto desarrollador de Revit API con profundo conocimiento en:
- Autodesk Revit 2025 API
- C# .NET Framework 4.8
- Microsoft Roslyn Scripting
- Mejores prácticas de desarrollo para Revit

Genera código limpio, eficiente y bien documentado.
Siempre incluye manejo de errores y validaciones.
Los comentarios deben estar en español.
El código debe ser compatible con el entorno de scripting de Roslyn.

Reglas para URL/HTTP requests cuando el usuario lo solicite:
- Usar System.Net.Http.HttpClient con async/await. No usar .Result ni .Wait().
- Establecer un timeout razonable (p. ej. 30 s) y headers adecuados (User-Agent, Accept: application/json).
    - Para POST JSON usar: new StringContent(json, Encoding.UTF8, ""application/json"").
- Validar response.IsSuccessStatusCode. En error, mostrar código de estado y un extracto del contenido.
- Capturar HttpRequestException y TaskCanceledException (timeout) y reportar mensajes claros via TaskDialog.
- Para parsear JSON puedes usar Newtonsoft.Json (JObject/JArray) o System.Text.Json.
- No incluir secretos/keys en claro en el código generado.

Variables disponibles en el contexto:
- doc: Document (documento activo)
- uidoc: UIDocument (documento de interfaz)
- app: Application (aplicación Revit)
- uiapp: UIApplication (aplicación de interfaz)

Responde SOLO con el código C#, sin explicaciones adicionales.";
        }

        /// <summary>
        /// Extrae el código C# de la respuesta de la AI
        /// </summary>
        private string ExtractCodeFromResponse(JsonElement response)
        {
            try
            {
                var choices = response.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    var message = firstChoice.GetProperty("message");
                    var content = message.GetProperty("content").GetString();
                    
                    // Limpiar el contenido para extraer solo el código C#
                    return CleanGeneratedCode(content);
                }
            }
            catch (Exception)
            {
                // Si hay error al parsear, intentar extraer código básico
            }

            return "// Error: No se pudo generar código";
        }

        /// <summary>
        /// Limpia y formatea el código generado
        /// </summary>
        private string CleanGeneratedCode(string rawContent)
        {
            if (string.IsNullOrEmpty(rawContent))
                return "// No se generó código";

            var content = rawContent.Trim();

            // Remover marcadores de código si existen
            if (content.StartsWith("```csharp"))
                content = content.Substring(9);
            else if (content.StartsWith("```"))
                content = content.Substring(3);

            if (content.EndsWith("```"))
                content = content.Substring(0, content.Length - 3);

            // Limpiar espacios y líneas vacías al inicio/final
            content = content.Trim();

            // Si no tiene el formato esperado, agregarlo
            if (!content.Contains("try") && !content.Contains("catch"))
            {
                content = WrapInTryCatch(content);
            }

            return content;
        }

        /// <summary>
        /// Envuelve el código en un bloque try-catch si no lo tiene
        /// </summary>
        private string WrapInTryCatch(string code)
        {
            return $@"try
{{
{IndentCode(code, 1)}
}}
catch (Exception ex)
{{
    TaskDialog.Show(""Error"", $""Error al ejecutar script: {{ex.Message}}"");
}}";
        }

        /// <summary>
        /// Indenta el código
        /// </summary>
        private string IndentCode(string code, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            var lines = code.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    lines[i] = indent + lines[i].Trim();
            }
            
            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Verifica si GitHub AI está disponible
        /// </summary>
        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var tokenData = SecureTokenStorage.LoadToken();
                return tokenData?.AccessToken != null;
            }
            catch
            {
                return false;
            }
        }

        #region Additional Template Generators

        private string GenerateAreaCalculationTemplate()
        {
            return @"// Calcular áreas de elementos seleccionados
using (Transaction trans = new Transaction(doc, ""Calcular Áreas""))
{
    trans.Start();
    
    var areas = new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_Areas)
        .WhereElementIsNotElementType()
        .Cast<Area>()
        .ToList();
    
    double totalArea = 0;
    foreach (var area in areas)
    {
        Parameter areaParam = area.get_Parameter(BuiltInParameter.ROOM_AREA);
        if (areaParam != null)
        {
            totalArea += areaParam.AsDouble();
        }
    }
    
    trans.Commit();
    TaskDialog.Show(""Área Total"", $""Área total: {totalArea:F2} pies cuadrados"");
}";
        }

        private string GenerateDoorTemplate()
        {
            return @"// Crear puerta en muro seleccionado
using (Transaction trans = new Transaction(doc, ""Crear Puerta""))
{
    trans.Start();
    
    // Obtener el primer muro
    var wall = new FilteredElementCollector(doc)
        .OfClass(typeof(Wall))
        .FirstOrDefault() as Wall;
    
    if (wall != null)
    {
        // Obtener tipo de puerta por defecto
        var doorType = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_Doors)
            .FirstOrDefault() as FamilySymbol;
        
        if (doorType != null)
        {
            if (!doorType.IsActive)
                doorType.Activate();
            
            // Crear puerta en el centro del muro
            LocationCurve locationCurve = wall.Location as LocationCurve;
            XYZ point1 = locationCurve.Curve.GetEndPoint(0);
            XYZ point2 = locationCurve.Curve.GetEndPoint(1);
            XYZ midPoint = (point1 + point2) / 2;
            
            doc.Create.NewFamilyInstance(midPoint, doorType, wall, null);
        }
    }
    
    trans.Commit();
}";
        }

        private string GenerateWindowTemplate()
        {
            return @"// Crear ventana en muro seleccionado
using (Transaction trans = new Transaction(doc, ""Crear Ventana""))
{
    trans.Start();
    
    // Obtener el primer muro
    var wall = new FilteredElementCollector(doc)
        .OfClass(typeof(Wall))
        .FirstOrDefault() as Wall;
    
    if (wall != null)
    {
        // Obtener tipo de ventana por defecto
        var windowType = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .OfCategory(BuiltInCategory.OST_Windows)
            .FirstOrDefault() as FamilySymbol;
        
        if (windowType != null)
        {
            if (!windowType.IsActive)
                windowType.Activate();
            
            // Crear ventana en el centro del muro
            LocationCurve locationCurve = wall.Location as LocationCurve;
            XYZ point1 = locationCurve.Curve.GetEndPoint(0);
            XYZ point2 = locationCurve.Curve.GetEndPoint(1);
            XYZ midPoint = (point1 + point2) / 2;
            
            doc.Create.NewFamilyInstance(midPoint, windowType, wall, null);
        }
    }
    
    trans.Commit();
}";
        }

        private string GenerateSelectionTemplate()
        {
            return @"// Seleccionar elementos por criterio
var selectedElements = new FilteredElementCollector(doc)
    .WhereElementIsNotElementType()
    .Where(e => e.Category != null && e.Category.Name.Contains(""Wall""))
    .ToList();

TaskDialog.Show(""Selección"", $""Se encontraron {selectedElements.Count} elementos"");

// Para seleccionar visualmente en la interfaz
var elementIds = selectedElements.Select(e => e.Id).ToList();
uidoc.Selection.SetElementIds(elementIds);";
        }

        #endregion

        /// <summary>
        /// Obtiene sugerencias de código en tiempo real
        /// </summary>
        public async Task<List<string>> GetCodeSuggestionsAsync(string partialCode, int cursorPosition)
        {
            try
            {
                var suggestions = new List<string>();
                
                // Implementar lógica de autocompletado inteligente
                // Por ahora, sugerencias básicas de Revit API
                suggestions.AddRange(GetRevitAPISuggestions(partialCode));
                
                return suggestions;
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Sugerencias básicas de Revit API
        /// </summary>
        private List<string> GetRevitAPISuggestions(string partialCode)
        {
            var suggestions = new List<string>();
            
            if (partialCode.Contains("FilteredElementCollector"))
            {
                suggestions.Add(".OfClass(typeof(Wall))");
                suggestions.Add(".OfCategory(BuiltInCategory.OST_Walls)");
                suggestions.Add(".WhereElementIsNotElementType()");
                suggestions.Add(".ToElements()");
            }
            
            if (partialCode.Contains("Transaction"))
            {
                suggestions.Add("using (Transaction trans = new Transaction(doc, \"Nombre transacción\"))");
                suggestions.Add("trans.Start();");
                suggestions.Add("trans.Commit();");
            }
            
            return suggestions;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}