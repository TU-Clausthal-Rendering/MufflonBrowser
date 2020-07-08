using JetBrains.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MufflonBrowser.ViewModel
{
    /**
     * This class represents the interaction with a loading bar which indicates app progression.
     */
    public class LoadingBarViewModel : INotifyPropertyChanged
    {
        private bool active = false;
        private string text = null;
        private int val = 1;
        private int max = 1;
        private bool indeterminate = true;

        // Indicates if the loading bar should be active or not
        public bool Active
        {
            get => active;
            set
            {
                if (value == active) return;
                active = value;
                OnPropertyChanged(nameof(Active));
            }
        }
        // Status text to be displayed alongside the loading bar
        public string Text
        {
            get => text;
            set
            {
                if (value == text) return;
                text = value;
                OnPropertyChanged(nameof(Text));
            }
        }
        public Visibility Visible { get => Active ? Visibility.Visible : Visibility.Hidden; }

        // Current value (relative to Minimum and Maximum, first is fixed to 1)
        public int Value
        {
            get => val;
            set
            {
                if (value == val) return;
                val = value;
                OnPropertyChanged(nameof(Value));
            }
        }
        public int Maximum
        {
            get => max;
            set
            {
                if (value == max) return;
                max = value;
                OnPropertyChanged(nameof(Maximum));
            }
        }
        // Some operations cannot progressively update their progress, for these
        // we can switch the loading bar to indeterminate style
        public bool IsIndeterminate
        {
            get => indeterminate;
            set
            {
                if (value == indeterminate) return;
                indeterminate = value;
                OnPropertyChanged(nameof(IsIndeterminate));
            }
        }

        public LoadingBarViewModel()
        {
            PropertyChanged += OnActiveChanged;
        }

        private void OnActiveChanged(object sender, PropertyChangedEventArgs args)
        {
            // If activity status changes, the visibility changes as well
            if (sender == this && args.PropertyName == nameof(Active))
                OnPropertyChanged(nameof(Visible));
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
