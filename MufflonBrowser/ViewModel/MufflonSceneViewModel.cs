using JetBrains.Annotations;
using MufflonBrowser.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Data;

namespace MufflonBrowser.ViewModel
{
    public class MufflonSceneViewModel : INotifyPropertyChanged
    {
        private readonly Models models;
        private readonly CollectionViewSource filteredObjects;
        private MufflonObjectModel selectedObject;
        private MufflonLodModel selectedLod = null;
        private bool? retainObjects = true;
        private string filter = null;
        private string caseInvariantFilter = null;
        private string instanceCountMinStr = "";
        private string instanceCountMaxStr = "";
        private int instanceCountMin = int.MinValue;
        private int instanceCountMax = int.MaxValue;
        private string keyframeMinStr = "";
        private string keyframeMaxStr = "";
        private uint keyframeMin = uint.MinValue;
        private uint keyframeMax = uint.MaxValue;

        public IReadOnlyList<string> Materials { get => models.MufflonFile?.Scene?.Materials; }
        public ICollectionView FilteredObjects { get => filteredObjects.View; }
        public List<MufflonLodModel> SelectedObjectLods { get => SelectedObject?.Lods; }
        public MufflonObjectModel SelectedObject
        {
            get => selectedObject;
            set
            {
                if (value == selectedObject) return;
                selectedObject = value;
                OnPropertyChanged(nameof(SelectedObject));
            }
        }
        public MufflonLodModel SelectedLod
        {
            get => selectedLod;
            set
            {
                if (value == selectedLod) return;
                selectedLod = value;
                OnPropertyChanged(nameof(SelectedLod));
            }
        }

        public bool? RetainObjects
        {
            get => retainObjects;
            set
            {
                if (value == retainObjects) return;
                retainObjects = value;
                OnPropertyChanged(nameof(RetainObjects));
            }
        }

        // TODO: the filter things should probably be moved to a separate view model
        public string Filter
        {
            get => filter;
            set
            {
                if (value == filter) return;
                filter = value;
                OnPropertyChanged(nameof(Filter));
            }
        }
        public string InstanceCountMin
        {
            get => instanceCountMinStr;
            set
            {
                if (value == instanceCountMinStr) return;
                instanceCountMinStr = value;
                OnPropertyChanged(nameof(InstanceCountMin));
            }
        }
        public string InstanceCountMax
        {
            get => instanceCountMaxStr;
            set
            {
                if (value == instanceCountMaxStr) return;
                instanceCountMaxStr = value;
                OnPropertyChanged(nameof(InstanceCountMax));
            }
        }
        public string KeyframeMin
        {
            get => keyframeMinStr;
            set
            {
                if (value == keyframeMinStr) return;
                keyframeMinStr = value;
                OnPropertyChanged(nameof(KeyframeMin));
            }
        }
        public string KeyframeMax
        {
            get => keyframeMaxStr;
            set
            {
                if (value == keyframeMaxStr) return;
                keyframeMaxStr = value;
                OnPropertyChanged(nameof(KeyframeMax));
            }
        }

        public MufflonSceneViewModel(Models models)
        {
            this.models = models;
            filteredObjects = new CollectionViewSource();
            filteredObjects.Filter += OnFilterObjects;
            models.PropertyChanged += OnModelLoaded;
            this.PropertyChanged += OnViewModelChanged;
        }

        private void OnModelLoaded(object sender, PropertyChangedEventArgs args)
        {
            if (sender == models && args.PropertyName == nameof(Models.MufflonFile))
            {
                // A change in model triggers a lot of other changes to the display data
                filteredObjects.Source = models.MufflonFile?.Scene?.Objects;
                OnPropertyChanged(nameof(Materials));
                OnPropertyChanged(nameof(FilteredObjects));
                // Select the first object
                if (models.MufflonFile?.Scene?.Objects != null)
                    SelectedObject = models.MufflonFile.Scene.Objects[0];
            }
        }

        private void OnViewModelChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case nameof(SelectedObject):
                    OnPropertyChanged(nameof(SelectedObjectLods));
                    // Also select the first LoD
                    SelectedLod = SelectedObject?.Lods[0];
                    break;
                case nameof(RetainObjects):
                    if (RetainObjects != null && FilteredObjects != null)
                    {
                        // Set the retain status of all filtered objects (make sure to unregister the handler to avoid immediate re-trigger)
                        foreach (var obj in FilteredObjects)
                        {
                            var o = obj as MufflonObjectModel;
                            o.PropertyChanged -= OnObjectRetainChanged;
                            o.Retain = (bool)RetainObjects;
                            o.PropertyChanged += OnObjectRetainChanged;
                        }
                    }
                    break;
                case nameof(Filter):
                case nameof(InstanceCountMin):
                case nameof(InstanceCountMax):
                case nameof(KeyframeMin):
                case nameof(KeyframeMax):
                    ApplyObjectFilter();
                    break;
                case nameof(FilteredObjects):
                    RetainObjects = GetFilteredObjectRetainStatus();
                    break;
            }
        }

        private void OnObjectRetainChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(MufflonObjectModel.Retain))
            {
                // There are some special cases where we could avoid scanning all filtered objects
                // to determine the new RetainObjects status, but since all of them require only
                // a single object be present the logic is not worth it
                RetainObjects = GetFilteredObjectRetainStatus();
            }
        }

        /**
         * Checks all currently filtered objects for their retain status.
         * Return true/false if all/none are marked as retain, null otherwise.
         */
        private bool? GetFilteredObjectRetainStatus()
        {
            if (FilteredObjects == null)
                return false;
            // Scan all
            bool allSelected = true;
            bool noneSelected = true;
            foreach (var obj in FilteredObjects)
            {
                if (obj != null)
                {
                    var o = obj as MufflonObjectModel;
                    allSelected &= o.Retain;
                    noneSelected &= !o.Retain;
                }
            }
            if (allSelected)
                return true;
            else if (noneSelected)
                return false;
            else
                return null;
        }

        /**
         * (Re-)filters the objects to match the filter settings.
         * To avoid issues with thread ownership we call the Application dispatcher.
         */
        private void ApplyObjectFilter()
        {
            // Update the filter criteria
            caseInvariantFilter = Filter.ToLowerInvariant();
            if (!int.TryParse(instanceCountMinStr, out instanceCountMin))
                instanceCountMin = int.MinValue;
            if (!int.TryParse(instanceCountMaxStr, out instanceCountMax))
                instanceCountMax = int.MaxValue;
            if (!uint.TryParse(keyframeMinStr, out keyframeMin))
                keyframeMin = uint.MinValue;
            if (!uint.TryParse(keyframeMaxStr, out keyframeMax))
                keyframeMax = uint.MaxValue;

            FilteredObjects.Refresh();
            OnPropertyChanged(nameof(FilteredObjects));
        }

        /**
         * Filter function for the objects.
         * Objects have to match all criteria (name, instance count, frame range) to be let through.
         */
        private void OnFilterObjects(object sender, FilterEventArgs args)
        {
            var obj = args.Item as MufflonObjectModel;
            if (Filter == null || Filter.Length == 0)
                args.Accepted = true;
            else
                args.Accepted = obj.Name.IndexOf(caseInvariantFilter, StringComparison.OrdinalIgnoreCase) >= 0;

            args.Accepted &= obj.InstanceCount >= instanceCountMin && obj.InstanceCount <= instanceCountMax;
            args.Accepted &= obj.Keyframe >= keyframeMin && obj.Keyframe <= keyframeMax;
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
