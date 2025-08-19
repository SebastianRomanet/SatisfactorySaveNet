# SatisfactorySaveNet
SatisfactorySaveNet is a fully managed C# library for reading and writing save files for the game [Satisfactory](https://www.satisfactorygame.com/) by Coffee Stain Studios.
This component provides type-safe access to all data of the `.sav` file type and exposes injectable readers that can be replaced or wrapped.
You can integrate it using [nuget.org](https://www.nuget.org/packages/SatisfactorySaveNet/).

Further, (external and promising looking) documentation of the save game format is available [here](https://github.com/moritz-h/satisfactory-3d-map/blob/master/docs/SATISFACTORY_SAVE.md). **Link fixed now sorry**

## Installation
Install the library from [NuGet](https://www.nuget.org/packages/SatisfactorySaveNet/) using your preferred method.

### .NET CLI
```bash
dotnet add package SatisfactorySaveNet
```

### Package Manager
```powershell
Install-Package SatisfactorySaveNet
```

### Project File
```xml
<PackageReference Include="SatisfactorySaveNet" Version="3.1.0" />
```

## Version compatibility

| Component      | Supported versions                                                                 |
| -------------- | ----------------------------------------------------------------------------------- |
| Save header    | up to `SaveHeaderVersion.SaveNameInHeader`                                         |
| Save format    | `FSaveCustomVersion.DROPPED_WireSpanFromConnnectionComponents` – `FSaveCustomVersion.TrainBlueprintClassAdded` |

Saves outside of these ranges throw a `NotSupportedException` during deserialization.

## Common serialization/deserialization scenarios

### From file
```csharp
ISaveFileSerializer serializer = SaveFileSerializer.Instance;
var save = serializer.Deserialize(@"C:\\mySaveFile.sav");
serializer.Serialize(save, @"C:\\myNewSaveFile.sav");
```

### From byte array
```csharp
var save = serializer.Deserialize(fileBytes);
var bytes = serializer.Serialize(save);
```

### Using streams
```csharp
using var readStream = File.OpenRead(@"C:\\mySaveFile.sav");
var save = serializer.Deserialize(readStream);
using var writeStream = File.Create(@"C:\\myNewSaveFile.sav");
serializer.Serialize(save, writeStream);
```

### Asynchronous
```csharp
var save = await serializer.DeserializeAsync(@"C:\\mySaveFile.sav");
await serializer.SerializeAsync(save, @"C:\\myNewSaveFile.sav");
```

## Configuration options
`SaveFileSerializer` accepts injectable components, allowing you to override specific parts of the pipeline:

```csharp
var serializer = new SaveFileSerializer(
    HeaderSerializer.Instance,
    ChunkSerializer.Instance,
    BodySerializer.Instance);
```

- Replace `IHeaderSerializer`, `IChunkSerializer`, or `IBodySerializer` with custom implementations to handle experimental features or logging.
- Choose between synchronous and asynchronous APIs depending on your workload.

