// ============================================================================
// Proyecto: ImportadorFotos_Dinamico_WPF
// Fichero: ConfiguracionModel.cs
// Autor original: El Guille (elguillemola.com)
// Diseño de arquitectura JSON: Gemini (La IA de Google)
// Fecha: Junio de 2026
// ============================================================================

using System;
using System.Collections.Generic;

namespace ImportadorFotos_Dinamico_WPF
{
    /// <summary>
    /// Representa la estructura global del archivo de configuración JSON.
    /// </summary>
    public class ConfiguracionApp
    {
        public string UltimoPerfilId { get; set; } = string.Empty;


        //public bool CopiarRAWSeparado { get; set; } = true; // Por defecto true

        public string DirectorioDestino { get; set; } = string.Empty;
        public DateTime? FechaFiltro { get; set; } = DateTime.Today;
        public bool ReemplazarExistentes { get; set; }
        public bool UsarDateTaken { get; set; } = true;
        public string TextoSesionGlobal { get; set; } = "sesion 1";

        // Lista dinámica de perfiles u orígenes configurados por el usuario
        public List<PerfilOrigen> PerfilesOrigen { get; set; } = new List<PerfilOrigen>();
    }

    /// <summary>
    /// Representa cada uno de los orígenes de fotos dinámicos (perfiles).
    /// </summary>
    public class PerfilOrigen
    {
        public string Id { get; set; } = string.Empty;       // Ej: "iPhone de Guille"
        public string Ruta { get; set; } = string.Empty;     // Ej: "S:\iPhone"
        public string Plantilla { get; set; } = string.Empty; // Ej: "$MM $dd 1 iP7"
        public bool UsarSesion { get; set; }                // Checkbox individual de sesión
    }
}
