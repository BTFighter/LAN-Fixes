![Hover](https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/280180/header.jpg)  
# Hover
Tested with Build 2800045.

[**Photon SDK Server v4.0.29.11263**](https://archive.org/details/photon-server-sdk_v4-0-29-11263) is required.

Don't forget to back up your **Assembly-CSharp.dll**.

## Instructions

**Install and use Photon Server** (Host only):

_1._ Go to "_(Your Photon Server)\bin_Win64_" and run PhotonControl.exe.

_2._ Open Photon Control in the Hidden Icon Menu (Taskbar arrow), to Game Server IP Config and press 'Set Local IP'.
  - This sets the IP of the server, if you don't do this players joining you will connect to 127.0.0.1 instead.

_3._ Go to LoadBalancing and press "Start as application", and the server should be running.

**Game** (**ALL** players):

_4._ Place files in **"out"** folder to your game folder.

_5._ Open "LANSettings.ini" then change "ServerAddress" to the IP of who is hosting.

_6._ Save and run the game.

Then you can proceed to start the game and you'll automatically join to a server.
