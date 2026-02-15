![Half Dead](https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/434730/header.jpg)  
# Half Dead
Tested with Build 2143410.

[**Photon SDK Server v4.0.28.2962**](https://archive.org/details/photon-server-v-4.0.28.2962) is required.

Don't forget to back up your **Assembly-CSharp.dll**.

## Issues

- Game sometimes freezes and crashes when you create and close games within a short time.
- "Join" button rarely works, join a game by using "Random" instead.

## Instructions

**Install and use Photon Server** (Host only):

_1._ Go to (Your Photon Server)\bin_Win64 and run PhotonControl.exe.

_2._ Open Photon Control in the Hidden Icon Menu (Taskbar arrow), to Game Server IP Config and press 'Set Local IP'.
  - This sets the IP of the server, if you don't do this players joining you will connect to 127.0.0.1 instead.

_3._ Go to LoadBalancing and press "Start as application", and the server should be running.

**Game** (**ALL** players):

_4._ Place **Assembly-CSharp.dll** at _"HalfDead_Data\Managed\"_

_5._ Open "photon_config.ini" change "Address" to the IP of who is hosting.

_6._ Save and run the game.

Then you can proceed to create a private match and other players can join in the server browser by pressing "Random".
