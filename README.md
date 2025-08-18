# SatisfactorySaveNet
SatisfactorySaveNet is a fully managed C# library for reading and writing save files for the game [Satisfactory](https://www.satisfactorygame.com/) by Coffee Stain Studios.
This component provides type-safe access to all data of the `.sav` file type and exposes injectable readers that can be replaced or wrapped.
You can integrate it using [nuget.org](https://www.nuget.org/packages/SatisfactorySaveNet/).

Further, (external and promising looking) documentation of the save game format is available [here](https://github.com/moritz-h/satisfactory-3d-map/blob/master/docs/SATISFACTORY_SAVE.md). **Link fixed now sorry**

## Version compatibility
The library supports save headers up to `SaveHeaderVersion.VersionPlusOne - 1` and save versions from `FSaveCustomVersion.DROPPED_WireSpanFromConnnectionComponents` through `FSaveCustomVersion.VersionPlusOne - 1`. Newer or older saves outside of this range will result in a `NotSupportedException` during deserialization.

## How to use
```CSharp
ISaveFileSerializer serializer = SaveFileSerializer.Instance;
var saveGame = serializer.Deserialize(@"C:\\mySaveFile.sav");
serializer.Serialize(saveGame, @"C:\\myNewSaveFile.sav");
```

## Injection example
```CSharp
ISaveFileSerializer Instance = new SaveFileSerializer(HeaderSerializer.Instance, ChunkSerializer.Instance, BodySerializer.Instance);
```
