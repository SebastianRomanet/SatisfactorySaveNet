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

| Component   | Supported versions |
| ----------- | ------------------ |
| Game builds | `BuildVersions.Patch0400` (146871)<br>`BuildVersions.Patch0613` (202470)<br>`BuildVersions.Patch0700` (208250)<br>`BuildVersions.Patch1000` (424353) |
| Save header | up to `SaveHeaderVersion.SaveNameInHeader` (14) |
| Save format | `FSaveCustomVersion.DROPPED_WireSpanFromConnnectionComponents` – `FSaveCustomVersion.TrainBlueprintClassAdded` |

Saves outside of these ranges throw a `NotSupportedException` during deserialization.

Header version 14 and the latest save versions are already handled; they become available as soon as the corresponding build numbers are added to `BuildVersions.cs`.

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

## Testing with Example Saves

`LatestSaveRoundtripTests` runs roundtrip checks against all `.sav` files in `SatisfactorySaveNet/Example Files` to ensure the library can deserialize and reserialize real game saves.

Run all tests:

```bash
dotnet test
```

Run only the roundtrip tests:

```bash
dotnet test --filter LatestSaveRoundtripTests
```

