# Total Mass Sensor With Signal

Oxygen Not Included mod. Adds the Total Mass Sensor to the Automation build menu.

## Building

The Total Mass Sensor is a 1x1 building that sums the mass of the connected body of the same element (4-connected tiles) at its own cell. It sends a Green signal when the total mass is above (or below) the configured threshold, and a Red signal otherwise. The threshold is set in the standard threshold side screen, from 0 to 2,147,483,647 kg; the input field is widened to 10 characters for this sensor. The sensor checks every 200 ms, and its settings are saved with the building.

## Steam Description (Copy/Paste)

Adds the **Total Mass Sensor** to the Automation build menu.

It sums the mass of the whole connected body of the same element at its own cell (gas, liquid or solid) and sends an automation signal when the total goes above or below a threshold you set.

- Threshold from 0 to 2,147,483,647 kg, with a widened input field
- Green when the condition is met, Red otherwise
- Works with the standard threshold side screen
- English and Japanese

Settings are saved with the building. Safe to add to existing saves.

## Workshop Upload

- Thumbnail: `Workshop/thumbnail-320.png`
- Build with `dotnet build .\MassSensorWithSignal.sln -c Release` and upload the contents of the mod install directory (DLL, `mod.yaml`, `mod_info.yaml`, `strings`, `anim`).
- Bump `version` in `mod_info.yaml` for every update.

## Localization

English is the default. A Japanese translation is in `strings/ja.po`.

## Build

`Local.Build.props` points to the Oxygen Not Included `Managed` directory and the local mod install directory. It is machine-specific and ignored by Git. To configure another machine, copy `Local.Build.props.example` to `Local.Build.props` and update both paths.

Build from the solution directory:

```powershell
dotnet build .\MassSensorWithSignal.sln
```

When `ModInstallPath` is configured, the build merges `PLib.dll` into the mod DLL and copies it, `mod_info.yaml`, `mod.yaml` and the `strings` directory to that local mod directory.

## License

MIT
