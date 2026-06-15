' ============================================================================
' Proyecto: ImportadorFotos_Dinamico_WPF_VB
' Fichero: MainWindow.xaml.vb (BLOQUE 1 DE 2)
' Autor original: El Guille (elguillemola.com)
' Lógica de control y persistencia JSON: Gemini (La IA de Google)
' Fecha: Junio de 2026
' ============================================================================

Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Interop
Imports System.Windows.Media

Public Class MainWindow
    ' Objeto global que mantiene el estado de toda la aplicación en memoria
    Private _config As New ConfiguracionApp()

    ' Bandera para evitar que los eventos se disparen durante la carga inicial
    Private _inicializando As Boolean = True

    Private ReadOnly ExtensionesRAW As New List(Of String) From {
        ".CRW", ".CR2", ".CR3", ".NEF", ".NRW", ".ARW", ".SRF", ".SR2",
        ".ORF", ".RW2", ".PEF", ".RAF", ".GPR", ".DNG"
    }

    ' --- INTEROPERABILIDAD NATIVA (user32.dll) ---
    <DllImport("user32.dll")>
    Private Shared Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SetWindowLong(hWnd As IntPtr, nIndex As Integer, dwNewLong As Integer) As Integer
    End Function

    Private Const GWL_STYLE As Integer = -16
    Private Const WS_MAXIMIZEBOX As Integer = &H10000

    Private Sub Window_SourceInitialized(sender As Object, e As EventArgs)
        Dim hwnd = New WindowInteropHelper(DirectCast(sender, Window)).Handle
        Dim value = GetWindowLong(hwnd, GWL_STYLE)
        SetWindowLong(hwnd, GWL_STYLE, CInt(value And Not WS_MAXIMIZEBOX))
    End Sub

    ' --- MANEJADORES DEL CICLO DE VIDA DE LA VENTANA ---

    Private Async Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded
        LabelInfo.Text = "Cargando configuración JSON..."

        ' Cargamos la configuración asíncronamente
        _config = Await GestorConfiguracion.CargarAsync()

        ' Enlazamos los perfiles almacenados al ComboBox
        ActualizarComboPerfiles()

        ' Cargamos los parámetros globales
        TxtDirDestino.Text = _config.DirectorioDestino
        TxtTextoSesionGlobal.Text = _config.TextoSesionGlobal
        ChkReemplazar.IsChecked = _config.ReemplazarExistentes
        ChkUsarDateTaken.IsChecked = _config.UsarDateTaken

        If _config.FechaFiltro.HasValue AndAlso _config.FechaFiltro.Value <> DateTime.MinValue Then
            DpFechaFiltro.SelectedDate = _config.FechaFiltro.Value
        ElseIf DpFechaFiltro.SelectedDate Is Nothing Then
            DpFechaFiltro.SelectedDate = Date.Today
        End If

        _inicializando = False

        ' Restauramos de forma inteligente el último perfil activo utilizado
        If CmbPerfiles.Items.Count > 0 Then
            Dim perfilA_Restaurar = _config.PerfilesOrigen.FirstOrDefault(Function(p) p.Id = _config.UltimoPerfilId)

            If perfilA_Restaurar IsNot Nothing Then
                CmbPerfiles.SelectedItem = perfilA_Restaurar
            Else
                CmbPerfiles.SelectedIndex = 0
            End If
        End If

        ActualizarCabeceraFecha()
        LabelInfo.Text = "© Guillermo (elGuille) Som, 2018-" & Date.Now.Year.ToString() & " - v3.0"
        LabelCopyR.Text = LabelInfo.Text
        LabelInfo.Text = "Aplicación lista. Perfiles cargados con éxito."
    End Sub

    Private Async Sub MainWindow_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles MyBase.Closing
        ' Guardamos los parámetros globales en el objeto de configuración
        _config.DirectorioDestino = TxtDirDestino.Text
        _config.TextoSesionGlobal = TxtTextoSesionGlobal.Text
        _config.ReemplazarExistentes = ChkReemplazar.IsChecked.GetValueOrDefault()
        _config.UsarDateTaken = ChkUsarDateTaken.IsChecked.GetValueOrDefault()
        _config.FechaFiltro = DpFechaFiltro.SelectedDate

        ' Guardamos el ID del último perfil activo seleccionado
        Dim perfilActivo = TryCast(CmbPerfiles.SelectedItem, PerfilOrigen)
        If perfilActivo IsNot Nothing Then
            _config.UltimoPerfilId = perfilActivo.Id
        End If

        ' Forzamos la última sincronización de los TextBox del perfil actual
        SincronizarPerfilActivo()

        ' Serializamos y guardamos el archivo JSON asíncronamente
        Await GestorConfiguracion.GuardarAsync(_config)
    End Sub

    ' --- GESTIÓN DINÁMICA DE PERFILES ---

    Private Sub ActualizarComboPerfiles()
        CmbPerfiles.ItemsSource = Nothing
        CmbPerfiles.ItemsSource = _config.PerfilesOrigen
    End Sub

    Private Sub CmbPerfiles_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        If _inicializando Then Exit Sub

        ' SINCRONIZACIÓN DE PERSISTENCIA: Guardamos el perfil que el usuario acaba de abandonar
        If e.RemovedItems.Count > 0 Then
            Dim perfilViejo = TryCast(e.RemovedItems(0), PerfilOrigen)
            If perfilViejo IsNot Nothing Then
                perfilViejo.Ruta = TxtDirOrigen.Text
                perfilViejo.Plantilla = TxtPlantilla.Text
                perfilViejo.UsarSesion = ChkUsarSesion.IsChecked.GetValueOrDefault()
            End If
        End If

        If CmbPerfiles.SelectedItem Is Nothing Then Exit Sub

        ' Cargamos en las cajas de texto los parámetros del nuevo perfil activo
        Dim perfilActivo = DirectCast(CmbPerfiles.SelectedItem, PerfilOrigen)
        TxtDirOrigen.Text = perfilActivo.Ruta
        TxtPlantilla.Text = perfilActivo.Plantilla
        ChkUsarSesion.IsChecked = perfilActivo.UsarSesion

        TxtTextoSesionGlobal.IsEnabled = perfilActivo.UsarSesion

        LvFicheros.Items.Clear()
        LabelInfo.Text = $"Perfil '{perfilActivo.Id}' cargado."
    End Sub

    Private Sub SincronizarPerfilActivo()
        Dim perfil = TryCast(CmbPerfiles.SelectedItem, PerfilOrigen)
        If perfil IsNot Nothing Then
            perfil.Ruta = TxtDirOrigen.Text
            perfil.Plantilla = TxtPlantilla.Text
            perfil.UsarSesion = ChkUsarSesion.IsChecked.GetValueOrDefault()
        End If
    End Sub

    Private Sub TxtPerfilControl_LostFocus(sender As Object, e As RoutedEventArgs)
        If _inicializando Then Exit Sub
        SincronizarPerfilActivo()

        ' Habilitamos o deshabilitamos según el estado del CheckBox actual
        TxtTextoSesionGlobal.IsEnabled = ChkUsarSesion.IsChecked.GetValueOrDefault()
    End Sub

    Private Sub BtnNuevoPerfil_Click(sender As Object, e As RoutedEventArgs)
        Dim nombre As String = Microsoft.VisualBasic.Interaction.InputBox(
            "Introduce el nombre identificativo para la cámara o tarjeta:",
            "Nuevo Perfil de Origen", "Nueva Cámara")

        If String.IsNullOrWhiteSpace(nombre) Then Exit Sub

        Dim nuevo As New PerfilOrigen With {
            .Id = nombre,
            .Ruta = "C:\",
            .Plantilla = "$MM $dd"
        }

        _config.PerfilesOrigen.Add(nuevo)

        ActualizarComboPerfiles()
        CmbPerfiles.SelectedItem = nuevo
        LabelInfo.Text = $"Perfil '{nombre}' creado."
    End Sub

    Private Sub BtnEliminarPerfil_Click(sender As Object, e As RoutedEventArgs)
        Dim seleccionado = TryCast(CmbPerfiles.SelectedItem, PerfilOrigen)
        If seleccionado Is Nothing Then Exit Sub

        Dim resultado = MessageBox.Show(
            $"¿Seguro que deseas eliminar el perfil '{seleccionado.Id}'?",
            "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning)

        If resultado = MessageBoxResult.Yes Then
            _config.PerfilesOrigen.Remove(seleccionado)
            ActualizarComboPerfiles()

            If CmbPerfiles.Items.Count > 0 Then
                CmbPerfiles.SelectedIndex = 0
            Else
                LimpiarControlesPerfil()
            End If

            LabelInfo.Text = "Perfil eliminado."
        End If
    End Sub

    Private Sub LimpiarControlesPerfil()
        TxtDirOrigen.Text = String.Empty
        TxtPlantilla.Text = String.Empty
        ChkUsarSesion.IsChecked = False
        LvFicheros.Items.Clear()
    End Sub

    ' ============================================================================
    ' Proyecto: ImportadorFotos_Dinamico_WPF_VB
    ' Fichero: MainWindow.xaml.vb (BLOQUE 2 DE 2)
    ' Autor original: El Guille (elguillemola.com)
    ' Lógica Asíncrona de Procesamiento de Imágenes y Cierre
    ' ============================================================================

    ' --- LÓGICA PRINCIPAL DE ESCRITURA Y LECTURA ASÍNCRONA ---

    Private Async Sub Llenar(lvFiles As ListView, dir As String)
        lvFiles.Items.Clear()
        If String.IsNullOrWhiteSpace(dir) Then Exit Sub

        Dim dirI As New DirectoryInfo(dir)
        If Not dirI.Exists Then
            LabelInfo.Text = $"El directorio {dir} no existe."
            Exit Sub
        End If

        Dim usarDateTaken As Boolean = ChkUsarDateTaken.IsChecked.GetValueOrDefault()
        Dim textoOriginal As String = Convert.ToString(LabelInfo.Text)
        LabelInfo.Text = "Leyendo ficheros del disco..."

        ' Transferimos el escaneo del directorio a un hilo secundario mediante Task.Run
        Dim listaResultado As List(Of ItemFic) = Await Task.Run(
            Function()
                Dim listaTemporal As New List(Of ItemFic)()
                For Each fi In dirI.GetFiles()
                    Dim fechaFic As Date
                    If usarDateTaken Then
                        Dim ficEXIF = InfoFoto(fi.FullName)
                        If ficEXIF Is Nothing OrElse String.IsNullOrEmpty(ficEXIF.DateTaken) Then
                            fechaFic = fi.LastWriteTime
                        Else
                            fechaFic = Convert.ToDateTime(ficEXIF.DateTaken)
                        End If
                    Else
                        fechaFic = fi.LastWriteTime
                    End If
                    listaTemporal.Add(New ItemFic With {.Nombre = fi.Name, .Fecha = fechaFic})
                Next
                Return listaTemporal
            End Function)

        ' Retornamos al hilo principal para volcar los ítems en el ListView unificado
        For Each item In listaResultado
            lvFiles.Items.Add(item)
        Next
        LabelInfo.Text = textoOriginal
    End Sub

    Private Async Function CopiarFicherosAsync(fecha As Date, lv As ListView, dir As String, plantilla As String, usarSesionOrigen As Boolean) As Task(Of (fics As Integer, dirs As Integer, copiados As Integer))
        If String.IsNullOrWhiteSpace(dir) Then Return (0, 0, 0)

        Dim reemplazar As Boolean = ChkReemplazar.IsChecked.GetValueOrDefault()
        Dim dirDestBase As String = TxtDirDestino.Text
        Dim textoSesionGlobal As String = TxtTextoSesionGlobal.Text

        Dim itemsProcesar As New List(Of ItemFic)()
        For Each item In lv.Items
            Dim fic = TryCast(item, ItemFic)
            If fic IsNot Nothing Then itemsProcesar.Add(fic)
        Next

        Dim rutasExistentesEnLvDirs As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each item In LvDirectorios.Items
            Dim it = TryCast(item, ItemDir)
            If it IsNot Nothing Then rutasExistentesEnLvDirs.Add(it.Nombre)
        Next

        ' Procesamiento de copias físicas en segundo plano
        Dim resultado = Await Task.Run(
            Function()
                Dim n As Integer = 0
                Dim dirsCount As Integer = 0
                Dim copiadosCount As Integer = 0

                For i As Integer = 0 To itemsProcesar.Count - 1
                    Dim fic = itemsProcesar(i)
                    Dim file As String = Path.Combine(dir, fic.Nombre)
                    Dim fInfo As New FileInfo(file)

                    ' Informamos del progreso al usuario usando el Dispatcher de la UI de WPF
                    Application.Current.Dispatcher.Invoke(
                        Sub()
                            LabelInfo.Text = $"Procesando {fInfo.Name}"
                        End Sub)

                    Dim fechaFic As Date
                    Dim ficEXIF = InfoFoto(file)
                    If ficEXIF Is Nothing OrElse String.IsNullOrEmpty(ficEXIF.DateTaken) Then
                        fechaFic = fInfo.LastWriteTime
                    Else
                        fechaFic = Convert.ToDateTime(ficEXIF.DateTaken)
                    End If

                    If fechaFic >= fecha Then
                        n += 1
                        Dim textoMes As String = fechaFic.ToString("MM")
                        Dim textoDia As String = fechaFic.ToString("dd")

                        ' Formateamos la plantilla admitiendo la nomenclatura dinámica con o sin '$'
                        Dim subCarpeta As String = plantilla _
                            .Replace("$MM", textoMes).Replace("MM", textoMes) _
                            .Replace("$dd", textoDia).Replace("dd", textoDia)

                        ' Si el origen tiene habilitada su casilla de sesión, inyectamos el texto global
                        If usarSesionOrigen AndAlso Not String.IsNullOrWhiteSpace(textoSesionGlobal) Then
                            subCarpeta = Path.Combine(subCarpeta, textoSesionGlobal)
                        End If

                        Dim dirDest As String = Path.Combine(dirDestBase, subCarpeta)

                        ' El programa aísla los formatos RAW de forma automática según la etiqueta informativa
                        If ExtensionesRAW.Contains(fInfo.Extension.ToUpper()) Then
                            dirDest &= " (RAW)"
                        End If

                        Dim d As New DirectoryInfo(dirDest)
                        Dim itemD As New ItemDir With {.Nombre = d.FullName}

                        If Not rutasExistentesEnLvDirs.Contains(itemD.Nombre) Then
                            rutasExistentesEnLvDirs.Add(itemD.Nombre)
                            dirsCount += 1
                            Application.Current.Dispatcher.Invoke(
                                Sub()
                                    LvDirectorios.Items.Add(itemD)
                                End Sub)
                        End If

                        If Not d.Exists Then d.Create()

                        Dim targetFile As String = Path.Combine(d.FullName, fInfo.Name)
                        Dim fDest As New FileInfo(targetFile)
                        Dim destExists As Boolean = fDest.Exists

                        If Not destExists OrElse (destExists AndAlso reemplazar) Then
                            Try
                                fInfo.CopyTo(targetFile, True)
                                copiadosCount += 1
                            Catch ex As Exception
                                Debug.WriteLine(ex.Message)
                                ' 'Debug' is not a member of 'System.Windows.Diagnostics'.
                                'Diagnostics.Debug.WriteLine(ex.Message)
                            End Try
                        End If
                    End If
                Next
                Return (fics:=n, dirs:=dirsCount, copiados:=copiadosCount)
            End Function)

        Return resultado
    End Function

    ' --- MANEJADORES DE ACCIONES Y EVENTOS DE INTERFAZ ---

    Private Sub BtnLeer_Click(sender As Object, e As RoutedEventArgs)
        Llenar(LvFicheros, TxtDirOrigen.Text)
    End Sub

    Private Async Sub BtnCopiar_Click(sender As Object, e As RoutedEventArgs)
        If DpFechaFiltro.SelectedDate Is Nothing Then
            LabelInfo.Text = "Por favor seleccione una fecha filtro válida."
            Exit Sub
        End If
        If String.IsNullOrWhiteSpace(TxtDirDestino.Text) Then
            LabelInfo.Text = "Debe indicar el directorio de destino."
            Exit Sub
        End If

        Dim perfilActivo = TryCast(CmbPerfiles.SelectedItem, PerfilOrigen)
        If perfilActivo Is Nothing Then
            LabelInfo.Text = "No hay ningún perfil seleccionado para copiar."
            Exit Sub
        End If

        BtnCopiar.IsEnabled = False
        Dim fechaFiltro As Date = DpFechaFiltro.SelectedDate.Value
        LvDirectorios.Items.Clear()
        LabelInfo.Text = "Iniciando proceso de copia..."

        ' Sincronizamos antes de operar
        SincronizarPerfilActivo()

        ' Copia asíncronamente el origen dinámico seleccionado
        Dim res = Await CopiarFicherosAsync(
            fechaFiltro,
            LvFicheros,
            TxtDirOrigen.Text,
            TxtPlantilla.Text,
            ChkUsarSesion.IsChecked.GetValueOrDefault()
        )

        LabelInfo.Text = $"Copia finalizada. Procesados: {res.fics}, Carpetas creadas: {res.dirs}, Ficheros copiados: {res.copiados}"
        BtnCopiar.IsEnabled = True
    End Sub

    Private Sub BtnSelDir_Click(sender As Object, e As RoutedEventArgs)
        Dim btn = TryCast(sender, Button)
        If btn Is Nothing Then Exit Sub

        Dim txtTarget As TextBox = If(btn Is BtnSelOrigen, TxtDirOrigen, TxtDirDestino)

        ' Usamos la clase OpenFolderDialog nativa de WPF e integrada en .NET 9 (Adiós WinForms)
        Dim dialog As New Microsoft.Win32.OpenFolderDialog()
        dialog.Title = "Selecciona el directorio correspondiente"
        dialog.InitialDirectory = txtTarget.Text

        If dialog.ShowDialog() = True Then
            txtTarget.Text = dialog.FolderName
            If btn Is BtnSelOrigen Then SincronizarPerfilActivo()
        End If
    End Sub

    Private Sub ChkUsarDateTaken_CheckedChanged(sender As Object, e As RoutedEventArgs)
        If _inicializando Then Exit Sub
        ActualizarCabeceraFecha()
        If LvFicheros.Items.Count > 0 Then
            Llenar(LvFicheros, TxtDirOrigen.Text)
        End If
    End Sub

    Private Sub ActualizarCabeceraFecha()
        Dim gridView = TryCast(LvFicheros.View, GridView)
        If gridView IsNot Nothing AndAlso gridView.Columns.Count > 1 Then
            Dim usarDateTaken As Boolean = ChkUsarDateTaken.IsChecked.GetValueOrDefault()
            gridView.Columns(1).Header = If(usarDateTaken, "Date Taken", "Fecha Modificación")
        End If
    End Sub

    Private Sub TxtDirOrigen_KeyDown(sender As Object, e As KeyEventArgs)
        If e.Key = Key.Enter OrElse e.Key = Key.Return Then
            SincronizarPerfilActivo()
            Llenar(LvFicheros, TxtDirOrigen.Text)
            e.Handled = True
        End If

    End Sub
    ' Extracción limpia de metadatos mediante objetos gráficos nativos de WPF
    Private Function InfoFoto(sImg As String) As BitmapMetadata
        Try
            Using fis As New StreamReader(sImg)
                Dim img = BitmapFrame.Create(fis.BaseStream)
                Return TryCast(img.Metadata, BitmapMetadata)
            End Using
        Catch
            Return Nothing
        End Try
    End Function
End Class ' <-- Cierre de la clase principal' --- CLASES DE MODELO EN MEMORIA PARA ENLACE DE DATOS (BINDING) ---
Public Class ItemFic
    Public Property Nombre As String = String.Empty
    Public Property Fecha As Date
End Class
Public Class ItemDir
    Public Property Nombre As String = String.Empty
End Class

