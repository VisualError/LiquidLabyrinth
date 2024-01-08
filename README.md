# Ryokune-Liquid_Labyrinth

[![GitHub Build Status](https://img.shields.io/github/actions/workflow/status/VisualError/LiquidLabyrinth/build.yml?style=for-the-badge&logo=github)](https://github.com/VisualError/LiquidLabyrinth/actions/workflows/build.yml)
[![Thunderstore Version](https://img.shields.io/thunderstore/v/Ryokune/Liquid_Labyrinth?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/Ryokune/Liquid_Labyrinth/)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/Ryokune/Liquid_Labyrinth?style=for-the-badge&logo=thunderstore&logoColor=white)](https://thunderstore.io/c/lethal-company/p/Ryokune/Liquid_Labyrinth/)

## This mod is currently on EARLY-ALPHA, please report any bugs & feel free to give suggestions on the [GitHub](https://github.com/VisualError/LiquidLabyrinth) or Message me on Discord: ryokune

### Known bugs
- Scan not working on bottles
- Toast bugged
- Bottle physics gets disabled when ship is moving (Needed to do this because the bottle would bug out of existence)
- Revive could be bugged.

### Planning
- Liquid API support
- Procedurally generated liquids
- Newly added dungeons
- Tools to inspect and change liquid properties
- Locally named liquids for clients
- [FULL CHANGELOG](https://github.com/VisualError/LiquidLabyrinth/blob/main/CHANGELOG.md)
## Installation

1. Ensure you have [BepInEx](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/) installed.
2. Download the latest release of the Lethal Parrying mod from [Thunderstore](https://thunderstore.io/c/lethal-company/p/Ryokune/LiquidLabyrinth).
3. Extract the contents into your Lethal Company's `BepInEx/plugins` folder.

## Contributing

### Template `LiquidLabyrinth/LiquidLabyrinth.csproj.user`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <LETHAL_COMPANY_DIR>C:/Program Files (x86)/Steam/steamapps/common/Lethal Company</LETHAL_COMPANY_DIR>
    <TEST_PROFILE_DIR>$(APPDATA)/r2modmanPlus-local/LethalCompany/profiles/Test Liquid Labyrinth</TEST_PROFILE_DIR>
    <NETCODE_PATCHER_DIR>$(SolutionDir)NetcodePatcher</NETCODE_PATCHER_DIR>
  </PropertyGroup>

    <!-- Create your 'Test Profile' using your modman of choice before enabling this. 
    Enable by setting the Condition attribute to "true". *nix users should switch out `copy` for `cp`. -->
    <Target Name="CopyToTestProfile" DependsOnTargets="NetcodePatch" AfterTargets="PostBuildEvent" Condition="false">
        <MakeDir
                Directories="$(TEST_PROFILE_DIR)/BepInEx/plugins/Ryokune-Liquid_Labyrinth"
                Condition="Exists('$(TEST_PROFILE_DIR)') And !Exists('$(TEST_PROFILE_DIR)/BepInEx/plugins/Ryokune-Liquid_Labyrinth')"
        />
        <Exec Command="cp &quot;$(TargetPath)&quot; &quot;$(TEST_PROFILE_DIR)/BepInEx/plugins/Ryokune-Liquid_Labyrinth/&quot;" />
    </Target>
</Project>
```

## Contributors

- [@Lordfirespeed](https://github.com/Lordfirespeed)
