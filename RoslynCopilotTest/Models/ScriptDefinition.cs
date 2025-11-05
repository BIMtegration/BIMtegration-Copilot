using System;
using System.Collections.Generic;

namespace RoslynCopilotTest.Models
{
    /// <summary>
    /// Representa la definición de un script ejecutable
    /// </summary>
    public class ScriptDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Icon { get; set; }
        public string Code { get; set; }
        public bool IsFavorite { get; set; }
        public bool ShowAsButton { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Constructor por defecto
        /// </summary>
        public ScriptDefinition()
        {
            CreatedDate = DateTime.Now;
            LastModified = DateTime.Now;
            IsFavorite = false;
            ShowAsButton = false;
        }

        /// <summary>
        /// Constructor completo
        /// </summary>
        public ScriptDefinition(string id, string name, string description, string category, string icon, string code)
        {
            Id = id;
            Name = name;
            Description = description;
            Category = category;
            Icon = icon;
            Code = code;
            IsFavorite = false;
            ShowAsButton = false;
            CreatedDate = DateTime.Now;
            LastModified = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{Category} -> {Name}";
        }
    }

    /// <summary>
    /// Agrupa scripts por categoría para la interfaz
    /// </summary>
    public class ScriptCategory
    {
        public string Name { get; set; }
        public List<ScriptDefinition> Scripts { get; set; }
        public bool IsExpanded { get; set; } = true;

        public ScriptCategory()
        {
            Scripts = new List<ScriptDefinition>();
        }

        public ScriptCategory(string name)
        {
            Name = name;
            Scripts = new List<ScriptDefinition>();
        }
    }
}