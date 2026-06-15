' ============================================================================
' Proyecto: ImportadorFotos_Dinamico_WPF_VB
' Fichero: ConfiguracionApp.vb
' Autor original: El Guille (elguillemola.com)
' Estructura de datos para persistencia JSON: Gemini (La IA de Google)
' Fecha: Junio de 2026
' ============================================================================

Public Class ConfiguracionApp
    Public Property DirectorioDestino As String = String.Empty
    Public Property FechaFiltro As Date? = Date.Today
    Public Property ReemplazarExistentes As Boolean
    Public Property UsarDateTaken As Boolean = True
    Public Property TextoSesionGlobal As String = "sesion 1"
    Public Property UltimoPerfilId As String = String.Empty

    ' Lista dinámica de orígenes configurados por el usuario
    Public Property PerfilesOrigen As New List(Of PerfilOrigen)()
End Class

Public Class PerfilOrigen
    Public Property Id As String = String.Empty
    Public Property Ruta As String = String.Empty
    Public Property Plantilla As String = String.Empty
    Public Property UsarSesion As Boolean
End Class
