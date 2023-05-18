
# Animated Aseprite for Godot
A **C# Godot plugin** to make working with exported **Aseprite animations** easier.

Implements a new node - **AnimatedAseprite**. Functionally very similar to Godot's own AnimatedSprite, but supports animation definitions and sprite sheets exported from Aseprite.

### Features
 -  No dependency on Aseprite. It only needs to be installed on the machine exporting the animation sets (typically done by the art team).
 - Handles packed sprite sheets with different sized frames.
 - Support for non-constant frame times (different frame delay per frame).
 - Supports all Aseprite animation modes (forward, reverse, ping-pong, reverse ping-pong).
 - Animation definitions will be imported and saved as a smaller binary Godot resource file. 
 - Re-import animations automatically simply by overwriting the existing exported files.

### Usage
The AnimatedAseprite should feel familiar to anyone that's used the AnimatedSprite node before, but instead of managing a SpriteFrames resource you need to set the `SpriteSheet` and `AnimationData` properties to an animation exported from Aseprite.

The following is the typical work-flow to import an animation:

 1. Export a sprite sheet from Aseprite. You will **need to output both the sprite sheet and the JSON data**. Make sure `Tags` is checked in the `Output` section if your sheet has multiple animations. For optimal results you can set the `Sheet Type` to `Packed`, and check the `Trim Sprite` and `Trim Cells` options to make a smaller texture.
 2. Rename the export's `.json` file to `.ase-json`. 
 3. Include the exported `.ase-json` and sprite sheet (usually a `.png`) into your project.
 4. Create a new AnimatedAseprite node. Use the inspector to set the `Sprite Sheet` property to your exported texture file, and the `Animation Data` property to your `.ase-json` file. You can drag and drop from the `FileSystem` dock to the fields in the `Inspector` doc.
 5. Select your (starting) animation from the `Animation` dropdown in the inspector.

To update your animations, simply overwrite the sprite sheet texture and `ase-json` file with your newer version. They will be automatically re-imported, and the currently chosen `Animation` and `Frame` will be preserved.
 
### Installation

Godot doesn't have the best experience for C# plugins yet. Library packaging is not supported so you will need to include the source files into your project.

##### Method 1: Git Submodule (Recommended)
C# Godot plugins require distribution through source code which makes them a good use case for git submodules.
 1. Assuming your project is already managed by git, you can include this repository as a submodule within your project. From your project root you can run: `git submodule add https://github.com/lavabird-games/godot-animated-aseprite.git addons/Lavabird.AnimatedAseprite/`
 2. Build your project once.
 3. From the Godot menu bar, select `Project` -> `Project Settings` to open the settings window. Select the `Plugins` tab and tick the `Enabled` checkbox next to the AnimatedAseprite plugin.

##### Method 2: Manual Install
 1. Download the latest release and copy it to your Godot project at `addons/Lavabird.AnimatedAseprite/`. You might need to create the `addons` folder if this is the first plugin in the project.
 2. Build your project once.
 3. From the Godot menu bar, select `Project` -> `Project Settings` to open the settings window. Select the `Plugins` tab and tick the `Enabled` checkbox next to the AnimatedAseprite plugin.
 
##### Requires C# 8.0 or higher

This project is using nullable reference types, and will require a C# language version of 8.0 or higher. For 3.x projects, you may need to update your `.csproj` file and include the following `PropertyGroup` entry:

```<LangVersion>8</LangVersion>```

### Limitations

 - Aseprite's `.json` files must be renamed to `.ase-json` before importing. This is done deliberately so the plugin doesn't try and process every json file by default (otherwise you would need to manually select the import format in Godot each time a JSON file was included).
 - Only *exported* sprite sheets are supported. `.aseprite` files can't be used directly as we didn't want to include a dependency on Aseprite in the plugin.

### Dependencies

 - Godot 3.5 (Mono) LTS
 - Newtonsoft.Json >= 12.0.0

