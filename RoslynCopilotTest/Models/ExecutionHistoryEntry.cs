using System;

namespace RoslynCopilotTest.Models
{
    /// <summary>
    /// Representa una entrada en el historial de ejecuciones de scripts
    /// </summary>
    public class ExecutionHistoryEntry
    {
        public string Id { get; set; }
        public string ScriptId { get; set; }
        public string ScriptName { get; set; }
        public string ScriptCategory { get; set; }
        public DateTime ExecutionTime { get; set; }
        public bool Success { get; set; }
        public string Result { get; set; }
        public string ErrorMessage { get; set; }
        public double ExecutionDurationMs { get; set; }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public ExecutionHistoryEntry()
        {
            Id = Guid.NewGuid().ToString();
            ExecutionTime = DateTime.Now;
        }

        /// <summary>
        /// Constructor completo
        /// </summary>
        public ExecutionHistoryEntry(string scriptId, string scriptName, string scriptCategory, 
                                   bool success, string result, string errorMessage, double durationMs)
        {
            Id = Guid.NewGuid().ToString();
            ScriptId = scriptId;
            ScriptName = scriptName;
            ScriptCategory = scriptCategory;
            ExecutionTime = DateTime.Now;
            Success = success;
            Result = result;
            ErrorMessage = errorMessage;
            ExecutionDurationMs = durationMs;
        }

        /// <summary>
        /// Descripción del resultado para mostrar en la UI
        /// </summary>
        public string ResultDescription
        {
            get
            {
                if (Success)
                {
                    return string.IsNullOrEmpty(Result) ? "Ejecución exitosa" : Result;
                }
                else
                {
                    return string.IsNullOrEmpty(ErrorMessage) ? "Error en la ejecución" : ErrorMessage;
                }
            }
        }

        /// <summary>
        /// Icono que representa el estado de la ejecución
        /// </summary>
        public string StatusIcon
        {
            get
            {
                return Success ? "✅" : "❌";
            }
        }

        /// <summary>
        /// Duración formateada para mostrar en la UI
        /// </summary>
        public string FormattedDuration
        {
            get
            {
                if (ExecutionDurationMs < 1000)
                {
                    return $"{ExecutionDurationMs:F0} ms";
                }
                else
                {
                    return $"{(ExecutionDurationMs / 1000):F1} s";
                }
            }
        }

        public override string ToString()
        {
            return $"{ExecutionTime:yyyy-MM-dd HH:mm:ss} - {ScriptName} ({StatusIcon})";
        }
    }
}