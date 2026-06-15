' ============================================================================
' Proyecto: ImportadorFotos_Dinamico_WPF_VB
' Fichero: GestorConfiguracion.vb
' Autor original: El Guille (elguillemola.com)
' Persistencia asíncrona System.Text.Json: Gemini (La IA de Google)
' Fecha: Junio de 2026
' ============================================================================

Imports System.IO
Imports System.Text.Json

Public NotInheritable Class GestorConfiguracion
    Private Sub New()
    End Sub

    ' Ubicación del archivo de texto de configuración en la carpeta base de la app
    Private Shared ReadOnly RutaArchivo As String = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "config.json")

    ' Formateo indentado para que el JSON sea legible en texto plano
    Private Shared ReadOnly Opciones As New JsonSerializerOptions With {
        .WriteIndented = True
    }

    ''' <summary>
    ''' Guarda la configuración de forma asíncrona en un archivo de texto JSON.
    ''' </summary>
    Public Shared Async Function GuardarAsync(config As ConfiguracionApp) As Task
        Try
            Dim jsonString As String = JsonSerializer.Serialize(config, Opciones)
            Await File.WriteAllTextAsync(RutaArchivo, jsonString)
        Catch ex As Exception
            Diagnostics.Debug.WriteLine($"Error al guardar JSON en VB: {ex.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Lee el archivo JSON de forma asíncrona. Si no existe, devuelve una instancia limpia.
    ''' </summary>
    Public Shared Async Function CargarAsync() As Task(Of ConfiguracionApp)
        If Not File.Exists(RutaArchivo) Then
            Return New ConfiguracionApp()
        End If

        Try
            Dim jsonString As String = Await File.ReadAllTextAsync(RutaArchivo)
            Return JsonSerializer.Deserialize(Of ConfiguracionApp)(jsonString)
        Catch ex As Exception
            Diagnostics.Debug.WriteLine($"Error al leer JSON en VB: {ex.Message}")
            Return New ConfiguracionApp()
        End Try
    End Function
End Class
