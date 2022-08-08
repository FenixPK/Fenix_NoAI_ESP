# Fenix_NoAI_ESP
Eliminate the AI's Extrasensory Perception in SPT-AKI Escape from Tarkov Singleplayer

The AIPatcher.cs file has a longer description of changes in comments.
TL;DR: 
I have used Harmony via AKI.Reflection to skip processing the IsEnemyLookingAtMe() function of Bots that causes them to go on the attack as soon as you put your crosshair over them, even if they have their back turned to you. There are config options (requiring a game re-start for changes to take effect) allowing you to turn this override on for the player and/or other bots. 
