MufflonBrowser\
Content browser for the Mufflon Binary File format.
=

[Mufflon](https://github.com/TU-Clausthal-Rendering/Mufflon) is a scientific renderer with its own file format and [Blender exporter and plugin](https://github.com/TU-Clausthal-Rendering/MufflonExporter) (works for 2.82). Since these files can get large, a browser to inspect and modify the contents of them without having to re-export is desirable.

The currently supported operations with the Mufflon Browser are
* Display of all objects, LoDs, and materials (only meta-information, not actual 3D preview)
* Filtering of objects based on name, instance count, and animation keyframe
* Re-exporting the file with selected objects removed