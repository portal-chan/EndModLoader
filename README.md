<p align="center"> 
    <img src="http://i.imgur.com/RPIfEpl.png" alt="Screenshot of the MOD loader">
</p>

### Download HERE:
[😼 v1.1 (latest)](https://github.com/portal-chan/EndModLoader/releases/download/v1.1/EndModLoader.exe)    
[🍉 v1.0](https://github.com/portal-chan/EndModLoader/releases/download/v1.0/EndModLoader.exe)

### For players:
Once you launch the program, assuming you have a Steam version of the game, the program will
automatically initialize a `mods` folder in your games directory. Otherwise, click the "Select"
button and navigate to your directory of choice.

After that's done, put any and all mod .zip files into the `mods` folder. They should automatically
be detected by the program, however if they don't show up, try relaunching.

To launch a mod, select it in the list and click the "Play MOD" button (shoutout to teamPROBIRTH)
and your game will launch running the mod. Once you exit the game, the mod files are cleaned up and
your game is back to normal.

### For modders:
Compress 🗜️ all of your modified unpacked folders into a single .zip file.
In addition, you can include a `meta.xml` file in the root of the .zip file following the example below.
If no `meta.xml` is present, the title defaults to the name of the .zip file and no author/version is displayed.
```xml
<mod>
    <title>Your mod title</title>
    <desc>Short mod info/description</desc>
    <author>Your name</author>
    <version>v1.0</version>
</mod>
```

With that your .zip file is ready to be distributed and shilled freely.
