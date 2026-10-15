# ProbablyStolenBoombox
A small mod that allows you to easily create custom cassettes for your cassette player in Probably Stolen.

I opened the game and immediately knew what i had to do, so i closed the game and started making this mod.

It was made in day. I can finally play the game now. 

~~I'll try to update the mod whenever it breaks, but don't expect me to maintain this forever though.~~
Playtest is over and the game might change a lot internally, i'm not really interested in keeping this mod working so you can consider this to be a broken proof of concept mod.

## Demo
https://github.com/user-attachments/assets/801ab96f-a068-494b-ad2c-86ab2a7c9e63

## Other part of the description
Currently the only way to acquire the new cassettes is to scavenge in the dumping grounds. All cassettes are in the same unique loot table that has base weight of 0.1 (less than 1%, configurable in UserData/MelonPreferences.cfg), every cassette also has it's own weight, so you can make some cassettes more rare than others.

**This mod requires MelonLoader to function!**

To add a cassette, you have to put an .mp3/.ogg/.wav into the Mods/Boombox folder, and create a .txt file with **EXACT SAME NAME AS THE AUDIO FILE** in the same folder. When you download the mod you'll find a file that you can use as a reference.

The values that you can set are as follows:
- id - Allows the game to distinguish between the cassettes.
- name - Name of the cassette. (Name of the music)
- composer - Name of the music's authors.
- sprite - Id of the sprite that this cassette will have. (Supported values are "cassetteX", where X is numbers 1-10, e.g. "cassette7")
- weight - Weight of this specific cassette. Making this higher will make this cassette drop more then other cassettes and vice versa.

This mod also has a feature to make cassettes take up less space (1x1 instead of 2x2). It's turned on by default and you can disable it in UserData/MelonPreferences.cfg. (I didn't test much but it seems to work correctly, but still beware of possible bugs. If you fear that you can get your save file bricked even if the chance is really low, turn this off!)

**New game is NOT REQUIRED**, but cassettes that were created before installing this game will not be turned into 1x1 if that setting is on and will be a bit funky.

It is **NOT RECOMMENDED** to remove a cassette from the Boombox folder if you have it in your save, this might brick your save!
