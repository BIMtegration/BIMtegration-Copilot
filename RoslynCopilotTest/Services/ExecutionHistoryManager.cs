using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RoslynCopilotTest.Models;

namespace RoslynCopilotTest.Services
{
    /// <summary>
    /// Servicio para gestionar el historial de ejecuciones de scripts
    /// </summary>
    public static class ExecutionHistoryManager
    {
        private static readonly string HistoryFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoslynCopilotTest",
            "execution-history.json"
        );

        private static readonly int MaxHistoryEntries = 1000; // Limitar a 1000 entradas

        /// <summary>
        /// Carga el historial de ejecuciones desde el archivo
        /// </summary>
        public static List<ExecutionHistoryEntry> LoadHistory()
        {
            try
            {
                if (!File.Exists(HistoryFilePath))
                {
                    return new List<ExecutionHistoryEntry>();
                }

                var json = File.ReadAllText(HistoryFilePath);
                var history = JsonConvert.DeserializeObject<List<ExecutionHistoryEntry>>(json) ?? new List<ExecutionHistoryEntry>();
                
                // Ordenar por fecha descendente (más recientes primero)
                return history.OrderByDescending(h => h.ExecutionTime).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading execution history: {ex.Message}");
                return new List<ExecutionHistoryEntry>();
            }
        }

        /// <summary>
        /// Guarda el historial de ejecuciones en el archivo
        /// </summary>
        private static void SaveHistory(List<ExecutionHistoryEntry> history)
        {
            try
            {
                // Asegurar que el directorio existe
                var directory = Path.GetDirectoryName(HistoryFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Limitar el número de entradas para evitar archivos muy grandes
                var limitedHistory = history.Take(MaxHistoryEntries).ToList();

                var json = JsonConvert.SerializeObject(limitedHistory, Formatting.Indented);
                File.WriteAllText(HistoryFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving execution history: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra una nueva ejecución en el historial
        /// </summary>
        public static void RecordExecution(string scriptId, string scriptName, string scriptCategory, 
                                         bool success, string result, string errorMessage, double durationMs)
        {
            try
            {
                var history = LoadHistory();
                
                var newEntry = new ExecutionHistoryEntry(scriptId, scriptName, scriptCategory, 
                                                        success, result, errorMessage, durationMs);
                
                history.Insert(0, newEntry); // Insertar al principio (más reciente)
                
                SaveHistory(history);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error recording execution: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene las ejecuciones más recientes
        /// </summary>
        public static List<ExecutionHistoryEntry> GetRecentExecutions(int count = 50)
        {
            var history = LoadHistory();
            return history.Take(count).ToList();
        }

        /// <summary>
        /// Filtra el historial por script específico
        /// </summary>
        public static List<ExecutionHistoryEntry> GetExecutionsForScript(string scriptId)
        {
            var history = LoadHistory();
            return history.Where(h => h.ScriptId == scriptId).ToList();
        }

        /// <summary>
        /// Filtra el historial por rango de fechas
        /// </summary>
        public static List<ExecutionHistoryEntry> GetExecutionsByDateRange(DateTime from, DateTime to)
        {
            var history = LoadHistory();
            return history.Where(h => h.ExecutionTime >= from && h.ExecutionTime <= to).ToList();
        }

        /// <summary>
        /// Filtra el historial por estado de éxito
        /// </summary>
        public static List<ExecutionHistoryEntry> GetExecutionsByStatus(bool successOnly)
        {
            var history = LoadHistory();
            return history.Where(h => h.Success == successOnly).ToList();
        }

        /// <summary>
        /// Obtiene estadísticas del historial
        /// </summary>
        public static ExecutionStatistics GetStatistics()
        {
            var history = LoadHistory();
            
            return new ExecutionStatistics
            {
                TotalExecutions = history.Count,
                SuccessfulExecutions = history.Count(h => h.Success),
                FailedExecutions = history.Count(h => !h.Success),
                MostExecutedScript = history.GroupBy(h => h.ScriptName)
                                          .OrderByDescending(g => g.Count())
                                          .FirstOrDefault()?.Key ?? "N/A",
                AverageExecutionTime = history.Any() ? history.Average(h => h.ExecutionDurationMs) : 0,
                LastExecutionTime = history.Any() ? history.Max(h => h.ExecutionTime) : DateTime.MinValue
            };
        }

        /// <summary>
        /// Limpia el historial eliminando entradas antiguas
        /// </summary>
        public static void CleanupOldEntries(int daysToKeep = 30)
        {
            try
            {
                var history = LoadHistory();
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                
                var filteredHistory = history.Where(h => h.ExecutionTime >= cutoffDate).ToList();
                
                if (filteredHistory.Count != history.Count)
                {
                    SaveHistory(filteredHistory);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up history: {ex.Message}");
            }
        }

        /// <summary>
        /// Limpia completamente el historial
        /// </summary>
        public static void ClearHistory()
        {
            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    File.Delete(HistoryFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing history: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Estadísticas del historial de ejecuciones
    /// </summary>
    public class ExecutionStatistics
    {
        public int TotalExecutions { get; set; }
        public int SuccessfulExecutions { get; set; }
        public int FailedExecutions { get; set; }
        public string MostExecutedScript { get; set; }
        public double AverageExecutionTime { get; set; }
        public DateTime LastExecutionTime { get; set; }

        public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions * 100 : 0;
        
        public string FormattedAverageTime
        {
            get
            {
                if (AverageExecutionTime < 1000)
                {
                    return $"{AverageExecutionTime:F0} ms";
                }
                else
                {
                    return $"{(AverageExecutionTime / 1000):F1} s";
                }
            }
        }
    }
}