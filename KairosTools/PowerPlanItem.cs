using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KairosTools
{
    public class PowerPlanItem : INotifyPropertyChanged
    {
        public string Guid { get; set; }
        public string Name { get; set; }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}