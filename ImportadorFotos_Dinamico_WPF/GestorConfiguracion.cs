// ============================================================================
// Proyecto: ImportadorFotos_Dinamico_WPF
// Fichero: GestorConfiguracion.cs
// Autor original: El Guille (elguillemola.com)
// Motor de persistencia System.Text.Json: Gemini (La IA de Google)
// Fecha: Junio de 2026
// ============================================================================

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImportadorFotos_Dinamico_WPF
{
    public static class GestorConfiguracion
    {
        // El archivo se guardará en la misma carpeta del ejecutable de la app
        private static readonly string RutaArchivo = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "config.json");

        // Opciones para que el JSON se grabe indentado y bonito en el archivo de texto
        private static readonly JsonSerializerOptions Opciones = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        /// <summary>
        /// Guarda de forma asíncrona la configuración completa en un archivo de texto JSON.
        /// </summary>
        public static async Task GuardarAsync(ConfiguracionApp config)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(config, Opciones);
                await File.WriteAllTextAsync(RutaArchivo, jsonString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Lee de forma asíncrona el archivo JSON. Si no existe, devuelve una configuración nueva por defecto.
        /// </summary>
        public static async Task<ConfiguracionApp> CargarAsync()
        {
            if (!File.Exists(RutaArchivo))
            {
                // Devolvemos una instancia vacía sin perfiles ficticios
                return new ConfiguracionApp();
            }

            //if (!File.Exists(RutaArchivo))
            //{
            //    // Devolvemos una instancia nueva con un perfil inicial de demostración
            //    var nuevaConfig = new ConfiguracionApp();
            //    nuevaConfig.PerfilesOrigen.Add(new PerfilOrigen
            //    {
            //        Id = "Origen Inicial (Ejemplo)",
            //        Ruta = @"C:\FotosOrigen",
            //        Plantilla = "$MM $dd 1"
            //    });
            //    return nuevaConfig;
            //}

            try
            {
                string jsonString = await File.ReadAllTextAsync(RutaArchivo);
                return JsonSerializer.Deserialize<ConfiguracionApp>(jsonString) ?? new ConfiguracionApp();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al leer JSON: {ex.Message}");
                return new ConfiguracionApp();
            }
        }
    }
}
