using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace RoslynCopilotTest.Services
{
    /// <summary>
    /// Servicio para almacenar y recuperar tokens de forma segura
    /// </summary>
    public class SecureTokenStorage
    {
        private static readonly string TokenFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoslynCopilot",
            "github_auth.dat"
        );

        private static readonly byte[] entropy = Encoding.UTF8.GetBytes("RoslynCopilot2025");

        /// <summary>
        /// Guarda el token de forma segura
        /// </summary>
        public static bool SaveToken(GitHubAuthResult authResult)
        {
            try
            {
                var tokenData = new StoredTokenData
                {
                    AccessToken = authResult.AccessToken,
                    Username = authResult.Username,
                    UserEmail = authResult.UserEmail,
                    AvatarUrl = authResult.AvatarUrl,
                    SavedAt = DateTime.UtcNow
                };

                var json = JsonConvert.SerializeObject(tokenData);
                var jsonBytes = Encoding.UTF8.GetBytes(json);

                // Encriptar usando DPAPI (Windows Data Protection API)
                var encryptedData = ProtectedData.Protect(jsonBytes, entropy, DataProtectionScope.CurrentUser);

                // Crear directorio si no existe
                var directory = Path.GetDirectoryName(TokenFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Guardar archivo encriptado
                File.WriteAllBytes(TokenFilePath, encryptedData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Recupera el token guardado
        /// </summary>
        public static StoredTokenData LoadToken()
        {
            try
            {
                if (!File.Exists(TokenFilePath))
                    return null;

                // Leer archivo encriptado
                var encryptedData = File.ReadAllBytes(TokenFilePath);

                // Desencriptar
                var jsonBytes = ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(jsonBytes);

                return JsonConvert.DeserializeObject<StoredTokenData>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Elimina el token guardado
        /// </summary>
        public static bool DeleteToken()
        {
            try
            {
                if (File.Exists(TokenFilePath))
                {
                    File.Delete(TokenFilePath);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica si hay un token guardado válido
        /// </summary>
        public static bool HasValidToken()
        {
            var tokenData = LoadToken();
            if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
                return false;

            // Verificar que no sea demasiado viejo (tokens de GitHub no expiran, pero por seguridad)
            var daysSinceSaved = (DateTime.UtcNow - tokenData.SavedAt).TotalDays;
            return daysSinceSaved < 365; // Válido por 1 año
        }
    }

    /// <summary>
    /// Datos del token almacenado
    /// </summary>
    public class StoredTokenData
    {
        public string AccessToken { get; set; }
        public string Username { get; set; }
        public string UserEmail { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime SavedAt { get; set; }
    }
}