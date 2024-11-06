# TES3Unity

TES3Unity is an attempt to recreate the Morrowind Engine into the Unity3D game engine.

## Requirements
- Unity 6.0

## Status
| Feature | Status |
|---------|--------|
| World Loading | OK |
| NPC Loading | In progress |
| Creature Loading | In Progress |
| Script support | Not started |
| Mod support | Not started |
| Animation support | Need Help |
| UI recreation | In progress |
| Interactions | Started |
| Player Management | Started |
| VR support | OK |


## Getting Started

**TES3Unity requires a valid installation of Morrowind to run!**, you can get it on Steam or Gog.com.

To get started, go to the release tab and download the latest release for your device.
Alternatively you can download the source code as a ZIP file, extract it, and open the TES3Unity folder in Unity.

If you want to enjoy VR, take a look at `README-VR.md`. Finally input mapping is located at `README-Input.md`.

### StreamingAssets folder
You can copy data files into the `StreamingAssets` folder, that way you don't need to worry about file location.
The required path is `StreamingAssets/Data Files/`.

### Desktop
The game will ask you where is the **Data Files** folder the first time you start the game. If you move your Morrowind installation, the game will ask you again where is the **Data Files** folder.
  
### Mobile
You've to copy the **Data Files** folder into a folder named `TES3Unity` on your SDCard. If the game can't open the game, please open an issue.

### Supported Platforms
| Platform                | Status | Graphics API |
|-------------------------|--------|-------------|
| Windows                 | Supported | Direct3D 11 |
| Linux                   | Experimental | Vulkan |
| Macos                   | Experimental | Metal |
| Android (Flat)          | Experimental | Vulkan |
| Oculus Quest            | Supported | Vulkan |
| Apple Vision Pro(*)(**) | Experimental | Metal |
| iOS (**)                | Experimental | Metal |

(*) You need a pro license to compile the Apple Vision Pro version.
(**) On iOS/VisionOS platforms, the best way to get the game working is to package game data with the game, using the `StreamingAssets` folder.

## Early Screenshots
![mor0](https://user-images.githubusercontent.com/90804824/232175429-1e6682af-72f5-4d42-a937-e1fd1d95b51b.PNG)
![mor1](https://user-images.githubusercontent.com/90804824/232175436-7d95b1fa-dc4c-4bff-9d75-78780987d1b7.PNG)
![mor2](https://user-images.githubusercontent.com/90804824/232175440-2d4da8dd-52b9-4a97-90f7-aa1cd3a1c0b1.PNG)
![mor3](https://user-images.githubusercontent.com/90804824/232175443-e09c1888-890c-460e-99da-5e31e11759a1.PNG)

## Contribute
Bugs and feature requests are listed on the [issues page](https://github.com/demonixis/TES3Unity/issues). 
Please take a look at the wiki for more informations about the Morrowind file formats. Don't hesitate to contact me if you want to help but if you don't know where to start. We've have a lot of tasks, some very easy, other hard.

### Thanks
Special thanks to [Cole Dean Shepherd](https://github.com/ColeDeanShepherd) which have started [this work](https://github.com/ColeDeanShepherd/TESUnity).

## License
This project is released under the [MIT License](https://github.com/demonixis/TES3Unity/blob/master/LICENSE.txt)
