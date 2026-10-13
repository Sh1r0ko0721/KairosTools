using System.Windows;

namespace KairosTools
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppSettings.Instance.Load();
            base.OnStartup(e);
        }
    }
}