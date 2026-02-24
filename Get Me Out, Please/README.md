![Get Me Out, Please](https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/2441360/header.jpg)  
# Get Me Out, Please
Tested with v1.1.0.

[**Photon SDK Server v5.0.13.32034**](https://archive.org/details/photon-onpremises-server-classic-sdk_v5-0-13-32034.7z) and [**additional setup**](https://gist.github.com/Modac/01bd2b7a997a9fa7c36b0d8b548e7c47) is required.

Don't forget to back up your **Assembly-CSharp.dll**.

## Instructions

**Install and use Photon Server** (Host only):

1. Go to "PhotonServer\bin_Win64" and run PhotonControl.exe.

2. Open Photon Control in the Hidden Icon Menu (Taskbar arrow), press 'Edit NameServer.json' and input your IP Address there. (IPv4 only), afterwards save the file.

3. Go back to Photon Control, to Game Server IP Config and press 'Set Local IP'.  
	- This sets the IP of the server, if you don't do this players joining you will connect to 127.0.0.1 instead.

4. Go back to PhotonServer\bin_Win64 and run CLRPreloadLauncher.exe.  
	- Keep pressing 'OK'. You should get the message 'Started CLR instance!'. Check the server (refer to the pic) if it's running and not shutting down after 10 seconds.
	 
Image

**Game** (**ALL** players):

_5._ Place files in **"out"** folder to your game folder.

_6._ Open "LANSettings.ini" then change "ServerAddress" to the IP of who is hosting.

_7._ Save and run the game.

Then you can proceed to create a match and invite other players using the Overlay.
