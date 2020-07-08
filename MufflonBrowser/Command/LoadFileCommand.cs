using Microsoft.Win32;
using MufflonBrowser.IO;
using MufflonBrowser.Model;
using MufflonBrowser.ViewModel;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MufflonBrowser.Commands
{
    public class LoadFileCommand : ICommand
    {
        private readonly Models models;
        private readonly LoadingBarViewModel loader;

        public LoadFileCommand(Models models, LoadingBarViewModel loader)
        {
            this.models = models;
            this.loader = loader;
            loader.PropertyChanged += OnLoaderChanged;
        }

        private void OnLoaderChanged(object sender, PropertyChangedEventArgs args)
        {
            if (sender == loader && args.PropertyName == nameof(ViewModels.LoadingBar.Active))
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return !loader.Active;
        }

        // Updates the loader progress
        private void UpdateProgress(string str, bool indeterminate, int current, int max)
        {
            if (str != null)
                loader.Text = "Loading file... (" + str + ")";
            loader.Value = current;
            loader.Maximum = max;
            loader.IsIndeterminate = indeterminate;
        }

        public async void Execute(object parameter)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Mufflon Binary Files (*.mff)|*.mff"
            };

            if (dialog.ShowDialog() == true)
            {
                loader.Active = true;
                // Perform the load asynchroneously, but make sure to replace the model in the GUI thread
                MufflonFileModel newModel = null;
                await Task.Run(() => newModel = MufflonFileImporter.LoadFile(dialog.FileName, UpdateProgress) );
                models.MufflonFile = newModel;
                loader.Active = false;
            }

        }
    }
}
