using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KairosTools
{
    public class BasicItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string RecommendText { get; set; }
        public bool Recommended { get; set; }
        public bool NeedRestart { get; set; } = false;

        private bool _isOn;
        public bool IsOn
        {
            get => _isOn;
            set
            {
                if (_isOn == value) return;
                _isOn = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOnRecommended));
            }
        }

        public bool IsOnRecommended => IsOn == Recommended;

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        // ★ 弱PC模式：每个 item 单独一份，用于 XAML 里直接绑定
        private bool _isWeakPcMode;
        public bool IsWeakPcMode
        {
            get => _isWeakPcMode;
            set { _isWeakPcMode = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}