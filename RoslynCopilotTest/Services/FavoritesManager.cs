using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RoslynCopilotTest.Models;

namespace RoslynCopilotTest.Services
{
    /// <summary>
    /// Servicio para gestionar los scripts favoritos
    /// </summary>
    public static class FavoritesManager
    {
        private static readonly string FavoritesFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoslynCopilotTest",
            "favorites.json"
        );

        /// <summary>
        /// Carga la lista de IDs de scripts favoritos desde el archivo
        /// </summary>
        public static HashSet<string> LoadFavorites()
        {
            try
            {
                if (!File.Exists(FavoritesFilePath))
                {
                    return new HashSet<string>();
                }

                var json = File.ReadAllText(FavoritesFilePath);
                var favoriteIds = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                return new HashSet<string>(favoriteIds);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading favorites: {ex.Message}");
                return new HashSet<string>();
            }
        }

        /// <summary>
        /// Guarda la lista de IDs de scripts favoritos en el archivo
        /// </summary>
        public static void SaveFavorites(HashSet<string> favoriteIds)
        {
            try
            {
                // Asegurar que el directorio existe
                var directory = Path.GetDirectoryName(FavoritesFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(favoriteIds.ToList(), Formatting.Indented);
                File.WriteAllText(FavoritesFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving favorites: {ex.Message}");
            }
        }

        /// <summary>
        /// Marca o desmarca un script como favorito
        /// </summary>
        public static bool ToggleFavorite(string scriptId)
        {
            var favorites = LoadFavorites();
            bool isFavorite;

            if (favorites.Contains(scriptId))
            {
                favorites.Remove(scriptId);
                isFavorite = false;
            }
            else
            {
                favorites.Add(scriptId);
                isFavorite = true;
            }

            SaveFavorites(favorites);
            return isFavorite;
        }

        /// <summary>
        /// Verifica si un script es favorito
        /// </summary>
        public static bool IsFavorite(string scriptId)
        {
            var favorites = LoadFavorites();
            return favorites.Contains(scriptId);
        }

        /// <summary>
        /// Filtra una lista de scripts para mostrar solo los favoritos
        /// </summary>
        public static List<ScriptDefinition> FilterFavorites(List<ScriptDefinition> scripts)
        {
            var favorites = LoadFavorites();
            return scripts.Where(s => favorites.Contains(s.Id)).ToList();
        }

        /// <summary>
        /// Actualiza el estado de favorito en una lista de scripts
        /// </summary>
        public static void UpdateFavoriteStatus(List<ScriptDefinition> scripts)
        {
            var favorites = LoadFavorites();
            foreach (var script in scripts)
            {
                script.IsFavorite = favorites.Contains(script.Id);
            }
        }

        /// <summary>
        /// Elimina un script de la lista de favoritos (útil cuando se elimina un script)
        /// </summary>
        public static void RemoveFromFavorites(string scriptId)
        {
            var favorites = LoadFavorites();
            if (favorites.Contains(scriptId))
            {
                favorites.Remove(scriptId);
                SaveFavorites(favorites);
            }
        }
    }
}