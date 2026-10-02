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

        // ★ 是否需要重启才能生效
        public bool NeedRestart { get; set; } = false;

        private bool _isOn;
        public bool IsOn
        {
            get => _isOn;
            set
            {
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}