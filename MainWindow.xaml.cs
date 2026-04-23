using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Text.Json;
using LibVLCSharp.Shared;


namespace VideoManager
{
    public class VideoItem : INotifyPropertyChanged
    {
        // Cached frozen brushes - avoids creating new Brush objects on every binding update
        private static readonly Brush BrushSuccess = CreateFrozenBrush(52, 199, 89);
        private static readonly Brush BrushError = CreateFrozenBrush(255, 59, 48);
        private static readonly Brush BrushProgress = CreateFrozenBrush(26, 115, 232);
        private static readonly Brush BrushDefault = CreateFrozenBrush(160, 160, 165);
        private static Brush CreateFrozenBrush(byte r, byte g, byte b) { var b2 = new SolidColorBrush(Color.FromRgb(r, g, b)); b2.Freeze(); return b2; }

        private string _format = "Analyse...";
        private string _status = "En attente";
        private string _location = "Inconnu";
        private bool _isSelected;

        private string _duration = "--:--";
        private ImageSource? _thumbnail;

        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";

        public ImageSource? Thumbnail { get => _thumbnail; set { _thumbnail = value; OnPropertyChanged(nameof(Thumbnail)); } }

        public string Duration { get => _duration; set { _duration = value; OnPropertyChanged(nameof(Duration)); } }

        public string Format { get => _format; set { _format = value; OnPropertyChanged(nameof(Format)); } }
        public string Location { get => _location; set { _location = value; OnPropertyChanged(nameof(Location)); } }
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } }
        
        private int _columnSpan = 1;
        public int ColumnSpan { get => _columnSpan; set { _columnSpan = value; OnPropertyChanged(nameof(ColumnSpan)); } }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusColorBrush));
            }
        }

        public string StatusIcon
        {
            get
            {
                if (Status.Contains("Terminé") || Status.Contains("Succès") || Status.Contains("Prêt")) return "\uE73E";
                if (Status.Contains("Erreur") || Status.Contains("Échec")) return "\uE783";
                if (Status.Contains("cours") || Status.Contains("Conversion")) return "\uE895";
                if (Status.Contains("convertir")) return "\uE8A6";
                return "\uE916";
            }
        }

        public Brush StatusColorBrush
        {
            get
            {
                if (Status.Contains("Terminé") || Status.Contains("Succès") || Status.Contains("Prêt")) return BrushSuccess;
                if (Status.Contains("Erreur") || Status.Contains("Échec")) return BrushError;
                if (Status.Contains("cours") || Status.Contains("Conversion")) return BrushProgress;
                return BrushDefault;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class PhotoItem : INotifyPropertyChanged
    {
        // Cached frozen brushes
        private static readonly Brush BrushSuccess = CreateFrozenBrush(52, 199, 89);
        private static readonly Brush BrushError = CreateFrozenBrush(255, 59, 48);
        private static readonly Brush BrushProgress = CreateFrozenBrush(26, 115, 232);
        private static readonly Brush BrushDefault = CreateFrozenBrush(160, 160, 165);
        private static Brush CreateFrozenBrush(byte r, byte g, byte b) { var b2 = new SolidColorBrush(Color.FromRgb(r, g, b)); b2.Freeze(); return b2; }

        private string _status = "En attente";
        private string _location = "Inconnu";
        private bool _isSelected;
        private ImageSource? _thumbnail;

        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string Format { get; set; } = "";
        public string Location { get => _location; set { _location = value; OnPropertyChanged(nameof(Location)); } }
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } }
        public ImageSource? Thumbnail { get => _thumbnail; set { _thumbnail = value; OnPropertyChanged(nameof(Thumbnail)); } }
        
        private int _columnSpan = 1;
        public int ColumnSpan { get => _columnSpan; set { _columnSpan = value; OnPropertyChanged(nameof(ColumnSpan)); } }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusColorBrush));
            }
        }

        public string StatusIcon
        {
            get
            {
                if (Status.Contains("Terminé") || Status.Contains("Succès") || Status.Contains("Prêt")) return "\uE73E";
                if (Status.Contains("Erreur") || Status.Contains("Échec")) return "\uE783";
                if (Status.Contains("cours")) return "\uE895";
                return "\uE916";
            }
        }

        public Brush StatusColorBrush
        {
            get
            {
                if (Status.Contains("Terminé") || Status.Contains("Succès") || Status.Contains("Prêt")) return BrushSuccess;
                if (Status.Contains("Erreur") || Status.Contains("Échec")) return BrushError;
                if (Status.Contains("cours")) return BrushProgress;
                return BrushDefault;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public static class WindowsThumbnailProvider
    {
        private const int SIIGBF_RESIZETOFIT = 0x00000000;
        private const int SIIGBF_MEMORYONLY = 0x00000002;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItemImageFactory ppv);

        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            void GetImage(
                [In, MarshalAs(UnmanagedType.Struct)] SIZE size,
                [In] int flags,
                [Out] out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
            public SIZE(int cx, int cy) { this.cx = cx; this.cy = cy; }
        }

        public static BitmapSource? GetThumbnail(string fileName, int width, int height)
        {
            try
            {
                Guid iidIshellItemImageFactory = new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b");
                SHCreateItemFromParsingName(fileName, IntPtr.Zero, iidIshellItemImageFactory, out IShellItemImageFactory factory);

                factory.GetImage(new SIZE(width, height), SIIGBF_RESIZETOFIT, out IntPtr hBitmap);

                BitmapSource bitmap = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(hBitmap);
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

        private string dossierSource = "";
        private string dossierDestBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "A6700");
        private string dossierSourcePhotos = "";
        private string dossierDestPhotosBase = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "A6700");
        private readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VideoManager", "config.json");

        private TaskCompletionSource<bool>? _tcsOverwrite;
        private TaskCompletionSource<bool>? _tcsDelete;

        private SemaphoreSlim _mediaInfoSemaphore = new SemaphoreSlim(4);
        private SemaphoreSlim _thumbnailSemaphore = new SemaphoreSlim(5);
        private CancellationTokenSource? _refreshVideosCts;
        private CancellationTokenSource? _refreshPhotosCts;

        private GridViewColumnHeader? _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private string _lastSortBy = "";

        private GridViewColumnHeader? _lastHeaderClickedPhotos = null;
        private ListSortDirection _lastDirectionPhotos = ListSortDirection.Ascending;
        private string _lastSortByPhotos = "";


        public ObservableCollection<VideoItem> VideoList { get; set; } = new ObservableCollection<VideoItem>();
        public ICollectionView VideosView { get; set; }

        public ObservableCollection<PhotoItem> PhotoList { get; set; } = new ObservableCollection<PhotoItem>();
        public ICollectionView PhotosView { get; set; }

        private int _totalPhotosSD;
        private int _totalVideosSD;
        private int _totalPhotosPC;
        private int _totalVideosPC;
        private string _shutterCount = "Calcul...";

        public int DashPhotosSD => _totalPhotosSD;
        public int DashPhotosPC => _totalPhotosPC;
        public int DashPhotosTotal => _totalPhotosSD + _totalPhotosPC;
        public int DashVideosSD => _totalVideosSD;
        public int DashVideosPC => _totalVideosPC;
        public int DashVideosTotal => _totalVideosSD + _totalVideosPC;
        public string DashShutterCount { get => _shutterCount; set { _shutterCount = value; OnPropertyChanged(nameof(DashShutterCount)); } }

        public ObservableCollection<object> DashboardItems { get; set; } = new ObservableCollection<object>();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private LibVLC _libVLC;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => File.AppendAllText("crash.log", e.ExceptionObject.ToString() + "\n");
            Application.Current.DispatcherUnhandledException += (s, e) => { File.AppendAllText("crash.log", e.Exception.ToString() + "\n"); e.Handled = true; };
            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            
            InitializeComponent();
            VlcVideoView.MediaPlayer = _mediaPlayer;
            _mediaPlayer.Playing += (s, e) => Dispatcher.InvokeAsync(() => 
            {
                LoadingShimmer.Visibility = Visibility.Collapsed;
                VlcVideoView.Visibility = Visibility.Visible;
            });
            
            LoadSettings();
            TxtDestination.Text = dossierDestBase;
            TxtDestinationPhotos.Text = dossierDestPhotosBase;
            VideosView = CollectionViewSource.GetDefaultView(VideoList);
            VideosView.Filter = FiltrerListe;
            
            PhotosView = CollectionViewSource.GetDefaultView(PhotoList);
            PhotosView.Filter = FiltrerListePhotos;
            
            DataContext = this;
            Dispatcher.InvokeAsync(ActualiserTout, DispatcherPriority.Background);
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        if (!string.IsNullOrEmpty(settings.DossierDestVideos)) dossierDestBase = settings.DossierDestVideos;
                        if (!string.IsNullOrEmpty(settings.DossierDestPhotos)) dossierDestPhotosBase = settings.DossierDestPhotos;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur chargement paramètres : {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings
                {
                    DossierDestVideos = dossierDestBase,
                    DossierDestPhotos = dossierDestPhotosBase
                };
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erreur sauvegarde paramètres : {ex.Message}");
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int attr = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE
            int value = 1;
            DwmSetWindowAttribute(hwnd, attr, ref value, sizeof(int));

            // Bordure de fenêtre personnalisée
            int borderColor = 0x001A1818; // BGR: #18181A
            DwmSetWindowAttribute(hwnd, 34, ref borderColor, sizeof(int));

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(HwndHandler);
        }

        private IntPtr HwndHandler(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (msg == WM_DEVICECHANGE)
            {
                int eventType = wparam.ToInt32();
                if (eventType == DBT_DEVICEARRIVAL || eventType == DBT_DEVICEREMOVECOMPLETE)
                {
                    OverlayModal.Visibility = Visibility.Visible;
                }
            }
            return IntPtr.Zero;
        }

        private string DetecterSD()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Removable)
                {
                    string path = Path.Combine(drive.RootDirectory.FullName, "PRIVATE", "M4ROOT", "CLIP");
                    if (Directory.Exists(path)) return path;
                }
            }
            return "";
        }

        private string DetecterSDPhotos()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.DriveType == DriveType.Removable)
                {
                    string path = Path.Combine(drive.RootDirectory.FullName, "DCIM");
                    if (Directory.Exists(path)) return path;
                }
            }
            return "";
        }

        private async void ActualiserTout()
        {
            _refreshVideosCts?.Cancel();
            _refreshVideosCts = new CancellationTokenSource();
            var tokenVideos = _refreshVideosCts.Token;

            ProgBar.Visibility = Visibility.Visible;
            dossierSource = DetecterSD();
            TxtSource.Text = string.IsNullOrEmpty(dossierSource) ? "Non détectée" : dossierSource;
            BtnEjecter.IsEnabled = !string.IsNullOrEmpty(dossierSource);

            var allVideos = new List<VideoItem>();

            if (!string.IsNullOrEmpty(dossierSource))
            {
                var files = await Task.Run(() => Directory.GetFiles(dossierSource, "*.MP4"));
                foreach (var f in files)
                {
                    if (tokenVideos.IsCancellationRequested) break;
                    allVideos.Add(new VideoItem { FileName = Path.GetFileName(f), FilePath = f, Location = "SD Card" });
                }
            }

            if (Directory.Exists(dossierDestBase))
            {
                var files = await Task.Run(() => Directory.GetFiles(dossierDestBase, "*.MP4", System.IO.SearchOption.AllDirectories));
                foreach (var f in files)
                {
                    if (tokenVideos.IsCancellationRequested) break;
                    bool is420 = f.Contains(@"\420\") || f.Contains("/420/");
                    string loc = is420 ? "Destination / 4:2:0" : "Destination / 4:2:2";
                    string stat = is420 ? "Terminé" : "À convertir";
                    allVideos.Add(new VideoItem { FileName = Path.GetFileName(f), FilePath = f, Location = loc, Status = stat });
                }
            }

            VideoList.Clear();
            foreach (var v in allVideos) VideoList.Add(v);

            // Lancer le chargement des miniatures APRÈS l'ajout groupé
            foreach (var v in VideoList)
                _ = ChargerMediaInfoAsync(v, tokenVideos);

            ProgBar.Visibility = Visibility.Collapsed;
            TxtTotalFooter.Text = $"Total : {VideoList.Count} vidéos";
            BtnTrier.IsEnabled = false;
            BtnConvertir.IsEnabled = false;
            TxtBtnTrier.Text = "Trier vers PC";
            TxtBtnConvertir.Text = "Convertir en 4:2:0";

            ActualiserPhotos();
        }

        private async void ActualiserPhotos()
        {
            _refreshPhotosCts?.Cancel();
            _refreshPhotosCts = new CancellationTokenSource();
            var tokenPhotos = _refreshPhotosCts.Token;

            ProgBarPhotos.Visibility = Visibility.Visible;
            dossierSourcePhotos = DetecterSDPhotos();
            TxtSourcePhotos.Text = string.IsNullOrEmpty(dossierSourcePhotos) ? "Non détectée" : dossierSourcePhotos;
            BtnEjecterPhotos.IsEnabled = !string.IsNullOrEmpty(dossierSourcePhotos);

            var allPhotos = new List<PhotoItem>();

            if (!string.IsNullOrEmpty(dossierSourcePhotos))
            {
                var files = await Task.Run(() => Directory.GetFiles(dossierSourcePhotos, "*.*", System.IO.SearchOption.AllDirectories)
                                     .Where(f => f.EndsWith(".JPG", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".ARW", StringComparison.OrdinalIgnoreCase)).ToList());
                foreach (var f in files)
                {
                    if (tokenPhotos.IsCancellationRequested) break;
                    allPhotos.Add(new PhotoItem { 
                        FileName = Path.GetFileName(f), 
                        FilePath = f, 
                        Location = "SD Card", 
                        Format = Path.GetExtension(f).ToUpper().Replace(".", "") 
                    });
                }
            }

            if (Directory.Exists(dossierDestPhotosBase))
            {
                var files = await Task.Run(() => Directory.GetFiles(dossierDestPhotosBase, "*.*", System.IO.SearchOption.AllDirectories)
                                     .Where(f => f.EndsWith(".JPG", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".ARW", StringComparison.OrdinalIgnoreCase)).ToList());
                foreach (var f in files)
                {
                    if (tokenPhotos.IsCancellationRequested) break;
                    string loc = f.Contains(@"\RAW\") || f.Contains("/RAW/") ? "Destination / RAW" : "Destination / JPEG";
                    allPhotos.Add(new PhotoItem { 
                        FileName = Path.GetFileName(f), 
                        FilePath = f, 
                        Location = loc, 
                        Status = "Terminé",
                        Format = Path.GetExtension(f).ToUpper().Replace(".", "") 
                    });
                }
            }

            PhotoList.Clear();
            foreach (var p in allPhotos) PhotoList.Add(p);

            // Lancer le chargement des miniatures APRÈS l'ajout groupé
            foreach (var p in PhotoList)
                _ = ChargerMiniaturePhotoAsync(p, tokenPhotos);

            ProgBarPhotos.Visibility = Visibility.Collapsed;
            TxtTotalFooterPhotos.Text = $"Total : {PhotoList.Count} photos";
            BtnTrierPhotos.IsEnabled = false;
            TxtBtnTrierPhotos.Text = "Trier vers PC";

            _ = CalculerStatsDashboardAsync();
        }

        private async Task CalculerStatsDashboardAsync()
        {
            _totalVideosSD = VideoList.Count(v => v.Location == "SD Card");
            _totalPhotosSD = PhotoList.Count(p => p.Location == "SD Card");
            _totalVideosPC = VideoList.Count(v => v.Location != "SD Card");
            _totalPhotosPC = PhotoList.Count(p => p.Location != "SD Card");
            OnPropertyChanged(nameof(DashPhotosSD));
            OnPropertyChanged(nameof(DashPhotosPC));
            OnPropertyChanged(nameof(DashPhotosTotal));
            OnPropertyChanged(nameof(DashVideosSD));
            OnPropertyChanged(nameof(DashVideosPC));
            OnPropertyChanged(nameof(DashVideosTotal));

            var random = new Random();
            
            var mixedItems = PhotoList.Cast<object>()
                .Concat(VideoList.Cast<object>())
                .OrderBy(x => random.Next())
                .Take(30)
                .ToList();

            foreach (var item in mixedItems)
            {
                if (item is PhotoItem p) p.ColumnSpan = random.NextDouble() > 0.7 ? 2 : 1;
                if (item is VideoItem v) v.ColumnSpan = random.NextDouble() > 0.7 ? 2 : 1;
            }

            DashboardItems.Clear();
            foreach (var item in mixedItems) DashboardItems.Add(item);

            var firstPhoto = PhotoList
                .OrderByDescending(p => p.FileName)
                .FirstOrDefault(p => p.FilePath.EndsWith(".ARW", StringComparison.OrdinalIgnoreCase)) 
                ?? PhotoList.OrderByDescending(p => p.FileName).FirstOrDefault(p => p.FilePath.EndsWith(".JPG", StringComparison.OrdinalIgnoreCase))
                ?? PhotoList.OrderByDescending(p => p.FileName).FirstOrDefault();
            
            if (firstPhoto != null && File.Exists(Path.Combine(baseDir, "exiftool.exe")))
            {
                DashShutterCount = "Calcul...";
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = Path.Combine(baseDir, "exiftool.exe"),
                        Arguments = $"-ShutterCount -ImageCount -s -s -s \"{firstPhoto.FilePath}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        string output = await proc.StandardOutput.ReadToEndAsync();
                        await proc.WaitForExitAsync();
                        string result = output.Trim().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "";
                        DashShutterCount = !string.IsNullOrEmpty(result) ? result : "Introuvable";
                    }
                }
                catch { DashShutterCount = "Erreur"; }
            }
            else DashShutterCount = "Non dispo";
        }

        private async Task ChargerMediaInfoAsync(VideoItem item, CancellationToken token)
        {
            await _mediaInfoSemaphore.WaitAsync(token);
            try
            {
                string ffprobePath = Path.Combine(baseDir, "ffprobe.exe");
                if (!File.Exists(ffprobePath)) { item.Format = "ffprobe manquant"; return; }

                var startInfo = new ProcessStartInfo
                {
                    FileName = ffprobePath,
                    Arguments = $"-v error -select_streams v:0 -show_entries stream=pix_fmt,duration -of default=noprint_wrappers=1:nokey=1 \"{item.FilePath}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) return;
                string output = await process.StandardOutput.ReadToEndAsync(token);
                await process.WaitForExitAsync(token);

                var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length >= 2)
                {
                    string rawFormat = lines[0].Trim();
                    if (rawFormat.Contains("422")) item.Format = "4:2:2 10bits";
                    else if (rawFormat.Contains("420")) item.Format = "4:2:0 8bits";
                    else item.Format = rawFormat;

                    if (double.TryParse(lines[1].Trim(), CultureInfo.InvariantCulture, out double duration))
                    {
                        var ts = TimeSpan.FromSeconds(duration);
                        item.Duration = ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");
                    }
                }
                
                _ = ChargerMiniatureAsync(item, token);
            }
            catch (Exception ex) { item.Format = "Erreur"; Debug.WriteLine(ex.Message); }
            finally { _mediaInfoSemaphore.Release(); }
        }

        private async Task ChargerMiniatureAsync(VideoItem item, CancellationToken token)
        {
            await _thumbnailSemaphore.WaitAsync(token);
            try
            {
                var bitmap = await Task.Run(() => WindowsThumbnailProvider.GetThumbnail(item.FilePath, 150, 150), token);
                if (bitmap != null)
                {
                    await Dispatcher.InvokeAsync(() => item.Thumbnail = bitmap, DispatcherPriority.Background);
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Thumbnail error: {ex.Message}"); }
            finally { _thumbnailSemaphore.Release(); }
        }

        private async Task ChargerMiniaturePhotoAsync(PhotoItem item, CancellationToken token)
        {
            await _thumbnailSemaphore.WaitAsync(token);
            try
            {
                var bitmap = await Task.Run(() => WindowsThumbnailProvider.GetThumbnail(item.FilePath, 150, 150), token);
                if (bitmap != null)
                {
                    await Dispatcher.InvokeAsync(() => item.Thumbnail = bitmap, DispatcherPriority.Background);
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Thumbnail photo error: {ex.Message}"); }
            finally { _thumbnailSemaphore.Release(); }
        }

        private void BtnModifierDest_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { InitialDirectory = dossierDestBase };
            if (dialog.ShowDialog() == true)
            {
                dossierDestBase = dialog.FolderName;
                TxtDestination.Text = dossierDestBase;
                SaveSettings();
                ActualiserTout();
            }
        }

        private void BtnModifierDestPhotos_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { InitialDirectory = dossierDestPhotosBase };
            if (dialog.ShowDialog() == true)
            {
                dossierDestPhotosBase = dialog.FolderName;
                TxtDestinationPhotos.Text = dossierDestPhotosBase;
                SaveSettings();
                ActualiserPhotos();
            }
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e) => ActualiserTout();
        private void BtnActualiserPhotos_Click(object sender, RoutedEventArgs e) => ActualiserPhotos();

        private async void BtnTrier_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(dossierSource)) return;
            var aTrier = ListVideos.SelectedItems.OfType<VideoItem>().Where(v => v.Location == "SD Card").ToList();
            if (!aTrier.Any()) return;

            BtnTrier.IsEnabled = false;
            ProgBar.Visibility = Visibility.Visible;
            ProgBar.IsIndeterminate = true;
            ProgBar.Value = 0;
            ProgBar.Maximum = aTrier.Count;

            int success = 0;
            foreach (var video in aTrier)
            {
                video.Status = "En cours";
                string destFile = Path.Combine(dossierDestBase, video.FileName);
                
                if (File.Exists(destFile))
                {
                    TxtOverwriteMessage.Text = $"Le fichier {video.FileName} existe déjà. Écraser ?";
                    OverlayOverwrite.Visibility = Visibility.Visible;
                    _tcsOverwrite = new TaskCompletionSource<bool>();
                    bool overwrite = await _tcsOverwrite.Task;
                    OverlayOverwrite.Visibility = Visibility.Collapsed;
                    if (!overwrite) { video.Status = "Ignoré"; continue; }
                }

                try
                {
                    await Task.Run(() => File.Copy(video.FilePath, destFile, true));
                    string xmlSource = Path.ChangeExtension(video.FilePath, ".XML");
                    if (File.Exists(xmlSource)) File.Copy(xmlSource, Path.ChangeExtension(destFile, ".XML"), true);

                    if (ChkKeepOnSD.IsChecked != true)
                    {
                        video.Status = "Suppression SD...";
                        await Task.Run(() => File.Delete(video.FilePath));
                        if (File.Exists(xmlSource)) File.Delete(xmlSource);
                    }
                    video.Status = "À convertir";
                    video.Location = "Destination / 4:2:2";
                    video.FilePath = destFile;
                    success++;
                }
                catch (Exception ex) { video.Status = "Erreur : " + ex.Message; }
                ProgBar.Value++;
            }

            ProgBar.Visibility = Visibility.Collapsed;
            BtnTrier.IsEnabled = true;
            ActualiserTout();
        }

        private async void BtnTrierPhotos_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(dossierSourcePhotos)) return;
            var aTrier = ListPhotos.SelectedItems.OfType<PhotoItem>().Where(p => p.Location == "SD Card").ToList();
            if (!aTrier.Any()) return;

            BtnTrierPhotos.IsEnabled = false;
            ProgBarPhotos.Visibility = Visibility.Visible;
            ProgBarPhotos.IsIndeterminate = true;
            ProgBarPhotos.Value = 0;
            ProgBarPhotos.Maximum = aTrier.Count;

            foreach (var photo in aTrier)
            {
                photo.Status = "En cours";
                string subFolder = photo.Format == "ARW" ? "RAW" : "JPEG";
                string destDir = Path.Combine(dossierDestPhotosBase, subFolder);
                Directory.CreateDirectory(destDir);
                string destFile = Path.Combine(destDir, photo.FileName);

                if (File.Exists(destFile))
                {
                    TxtOverwriteMessage.Text = $"Le fichier {photo.FileName} existe déjà. Écraser ?";
                    OverlayOverwrite.Visibility = Visibility.Visible;
                    _tcsOverwrite = new TaskCompletionSource<bool>();
                    bool overwrite = await _tcsOverwrite.Task;
                    OverlayOverwrite.Visibility = Visibility.Collapsed;
                    if (!overwrite) { photo.Status = "Ignoré"; continue; }
                }

                try
                {
                    await Task.Run(() => File.Copy(photo.FilePath, destFile, true));
                    if (ChkKeepOnSDPhotos.IsChecked != true) await Task.Run(() => File.Delete(photo.FilePath));
                    photo.Status = "Terminé";
                    photo.Location = subFolder == "RAW" ? "Destination / RAW" : "Destination / JPEG";
                    photo.FilePath = destFile;
                }
                catch (Exception ex) { photo.Status = "Erreur : " + ex.Message; }
                ProgBarPhotos.Value++;
            }

            ProgBarPhotos.Visibility = Visibility.Collapsed;
            BtnTrierPhotos.IsEnabled = true;
            ActualiserPhotos();
        }

        private async void BtnConvertir_Click(object sender, RoutedEventArgs e)
        {
            var aConvertir = ListVideos.SelectedItems.OfType<VideoItem>().Where(v => v.Format.Contains("4:2:2")).ToList();
            if (!aConvertir.Any()) { MessageBox.Show("Sélectionnez des vidéos 4:2:2 (10-bits) à convertir."); return; }

            BtnConvertir.IsEnabled = false;
            ProgBar.Visibility = Visibility.Visible;
            ProgBar.IsIndeterminate = true;
            ProgBar.Value = 0;
            ProgBar.Maximum = aConvertir.Count;

            string hbPath = Path.Combine(baseDir, "HandBrakeCLI.exe");
            string presetPath = Path.Combine(baseDir, "Conversion420.json");

            foreach (var video in aConvertir)
            {
                video.Status = "Conversion";
                string dir420 = Path.Combine(Path.GetDirectoryName(video.FilePath)!, "420");
                Directory.CreateDirectory(dir420);
                string output = Path.Combine(dir420, Path.GetFileName(video.FilePath));

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = hbPath,
                        Arguments = $"-i \"{video.FilePath}\" -o \"{output}\" --preset-import-file \"{presetPath}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var process = Process.Start(startInfo);
                    if (process != null) await process.WaitForExitAsync();

                    if (ChkKeepSource422.IsChecked != true)
                    {
                        FileSystem.DeleteFile(video.FilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        string xml = Path.ChangeExtension(video.FilePath, ".XML");
                        if (File.Exists(xml)) FileSystem.DeleteFile(xml, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }
                    video.Status = "Terminé";
                    video.Location = "Destination / 4:2:0";
                }
                catch (Exception ex) { video.Status = "Échec conversion"; Debug.WriteLine(ex.Message); }
                ProgBar.Value++;
            }
            ProgBar.Visibility = Visibility.Collapsed;
            BtnConvertir.IsEnabled = true;
            ActualiserTout();
        }



        private void BtnEjecter_Click(object sender, RoutedEventArgs e)
        {
            string driveRoot = Path.GetPathRoot(dossierSource) ?? "";
            if (string.IsNullOrEmpty(driveRoot)) return;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"$driveEject = New-Object -ComObject Shell.Application; $driveEject.Namespace(17).ParseName('{driveRoot.TrimEnd('\\')}').InvokeVerb('Eject')\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi)?.WaitForExit();
                OverlayEjectSuccess.Visibility = Visibility.Visible;
                ActualiserTout();
            }
            catch (Exception ex) { MessageBox.Show("Erreur éjection : " + ex.Message); }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnMaximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void WindowTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ClickCount == 2) BtnMaximize_Click(sender, e); else DragMove(); }

        private void MenuDashboard_Checked(object sender, RoutedEventArgs e) { if (ViewDashboard != null) ViewDashboard.Visibility = Visibility.Visible; if (ViewVideos != null) ViewVideos.Visibility = Visibility.Collapsed; if (ViewPhotos != null) ViewPhotos.Visibility = Visibility.Collapsed; }
        private void MenuVideos_Checked(object sender, RoutedEventArgs e) { if (ViewDashboard != null) ViewDashboard.Visibility = Visibility.Collapsed; if (ViewVideos != null) ViewVideos.Visibility = Visibility.Visible; if (ViewPhotos != null) ViewPhotos.Visibility = Visibility.Collapsed; }
        private void MenuPhotos_Checked(object sender, RoutedEventArgs e) { if (ViewDashboard != null) ViewDashboard.Visibility = Visibility.Collapsed; if (ViewVideos != null) ViewVideos.Visibility = Visibility.Collapsed; if (ViewPhotos != null) ViewPhotos.Visibility = Visibility.Visible; }

        private void ListVideos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int count = ListVideos.SelectedItems.Count;
            TxtSelectedCount.Text = count > 0 ? $"({count} sélectionné{(count > 1 ? "s" : "")})" : "";

            var selectedVideos = ListVideos.SelectedItems.OfType<VideoItem>().ToList();
            
            var sdSelected = selectedVideos.Where(v => v.Location == "SD Card").ToList();
            BtnTrier.IsEnabled = sdSelected.Any();
            TxtBtnTrier.Text = sdSelected.Any() ? $"Trier vers PC ({sdSelected.Count})" : "Trier vers PC";

            var convertSelected = selectedVideos.Where(v => v.Format.Contains("4:2:2")).ToList();
            BtnConvertir.IsEnabled = convertSelected.Any();
            TxtBtnConvertir.Text = convertSelected.Any() ? $"Convertir en 4:2:0 ({convertSelected.Count})" : "Convertir en 4:2:0";
        }

        private void ListPhotos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int count = ListPhotos.SelectedItems.Count;
            TxtSelectedCountPhotos.Text = count > 0 ? $"({count} sélectionné{(count > 1 ? "s" : "")})" : "";

            var sdSelected = ListPhotos.SelectedItems.OfType<PhotoItem>().Where(p => p.Location == "SD Card").ToList();
            BtnTrierPhotos.IsEnabled = sdSelected.Any();
            TxtBtnTrierPhotos.Text = sdSelected.Any() ? $"Trier vers PC ({sdSelected.Count})" : "Trier vers PC";
        }

        private bool FiltrerListe(object obj)
        {
            if (obj is VideoItem item)
            {
                if (ChkFiltreSD?.IsChecked == true && item.Location != "SD Card") return false;
                return true;
            }
            return false;
        }

        private bool FiltrerListePhotos(object obj)
        {
            if (obj is PhotoItem item)
            {
                if (ChkFiltreSDPhotos?.IsChecked == true && item.Location != "SD Card") return false;
                return true;
            }
            return false;
        }

        private void ChkFiltreSD_Click(object sender, RoutedEventArgs e) => VideosView.Refresh();
        private void ChkFiltreSDPhotos_Click(object sender, RoutedEventArgs e) => PhotosView.Refresh();

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is GridViewColumnHeader header && header.Tag != null)
            {
                if (_lastHeaderClicked != null && _lastHeaderClicked != header)
                    _lastHeaderClicked.Content = _lastHeaderClicked.Content.ToString()!.TrimEnd(' ', '▲', '▼');

                string sortBy = header.Tag.ToString()!;
                if (_lastSortBy == sortBy) _lastDirection = _lastDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                else { _lastSortBy = sortBy; _lastDirection = ListSortDirection.Ascending; }
                
                string baseContent = header.Content.ToString()!.TrimEnd(' ', '▲', '▼');
                header.Content = baseContent + (_lastDirection == ListSortDirection.Ascending ? " ▲" : " ▼");
                _lastHeaderClicked = header;

                VideosView.SortDescriptions.Clear();
                VideosView.SortDescriptions.Add(new SortDescription(sortBy, _lastDirection));
            }
        }

        private void GridViewColumnHeaderPhotos_Click(object sender, RoutedEventArgs e)
        {
            if (sender is GridViewColumnHeader header && header.Tag != null)
            {
                if (_lastHeaderClickedPhotos != null && _lastHeaderClickedPhotos != header)
                    _lastHeaderClickedPhotos.Content = _lastHeaderClickedPhotos.Content.ToString()!.TrimEnd(' ', '▲', '▼');

                string sortBy = header.Tag.ToString()!;
                if (_lastSortByPhotos == sortBy) _lastDirectionPhotos = _lastDirectionPhotos == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                else { _lastSortByPhotos = sortBy; _lastDirectionPhotos = ListSortDirection.Ascending; }
                
                string baseContent = header.Content.ToString()!.TrimEnd(' ', '▲', '▼');
                header.Content = baseContent + (_lastDirectionPhotos == ListSortDirection.Ascending ? " ▲" : " ▼");
                _lastHeaderClickedPhotos = header;

                PhotosView.SortDescriptions.Clear();
                PhotosView.SortDescriptions.Add(new SortDescription(sortBy, _lastDirectionPhotos));
            }
        }

        private void BtnModalOui_Click(object sender, RoutedEventArgs e) { OverlayModal.Visibility = Visibility.Collapsed; ActualiserTout(); }
        private void BtnModalNon_Click(object sender, RoutedEventArgs e) => OverlayModal.Visibility = Visibility.Collapsed;
        private void BtnOverwriteOui_Click(object sender, RoutedEventArgs e) => _tcsOverwrite?.SetResult(true);
        private void BtnOverwriteNon_Click(object sender, RoutedEventArgs e) => _tcsOverwrite?.SetResult(false);
        private void BtnCloseEject_Click(object sender, RoutedEventArgs e) => OverlayEjectSuccess.Visibility = Visibility.Collapsed;

        private async void MenuSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is VideoItem item)
            {
                var selectedItems = ListVideos.SelectedItems.OfType<VideoItem>().ToList();
                var itemsToDelete = selectedItems.Contains(item) ? selectedItems : new List<VideoItem> { item };

                TxtDeleteMessage.Text = itemsToDelete.Count > 1 
                    ? $"Voulez-vous mettre {itemsToDelete.Count} éléments à la corbeille ?" 
                    : $"Voulez-vous mettre {item.FileName} à la corbeille ?";
                
                OverlayDelete.Visibility = Visibility.Visible;
                _tcsDelete = new TaskCompletionSource<bool>();
                if (await _tcsDelete.Task)
                {
                    try
                    {
                        foreach (var v in itemsToDelete)
                        {
                            FileSystem.DeleteFile(v.FilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                            string xml = Path.ChangeExtension(v.FilePath, ".XML");
                            if (File.Exists(xml)) FileSystem.DeleteFile(xml, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        }
                        ActualiserTout();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message); }
                }
                OverlayDelete.Visibility = Visibility.Collapsed;
            }
        }

        private async void MenuSupprimerPhoto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PhotoItem item)
            {
                var selectedItems = ListPhotos.SelectedItems.OfType<PhotoItem>().ToList();
                var itemsToDelete = selectedItems.Contains(item) ? selectedItems : new List<PhotoItem> { item };

                TxtDeleteMessage.Text = itemsToDelete.Count > 1 
                    ? $"Voulez-vous mettre {itemsToDelete.Count} éléments à la corbeille ?" 
                    : $"Voulez-vous mettre {item.FileName} à la corbeille ?";

                OverlayDelete.Visibility = Visibility.Visible;
                _tcsDelete = new TaskCompletionSource<bool>();
                if (await _tcsDelete.Task)
                {
                    try
                    {
                        foreach (var p in itemsToDelete)
                        {
                            FileSystem.DeleteFile(p.FilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                        }
                        ActualiserPhotos();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message); }
                }
                OverlayDelete.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnDeleteOui_Click(object sender, RoutedEventArgs e) => _tcsDelete?.SetResult(true);
        private void BtnDeleteNon_Click(object sender, RoutedEventArgs e) => _tcsDelete?.SetResult(false);

        private void ListPhotos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListPhotos.SelectedItem is PhotoItem photo) ShowFullImage(photo.FilePath);
        }

        private void Thumbnail_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is PhotoItem photo) ShowFullImage(photo.FilePath);
        }

        private void DashboardItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid grid)
            {
                if (grid.DataContext is PhotoItem photo) ShowFullImage(photo.FilePath);
                else if (grid.DataContext is VideoItem video) ShowVideo(video.FilePath);
            }
        }

        private async void ShowFullImage(string filePath)
        {
            if (!File.Exists(filePath)) return;
            FullSizeImage.Source = null;
            FullSizeImage.Visibility = Visibility.Visible;
            VlcVideoView.Visibility = Visibility.Collapsed;
            LoadingShimmer.Visibility = Visibility.Visible;
            OverlayMedia.Visibility = Visibility.Visible;

            try
            {
                var bitmap = await Task.Run(() => {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.UriSource = new Uri(filePath);
                    img.DecodePixelWidth = (int)SystemParameters.PrimaryScreenWidth;
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    img.Freeze();
                    return img;
                });
                FullSizeImage.Source = bitmap;
                LoadingShimmer.Visibility = Visibility.Collapsed;
            }
            catch { LoadingShimmer.Visibility = Visibility.Collapsed; }
        }

        private void BtnCloseMedia_Click(object sender, RoutedEventArgs e) 
        { 
            if (_mediaPlayer.IsPlaying) _mediaPlayer.Stop();
            OverlayMedia.Visibility = Visibility.Collapsed; 
        }

        private void OverlayMedia_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) 
        { 
            if (_mediaPlayer.IsPlaying) _mediaPlayer.Stop();
            OverlayMedia.Visibility = Visibility.Collapsed; 
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && OverlayMedia.Visibility == Visibility.Visible)
            {
                if (_mediaPlayer.IsPlaying) _mediaPlayer.Stop();
                OverlayMedia.Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
        }

        private void ListVideos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListVideos.SelectedItem is VideoItem video) ShowVideo(video.FilePath);
        }

        private void VideoThumbnail_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is VideoItem video) ShowVideo(video.FilePath);
        }

        private void ShowVideo(string filePath)
        {
            if (!File.Exists(filePath)) return;
            FullSizeImage.Visibility = Visibility.Collapsed;
            VlcVideoView.Visibility = Visibility.Collapsed;
            LoadingShimmer.Visibility = Visibility.Visible;
            OverlayMedia.Visibility = Visibility.Visible;

            var media = new Media(_libVLC, new Uri(filePath));
            _mediaPlayer.Play(media);
        }
    }

    public class AppSettings
    {
        public string DossierDestVideos { get; set; } = "";
        public string DossierDestPhotos { get; set; } = "";
    }
}
