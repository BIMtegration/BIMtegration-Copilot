using System;
using System.Collections.Generic;

namespace RoslynCopilotTest.Models
{
    /// <summary>
    /// Represents a company data variable loaded from the Data Server API
    /// </summary>
    public class CompanyDataVariable
    {
        /// <summary>
        /// Name of the variable (e.g., "Classifications", "Zones", "Materials")
        /// </summary>
        public string VariableName { get; set; }

        /// <summary>
        /// Google Sheets ID containing the data
        /// </summary>
        public string SheetId { get; set; }

        /// <summary>
        /// Sheet name within the spreadsheet
        /// </summary>
        public string SheetName { get; set; }

        /// <summary>
        /// Current status: "Loaded", "Loading", or "Error"
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Error message if status is "Error"
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Size of the data in kilobytes
        /// </summary>
        public int DataSize { get; set; }

        /// <summary>
        /// When this variable was loaded
        /// </summary>
        public DateTime LoadedAt { get; set; }

        /// <summary>
        /// The actual data loaded from the server
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Create a new company data variable
        /// </summary>
        public CompanyDataVariable()
        {
            VariableName = "";
            SheetId = "";
            SheetName = "";
            Status = "Loading";
            ErrorMessage = "";
            DataSize = 0;
            LoadedAt = DateTime.UtcNow;
            Data = null;
        }

        /// <summary>
        /// Create a company data variable with initial values
        /// </summary>
        public CompanyDataVariable(string variableName, string sheetId, string sheetName)
        {
            VariableName = variableName;
            SheetId = sheetId;
            SheetName = sheetName;
            Status = "Loading";
            ErrorMessage = "";
            DataSize = 0;
            LoadedAt = DateTime.UtcNow;
            Data = null;
        }

        /// <summary>
        /// Mark as successfully loaded
        /// </summary>
        public void MarkAsLoaded(object data, int sizeInKb)
        {
            Status = "Loaded";
            Data = data;
            DataSize = sizeInKb;
            LoadedAt = DateTime.UtcNow;
            ErrorMessage = "";
        }

        /// <summary>
        /// Mark as failed with error message
        /// </summary>
        public void MarkAsError(string errorMsg)
        {
            Status = "Error";
            ErrorMessage = errorMsg;
            Data = null;
            DataSize = 0;
            LoadedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Get a display-friendly status indicator
        /// </summary>
        public string GetStatusIcon()
        {
            return Status switch
            {
                "Loaded" => "✅",
                "Loading" => "⏳",
                "Error" => "❌",
                _ => "❓"
            };
        }
    }
}
