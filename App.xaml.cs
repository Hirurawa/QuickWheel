using System.Windows;
using QuickWheel.Interfaces;
using QuickWheel.Services;
using QuickWheel.ViewModels;

namespace QuickWheel
{
    public partial class App : Application
    {
        private ILogger _logger;
        private IInputService _inputService;
        private ISettingsService _settingsService;
        private MainViewModel _mainViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ensure app keeps running even when the main window is hidden
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Composition Root
            _logger = new FileLogger();
            _inputService = new GlobalInputService();
            _settingsService = new JsonSettingsService(_logger);
            var actionFactory = new ActionFactory();
            var inputSender = new InputSender();

            _mainViewModel = new MainViewModel(_logger, _inputService, _settingsService, actionFactory, inputSender);
            _mainViewModel.Initialize();

            var window = new MainWindow();
            window.DataContext = _mainViewModel;

            // Do NOT call window.Show() here.
            // The MainWindow constructor handles the "Warmup" (Show/Hide) trick,
            // and the VM handles showing it on Input events.
        }
    }
}
