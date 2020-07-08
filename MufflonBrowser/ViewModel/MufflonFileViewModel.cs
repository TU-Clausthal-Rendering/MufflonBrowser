using JetBrains.Annotations;
using MufflonBrowser.Model;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MufflonBrowser.ViewModel
{
    public class MufflonFileViewModel : INotifyPropertyChanged
    {
        private Models models;

        public String FilePath { get => models.MufflonFile.FilePath; }

        public MufflonFileViewModel(Models models)
        {
            this.models = models;
            this.models.PropertyChanged += OnModelChanged;
        }

        private void OnModelChanged(object sender, PropertyChangedEventArgs args)
        {
            if (sender == models && args.PropertyName == nameof(Models.MufflonFile))
                OnPropertyChanged(nameof(FilePath));
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
