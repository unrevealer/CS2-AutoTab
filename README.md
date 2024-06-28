# CS2 Window Focus Selector
This program is designed to allow the user to automatically maximize the window for the video game 'Counter-Strike 2'. Taking advantage of the game's built-in Game State Integration allows for the ability to maximize the game based on certain events such as when a match starts, reaches halftime, etc.

# Usage
The simple console app lists a selection of game events to choose from as well as pausing the software. Currently, this program must be started after the game is opened in order to identify the GSI server and obtain the CS2 process handle. 

# How it works
CS2-AutoTab works by using CS2's official [GameState Integration](https://developer.valvesoftware.com/wiki/Counter-Strike:_Global_Offensive_Game_State_Integration) to receive information about the game.

Credit to [rakijah](https://github.com/rakijah) for creating the [CS2GSI](https://github.com/rakijah/CSGSI) C# library.
