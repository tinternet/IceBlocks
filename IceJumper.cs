using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace IceCubes
{
    //Coresponds to the possible player movements and the movement of the ice cubes
    enum Directions
    {
        Right, Left, Down, Up
    }
    //Holds all information for the current player
    struct Player
    {
        public string name;
        public int lives;
        public int scores;
        public int positionX;
        public int positionY;
        public Directions directionWhenOnIce; //holds the current direction of the player's movement when on ice cube (?)
        public bool isLoaded;   //Indicates if the player's status is loaded from save file
    }
    //Structure for an object of type 
    struct IceCube
    {
        public int startIndex; //Start index of the ice cube in the given row of the river structure
        public int length;
        public int row;         //The given row in witch the ice cube is situated

        public IceCube(int startIndex, int length, int row)
        {
            this.startIndex = startIndex;
            this.length = length;
            this.row = row;
        }
    }

    class IceCubes
    {
        // Game window size and name
        static int gameWindowWidth = 90;
        static int gameWindowHeight = 40;
        static string GameName = "Ice Jumper";

        // River dimensions and position
        static int riverWidth = gameWindowWidth - 20;
        static int riverHeight = 7; // side[2], water[0], ice[1], water[0], ice[1], water[0], side[2]
        static int defaultRiverPositionX = (gameWindowWidth - riverWidth) / 2;
        static int defaultRiverPositionY = 5;
        static int[][,] iceCubes;       //Dynamic rows of the river. (Ice-and-water lines.)

        static Player player;           //Object to reperesent the player status
        static int currentLevel = 1;
        static int lastLevel = 10;
        static bool isLevelComplete = false;
        static bool isGameComplete = false;
        static bool hasDrown = false;   //Indicates if the player has drowned

        static Random generateRandomNumber = new Random(); //Used to initialize each ice cube's length

        static string ScoreFilePath = "HighScores.txt";
        static string SaveFilePath = "Saves.txt";

        //Prints initial river background - banks and water
        static void PrintRiver(int rows)
        {
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < riverWidth; col++)
                {
                    if (row == 0 || row == riverHeight - 1)
                    {
                        SideColor();
                    }
                    else
                    {
                        WaterColor();
                    }

                    Console.SetCursorPosition(col + defaultRiverPositionX, row + defaultRiverPositionY);
                    Console.Write(' ');
                }
            }
            Console.ResetColor();
        }
        //Method that initializes the ice cubes array and updates it for each new level
        static void InitializeIceCubes(int rows)
        {
            List<List<IceCube>> iceCubeRows = new List<List<IceCube>>();
            //Create, initialize and add ice-rows to the rows list
            for (int row = 0; row < rows; row++)
            {
                List<IceCube> iceCubesInCurentRow = new List<IceCube>();//Create new ice-row
                int lastCubeEndIndex = 0; //Indicates last taken place for an ice cube
                //Fills a row of ice cubes
                while (lastCubeEndIndex < riverWidth - 1)
                {
                    int iceCubeIndex = generateRandomNumber.Next(lastCubeEndIndex + 3, lastCubeEndIndex + 5);
                    int iceCubeLength = generateRandomNumber.Next(2, 8);
                    if (iceCubeIndex + iceCubeLength > riverWidth - 1)
                    {
                        break;
                    }
                    iceCubesInCurentRow.Add(new IceCube(iceCubeIndex, iceCubeLength, row));
                    lastCubeEndIndex = iceCubeIndex + iceCubeLength - 1;
                }
                iceCubeRows.Add(iceCubesInCurentRow);
            }

            //Initialize the iceCubes array
            iceCubes = new int[iceCubeRows.Count][,];
            for (int row = 0; row < iceCubeRows.Count; row++)
            {
                //Initializes a row of ice cubes with second dimension with
                //2 elements - [0]=startIndex; [1]=length;
                iceCubes[row] = new int[iceCubeRows[row].Count, 2];
            }
            //Save each ice cube's data(startIndex in the row and length) in the iceCubes array
            for (int row = 0; row < iceCubeRows.Count; row++)
                for (int iceCubeCounter = 0; iceCubeCounter < iceCubeRows[row].Count; iceCubeCounter++)
                {
                    iceCubes[row][iceCubeCounter, 0] = iceCubeRows[row][iceCubeCounter].startIndex;
                    iceCubes[row][iceCubeCounter, 1] = iceCubeRows[row][iceCubeCounter].length;
                }
        }
        //Method that show the player and game info, including curent level time, current level, player lives, player scores
        //and player name
        static void PrintGameInfo(int levelTime)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(new string('=', gameWindowWidth - 1));
            Console.WriteLine("PLAYER: {0}\t|\tSCORES: {1}\t|\tLEVEL: {2}\t|\tTIME: {3} ", player.name, player.scores, currentLevel, levelTime);
            Console.WriteLine(new string('=', gameWindowWidth - 1));
        }
        //Redraw player at new position and redraw previous player position
        static void PrintPlayer(Player player, bool isMoving)
        {

            if (player.positionY == 0 || player.positionY == riverHeight - 1)
            {
                SideColor();
            }
            else
            {
                IceColor();
            }

            Console.ForegroundColor = ConsoleColor.Red;
            if (isMoving)       //Redraws the PREVIOUS player position
            {
                Console.SetCursorPosition(player.positionX + defaultRiverPositionX, player.positionY + defaultRiverPositionY);
                Console.Write(' ');
            }
            else //Redraw player at new position ( @ )
            {
                //Check if the player has drowned. The check is made in the CheckForDrown() method and 
                //the status of the isDrown player characteristic is changed.
                bool isOnCubeStartIndex, isOnCubeEndIndex;
                CheckForDrown(out isOnCubeStartIndex, out isOnCubeEndIndex);
                if (hasDrown)        //If the player has drowned the background is water 
                    WaterColor();
                Console.SetCursorPosition(player.positionX + defaultRiverPositionX, player.positionY + defaultRiverPositionY);
                Console.Write('@');
            }
            Console.ResetColor();
        }
        //Initial drawing of the ice cubes. This method is called at the begining of each level.
        static void PrintIceCubes()
        {
            int defaultCubePositionY = defaultRiverPositionY + 2;
            for (int cubeRow = 0; cubeRow < iceCubes.Length; cubeRow++)
            {
                for (int cubeIndexInRows = 0; cubeIndexInRows < iceCubes[cubeRow].GetLength(0); cubeIndexInRows++)
                {
                    IceColor();
                    int currentCubeStartIndex = iceCubes[cubeRow][cubeIndexInRows, 0];
                    int currentCubeLength = iceCubes[cubeRow][cubeIndexInRows, 1];
                    Console.SetCursorPosition(currentCubeStartIndex + defaultRiverPositionX, defaultCubePositionY + cubeRow * 2);
                    for (int i = 0; i < currentCubeLength; i++)
                    {
                        Console.Write(' ');
                    }
                }
                Console.ResetColor();
            }
        }

        static void IceColor()
        {
            Console.BackgroundColor = ConsoleColor.Cyan;
        }

        static void WaterColor()
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
        }

        static void SideColor()
        {
            Console.BackgroundColor = ConsoleColor.DarkGreen;
        }
        //This method prints the game's logo
        static void PrintGameLogo()
        {
            string[] logo = 
            {    
                "22222222222222222222222222222222222222222222222222222222222222222222222222222222222",
                "20000000000000000000000000000000000000000000000000000000000000000000000000000000002",
                "20011111000011110001111100000011111000100001000110001100011111000011111000111110002",
                "20000100000100000001000000000000100000100001000101010100010000100010000000100001002",
                "20000100000100000001110000000000100000100001000100100100011111000011100000111110002",
                "20000100000100000001000000000100100000100001000100000100010000000010000000100100002",
                "20011111000011110001111100000011000000011110000100000100010000000011111000100011002",
                "20000000000000000000000000000000000000000000000000000000000000000000000000000000002",
                "22222222222222222222222222222222222222222222222222222222222222222222222222222222222"        
            };

            int logoPositionX = (gameWindowWidth / 2) - logo[0].Length / 2;
            int logoPositionY = (gameWindowHeight - logo.Length) - 1;

            for (int i = 0; i < logo.Length; i++)
            {
                Console.SetCursorPosition(logoPositionX, logoPositionY);

                string row = logo[i];
                for (int j = 0; j < row.Length; j++)
                {
                    if (row[j] == '2')
                    {
                        SideColor();
                    }
                    else if (row[j] == '1')
                    {
                        IceColor();
                    }
                    else
                    {
                        WaterColor();
                    }
                    Console.Write(" ");
                }
                logoPositionY++;
            }
            Console.ResetColor();
        }
        //Method that counts down to level start and show current level number
        static void StartLevel(int currentLevel)
        {
            Console.Clear();
            PrintGameLogo();

            string levelString = "LEVEL " + currentLevel;
            int stringPositionX = (gameWindowWidth / 2) - (levelString.Length / 2);
            int stringPositionY = gameWindowHeight / 2;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.SetCursorPosition(stringPositionX, stringPositionY);
            Console.WriteLine(levelString);

            for (int i = 3; i >= 1; i--)
            {
                Console.SetCursorPosition(gameWindowWidth / 2, stringPositionY + 2);
                Console.Write(i);
                Console.Beep(500, 100);
                Thread.Sleep(500);
            }
            Console.Clear();
            Console.Beep(2000, 700);
            Console.ResetColor();
        }
        //Method that initializes all activities in the work of each new level - countdown, printing of: game info, game logo
        //and drawings of the river background, ice cubes and the player. The method also start stop watches for the level
        //and handles the player movement.
        static bool PlayLevel()
        {
            int levelTime = 15;
            isLevelComplete = false;
            hasDrown = false;
            InitializeIceCubes(currentLevel + 1);
            StartLevel(currentLevel);
            PrintGameInfo(levelTime);
            PrintGameLogo();
            PrintRiver(riverHeight);
            PrintIceCubes();

            player.positionX = riverWidth / 2;
            player.positionY = 0;
            player.directionWhenOnIce = Directions.Left;

            PrintPlayer(player, false);

            var iceBlocksUpdateWatch = Stopwatch.StartNew(); //A stopwatch for the update of the ice cube array
            var levelTimeWatch = Stopwatch.StartNew();      //A stopwatch for the level timing


            while (true)
            {
                //Handle of player movements
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.UpArrow)
                    {
                        MovePlayer(Directions.Up);
                    }
                    else if (keyInfo.Key == ConsoleKey.DownArrow)
                    {
                        MovePlayer(Directions.Down);
                    }
                    else if (keyInfo.Key == ConsoleKey.LeftArrow)
                    {
                        MovePlayer(Directions.Left);
                    }
                    else if (keyInfo.Key == ConsoleKey.RightArrow)
                    {
                        MovePlayer(Directions.Right);
                    }
                }
                //End of level if the has ran out or the player has drowned
                if (hasDrown || levelTime == 0)
                {
                    return isLevelComplete;
                }
                else if (CheckForCompleteLevel())
                {
                    //If the player has reached the other side calculate scores
                    player.scores += (int)Math.Pow(levelTime, 2);
                    isLevelComplete = true;
                    return isLevelComplete;
                }
                //Constant update of the ice cubes' positions 
                if (iceBlocksUpdateWatch.ElapsedMilliseconds >= 100)
                {
                    UpdateIceBlockPosition();
                    iceBlocksUpdateWatch.Restart();
                }
                //Stopwatch for level time
                if (levelTimeWatch.ElapsedMilliseconds >= 1000)
                {
                    levelTime--;
                    PrintGameInfo(levelTime);
                    levelTimeWatch.Restart();
                }
            }
        }
        //A method for updating the ice cubes' positions
        static void UpdateIceBlockPosition()
        {
            for (int cubeRow = 0; cubeRow < iceCubes.Length; cubeRow++)
            {
                Directions direction = (Directions)(cubeRow % 2);
                for (int cubeIndexInCurrentRow = 0; cubeIndexInCurrentRow < iceCubes[cubeRow].GetLength(0); cubeIndexInCurrentRow++)
                {
                    if (direction == Directions.Right)
                    {
                        if (iceCubes[cubeRow][cubeIndexInCurrentRow, 0] == riverWidth - 1) //Ice cube reaches the left end of the river
                        {
                            iceCubes[cubeRow][cubeIndexInCurrentRow, 0] = 0;
                        }
                        else
                        {
                            iceCubes[cubeRow][cubeIndexInCurrentRow, 0] += 1;
                        }
                    }
                    else
                    {
                        if (iceCubes[cubeRow][cubeIndexInCurrentRow, 0] == 0) //Ice cube reaches the right end of the river
                        {
                            iceCubes[cubeRow][cubeIndexInCurrentRow, 0] = riverWidth - 1;
                        }
                        else
                        {
                            iceCubes[cubeRow][cubeIndexInCurrentRow, 0] -= 1;
                        }
                    }
                }
                MoveIceCubesOnConsole(cubeRow, direction); //When the entire row of ice is updated redraw it
            }
            //Move player to the coresponding position
            if (player.positionY > 0 && player.positionY < riverHeight - 1)
            {
                if (player.directionWhenOnIce == Directions.Left)
                {
                    MovePlayer(Directions.Left);
                }
                else
                {
                    MovePlayer(Directions.Right);
                }
            }
        }
        //This method moves the player in a direction
        static void MovePlayer(Directions direction)
        {
            var lastKnownPlayerPosition = player;
            if (direction == Directions.Down)   //If the direction is DOWN
            {
                if (player.positionY < riverHeight - 1)
                {
                    player.positionY += 2;
                    PrintPlayer(player, false);
                    if (player.directionWhenOnIce == Directions.Left)//If the movement on ice is left then the next one will move to the right
                    {
                        player.directionWhenOnIce = Directions.Right;
                    }
                    else
                    {
                        player.directionWhenOnIce = Directions.Left;
                    }
                }
            }
            else if (direction == Directions.Up)    //If the direction is UP
            {
                if (player.positionY > 1)
                {
                    player.positionY -= 2;
                    PrintPlayer(player, false);
                    if (player.directionWhenOnIce == Directions.Left)
                    {
                        player.directionWhenOnIce = Directions.Right;
                    }
                    else
                    {
                        player.directionWhenOnIce = Directions.Left;
                    }
                }
            }
            else if (direction == Directions.Left)  //If the direction is LEFT
            {
                if (player.positionX > 0)
                {
                    player.positionX--;
                    PrintPlayer(player, false);
                }
            }
            else if (direction == Directions.Right) //If the direction is RIGHT
            {
                if (player.positionX < riverWidth - 1)
                {
                    player.positionX++;
                    PrintPlayer(player, false);
                }
            }
            bool isOnCubeStartIndex = false;
            bool isOnCubeEndIndex = false;
            CheckForDrown(out isOnCubeStartIndex, out isOnCubeEndIndex);    //Check if the player is drowned in the new position
            if (!isOnCubeStartIndex || (isOnCubeStartIndex && direction != Directions.Right))
            {
                if (!isOnCubeEndIndex || (isOnCubeEndIndex && direction != Directions.Left))
                {
                    PrintPlayer(lastKnownPlayerPosition, true);
                }
            }
        }
        //Draws an updated ice row
        static void MoveIceCubesOnConsole(int row, Directions direction)
        {
            for (int cubeIndexInCurrentRow = 0; cubeIndexInCurrentRow < iceCubes[row].GetLength(0); cubeIndexInCurrentRow++)
            {
                int currentCubeStartPositionY = (defaultRiverPositionY + 2) + row * 2;
                int currentCubeIndex = iceCubes[row][cubeIndexInCurrentRow, 0];
                int currentCubeStartPositionX = defaultRiverPositionX + currentCubeIndex;
                int currentCubeLength = iceCubes[row][cubeIndexInCurrentRow, 1];
                if (direction == Directions.Right)
                {
                    if (currentCubeIndex == 0)
                    {
                        Console.SetCursorPosition(defaultRiverPositionX + riverWidth - 1, currentCubeStartPositionY);
                    }
                    else
                    {
                        Console.SetCursorPosition(currentCubeStartPositionX - 1, currentCubeStartPositionY);
                    }
                    WaterColor();
                    Console.Write(' ');

                    if (currentCubeIndex + currentCubeLength > riverWidth)
                    {
                        int newIndex = (currentCubeIndex + currentCubeLength - 1) - (riverWidth);
                        Console.SetCursorPosition(defaultRiverPositionX + newIndex, currentCubeStartPositionY);
                    }
                    else
                    {
                        Console.SetCursorPosition(currentCubeStartPositionX + currentCubeLength - 1, currentCubeStartPositionY);
                    }
                    IceColor();
                    Console.Write(' ');
                }
                else
                {
                    Console.SetCursorPosition(currentCubeStartPositionX, currentCubeStartPositionY);
                    IceColor();
                    Console.Write(' ');
                    if (currentCubeIndex + currentCubeLength > riverWidth - 1)
                    {
                        int newIndex = (currentCubeIndex + currentCubeLength) - (riverWidth);
                        Console.SetCursorPosition(defaultRiverPositionX + newIndex, currentCubeStartPositionY);
                    }
                    else
                    {
                        Console.SetCursorPosition(currentCubeStartPositionX + currentCubeLength, currentCubeStartPositionY);
                    }
                    WaterColor();
                    Console.Write(' ');
                }
                Console.ResetColor();
            }
        }
        //Check if player is in water on current position
        static void CheckForDrown(out bool isOnCubeStartIndex, out bool isOnCubeEndIndex)
        {
            isOnCubeStartIndex = false;
            isOnCubeEndIndex = false;
            int playerPositionOnRow = player.positionY;
            int playerPositionOnColumn = player.positionX;

            if (playerPositionOnRow > 0)
            {
                if (playerPositionOnRow < riverHeight - 1)
                {
                    bool isOnIce = false;
                    for (int iceCubeIndexInRow = 0; iceCubeIndexInRow < iceCubes[playerPositionOnRow / 2 - 1].GetLength(0); iceCubeIndexInRow++)
                    {
                        int currentIceCubeStartIndex = iceCubes[playerPositionOnRow / 2 - 1][iceCubeIndexInRow, 0];
                        int currentIceCubeLength = iceCubes[playerPositionOnRow / 2 - 1][iceCubeIndexInRow, 1];
                        isOnCubeStartIndex = playerPositionOnColumn == currentIceCubeStartIndex;
                        isOnCubeEndIndex = playerPositionOnColumn == currentIceCubeStartIndex + currentIceCubeLength - 1;
                        if (!isOnCubeStartIndex && !isOnCubeEndIndex)
                        {
                            if (playerPositionOnColumn > currentIceCubeStartIndex && playerPositionOnColumn < currentIceCubeStartIndex + currentIceCubeLength - 1)
                            {
                                isOnIce = true;
                                break;
                            }
                        }
                        else
                        {
                            isOnIce = true;
                            break;
                        }
                    }
                    if (!isOnIce)
                    {
                        hasDrown = true;
                    }
                }
            }
        }
        //Check if the player has reached the other side and has finished the level
        static bool CheckForCompleteLevel()
        {
            if (player.positionY == riverHeight - 1) //Check if the player's position is ona the lqst row of the river - lower bank
            {
                return true;
            }
            return false;
        }
        //Method that handles the end of the game, either GAME OVER, SUCCESS or HIGHSCORE screen.
        static void GameOver()
        {
            Console.ResetColor();
            Console.Clear();
            bool isInHighScore = false;
            //Check is the has been completed. If it's not then the player has run out of lives.
            if (isGameComplete)
            {
                //Check if player's scores enter the top 10 best scores
                isInHighScore = GameComplete();
                //If the game is completed but the player's scores don't enter the top 10 scores
                if (!isInHighScore)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    PrintPrompt(new string[] { "SUCCESS!!!", "YOUR SCORE: " + player.scores.ToString() });
                }
            }
            //If the has not been completed, but player has died
            if (!isGameComplete)
            {
                string failString = "GAME OVER!";
                int stringPositionX = (gameWindowWidth / 2) - (failString.Length / 2);
                int stringPositionY = (gameWindowHeight / 2);
                Console.SetCursorPosition(stringPositionX, stringPositionY);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(failString);
                Console.Beep(350, 700);
            }
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.Black;
        }
        //Method for the printing of the high scores and the "CONGRATULATIONS" screen
        static void HighScorePrint(string[] highScores, int index)
        {
            string failString = "CONGRATULATIONS!!!";
            Console.ForegroundColor = ConsoleColor.Yellow;
            int stringPositionX = (gameWindowWidth / 2) - (failString.Length / 2);
            int stringPositionY = (gameWindowHeight / 2) - 12;
            Console.SetCursorPosition(stringPositionX, stringPositionY);
            Console.WriteLine(failString);
            int max = 0;

            for (int i = 0; i < 10; i++)
            {
                if (highScores[i].Length > max)
                {
                    max = highScores[i].Length;
                }
            }

            stringPositionX = (gameWindowWidth / 2) - (max / 2 + 4);
            for (int i = 0; i < highScores.Length; i++)
            {
                failString = (highScores[i].Split('|'))[0] + " -> " + (highScores[i].Split('|'))[1];
                stringPositionY = (gameWindowHeight / 2) - 10 + i * 2;
                if (i == index)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                }

                Console.SetCursorPosition(stringPositionX, stringPositionY);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write((i + 1).ToString() + ". ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write((highScores[i].Split('|'))[0]);
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(" -> ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine((highScores[i].Split('|'))[1]);
            }
            Console.Beep(350, 300);
            Console.Beep(500, 500);
            Console.Beep(700, 700);
        }
        //Prints indication for failiure and plays sound
        static void LevelFailed()
        {
            Console.Beep(700, 300);
            Console.Beep(500, 500);
            Console.Beep(350, 700);
            Console.ResetColor();
            Console.Clear();
            string[] prompts = { "LEVEL FAILED!", "PRESS ANY KEY TO RESTART LEVEL" };
            Console.ForegroundColor = ConsoleColor.Red;
            PrintPrompt(prompts);
            ConsoleKeyInfo keyInfo = Console.ReadKey();
            Console.Clear();
        }
        //Prints indication for level completion and plays sound and asks player if the progress should be saved 
        static void LevelComplete()
        {
            Console.Beep(350, 300);
            Console.Beep(500, 500);
            Console.Beep(700, 700);
            Console.ResetColor();
            Console.Clear();
            if (currentLevel == lastLevel + 1)
            {
                isGameComplete = true;
                GameOver();
            }
            else
            {
                string[] prompts = { "LEVEL COMPLETE!", "Would you like to save your progress?(Y/N)" };
                Console.ForegroundColor = ConsoleColor.Green;
                PrintPrompt(prompts);
                ConsoleKeyInfo keyInfo = Console.ReadKey();
                if (keyInfo.Key.ToString().Equals("Y"))
                {
                    SaveGame();
                }
                Console.Clear();
                prompts = new string[] { "PRESS ANY KEY TO PLAY NEXT LEVEL", "SCORES: " + player.scores };
                PrintPrompt(prompts);
                keyInfo = Console.ReadKey();
                Console.Clear();
            }
        }
        //Checks if the score is in the top 10 and if yes writes it in the HighScore.txt
        static bool GameComplete()
        {
            bool isInHighScore = false;
            string[] highScores = new string[10];
            try
            {
                StreamReader streamReader = new StreamReader(ScoreFilePath);
                using (streamReader)
                {
                    string line = streamReader.ReadLine();
                    for (int i = 0; i < 10; i++)
                    {
                        highScores[i] = line;
                        line = streamReader.ReadLine();
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine("The file cannot be found!");
            }
            catch (DirectoryNotFoundException)
            {
                Console.Error.WriteLine("The directory is invalid!");
            }
            catch (IOException)
            {
                Console.Error.WriteLine("Cannot access the file!");
            }
            if (player.isLoaded)
            {
                DeleteSaveGame();
            }
            //Check if the current score is in the top 10 and take the apropriate place/index for the new record
            int[] realHighScores = new int[10];

            int index = 0;
            for (int i = 0; i < realHighScores.Length; i++)
            {
                realHighScores[i] = Int32.Parse((highScores[i].Split('|'))[1]);
                if (player.scores >= realHighScores[i])
                {
                    isInHighScore = true;
                    index = i;
                    break;
                }
            }
            //If the current score is in the top 10 write the new record and move all that are lower one position down
            //Save changes on file
            if (isInHighScore == true)
            {
                if (index != 9)
                {
                    for (int i = highScores.Length - 2; i >= index; i--)
                    {
                        highScores[i + 1] = highScores[i];
                    }
                }
                highScores[index] = player.name + "|" + player.scores.ToString();
                File.WriteAllText(ScoreFilePath, String.Empty);
                StreamWriter writer = new StreamWriter(ScoreFilePath);
                using (writer)
                {
                    for (int jo = 0; jo < 10; jo++)
                    {
                        writer.WriteLine(highScores[jo]);
                    }
                }
            }
            if (isInHighScore == true)
            {
                HighScorePrint(highScores, index);
            }
            return isInHighScore;
        }
        //A method used to print prompts on the screen
        static void PrintPrompt(string[] prompts)
        {
            int promptPositionY = (gameWindowHeight / 2);
            foreach (var prompt in prompts)
            {
                int promptPositionX = (gameWindowWidth / 2) - (prompt.Length / 2);
                Console.SetCursorPosition(promptPositionX, promptPositionY);
                Console.Write(prompt);
                promptPositionY += 2;
            }
        }
        //Prompt the user to input player name
        static string GetPlayerName()
        {
            string[] prompts = { "WELCOME TO ICE JUMPER!", "ENTER YOUR NAME: " };
            Console.ForegroundColor = ConsoleColor.Green;
            PrintPrompt(prompts);
            Console.ResetColor();
            string name = Console.ReadLine();
            return name.ToUpper();
        }
        //This method returns a list of all records in the save file
        static List<string> LoadSaveFile()
        {
            List<string> saves = new List<string>();
            try
            {
                StreamReader reader = new StreamReader(SaveFilePath);
                using (reader)
                {
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        saves.Add(line);
                        line = reader.ReadLine();
                    }
                }

            }
            catch (FileNotFoundException)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                PrintPrompt(new string[] { "ERROR", "The save file is missing!" });
            }
            catch (DirectoryNotFoundException)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                PrintPrompt(new string[] { "ERROR", "The save file directory is missing!" });
            }
            return saves;

        }
        //Returns the index of the line in which the input userName is in the save file or -1 if there is no such save
        static int IndexOfUserInSaveFile(string userName)
        {
            List<string> saves = LoadSaveFile();
            int index = -1;
            for (int i = 0; i < saves.Count; i++)
            {
                if (saves[i].Split('|')[0].Equals(userName))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
        //Rewrites and existing save or creates a new one
        static void SaveGame()
        {
            try
            {
                if (IndexOfUserInSaveFile(player.name) == -1)   //If there is no such save
                {
                    StreamWriter writer = new StreamWriter(SaveFilePath, true);
                    using (writer)
                    {
                        writer.WriteLine(player.name + "|" + player.lives.ToString() + "|" + player.scores.ToString() + "|" + currentLevel.ToString());
                    }
                }
                else //If such save already exists
                {
                    int index = IndexOfUserInSaveFile(player.name);
                    List<string> saves = LoadSaveFile();
                    saves[index] = player.name + "|" + player.lives.ToString() + "|" + player.scores.ToString() + "|" + currentLevel.ToString();
                    File.WriteAllText(SaveFilePath, String.Empty);
                    StreamWriter writer = new StreamWriter(SaveFilePath);
                    using (writer)
                    {
                        foreach (string save in saves)
                        {
                            writer.WriteLine(save);
                        }
                    }
                }

            }
            catch (FileNotFoundException)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                PrintPrompt(new string[] { "ERROR", "The save file is missing!" });
            }
            catch (DirectoryNotFoundException)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                PrintPrompt(new string[] { "ERROR", "The save file directory is missing!" });
            }

        }
        //Loads a previous save with the same userName as player.name and sets the game in the saved state
        static void LoadGame()
        {
            int index = IndexOfUserInSaveFile(player.name);
            List<string> saves = LoadSaveFile();
            player.lives = Int32.Parse(saves[index].Split('|')[1]);
            player.scores = Int32.Parse(saves[index].Split('|')[2]);
            currentLevel = Int32.Parse(saves[index].Split('|')[3]);
            player.isLoaded = true;
            riverHeight = riverHeight + 2 * (currentLevel - 1);
        }
        //Deletes a save game with the same name as player.name. This method is called only if the game has been loaded and is completed
        static void DeleteSaveGame()
        {
            int index = IndexOfUserInSaveFile(player.name);
            List<string> saves = LoadSaveFile();
            saves.RemoveAt(index);
            File.WriteAllText(SaveFilePath, String.Empty);
            StreamWriter writer = new StreamWriter(SaveFilePath);
            using (writer)
            {
                foreach (string save in saves)
                {
                    writer.WriteLine(save);
                }
            }
        }
        //-----------------------------------------------------------------------
        static void Main()
        {
            Console.Title = GameName;
            Console.WindowWidth = gameWindowWidth;
            Console.WindowHeight = gameWindowHeight;
            Console.BufferHeight = Console.WindowHeight;
            Console.BufferWidth = Console.WindowWidth;
            PrintGameLogo();

            //Initialize the player object
            player = new Player();
            player.name = GetPlayerName();
            player.lives = 3;
            player.scores = 0;

            //If there is a save game ith the same user name ask the user if the save should be loaded
            if (IndexOfUserInSaveFile(player.name) != -1)
            {
                Console.Clear();
                PrintGameLogo();
                Console.ForegroundColor = ConsoleColor.Green;
                PrintPrompt(new string[] { "Would you like to load your previous save?", "Y/N" });
                ConsoleKeyInfo keyInfo = Console.ReadKey();
                Console.WriteLine();
                if (keyInfo.Key.ToString().Equals("Y"))
                {
                    LoadGame();
                }
            }
            Console.CursorVisible = false;

            //Play cicle
            while (currentLevel <= lastLevel && player.lives != 0)
            {
                PlayLevel();

                if (isLevelComplete)
                {
                    Console.ResetColor();
                    currentLevel++;

                    if (currentLevel != 1)
                    {
                        LevelComplete();
                    }

                    riverHeight += 2;
                }
                else
                {
                    if (player.lives != 1)
                    {
                        LevelFailed();
                        Console.ResetColor();
                        player.lives--;
                    }
                    else
                    {
                        GameOver();
                        break;
                    }
                }
            }
        }
    }
}