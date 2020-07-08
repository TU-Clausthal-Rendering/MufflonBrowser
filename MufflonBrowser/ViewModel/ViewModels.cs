using JetBrains.Annotations;
using MufflonBrowser.Commands;
using MufflonBrowser.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MufflonBrowser.ViewModel
{

    public class ViewModels
    {
        public MufflonSceneViewModel MufflonScene { get; }
        public LoadingBarViewModel LoadingBar { get; }
        public LoadFileCommand LoadFile { get; }
        public ExportFileCommand ExportFile { get; }

        public ViewModels(Models models)
        {
            MufflonScene = new MufflonSceneViewModel(models);
            LoadingBar = new LoadingBarViewModel();
            LoadFile = new LoadFileCommand(models, LoadingBar);
            ExportFile = new ExportFileCommand(models, LoadingBar);
        }
    }
}
