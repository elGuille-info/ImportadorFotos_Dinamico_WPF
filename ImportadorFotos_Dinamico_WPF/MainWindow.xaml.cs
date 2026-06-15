// ============================================================================
// Proyecto: ImportadorFotos_Dinamico_WPF
// Fichero: MainWindow.xaml.cs (BLOQUE 1 DE 2)
// Autor original: El Guille (elguillemola.com)
// Arquitectura dinámica y asíncrona: Gemini (La IA de Google)
// Fecha: Junio de 2026
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using System.Windows.Media.Imaging;

namespace ImportadorFotos_Dinamico_WPF
{
    public partial class MainWindow : Window
    {
        // Objeto global que mantiene el estado de toda la aplicación en memoria
        private ConfiguracionApp _config = new ConfiguracionApp();

        // Bandera para evitar que los eventos se disparen durante la carga inicial
        private bool _inicializando = true;

        private readonly List<string> ExtensionesRAW = new List<string>
        {
            ".CRW", ".CR2", ".CR3", ".NEF", ".NRW", ".ARW", ".SRF", ".SR2",
            ".ORF", ".RW2", ".PEF", ".RAF", ".GPR", ".DNG"
        };

        // --- INTEROPERABILIDAD NATIVA (user32.dll) ---
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper((Window)sender).Handle;
            var value = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, (int)(value & ~WS_MAXIMIZEBOX));
        }

        // --- MANEJADORES DEL CICLO DE VIDA DE LA VENTANA ---

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LabelInfo.Text = "Cargando configuración JSON...";

            // Cargamos los datos de forma asíncrona desde el archivo config.json
            _config = await GestorConfiguracion.CargarAsync();

            // Asignamos la lista de perfiles al ComboBox
            ActualizarComboPerfiles();

            // Cargamos los valores globales de la aplicación
            TxtDirDestino.Text = _config.DirectorioDestino;
            TxtTextoSesionGlobal.Text = _config.TextoSesionGlobal;
            ChkReemplazar.IsChecked = _config.ReemplazarExistentes;
            ChkUsarDateTaken.IsChecked = _config.UsarDateTaken;
            DpFechaFiltro.SelectedDate = _config.FechaFiltro ?? DateTime.Today;

            //ChkCopiarDNGSeparado.IsChecked = _config.CopiarRAWSeparado;

            //_inicializando = false;

            //// Forzamos la selección del primer perfil disponible
            //if (CmbPerfiles.Items.Count > 0)
            //{
            //    CmbPerfiles.SelectedIndex = 0;
            //}

            _inicializando = false;

            // Buscamos si existe el perfil guardado como último activo
            if (CmbPerfiles.Items.Count > 0)
            {
                var perfilA_Restaurar = _config.PerfilesOrigen
                    .FirstOrDefault(p => p.Id == _config.UltimoPerfilId);

                if (perfilA_Restaurar != null)
                {
                    CmbPerfiles.SelectedItem = perfilA_Restaurar;
                }
                else
                {
                    // Si no se encuentra o es la primera vez, seleccionamos el primero por defecto
                    CmbPerfiles.SelectedIndex = 0;
                }
            }


            //string s = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            var ensamblado = typeof(MainWindow).Assembly;
            var fvi = FileVersionInfo.GetVersionInfo(ensamblado.Location);
                        
            if (DateTime.Now.Year > 2020)
            {
                LabelCopyR.Text = "| © Guillermo (elGuille) Som, 2018-" + DateTime.Now.Year.ToString();
            }
            else
            {
                LabelCopyR.Text = "| © Guillermo (elGuille) Som, 2018-2026";
            }
            LabelCopyR.Text = LabelCopyR.Text + $" - v{fvi.ProductMajorPart}.{fvi.ProductMinorPart}.{fvi.ProductPrivatePart}";

            // Inicializamos la cabecera del ListView según el CheckBox
            ActualizarCabeceraFecha();
            LabelInfo.Text = "Aplicación lista. Perfiles cargados con éxito.";
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Guardamos los datos globales antes de destruir la ventana
            _config.DirectorioDestino = TxtDirDestino.Text;
            _config.TextoSesionGlobal = TxtTextoSesionGlobal.Text;
            _config.ReemplazarExistentes = ChkReemplazar.IsChecked.GetValueOrDefault();
            _config.UsarDateTaken = ChkUsarDateTaken.IsChecked.GetValueOrDefault();
            _config.FechaFiltro = DpFechaFiltro.SelectedDate;

            //_config.CopiarRAWSeparado = ChkCopiarDNGSeparado.IsChecked.GetValueOrDefault();

            if (CmbPerfiles.SelectedItem is PerfilOrigen perfilActivo)
            {
                _config.UltimoPerfilId = perfilActivo.Id;
            }


            // Volcamos los datos de la pantalla al perfil que esté seleccionado actualmente
            SincronizarPerfilActivo();

            // Guardamos físicamente en el archivo de texto
            await GestorConfiguracion.GuardarAsync(_config);
        }

        // --- GESTIÓN DINÁMICA DE PERFILES ---

        private void ActualizarComboPerfiles()
        {
            CmbPerfiles.ItemsSource = null;
            CmbPerfiles.ItemsSource = _config.PerfilesOrigen;
        }

        //private void CmbPerfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (_inicializando) return;

        //    // Si el cambio de selección viene de eliminar un perfil, salimos
        //    if (CmbPerfiles.SelectedItem == null) return;

        //    // Antes de cambiar de perfil, guardamos lo que el usuario escribió en el anterior
        //    SincronizarPerfilActivo();

        //    // Cargamos en los controles los datos del nuevo perfil seleccionado
        //    var perfilActivo = (PerfilOrigen)CmbPerfiles.SelectedItem;
        //    TxtDirOrigen.Text = perfilActivo.Ruta;
        //    TxtPlantilla.Text = perfilActivo.Plantilla;
        //    ChkUsarSesion.IsChecked = perfilActivo.UsarSesion;

        //    LvFicheros.Items.Clear();
        //    LabelInfo.Text = $"Perfil '{perfilActivo.Id}' cargado.";
        //}

        //private void CmbPerfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (_inicializando) return;

        //    // --- CORRECCIÓN DE PERSISTENCIA ---
        //    // Si cambiamos de perfil, guardamos lo que había en la pantalla en el perfil ANTERIOR
        //    // antes de machacar los TextBox con los datos del nuevo perfil
        //    if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is PerfilOrigen perfilViejo)
        //    {
        //        perfilViejo.Ruta = TxtDirOrigen.Text;
        //        perfilViejo.Plantilla = TxtPlantilla.Text;
        //        perfilViejo.UsarSesion = ChkUsarSesion.IsChecked.GetValueOrDefault();
        //    }
        //    // ----------------------------------

        //    if (CmbPerfiles.SelectedItem == null) return;

        //    // Cargamos en los controles los datos del nuevo perfil seleccionado
        //    var perfilActivo = (PerfilOrigen)CmbPerfiles.SelectedItem;
        //    TxtDirOrigen.Text = perfilActivo.Ruta;
        //    TxtPlantilla.Text = perfilActivo.Plantilla;
        //    ChkUsarSesion.IsChecked = perfilActivo.UsarSesion;

        //    LvFicheros.Items.Clear();
        //    LabelInfo.Text = $"Perfil '{perfilActivo.Id}' cargado.";
        //}

        private void CmbPerfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_inicializando) return;

            // --- CORRECCIÓN DE PERSISTENCIA DEFINITIVA ---
            // Si venimos de cambiar de dispositivo, guardamos lo que el usuario 
            // haya escrito en la pantalla en el perfil que acaba de abandonar.
            if (e.RemovedItems.Count > 0)
            {
                // Extraemos el primer elemento de la colección de ítems deseleccionados
                var perfilViejo = e.RemovedItems[0] as PerfilOrigen;

                if (perfilViejo != null)
                {
                    perfilViejo.Ruta = TxtDirOrigen.Text;
                    perfilViejo.Plantilla = TxtPlantilla.Text;
                    perfilViejo.UsarSesion = ChkUsarSesion.IsChecked.GetValueOrDefault();
                }
            }
            // ---------------------------------------------

            if (CmbPerfiles.SelectedItem == null) return;

            // Cargamos en los controles los datos del nuevo perfil activo
            var perfilActivo = (PerfilOrigen)CmbPerfiles.SelectedItem;
            TxtDirOrigen.Text = perfilActivo.Ruta;
            TxtPlantilla.Text = perfilActivo.Plantilla;
            ChkUsarSesion.IsChecked = perfilActivo.UsarSesion;

            LvFicheros.Items.Clear();
            LabelInfo.Text = $"Perfil '{perfilActivo.Id}' cargado.";
        }

        private void SincronizarPerfilActivo()
        {
            // Guarda los datos de las cajas de texto en el objeto de memoria correspondiente
            if (CmbPerfiles.SelectedItem is PerfilOrigen perfil)
            {
                perfil.Ruta = TxtDirOrigen.Text;
                perfil.Plantilla = TxtPlantilla.Text;
                perfil.UsarSesion = ChkUsarSesion.IsChecked.GetValueOrDefault();
            }
        }

        private void TxtPerfilControl_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_inicializando) return;
            // Si el usuario cambia la plantilla o el check de sesión, lo salvamos en memoria al instante
            SincronizarPerfilActivo();
        }

        private void BtnNuevoPerfil_Click(object sender, RoutedEventArgs e)
        {
            // Usamos un InputBox de toda la vida para pedir el nombre del nuevo perfil
            // Nota: Al estar en .NET 9, Microsoft permite usar Interaction.InputBox si añades la referencia de VisualBasic,
            // o bien podemos simularlo de forma rápida por código.
            string nombre = Microsoft.VisualBasic.Interaction.InputBox(
                "Introduce el nombre identificativo para la cámara o tarjeta:",
                "Nuevo Perfil de Origen", "Nueva Cámara");

            if (string.IsNullOrWhiteSpace(nombre)) return;

            var nuevo = new PerfilOrigen
            {
                Id = nombre,
                Ruta = @"C:\",
                Plantilla = "$MM $dd"
            };

            _config.PerfilesOrigen.Add(nuevo);

            ActualizarComboPerfiles();
            CmbPerfiles.SelectedItem = nuevo;
            LabelInfo.Text = $"Perfil '{nombre}' creado.";
        }

        private void BtnEliminarPerfil_Click(object sender, RoutedEventArgs e)
        {
            var seleccionado = CmbPerfiles.SelectedItem as PerfilOrigen;
            if (seleccionado == null) return;

            var resultado = MessageBox.Show(
                $"¿Seguro que deseas eliminar el perfil '{seleccionado.Id}'?",
                "Confirmar Eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                _config.PerfilesOrigen.Remove(seleccionado);
                ActualizarComboPerfiles();

                // Seleccionamos el primer elemento restante
                if (CmbPerfiles.Items.Count > 0) CmbPerfiles.SelectedIndex = 0;
                else LimpiarControlesPerfil();

                LabelInfo.Text = "Perfil eliminado.";
            }
        }

        private void LimpiarControlesPerfil()
        {
            TxtDirOrigen.Text = string.Empty;
            TxtPlantilla.Text = string.Empty;
            ChkUsarSesion.IsChecked = false;
            LvFicheros.Items.Clear();
        }

        // ============================================================================
        // Proyecto: ImportadorFotos_Dinamico_WPF
        // Fichero: MainWindow.xaml.cs (BLOQUE 2 DE 2)
        // Autor original: El Guille (elguillemola.com)
        // Lógica de Procesamiento de Imágenes y Cierre
        // ============================================================================

        // --- LÓGICA PRINCIPAL DE ESCRITURA Y LECTURA ASÍNCRONA ---

        private async void Llenar(ListView lvFiles, string dir)
        {
            lvFiles.Items.Clear();
            if (string.IsNullOrWhiteSpace(dir)) return;

            var dirI = new DirectoryInfo(dir);
            if (!dirI.Exists)
            {
                LabelInfo.Text = $"El directorio {dir} no existe.";
                return;
            }

            bool usarDateTaken = ChkUsarDateTaken.IsChecked.GetValueOrDefault();
            string textoOriginal = Convert.ToString(LabelInfo.Text) ?? string.Empty;
            LabelInfo.Text = "Leyendo ficheros del disco...";

            // Transferimos el escaneo del directorio a un hilo secundario
            var listaResultado = await Task.Run(() =>
            {
                var listaTemporal = new List<ItemFic>();
                foreach (var fi in dirI.GetFiles())
                {
                    DateTime fechaFic;
                    if (usarDateTaken)
                    {
                        var ficEXIF = InfoFoto(fi.FullName);
                        if (ficEXIF == null || string.IsNullOrEmpty(ficEXIF.DateTaken))
                        {
                            fechaFic = fi.LastWriteTime;
                        }
                        else
                        {
                            fechaFic = Convert.ToDateTime(ficEXIF.DateTaken);
                        }
                    }
                    else
                    {
                        fechaFic = fi.LastWriteTime;
                    }
                    listaTemporal.Add(new ItemFic { Nombre = fi.Name, Fecha = fechaFic });
                }
                return listaTemporal;
            });

            // Retornamos al hilo principal para volcar los ítems en el ListView unificado
            foreach (var item in listaResultado)
            {
                lvFiles.Items.Add(item);
            }
            LabelInfo.Text = textoOriginal;
        }

        private async Task<(int fics, int dirs, int copiados)>
            CopiarFicherosAsync(DateTime fecha, ListView lv, string dir, string plantilla, bool usarSesionOrigen)
        {
            if (string.IsNullOrWhiteSpace(dir)) return (0, 0, 0);

            bool reemplazar = ChkReemplazar.IsChecked.GetValueOrDefault();
            string dirDestBase = TxtDirDestino.Text;
            string textoSesionGlobal = TxtTextoSesionGlobal.Text;

            var itemsProcesar = new List<ItemFic>();
            foreach (var item in lv.Items)
            {
                if (item is ItemFic fic) itemsProcesar.Add(fic);
            }

            var rutasExistentesEnLvDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in LvDirectorios.Items)
            {
                if (item is ItemDir it) rutasExistentesEnLvDirs.Add(it.Nombre);
            }

            // Procesamiento de copias físicas en segundo plano
            var resultado = await Task.Run(() =>
            {
                int n = 0; int dirsCount = 0; int copiadosCount = 0;

                for (int i = 0; i < itemsProcesar.Count; i++)
                {
                    var fic = itemsProcesar[i];
                    string file = Path.Combine(dir, fic.Nombre);
                    var fInfo = new FileInfo(file);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LabelInfo.Text = $"Procesando {fInfo.Name}";
                    });

                    DateTime fechaFic;
                    var ficEXIF = InfoFoto(file);
                    if (ficEXIF == null || string.IsNullOrEmpty(ficEXIF.DateTaken))
                    {
                        fechaFic = fInfo.LastWriteTime;
                    }
                    else
                    {
                        fechaFic = Convert.ToDateTime(ficEXIF.DateTaken);
                    }

                    if (fechaFic >= fecha)
                    {
                        n++;
                        string textoMes = fechaFic.ToString("MM");
                        string textoDia = fechaFic.ToString("dd");

                        // Formateamos la plantilla admitiendo la nomenclatura dinámica con o sin '$'
                        string subCarpeta = plantilla
                            .Replace("$MM", textoMes).Replace("MM", textoMes)
                            .Replace("$dd", textoDia).Replace("dd", textoDia);

                        // Si el origen tiene habilitada su casilla de sesión, inyectamos el texto global
                        if (usarSesionOrigen && !string.IsNullOrWhiteSpace(textoSesionGlobal))
                        {
                            subCarpeta = Path.Combine(subCarpeta, textoSesionGlobal);
                        }

                        string dirDest = Path.Combine(dirDestBase, subCarpeta);

                        if (ExtensionesRAW.Contains(fInfo.Extension.ToUpper()))
                        {
                            dirDest += " (RAW)";
                        }

                        var d = new DirectoryInfo(dirDest);
                        var itemD = new ItemDir { Nombre = d.FullName };

                        if (!rutasExistentesEnLvDirs.Contains(itemD.Nombre))
                        {
                            rutasExistentesEnLvDirs.Add(itemD.Nombre);
                            dirsCount++;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                LvDirectorios.Items.Add(itemD);
                            });
                        }

                        if (!d.Exists) d.Create();

                        string targetFile = Path.Combine(d.FullName, fInfo.Name);
                        var fDest = new FileInfo(targetFile);
                        bool destExists = fDest.Exists;

                        if (!destExists || (destExists && reemplazar))
                        {
                            try
                            {
                                fInfo.CopyTo(targetFile, true);
                                copiadosCount++;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(ex.Message);
                            }
                        }
                    }
                }
                return (fics: n, dirs: dirsCount, copiados: copiadosCount);
            });

            return resultado;
        }

        // --- MANEJADORES DE ACCIONES Y EVENTOS DE INTERFAZ ---

        private void BtnLeer_Click(object sender, RoutedEventArgs e)
        {
            Llenar(LvFicheros, TxtDirOrigen.Text);
        }

        private async void BtnCopiar_Click(object sender, RoutedEventArgs e)
        {
            if (DpFechaFiltro.SelectedDate == null)
            {
                LabelInfo.Text = "Por favor seleccione una fecha filtro válida.";
                return;
            }
            if (string.IsNullOrWhiteSpace(TxtDirDestino.Text))
            {
                LabelInfo.Text = "Debe indicar el directorio de destino.";
                return;
            }

            var perfilActivo = CmbPerfiles.SelectedItem as PerfilOrigen;
            if (perfilActivo == null)
            {
                LabelInfo.Text = "No hay ningún perfil seleccionado para copiar.";
                return;
            }

            BtnCopiar.IsEnabled = false;
            DateTime fechaFiltro = DpFechaFiltro.SelectedDate.Value;
            LvDirectorios.Items.Clear();
            LabelInfo.Text = "Iniciando proceso de copia...";

            // Sincronizamos antes de operar
            SincronizarPerfilActivo();

            // Copia asíncrona dedicada exclusivamente al origen seleccionado
            var res = await CopiarFicherosAsync(
                fechaFiltro,
                LvFicheros,
                TxtDirOrigen.Text,
                TxtPlantilla.Text,
                ChkUsarSesion.IsChecked.GetValueOrDefault()
            );

            LabelInfo.Text = $"Copia finalizada. Procesados: {res.fics}, Carpetas creadas: {res.dirs}, Ficheros copiados: {res.copiados}";
            BtnCopiar.IsEnabled = true;
        }

        // Centraliza la selección de carpetas en disco para Origen y Destino
        //private void BtnSelDir_Click(object sender, RoutedEventArgs e)
        //{
        //    var btn = sender as Button;
        //    if (btn == null) return;

        //    TextBox txtTarget = (btn == BtnSelOrigen) ? TxtDirOrigen : TxtDirDestino;

        //    using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
        //    {
        //        dialog.Description = "Selecciona el directorio correspondiente";
        //        dialog.SelectedPath = txtTarget.Text;
        //        dialog.RootFolder = Environment.SpecialFolder.MyComputer;

        //        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //        {
        //            txtTarget.Text = dialog.SelectedPath;
        //            if (btn == BtnSelOrigen) SincronizarPerfilActivo();
        //        }
        //    }
        //}

        private void BtnSelDir_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            TextBox txtTarget = (btn == BtnSelOrigen) ? TxtDirOrigen : TxtDirDestino;

            // Clase oficial y nativa de WPF para seleccionar carpetas a partir de .NET 8/9
            var dialog = new Microsoft.Win32.OpenFolderDialog();

            dialog.Title = "Selecciona el directorio correspondiente";
            dialog.InitialDirectory = txtTarget.Text;

            if (dialog.ShowDialog() == true)
            {
                txtTarget.Text = dialog.FolderName;
                if (btn == BtnSelOrigen) SincronizarPerfilActivo();
            }
        }

        // Detecta el cambio en el CheckBox de usar EXIF o Fecha de Modificación
        private void ChkUsarDateTaken_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_inicializando) return;
            ActualizarCabeceraFecha();
            if (LvFicheros.Items.Count > 0)
            {
                Llenar(LvFicheros, TxtDirOrigen.Text);
            }
        }
        // Actualiza textualmente el encabezado de la columna de fecha en el GridView
        //private void ActualizarCabeceraFecha()
        //{
        //    var gridView = LvFicheros.View as GridView;
        //    if (gridView != null && gridView.Columns.Count > 1)
        //    {
        //        bool usarDateTaken = ChkUsarDateTaken.IsChecked.GetValueOrDefault();
        //        gridView.Columns[1].Header = usarDateTaken ? "Date Taken" : "Fecha Modificación";
        //    }
        //}

        private void ActualizarCabeceraFecha()
        {
            var gridView = LvFicheros.View as GridView;
            if (gridView != null && gridView.Columns.Count > 1)
            {
                bool usarDateTaken = ChkUsarDateTaken.IsChecked.GetValueOrDefault();
                // Especificamos el índice [1] explícitamente para que XAML no se confunda
                gridView.Columns[1].Header = usarDateTaken ? "Date Taken" : "Fecha Modificación";
            }
        }

        // Permite leer la carpeta al pulsar Intro dentro de la caja de texto de Origen
        private void TxtDirOrigen_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                SincronizarPerfilActivo();
                Llenar(LvFicheros, TxtDirOrigen.Text);
                e.Handled = true;
            }
        }

        // Extracción física de metadatos mediante flujos e imágenes nativas de WPF
        private BitmapMetadata? InfoFoto(string sImg)
        {
            try
            {
                using (var fis = new StreamReader(sImg))
                {
                    var img = BitmapFrame.Create(fis.BaseStream);
                    return img.Metadata as BitmapMetadata;
                }
            }
            catch
            {
                return null;
            }
        }
    } // <-- Cierre de la clase MainWindow

    // --- CLASES DE MODELO EN MEMORIA PARA ENLACE DE DATOS (BINDING) ---
    public class ItemFic
    {
        public string Nombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }

    public class ItemDir
    {
        public string Nombre { get; set; } = string.Empty;
    }
} // <-- Cierre definitivo del namespace
