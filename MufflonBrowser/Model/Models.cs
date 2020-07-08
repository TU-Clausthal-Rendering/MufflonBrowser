using JetBrains.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MufflonBrowser.Model
{
    public class Models : INotifyPropertyChanged
    {
        private MufflonFileModel mufflonFile = null;

        public MufflonFileModel MufflonFile
        {
            get => mufflonFile;
            set
            {
                if (value == mufflonFile) return;
                mufflonFile = value;
                OnPropertyChanged(nameof(MufflonFile));
            }
        }

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
