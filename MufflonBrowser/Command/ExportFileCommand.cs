using Microsoft.Win32;
using MufflonBrowser.IO;
using MufflonBrowser.Model;
using MufflonBrowser.ViewModel;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MufflonBrowser.Commands
{
    public class ExportFileCommand : ICommand
    {
        private readonly Models models;
        private readonly LoadingBarViewModel loader;

        public ExportFileCommand(Models models, LoadingBarViewModel loader)
        {
            this.models = models;
            this.loader = loader;
            this.models.PropertyChanged += OnModelChanged;
            this.loader.PropertyChanged += OnLoaderChanged;
        }

        private void OnModelChanged(object sender, PropertyChangedEventArgs args)
        {
            if(sender == models && args.PropertyName == nameof(Models.MufflonFile))
                 CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        private void OnLoaderChanged(object sender, PropertyChangedEventArgs args)
        {
            if (sender == loader && args.PropertyName == nameof(ViewModels.LoadingBar.Active))
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return models.MufflonFile != null && !loader.Active;
        }

        // Update the loading bar progress
        private void UpdateProgress(string str, bool indeterminate, int current, int max)
        {
            if (str != null)
                loader.Text = "Exporting file... (" + str + ")";
            loader.Value = current;
            loader.Maximum = max;
            loader.IsIndeterminate = indeterminate;
        }

        public async void Execute(object parameter)
        {
            var loadedFileName = Path.GetFileNameWithoutExtension(this.models.MufflonFile.FilePath);
            var suggestedExportName = loadedFileName + "_exported.mff";

            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = "Mufflon Binary Files (*.mff)|*.mff",
                FileName = suggestedExportName
            };
            if (dialog.ShowDialog() == true)
            {
                loader.Active = true;
                // Perform the export asynchroneously
                await Task.Run(() =>
                {
                    MufflonFileExporter.ExportFile(dialog.FileName, this.models.MufflonFile, UpdateProgress);
                });
                loader.Active = false;
            }

        }
    }
}
