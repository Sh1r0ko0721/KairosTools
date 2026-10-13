using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace KairosTools
{
    public class AppSettings : INotifyPropertyChanged
    {
        public static AppSettings Instance { get; } = new AppSettings();

        private static readonly string FolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KairosTools");
        private static readonly string FilePath = Path.Combine(FolderPath, "settings.txt");

        private AppSettings() { }

        private bool _isWeakPcMode;
        public bool IsWeakPcMode
        {
            get => _isWeakPcMode;
            set
            {
                if (_isWeakPcMode == value) return;
                _isWeakPcMode = value;
                OnPropertyChanged();
                Save();
            }
        }

        public void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    _isWeakPcMode = File.ReadAllText(FilePath).Trim() == "1";
                    OnPropertyChanged(nameof(IsWeakPcMode));
                }
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(FolderPath);
                File.WriteAllText(FilePath, _isWeakPcMode ? "1" : "0");
            }
            catch { }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}