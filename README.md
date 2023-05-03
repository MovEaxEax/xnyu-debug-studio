### Description

This software is for debugging games, with heavy focus on speedrunning and glitchhunting. Futhermore, it can be used for speedrun practice, game analysis or simple cheating.
This is the main repository includes the full build in the release section, however it needs the xnyu-debug.dll from the [xnyu-debug repository](https://github.com/MovEaxEax/xnyu-debug)

### Core features

- Input simulation (TAS) with scripting language
- Automation
- Inspect memory values
- Edit savefiles
- Invoke game intern functions
- Supervision to show hitboxes etc.
- Other usefull debug features

### Pre-conditions

To use this game you will need a template and a mod for the game. The templates are in the .ntt format and contains all informations about how the mod get's loaded. The mod itself is a .dll written in a format that the xnyu-debug understands. It needs specific exported functions and structs to work properly. Each mod must be individual written for the game you want to work with.
There are a bunch of mods in the [approved mods repository](https://github.com/MovEaxEax/xnyu-debug-approved-mods) which are ready to use and can be installed either manually or with the mod manager (included in the release build).
If you want to write your own mod please visit the [xnyu-debug repository](https://github.com/MovEaxEax/xnyu-debug), where you can find a devs guide.

### Getting started (step-by-step)

1. Download the latest release build from this repository
2. Extract the ZIP file
3. Start the mod manager
4. Select and install a mod for a game you like
5. Launch the game for the mod you installed
6. Start the debug studio
7. Select the template for the game
8. Press the inject button
9. The form expands itself and you are good to go

### More documentation

Will be added in the future
