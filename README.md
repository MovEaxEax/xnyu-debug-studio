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

### Controls

There are two components you can control: The mod inside the game and the studio which injected the mod.
The studio is for recording and playing scripts (TAS).
The mod inside the game is for debug features.

## 1. Mod control
Open the debug menu with the NUM_0 key. From here you can choose what you want to do. You navigate the mod with the cursor.
- Debug values: Select game values you want to inspect, they are drawn if the mod closes again and you activated the feature aswell. To activate it, click the button below.
- Debug functions: There are a lot of hotkeys, to bind them with a debug function just click on the upper rectangle and select your function. To pass the function parameter, click on the lower rectangle and write your parameters in. The parameter have a specific format:
int32/int64/byte = just a valid number
float/double = a number in the format 1.0, 2.0, 3.5, the . character is essential
string = a text sequence inside ""-characters
To seperate the parameter just type a ,-character
As soon as you close the mod and press the hotkey the function get's executed. You can bind different sets of hotkeys in different slots. Click on of the slots in the down right corner to select it and type a name for it if you wish. Now you can switch between them.
- Savefile editor: Select a save file of the game you want to edit. The available values are sorted in different categories, so first select this. If you wish to modify the values of the savefile type your new value in one of the fields and press save. If you press reload, you reload the current file again and your modifications getting overwritten.
- Super vision: This feature is for highlighting specific objects, such as hidden walls or hitbox and everything else what makes sense in the meaning of the game. To activate it, click the button below.
- Show cursor position: Displays the current position of the cursor. To activate it, click the button below.
- Performance mode: If you encounter fps drops with the usage of the mod, you may consider to activate this feature.
- Hotkey overlay: Draws an overview of the binded hotkey functions if the menu is closed.

## 2. Studio control
- Eject: Press this button to eject the mod.
- Play: Press this button to play the selected .ntl script. By pressing again, the simulation stops
- Record: Press this button to start recording the inputs you press ingame and save them to a .ntl file. You can specifiy the name in the textbox beneath.
There are 3 different modes of recording:
  - Normal record: Game runs and you can play/record normally
  - Frame by Frame: Game freezes until a frame is sent, you record the script with the input interface (joystick, keyboard, mouse)
  - Record after playing script: First get's a script played and precisely after it finished the recording starts
- Input interface: If "Frame by Frame" is activated, you can send inputs via this interface to the game and record them. By clicking the buttons on the joystick/keyboard/mouse icon the frame is getting created in the textbox above. Then you can press the "Add frame" button to send the frame. If you want to edit the frame before that, you can write them manually aswell.
- Force window stay active: If your game stops rendering when you loose focus on it, it may intefer with you recording. You can try to prevent the game from doing it, but this is an experimental feature, doesn't work everytime.
- Enable console output: Opens a CMD window to output extra information
- Enable dev mode: Enables extra output for specific activities to track exceptions and problem in the tool itself.

### Nyu TAS language

The scripts than can be played have several features aside of the normal frame{} instruction. It's a very simple scripting language, but you will be able to create logical operations and use the debug interface to execute/read/set debug functions/values inside the script.

### Hello frame

Here is an example how a script can look like:
```
init
{
  int32 iterator = 0;
  int32 max = 100;
  float positionX = 300.0;
  float positionY = 190.0;
  float positionZ = 12.55;
}

myFunc
{
  Position.SetPlayer(positionX, positionY, positionZ);
  frame { W(); }
  frame { W(); }
  frame { W(); }
  frame {  }
  frame { SPACE(); }
  frame { SPACE(); W(); }
  frame { W(); }
  frame { W(); }
  frame {  }
  frame {  }
  frame {  }
}

main
{
  while(iterator < max)
  {
    myFunc();
    if (Player.HP > 1000)
    {
      log("Health picked up!");
      Exit();
    }
    iterator += 1;
  }
}
```

The main section is the main execution of the script, but init is called before that.
You can import funcitons from other scripts with the import statement outside of a function:
```
import{"myScriptA.nts";}
import{
  "myScriptB.nts";
  "myScriptC.nts";
}
```

There are a few datatypes that a variable can have:
```
int32 varA = 12345;
int64 varB = 3000000000;
float varC = 30.0;
double varD = 1.2;
byte varE = 0x10;
bool varF = true;
string varG = "text";
```

The moment a variable is declared, she is static and can be used everywhere in the script. There are no privates.
Main feature of the scripting language is the frame{}, which determines the inputs which are pressed at a frame. The name of the keys varies from mod to mod, since they have custom input mapping, but the standard input mapping looks like this:
```
// Keyboard A, Space and 1 key are pressed
frame{ A(); SPACE(); 1();}
// Nothing is pressed
frame{ }
// Joystick DPAD up and Left stick are pressed
frame{ JOYUP(); LAXIS(-60); }
// Mouse moved X position and scrollwheel scrolled upside
frame{ MOUSEX(100); WHEEL(10); }
```

Everything you write in the scripts is not case sentitive. If you declare a variable called "MyVariable" you can access her with "myvariable" aswell.
You can call DebugFunctions that the specific mod implements in your script like this:
```
Player.SetPosition(100.0, 100.0, 30.0);
Game.DestroyObject("ObjectID");
```

The full path with .-character has to be typed. DebugValues can be used like variables, but you have to write the full name aswell:
```
Player.SetPosition(Player.Position-X, 100.0, 30.0);
string baseObject = Objects.BaseObjectIdentifier;
baseObject += "_123";
Game.DestroyObject(baseObject);
```

There are few loop instructions and conditions you can use with logical operations, but for example, there is no else-statement. The repeat-statemtn only integer as parameter and no variables:
```
while(Player.Position-Y < 1000.0)
{
  frame{ JOYUP(); }
}

repeat(100)
{
  frame{ JOYLEFT(); }
}

if (Player.Position-X >= 300.0)
{
  repeat(100)
  {
    frame{ JOYRIGHT(); }
  }
}

if (Globals.IsGamePaused)
{
  frame{ JOYSTART();}
}
```

For feedback you can use the log() and Exit() function, to log some text in the console or stop the script
```
log("Some text here!");
Exit();
```

Arithmetic operations allow just one parameter and need an =-character to calculate
```
main
{
	float distance = 0;
	float original = Player.Position-Y;
	repeat(25)
	{
		frame{JOYDOWN();}
	}
	distance = Player.Position-Y;
	distance -= original;
	log("Tracked distance walked: ", distance);
}
```

If errors occure with your script, you will find errors and information about what went wrong in the console, if you enabled it.

### Disclaimer
This is an early version of the tool, crashes of the game are very likely. If you don't encounter crashes, you are either blessed by the gods or deal with a game that is as complex as pong.
The scripting language is kind of fragile at the moment.
