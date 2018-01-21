using System.Collections;
using UnityEngine;

public class GenerateWorld : MonoBehaviour
{
    public enum TileType  // What type of tiles
    {
        Wall, Floor, Exit, Void   // Exit & Void maybe needed   
    }

    [Header("Level Variables - For general level options")]
    [Range(50, 150)]
    public int xWorld = 50;                                   // X - Size of world
    [Range(50, 150)]
    public int yWorld = 50;                                   // Y - Size of world

    [Header("Room Variables - For changing rooms")]
    public IntRange numRooms = new IntRange(4,6);             // The amount of rooms to generate.
    public IntRange roomWidth = new IntRange(6,8);            // The width of the rooms.
    public IntRange roomHeight = new IntRange(6,8);           // The height of the rooms.

    [Header("Corridor Variables - For changing corridors")]
    public IntRange corridorLength = new IntRange(2,6);       // The length of the connecting corridors.

    [Header("Game Objects - For spawn and loading")]
    public GameObject camera;
    public GameObject player;

    [Header("Prefab Lists - For generating varied levels")]
    public GameObject[] floorTiles;                           // An array of floor tile prefabs.
    public GameObject[] wallTiles;                            // An array of unwalkable tiles.

    [Header("Put the normal enemies here, enemies with multiple attacks")]
    public GameObject[] enemies;                              // An array of enemy prefabs.
    [Header("Put basic enemies here (Enemies that only have 1 attack")]
    public GameObject[] basicEnemies;                         // An array of the basic enemies.
    [Header("Put the boss enemies here, only spawns on higher difficulties")]
    public GameObject[] harderEnemies;

    public GameObject[] props;                                // An array of prop prefabs.
    public GameObject[] notes;                                // An array of note prefabs.
    public GameObject exitTile;
    private TileType[][] tiles;                               // A jagged array that holds information about what tile should be placed.
    private GenerateRoom[] rooms;                             // An array that holds the rooms that have been created.
    private GenerateCorridor[] corridors;                     // An array that holds all the corridors that have been created.
    private GameObject tileHolder;                            // A game object that holds all tiles.
    private GameObject enemyHolder;                           // A game object that holds all enemies.
    private GameObject propHolder;                            // A game object that holds all props.
    private int difficulty;                                   // Variable for game difficulty
    private float enemyDifficultyScale = 1.3f;                // Scales amount of enemies to spawn

    private Vector3 exitPos;                                  // Used for spawning level exit.
    private float lastDistance;                               // Used for checking where to spawn exit.
    private int furthestRoom;                                 // Used for keeping track of the room that is the furthest away from the player spawn.

    //Generates level on startup if the GenerateWorld.cs Script is attached to a game object.
    private void Start()
    {
        //Gets the difficulty from the Level Manager and then creates the level based on the difficulty.
        print(GameObject.FindGameObjectWithTag("Level Manager"));
        difficulty = GameObject.FindGameObjectWithTag("Level Manager").GetComponent<LevelManager>().difficulty;
        print("Difficulty i got from Level Manager = " + difficulty);

        // Create the gameobjects that are used to hold all the tiles and enemies.
        tileHolder = new GameObject("TileHolder");
        enemyHolder = new GameObject("EnemyHolder");
        propHolder = new GameObject("PropHolder");

        SetupTilesArray();

        CreateLevel();

        SetRoomTiles();

        SetCorridorTiles();

        InstantiateTiles();

        SpawnEnemies();

        CheckEnemyCount();

        SpawnProps();

        PlayerSpawn();

        NoteSpawn();

        ExitSpawn();

    }
    

    //Deletes current level and generates a new one
    void GenerateNewLevel()
    {
        int childs = tileHolder.transform.childCount; //Deletes tiles
        for (int i = childs - 1; i > -1; i--)
        {
            GameObject.Destroy(tileHolder.transform.GetChild(i).gameObject);
        }

        childs = enemyHolder.transform.childCount; //Deletes enemies
        for (int i = childs - 1; i > -1; i--)
        {
            GameObject.Destroy(enemyHolder.transform.GetChild(i).gameObject);
        }

        childs = propHolder.transform.childCount; //Deletes props
        for (int i = childs - 1; i > -1; i--)
        {
            GameObject.Destroy(propHolder.transform.GetChild(i).gameObject);
        }

        SetupTilesArray();

        CreateLevel();

        SetRoomTiles();

        SetCorridorTiles();

        InstantiateTiles();

        SpawnEnemies();

        CheckEnemyCount();

        SpawnProps();

        PlayerSpawn();

    }


    //Sets up the tile array that keeps track of what tile types to generate on the map
    void SetupTilesArray()
    {
        // Set the tiles jagged array to the correct width.
        tiles = new TileType[xWorld][];

        // Go through all the tile arrays...
        for (int i = 0; i < tiles.Length; i++)
        {
            // ... and set each tile array is the correct height.
            tiles[i] = new TileType[yWorld];
        }
    }

    //Creates all the rooms and corridors
    void CreateLevel()
    {
        rooms = new GenerateRoom[numRooms.Random + difficulty * 4];
        corridors = new GenerateCorridor[rooms.Length - 1];

        rooms[0] = new GenerateRoom();
        corridors[0] = new GenerateCorridor();
        
        rooms[0].roomGen(roomWidth, roomHeight, xWorld, yWorld);
        corridors[0].CorridorGen(rooms[0], corridorLength, roomWidth, roomHeight, xWorld, yWorld, true);

        for(int i = 1; i < rooms.Length; i++)
        {
            rooms[i] = new GenerateRoom();

            rooms[i].roomGen(roomWidth, roomHeight, xWorld, yWorld, corridors[i - 1], difficulty);

            if (i < corridors.Length)
            {
                corridors[i] = new GenerateCorridor();

                corridors[i].CorridorGen(rooms[i], corridorLength, roomWidth, roomHeight, xWorld, yWorld, false);
            }
        }
    }

    //Sets values for rooms in the Tile array
    void SetRoomTiles()
    {
        // Go through all the rooms...
        for (int i = 0; i < rooms.Length; i++)
        {
            GenerateRoom currentRoom = rooms[i];

            // ... and for each room go through it's width.
            for (int j = 0; j < currentRoom.roomWidth; j++)
            {
                int xCoord = currentRoom.xPos + j;

                // For each horizontal tile, go up vertically through the room's height.
                for (int k = 0; k < currentRoom.roomHeight; k++)
                {
                    int yCoord = currentRoom.yPos + k;

                    // The coordinates in the jagged array are based on the room's position and it's width and height.
                    tiles[xCoord][yCoord] = TileType.Floor;
                }
            }
        }
    }

    //Sets values for corridors in the Tile array
    void SetCorridorTiles()
    {
        for (int i = 0; i < corridors.Length; i++)
        {
            GenerateCorridor currentCorridor = corridors[i];

            // and go through it's length.
            for (int j = 0; j < currentCorridor.corridorLength; j++)
            {
                // Start the coordinates at the start of the corridor.
                int xCoord = currentCorridor.startXPos;
                int yCoord = currentCorridor.startYPos;

                // Depending on the direction, add or subtract from the appropriate
                // coordinate based on how far through the length the loop is.
                switch (currentCorridor.direction)
                {
                    case Direction.North:
                        yCoord += j;
                        break;
                    case Direction.East:
                        xCoord += j;
                        break;
                    case Direction.South:
                        yCoord -= j;
                        break;
                    case Direction.West:
                        xCoord -= j;
                        break;
                }

                // Set the tile at these coordinates to Floor.
                tiles[xCoord][yCoord] = TileType.Floor;
            }
        }
    }
    
    //Goes through the Tile array and instantiates the tiles for the level. Tiles are randomly grabbed from the InstantiateFromArray function to make the levels look more diverse.
    void InstantiateTiles()
    {
        // Go through all the tiles in the jagged array...
        for (int i = 0; i < tiles.Length; i++)
        {
            for (int j = 0; j < tiles[i].Length; j++)
            {
                // If tile type is floor, instantiate floor. 
                if (tiles[i][j] == TileType.Floor)
                { 
                    InstantiateFromArray(floorTiles, i, j, 0f);
                }
                else
                {
                    InstantiateFromArray(wallTiles, i, j, 0f); 
                }



            }
        }
    }

    //Picks a random prefab from the array of prefabs to increase variety in tiles
    void InstantiateFromArray(GameObject[] prefabs, float xCoord, float yCoord, float zCoord)
    {
        // Create a random index for the array.
        int randomIndex = Random.Range(0, prefabs.Length);

        // The position to be instantiated at is based on the coordinates.
        Vector3 position = new Vector3(xCoord, yCoord, zCoord);

        // Create an instance of the prefab from the random index of the array.
        GameObject instantiate = Instantiate(prefabs[randomIndex], position, Quaternion.identity) as GameObject;

        if(instantiate.tag == "Enemy")
        {
            instantiate.transform.parent = enemyHolder.transform;
        }
        else if(instantiate.tag == "Prop")
        {
            instantiate.transform.parent = propHolder.transform;
        }
        else
        {
            // Set the tile's parent to the board holder.
            instantiate.transform.parent = tileHolder.transform;
        }
        
    }
    
    //Spawns enemies in each room (when rooms overlap, they get too many enemies)
    void SpawnEnemies()
    {
        float xSpawn;
        float ySpawn;
        Vector2 sphereCenter;
        float sphereRadius = 2f; //Change this if you want enemies to spawn further away from eachother
        Collider2D[] hitColliders;
        int attempts = 0;

        for (int i = 1; i < rooms.GetLength(0); i++ ) // i = 1 to skip first room
        {
            IntRange xEnemySpawn = new IntRange(1, rooms[i].roomWidth -1);          //Lets enemies spawn in the entire room
            IntRange yEnemySpawn = new IntRange(1, rooms[i].roomHeight -1);
            
            for (int j = 0; j < Mathf.FloorToInt(difficulty * enemyDifficultyScale); j++)
            {
                do
                {
                    xSpawn = Mathf.Floor(rooms[i].xPos + xEnemySpawn.Random);
                    ySpawn = Mathf.Floor(rooms[i].yPos + yEnemySpawn.Random);
                    sphereCenter = new Vector2(xSpawn, ySpawn);
                    hitColliders = Physics2D.OverlapCircleAll(sphereCenter, sphereRadius, 1 << 8);
                    attempts++;
                } while (hitColliders.Length > 0 && attempts < 15); //Attempts is used to prevent an infinite loop when there's no more spots for enemies to spawn in the room
                if(attempts < 15)
                {
                    if(difficulty == 1)
                    {
                        InstantiateFromArray(basicEnemies, xSpawn, ySpawn, 0f);
                    }
                    else if(difficulty > 3)
                    {
                        InstantiateFromArray(harderEnemies, xSpawn, ySpawn, 0f);
                    }
                    else
                    {
                        InstantiateFromArray(enemies, xSpawn, ySpawn, 0f);
                    }
                }
                attempts = 0;
            }
            
        }
    }

    //Checks the count of enemies in a location and deletes any extra enemies that were spawned due to rooms overlapping (Works but not super well right now)
    void CheckEnemyCount()
    {
        Vector3 sphereCenter;
        float sphereRadius;

        for (int i = 0; i < rooms.Length; i++)
        {

            if (rooms[i].roomHeight > rooms[i].roomWidth)
            {
                sphereRadius = rooms[i].roomHeight / 2;
            }
            else
            {
                sphereRadius = rooms[i].roomWidth / 2;
            }

            sphereCenter = new Vector2((rooms[i].xPos + rooms[i].roomWidth / 2), (rooms[i].yPos + rooms[i].roomHeight / 2));
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(sphereCenter, sphereRadius * 1.8f, 1 << 8);
            for (int j = 0; j < hitColliders.Length; j++)
            {
                if (j > Mathf.FloorToInt(difficulty * enemyDifficultyScale))
                    GameObject.Destroy(hitColliders[j].gameObject);
            }

        }

    }

    //Spawns props in the level
    void SpawnProps()
    {
        int propCount = 20; //Higher value gives fewer props
        Vector2 sphereCenter;
        float sphereRadius = 2f;
        int attempts = 0;
        Collider2D[] hitColliders;

        for (int i = 1; i < rooms.Length; i++)
        {
            
            IntRange propX = new IntRange(1, rooms[i].roomWidth -1); //1 and -1 to make sure props dont spawn and block corridors
            IntRange propY = new IntRange(1, rooms[i].roomHeight -1);

            for (int j = 0; j < Mathf.Floor((rooms[i].roomHeight * rooms[i].roomWidth) / propCount); j++) //Will spawn an amount of props based on room size
            {

                do
                {
                    sphereCenter = new Vector2(rooms[i].xPos + propX.Random, rooms[i].yPos + propY.Random);
                    hitColliders = Physics2D.OverlapCircleAll(sphereCenter, sphereRadius, 1 << 8);
                    attempts++;
                } while (hitColliders.Length > 0 && attempts < 15); //Attempts is used to prevent an infinite loop when there's no more spots for props to spawn in the room
                if (attempts < 15)
                {
                    InstantiateFromArray(props, sphereCenter.x, sphereCenter.y, 0f);
                }
                attempts = 0;
            }
        }
    }

    //Initial positions for players and camera (placeholder)
    void PlayerSpawn()
    {
        //Spawns player and camera in the middle of the room
        float xHold = rooms[0].xPos + (rooms[0].roomWidth / 2);
        float yHold = rooms[0].yPos + (rooms[0].roomHeight / 2);
        xHold = Mathf.Floor(xHold);
        yHold = Mathf.Floor(yHold);

        Vector3 vPlayer = new Vector3(xHold, yHold, 0);
        Quaternion qPlayer = new Quaternion(0, 0, 0, 0);
        
        player.transform.SetPositionAndRotation(vPlayer, qPlayer);

        int sphereRadius;
        if (rooms[0].roomWidth > rooms[0].roomHeight)
        {
            sphereRadius = rooms[0].roomWidth;
        }
        else
        {
            sphereRadius = rooms[0].roomHeight;
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(vPlayer, sphereRadius -1, 1 << 8);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            GameObject.Destroy(hitColliders[i].gameObject);
        }
    }

    //Spawns the exit that is used for going to the next level.
    void ExitSpawn()
    {
        for(int i = 0; i < rooms.Length; i++)
        {
            exitPos.Set(rooms[i].xPos, rooms[i].yPos, 0f);
            float distance = Vector3.Distance(player.transform.position, exitPos);
            
            if(distance > lastDistance)
            {
                lastDistance = distance;
                furthestRoom = i;
            }
        }

        exitPos.Set(rooms[furthestRoom].xPos + rooms[furthestRoom].roomWidth/2, rooms[furthestRoom].yPos + rooms[furthestRoom].roomWidth / 2, 0f);

        //Spawn exit in the middle of room "i" 
        Instantiate(exitTile, exitPos, Quaternion.identity);
        
    }

    void NoteSpawn()
    {
        for (int i = 0; i < xWorld; i++)
        {
            for(int j = 0; j < yWorld; j++)
            {
                
                if(Random.Range(0, 10) == 1 && tiles[i][j] == TileType.Wall)
                {
                    InstantiateFromArray(notes, i, j, 0);
                }
            }
        }
    }
    
}




