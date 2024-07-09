using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using static WFC;

public enum direction { up, down, left, right, center, down_left, down_right, up_left, up_right, endpoint, rightRandom, leftRandom, upRandom, downRandom }

[System.Serializable]
public class MapTiles
{
    public GameObject mapTile;
    // Status Hint: 1 = Way, 0 = Decoration
    public int Right_Status;
    public int Left_Status;
    public int Up_Status;
    public int Down_Status;
    [HideInInspector] public int X_Coordinate;
    [HideInInspector] public int Y_Coordinate;
    [HideInInspector] public Node Node;
    public bool can_be_a_path;
}


public interface IGridOperations
{
    Node NodeFromWorldPoint(Vector3 WorldPos);
    Node NodeFromCoordinates(int x, int y);
    Vector3 WorldposFromNode(Node node);
    Vector3 WorldposFromCoordinates(int x, int y);
    List<Node> GetNeighbours(Node node);
    int MaxSize { get; }
    bool CompareTwoCoordinates(Node firstNode, Node secondNode);
    List<MapTiles> GetMapTilesFromCoordinate(int x, int y);
    void SetMapTilesFromCoordinate(int x, int y, List<MapTiles> mapTiles);

}

public interface IGridGizmoDrawer
{
    void DrawGizmos();
}



public interface IMapStyle
{
    void ApplyStyle(Grid grid);
}


public class ClassicMapStyle : IMapStyle
{

    public void ApplyStyle(Grid grid)
    {
        WFC wfc = instance;
        wfc.SetUpBase(direction.center);
        wfc.SetUpDownSide();
        wfc.SetUpLeftSide();
        wfc.SetUpRightSide();
        wfc.SetUpUpSide();
        wfc.SetupBorder();
    }
}
public class OneEnterMapStyle : IMapStyle
{

    public void ApplyStyle(Grid grid)
    {
        WFC wfc = instance;
        wfc.SetUpBase(direction.left);
        wfc.SetUpRightSide();
        wfc.SetupBorder();
    }
}
public class WorldAMapStyle : IMapStyle
{

    public void ApplyStyle(Grid grid)
    {
        WFC wfc = instance;
        wfc.SetUpBase(direction.down_left);
        wfc.SetUpEndSide();
        wfc.StartToCollapse();
    }
}
public class WorldBMapStyle : IMapStyle
{

    public void ApplyStyle(Grid grid)
    {
        WFC wfc = instance;
        wfc.SetUpBase(direction.down_right);
        wfc.SetUpEndSide();
        wfc.StartToCollapse();
    }
}public class WorldCMapStyle : IMapStyle
{

    public void ApplyStyle(Grid grid)
    {
        WFC wfc = instance;
        wfc.SetUpBase(direction.down);
        wfc.SetUpEndSide(wfc.North_Entrance_Tile, direction.up_right);
        wfc.SetUpEndSide(wfc.West_Entrance_Tile, direction.up_left);
        wfc.StartToCollapse();
    }
}public class WorldDMapStyle : IMapStyle
{

    public void ApplyStyle(Grid grid)
    {

        WFC wfc = instance;
        wfc.SetUpBase(direction.down);
        wfc.SetUpEndSide(wfc.North_Entrance_Tile, direction.up);
        wfc.SetUpEndSide(wfc.West_Entrance_Tile, direction.left);
        wfc.SetUpEndSide(wfc.East_Entrance_Tile, direction.right);
        wfc.StartToCollapse();
    }
}
public class WorldEMapStyle : IMapStyle
{

    public void ApplyStyle(Grid grid)
    {
        WFC wfc = instance;
        wfc.SetUpBase(direction.center);
        wfc.SetUpEndSide(wfc.North_Entrance_Tile, direction.upRandom);
        wfc.SetUpEndSide(wfc.West_Entrance_Tile, direction.leftRandom);
        wfc.SetUpEndSide(wfc.East_Entrance_Tile, direction.rightRandom);
        wfc.SetUpEndSide(wfc.South_Entrance_Tile, direction.downRandom);
        wfc.StartToCollapse();
    }
}


public class MapStyleFactory
{
    public static IMapStyle GetMapStyle(MapStyle mapStyle)
    {
        switch (mapStyle)
        {
            case MapStyle.Classic:
                return new ClassicMapStyle();
            case MapStyle.OneEnter:
                return new OneEnterMapStyle();
            case MapStyle.WorldA:
                return new WorldAMapStyle();
            case MapStyle.WorldB:
                return new WorldBMapStyle();
            case MapStyle.WorldC:
                return new WorldCMapStyle();
            case MapStyle.WorldD:
                return new WorldDMapStyle();
            case MapStyle.WorldE:
                return new WorldEMapStyle();
            default:
                throw new ArgumentException("Invalid map style");
        }
    }
}

public enum MapStyle
{
    Classic,
    OneEnter,
    WorldA,
    WorldB,
    WorldC,
    WorldD,
    WorldE
}

[RequireComponent(typeof(SpawnPoints_Manager))]
public class WFC : MonoBehaviour
{
    #region Values
    public MapTiles BaseTile;
    public MapTiles North_Entrance_Tile;
    public MapTiles East_Entrance_Tile;
    public MapTiles West_Entrance_Tile;
    public MapTiles South_Entrance_Tile;
    public MapTiles None;

    private IGridOperations grid;
    private List<MapTiles> _candidateMapTiles;
    public MapStyle _mapStyle;

    public static WFC instance;

    [HideInInspector] public int gridSizeX, gridSizeY;

    [HideInInspector] public Node BaseNode;
    [HideInInspector] public List<MapTiles> EndNodes = new List<MapTiles>();

    List<MapTiles> Canditate_MapTiles = new List<MapTiles>();
    public List<MapTiles> Backup_MapTiles = new List<MapTiles>();
    int random;
    CombineMeshes borderMeshCombiner, environmentMeshCombiner;
    #endregion

    #region Initialization

    void Awake()
    {
        instance = this;
        grid = Grid.instance;
        _candidateMapTiles = new List<MapTiles>();
    }

    private void ApplyMapStyle()
    {
        var mapStyle = MapStyleFactory.GetMapStyle(_mapStyle);
        mapStyle.ApplyStyle(Grid.instance);
    }

    public void MyStart() 
    {
        enemyRespawnPoints = Enemy_RespawnManger.instance;
        pool_Manager = Pool_Manager.instance;
        GetGridSize();
        ApplyMapStyle();
    }

    #endregion

    #region Setups
    public void SetupBorder()
    {
        var gridSizeRange = Enumerable.Range(0, gridSizeX + 1);

        var coordinatesToCheck = gridSizeRange.SelectMany(i => new[]
        {
            new { X = i, Y = 0 },
            new { X = i, Y = gridSizeY },
            new { X = 0, Y = i },
            new { X = gridSizeY, Y = i }
        });

        foreach (var coord in coordinatesToCheck)
        {
            var node = grid.NodeFromCoordinates(coord.X, coord.Y);
            if (node.Walkable)
            {
                node.Node_MapTile = None;
                node.Walkable = false;
                node.can_be_a_path = false;

                var newTileMap = Instantiate(None.mapTile);
                newTileMap.transform.position = grid.WorldposFromCoordinates(coord.X, coord.Y);
                newTileMap.transform.parent = Border.transform;

                var chosen_maptile = new List<MapTiles> { None };
                grid.SetMapTilesFromCoordinate(coord.X, coord.Y, chosen_maptile);

                // propagate changes
                propgate_changes(coord.X, coord.Y);
            }
        }

        CombineMeshes();

    }

    public void SetUpBase(direction dir)
    {
        switch (dir)
        {
            case direction.up:
                BaseTile.X_Coordinate = gridSizeX / 2;
                BaseTile.Y_Coordinate = gridSizeY;
                break;
            case direction.down:
                BaseTile.X_Coordinate = gridSizeX / 2;
                BaseTile.Y_Coordinate = 0;
                break;
            case direction.left:
                BaseTile.X_Coordinate = 0;
                BaseTile.Y_Coordinate = gridSizeY / 2;
                break;
            case direction.right:
                BaseTile.X_Coordinate = gridSizeX;
                BaseTile.Y_Coordinate = gridSizeY / 2;
                break;
            case direction.center:
                BaseTile.X_Coordinate = gridSizeX / 2;
                BaseTile.Y_Coordinate = gridSizeY / 2;
                break;
            case direction.down_left:
                BaseTile.X_Coordinate = 0;
                BaseTile.Y_Coordinate = 0;
                break;
            case direction.down_right:
                BaseTile.X_Coordinate = gridSizeX;
                BaseTile.Y_Coordinate = 0;
                break;
        }

        Node node = grid.NodeFromCoordinates(BaseTile.X_Coordinate, BaseTile.Y_Coordinate);
        node.Node_MapTile = BaseTile;
        node.Walkable = false;
        node.TheEnd = true;
        node.Explored = true;
        node.can_be_a_path = BaseTile.can_be_a_path;
        BaseNode = node;
        List<MapTiles> chosen_maptile = new List<MapTiles>
        {
            BaseTile
        };
        grid.SetMapTilesFromCoordinate(BaseTile.X_Coordinate, BaseTile.Y_Coordinate, chosen_maptile);
        BaseTile.mapTile.transform.position = grid.WorldposFromCoordinates(BaseTile.X_Coordinate, BaseTile.Y_Coordinate);
        // propagte changes
        propgate_changes(BaseTile.X_Coordinate, BaseTile.Y_Coordinate);
        expandList.Add(BaseTile.mapTile);
        SetupFlag(BaseTile.mapTile.transform);
    }

    public void SetUpRightSide()
    {
        int random = UnityEngine.Random.Range(1, gridSizeY - 1);
        East_Entrance_Tile.X_Coordinate = gridSizeX;
        East_Entrance_Tile.Y_Coordinate = random;
        Node node = grid.NodeFromCoordinates(East_Entrance_Tile.X_Coordinate, East_Entrance_Tile.Y_Coordinate);
        node.Node_MapTile = East_Entrance_Tile;
        node.TheEnd = true;
        node.Walkable = false;
        node.can_be_a_path = East_Entrance_Tile.can_be_a_path;
        List<MapTiles> chosen_maptile = new List<MapTiles>
        {
            East_Entrance_Tile
        };
        grid.SetMapTilesFromCoordinate(East_Entrance_Tile.X_Coordinate, East_Entrance_Tile.Y_Coordinate, chosen_maptile);
        East_Entrance_Tile.mapTile.transform.position = grid.WorldposFromCoordinates(East_Entrance_Tile.X_Coordinate, East_Entrance_Tile.Y_Coordinate);
        // propagte changes
        propgate_changes(East_Entrance_Tile.X_Coordinate, East_Entrance_Tile.Y_Coordinate);
    }

    public void SetUpLeftSide()
    {
        int random = UnityEngine.Random.Range(1, gridSizeY - 1);
        West_Entrance_Tile.X_Coordinate = 0;
        West_Entrance_Tile.Y_Coordinate = random;
        Node node = grid.NodeFromCoordinates(West_Entrance_Tile.X_Coordinate, West_Entrance_Tile.Y_Coordinate);
        node.Node_MapTile = West_Entrance_Tile;
        node.TheEnd = true;
        node.Walkable = false;
        node.can_be_a_path = West_Entrance_Tile.can_be_a_path;
        List<MapTiles> chosen_maptile = new List<MapTiles>
        {
            West_Entrance_Tile
        };
        grid.SetMapTilesFromCoordinate(West_Entrance_Tile.X_Coordinate, West_Entrance_Tile.Y_Coordinate, chosen_maptile);
        West_Entrance_Tile.mapTile.transform.position = grid.WorldposFromCoordinates(West_Entrance_Tile.X_Coordinate, West_Entrance_Tile.Y_Coordinate);
        // propagte changes
        propgate_changes(West_Entrance_Tile.X_Coordinate, West_Entrance_Tile.Y_Coordinate);
    }

    public void SetUpUpSide()
    {
        int random = UnityEngine.Random.Range(1, gridSizeY - 1);
        North_Entrance_Tile.X_Coordinate = random;
        North_Entrance_Tile.Y_Coordinate = gridSizeY;
        Node node = grid.NodeFromCoordinates(North_Entrance_Tile.X_Coordinate, North_Entrance_Tile.Y_Coordinate);
        node.Node_MapTile = North_Entrance_Tile;
        node.TheEnd = true;
        node.Walkable = false;
        node.can_be_a_path = South_Entrance_Tile.can_be_a_path;
        List<MapTiles> chosen_maptile = new List<MapTiles>
        {
            North_Entrance_Tile
        };
        grid.SetMapTilesFromCoordinate(North_Entrance_Tile.X_Coordinate, North_Entrance_Tile.Y_Coordinate, chosen_maptile);
        North_Entrance_Tile.mapTile.transform.position = grid.WorldposFromCoordinates(North_Entrance_Tile.X_Coordinate, North_Entrance_Tile.Y_Coordinate);
        // propagte changes
        propgate_changes(North_Entrance_Tile.X_Coordinate, North_Entrance_Tile.Y_Coordinate);
    }
    public void SetUpEndSide()
    {
        //int random = UnityEngine.Random.Range(1, gridSizeY - 1);
        switch (_mapStyle)
        {
            case MapStyle.WorldA:
                North_Entrance_Tile.X_Coordinate = gridSizeX;
                North_Entrance_Tile.Y_Coordinate = gridSizeY;

                break;
            case MapStyle.WorldB:
                North_Entrance_Tile.X_Coordinate = 0;
                North_Entrance_Tile.Y_Coordinate = gridSizeY;
                break;
            default:
                break;
        }

        Node node = grid.NodeFromCoordinates(North_Entrance_Tile.X_Coordinate, North_Entrance_Tile.Y_Coordinate);
        node.Node_MapTile = North_Entrance_Tile;
        node.Walkable = false;
        node.TheEnd = true;
        node.Explored = true;
        node.can_be_a_path = South_Entrance_Tile.can_be_a_path;
        EndNodes.Add(North_Entrance_Tile);
        List<MapTiles> chosen_maptile = new List<MapTiles>
        {
            North_Entrance_Tile
        };
        grid.SetMapTilesFromCoordinate(North_Entrance_Tile.X_Coordinate, North_Entrance_Tile.Y_Coordinate, chosen_maptile);
        North_Entrance_Tile.mapTile.transform.position = grid.WorldposFromCoordinates(North_Entrance_Tile.X_Coordinate, North_Entrance_Tile.Y_Coordinate);
        // propagte changes
        propgate_changes(North_Entrance_Tile.X_Coordinate, North_Entrance_Tile.Y_Coordinate);
        expandList.Add(North_Entrance_Tile.mapTile);
    }

    public void SetUpEndSide(MapTiles mapTile, direction dir)
    {
        //int random = UnityEngine.Random.Range(1, gridSizeY - 1);
        var directionCoordinates = new Dictionary<direction, (int X, int Y)>{
    { direction.up, (gridSizeX / 2, gridSizeY) },
    { direction.down, (gridSizeX / 2, 0) },
    { direction.left, (0, gridSizeY / 2) },
    { direction.right, (gridSizeX, gridSizeY / 2) },
    { direction.down_left, (0, 0) },
    { direction.down_right, (gridSizeX, 0) },
    { direction.up_left, (0, gridSizeY) },
    { direction.up_right, (gridSizeX, gridSizeY) },
    { direction.upRandom, (UnityEngine.Random.Range(2, gridSizeY - 2), gridSizeY) },
    { direction.rightRandom, (gridSizeX, UnityEngine.Random.Range(1, gridSizeY - 1)) },
    { direction.leftRandom, (0, UnityEngine.Random.Range(1, gridSizeY - 1)) },
    { direction.downRandom, (UnityEngine.Random.Range(2, gridSizeY - 2), 0) }};

        if (directionCoordinates.TryGetValue(dir, out var coordinates))
        {
            mapTile.X_Coordinate = coordinates.X;
            mapTile.Y_Coordinate = coordinates.Y;

            if (dir == direction.upRandom)
            {
                North_Entrance_Tile.X_Coordinate = coordinates.X;
                North_Entrance_Tile.Y_Coordinate = coordinates.Y;
            }
            else if (dir == direction.rightRandom)
            {
                East_Entrance_Tile.X_Coordinate = coordinates.X;
                East_Entrance_Tile.Y_Coordinate = coordinates.Y;
            }
            else if (dir == direction.leftRandom)
            {
                West_Entrance_Tile.X_Coordinate = coordinates.X;
                West_Entrance_Tile.Y_Coordinate = coordinates.Y;
            }
            else if (dir == direction.downRandom)
            {
                South_Entrance_Tile.X_Coordinate = coordinates.X;
                South_Entrance_Tile.Y_Coordinate = coordinates.Y;
            }

            Node node = grid.NodeFromCoordinates(mapTile.X_Coordinate, mapTile.Y_Coordinate);
            node.Node_MapTile = mapTile;
            node.Walkable = false;
            node.TheEnd = true;
            node.Explored = true;
            node.can_be_a_path = South_Entrance_Tile.can_be_a_path;

            EndNodes.Add(mapTile);

            var chosen_mapTile = new List<MapTiles> { mapTile };
            grid.SetMapTilesFromCoordinate(mapTile.X_Coordinate, mapTile.Y_Coordinate, chosen_mapTile);

            mapTile.mapTile.transform.position = grid.WorldposFromCoordinates(mapTile.X_Coordinate, mapTile.Y_Coordinate);

            // propagate changes
            propgate_changes(mapTile.X_Coordinate, mapTile.Y_Coordinate);

            expandList.Add(mapTile.mapTile);
        }
        else
        {
            Debug.LogError("This direction does not have a valid mapping for map tiles.");
        }

    }

    public void SetUpDownSide()
    {
        int random = UnityEngine.Random.Range(1, gridSizeY - 1);
        South_Entrance_Tile.X_Coordinate = random;
        South_Entrance_Tile.Y_Coordinate = 0;
        Node node = grid.NodeFromCoordinates(South_Entrance_Tile.X_Coordinate, South_Entrance_Tile.Y_Coordinate);
        node.Node_MapTile = South_Entrance_Tile;
        node.Walkable = false;
        node.TheEnd = true;
        node.can_be_a_path = South_Entrance_Tile.can_be_a_path;
        List<MapTiles> chosen_maptile = new List<MapTiles>
        {
            South_Entrance_Tile
        };
        grid.SetMapTilesFromCoordinate(South_Entrance_Tile.X_Coordinate, South_Entrance_Tile.Y_Coordinate, chosen_maptile);
        South_Entrance_Tile.mapTile.transform.position = grid.WorldposFromCoordinates(South_Entrance_Tile.X_Coordinate, South_Entrance_Tile.Y_Coordinate);
        // propagte changes
        propgate_changes(South_Entrance_Tile.X_Coordinate, South_Entrance_Tile.Y_Coordinate);
    }
    #endregion

    #region Check
    bool checkThe_Up_Tile(int x, int y, MapTiles Possibletile)
    {
        bool isConditionMet = (y + 1 <= gridSizeY) &&(grid.NodeFromCoordinates(x, y + 1).Walkable ||grid.NodeFromCoordinates(x, y + 1).Node_MapTile.Down_Status == Possibletile.Up_Status) ||(y + 1 > gridSizeY && Possibletile.Up_Status == 0);

        return isConditionMet;

    }
    bool checkThe_Down_Tile(int x, int y, MapTiles Possibletile)
    {
        bool isConditionMet = (y - 1 >= 0) &&(grid.NodeFromCoordinates(x, y - 1).Walkable ||grid.NodeFromCoordinates(x, y - 1).Node_MapTile.Up_Status == Possibletile.Down_Status) ||(y - 1 < 0 && Possibletile.Down_Status == 0);

        return isConditionMet;

    }
    bool checkThe_Left_Tile(int x, int y, MapTiles Possibletile)
    {
        bool isConditionMet = (x - 1 >= 0 && (grid.NodeFromCoordinates(x - 1, y).Walkable ||grid.NodeFromCoordinates(x - 1, y).Node_MapTile.Right_Status == Possibletile.Left_Status)) ||(x - 1 < 0 && Possibletile.Left_Status == 0);

        return isConditionMet;

    }
    bool checkThe_Right_Tile(int x, int y, MapTiles Possibletile)
    {
        bool isConditionMet = (x + 1 <= gridSizeX && (grid.NodeFromCoordinates(x + 1, y).Walkable || grid.NodeFromCoordinates(x + 1, y).Node_MapTile.Left_Status == Possibletile.Right_Status)) ||(x + 1 > gridSizeX && Possibletile.Right_Status == 0);

        return isConditionMet;

    }



    #endregion

    #region Methodes
    GameObject Environment;
    GameObject Border;
    GameObject ExpandTiles;
    int Table_area;
    Node Node_withLowest_Possibliti;
    void GetGridSize()
    {
        gridSizeX = Grid.instance.gridSizeX - 1;
        gridSizeY = Grid.instance.gridSizeY - 1;

        Table_area = (gridSizeX + 1) * (gridSizeY + 1);
        Environment = GameObject.FindGameObjectWithTag("Environment");
        Border = GameObject.FindGameObjectWithTag("Border");
        ExpandTiles = GameObject.FindGameObjectWithTag("ExpandTiles");
        borderMeshCombiner = Border.GetComponent<CombineMeshes>();
        environmentMeshCombiner = Environment.GetComponent<CombineMeshes>();
    }


    public Vector3 GetWorldPosFromCoordinate(int i, int j) => grid.WorldposFromCoordinates(i, j);

    #endregion

    #region Wave Functional Collapse
    public void StartToCollapse() => StartCoroutine(WaveFunctioanlCollapse());
    public IEnumerator WaveFunctioanlCollapse()
    {
        for (int i = 0; i <= Table_area; i++)
        {
            yield return new WaitForSeconds(.1f);
            Findlowest_Possibleti();
        }
        StartCoroutine(gameObject.GetComponent<SpawnPoints_Manager>().TriggerSpawnPoints());
    }
    public void FindPathForBoss()
    {
        gameObject.GetComponent<SpawnPoints_Manager>().ExploreForBoss();
        enemyRespawnPoints.Spawn();
    }
    float lowestnumber;
    public void Findlowest_Possibleti()
    {
        // Initialize lowestnumber to the count of Instance_Tiles
        lowestnumber = Grid.instance.Instance_Tiles.Count;

        // Find the node with the lowest number of possible tiles
        var allNodes = from i in Enumerable.Range(0, gridSizeX + 1)
                       from j in Enumerable.Range(0, gridSizeY + 1)
                       let node = grid.NodeFromCoordinates(i, j)
                       where node.Walkable && (!itsfirstime || node.Possible_Node_MapTile.Count != 1)
                       select node;

        var firstNode = grid.NodeFromCoordinates(0, 0);
        if (itsfirstime)
        {
            lowestnumber = firstNode.Possible_Node_MapTile.Count;
            Node_withLowest_Possibliti = firstNode;
        }
        else
        {
            var nodeWithLowestPossibility = allNodes.OrderBy(node => node.Possible_Node_MapTile.Count).FirstOrDefault();
            if (nodeWithLowestPossibility != null)
            {
                lowestnumber = nodeWithLowestPossibility.Possible_Node_MapTile.Count;
                Node_withLowest_Possibliti = nodeWithLowestPossibility;
            }
        }

        itsfirstime = false;

        // Start to collapse wave function
        if (Node_withLowest_Possibliti != null && Node_withLowest_Possibliti.Walkable)
        {
            collapse_at(Node_withLowest_Possibliti.GridX, Node_withLowest_Possibliti.GridY);
        }
    }

    #region ExpandMethodes
    //expanding the map
    public void TurnOnTheNextPart(int x_cordinate, int y_cordinate)
    {
        Node tile = grid.NodeFromCoordinates(x_cordinate, y_cordinate);
        var worldPos = tile.WorldPosition;
        var matchingItem = expandList.FirstOrDefault(item => item.transform.position == worldPos);
        if (matchingItem != null)
        {
            DustEffect(worldPos);
            matchingItem.SetActive(true);
            tile.Explored = true;
        }
    }

    bool CheckExpandList(int x, int y)
    {
        var tile = grid.NodeFromCoordinates(x, y).WorldPosition;

        bool isActive = expandList.Where(item => item.transform.position == tile).Any(item => item.activeInHierarchy);

        return isActive;
    }

    Pool_Manager pool_Manager;
    public void FindNextCandidateforExpande()
    {
        foreach (GameObject mapTile in expandList)
        {
            if (mapTile.activeInHierarchy)
            {
                Node tile = grid.NodeFromWorldPoint(mapTile.transform.position);
                if (tile.TheEnd) continue;
                int i = tile.GridX;
                int j = tile.GridY;
                //right
                if (i + 1 <= gridSizeX && !CheckExpandList(i + 1, j) && grid.NodeFromCoordinates(i + 1, j).Node_MapTile.Left_Status == 1)
                {
                    Vector3 pos = grid.WorldposFromCoordinates(i + 1, j);
                    var expandButton = pool_Manager.GetPoolObject(Pool_Manager.Object.Expand_Button);
                    expandButton.transform.position = pos;
                    expandButton.GetComponent<ExpandButton>().SetCoordinate(i + 1, j);//, gridSizeX, gridSizeY
                    expandButton.SetActive(true);
                }
                //left
                if (i - 1 >= 0 && !CheckExpandList(i - 1, j) && grid.NodeFromCoordinates(i - 1, j).Node_MapTile.Right_Status == 1)
                {
                    Vector3 pos = grid.WorldposFromCoordinates(i - 1, j);
                    var expandButton = pool_Manager.GetPoolObject(Pool_Manager.Object.Expand_Button);
                    expandButton.transform.position = pos;
                    expandButton.GetComponent<ExpandButton>().SetCoordinate(i - 1, j);//, gridSizeX, gridSizeY
                    expandButton.SetActive(true);
                }
                //up
                if (j + 1 <= gridSizeY && !CheckExpandList(i, j + 1) && grid.NodeFromCoordinates(i, j + 1).Node_MapTile.Down_Status == 1)
                {
                    Vector3 pos = grid.WorldposFromCoordinates(i, j + 1);
                    var expandButton = pool_Manager.GetPoolObject(Pool_Manager.Object.Expand_Button);
                    expandButton.transform.position = pos;
                    expandButton.GetComponent<ExpandButton>().SetCoordinate(i, j + 1);//, gridSizeX, gridSizeY
                    expandButton.SetActive(true);
                }
                //down
                if (j - 1 >= 0 && !CheckExpandList(i, j - 1) && grid.NodeFromCoordinates(i, j - 1).Node_MapTile.Up_Status == 1)
                {
                    Vector3 pos = grid.WorldposFromCoordinates(i, j - 1);
                    var expandButton = pool_Manager.GetPoolObject(Pool_Manager.Object.Expand_Button);
                    expandButton.transform.position = pos;
                    expandButton.GetComponent<ExpandButton>().SetCoordinate(i, j - 1);//, gridSizeX, gridSizeY
                    expandButton.SetActive(true);
                }
            }
        }
    }

    public void FindSpawnPoints()
    {
        // Clear previous spawn points and turn off spawn points
        enemyRespawnPoints.SpawnPoints.Clear();
        pool_Manager.TurnOffSpawnPoints();

        // Get all valid spawn points
        var spawnPoints = expandList.Where(mapTile => mapTile.activeInHierarchy).Select(mapTile => grid.NodeFromWorldPoint(mapTile.transform.position)).Where(tile => !tile.TheEnd && (tile.GridX != 0 || tile.GridY != 0)).SelectMany(tile =>
        {
            int i = tile.GridX;
            int j = tile.GridY;

            var directions = new List<(int offsetX, int offsetY, Func<Node, int> statusCheck)>{
            (1, 0, t => t.Node_MapTile.Left_Status),  // right
            (-1, 0, t => t.Node_MapTile.Right_Status), // left
            (0, 1, t => t.Node_MapTile.Down_Status), // up
            (0, -1, t => t.Node_MapTile.Up_Status)   // down
                };

            return directions.Where(direction => i + direction.offsetX >= 0 && i + direction.offsetX <= gridSizeX &&
                                    j + direction.offsetY >= 0 && j + direction.offsetY <= gridSizeY &&
                                    !CheckExpandList(i + direction.offsetX, j + direction.offsetY) &&
                                    direction.statusCheck(grid.NodeFromCoordinates(i + direction.offsetX, j + direction.offsetY)) == 1).Select(direction => grid.WorldposFromCoordinates(i + direction.offsetX, j + direction.offsetY));
        }).ToList();

        // Setup spawn points
        spawnPoints.ForEach(pos => SetupSpawnpoint(pos));

        // Finalize setup
        Tower_Holders_Manager.instance.UndoFreez();
        CombineMeshes();
        enemyRespawnPoints.Spawn();
    }

    Enemy_RespawnManger enemyRespawnPoints;
    void SetupSpawnpoint(Vector3 pos)
    {
        var spawnpoint = pool_Manager.GetPoolObject(Pool_Manager.Object.SpawnPoints);
        spawnpoint.transform.position = pos;
        spawnpoint.SetActive(true);
        spawnpoint.GetComponent<PathRequester>().RequestForExplore();
        //spawnpoint.Explore();
        enemyRespawnPoints.SpawnPoints.Add(spawnpoint);
    }
    #endregion

    bool itsfirstime = true;
    readonly List<GameObject> expandList = new List<GameObject>();
    void collapse_at(int x, int y)
    {
        // choose a random map tile frome nodes possiblity

        // and make node not walkable
        Canditate_MapTiles.Clear();

        var validMapTiles = grid.GetMapTilesFromCoordinate(x, y)
            .Where(item => checkThe_Down_Tile(x, y, item) && checkThe_Up_Tile(x, y, item) && checkThe_Left_Tile(x, y, item) && checkThe_Right_Tile(x, y, item) &&
                           CheckThe_Down_Compatibility(x, y, item) && CheckThe_Up_Compatibility(x, y, item) && CheckThe_Right_Compatibility(x, y, item) && CheckThe_Left_Compatibility(x, y, item));

        Canditate_MapTiles.AddRange(validMapTiles);

        if (!Canditate_MapTiles.Any())
        {
            var validBackupMapTiles = Backup_MapTiles
                .Where(item => checkThe_Down_Tile(x, y, item) && checkThe_Up_Tile(x, y, item) && checkThe_Left_Tile(x, y, item) && checkThe_Right_Tile(x, y, item) &&
                               CheckThe_Down_Compatibility(x, y, item) && CheckThe_Up_Compatibility(x, y, item) && CheckThe_Right_Compatibility(x, y, item) && CheckThe_Left_Compatibility(x, y, item));

            Canditate_MapTiles.AddRange(validBackupMapTiles);
        }

        int random = UnityEngine.Random.Range(0, Canditate_MapTiles.Count);
        MapTiles randomMapTile = Canditate_MapTiles[random];

        Node node = grid.NodeFromCoordinates(x, y);
        node.Node_MapTile = randomMapTile;
        node.Walkable = false;
        node.can_be_a_path = randomMapTile.can_be_a_path;

        var chosen_maptile = new List<MapTiles> { randomMapTile };
        grid.SetMapTilesFromCoordinate(x, y, chosen_maptile);

        GameObject newTileMap = Instantiate(randomMapTile.mapTile);
        newTileMap.transform.position = grid.WorldposFromCoordinates(x, y);

        if (randomMapTile.can_be_a_path)
        {
            expandList.Add(newTileMap);
            newTileMap.transform.parent = ExpandTiles.transform;
            newTileMap.SetActive(false);
        }
        else
        {
            newTileMap.transform.parent = Environment.transform;
        }

        // Propagate changes
        propgate_changes(x, y);
    }

    List<MapTiles> list = new List<MapTiles>();
    void propgate_changes(int x, int y)
    {
        //-----------------------LEFT-----------------------------
        //check neighbours in 4 directions till the end or find solid maptile(wich means till finding unwalkable node)
        //checking left side
        if (x - 1 >= 0)
        {
            var leftNode = grid.NodeFromCoordinates(x - 1, y);

            if (leftNode.Walkable)
            {
                var leftPossibleTiles = leftNode.Possible_Node_MapTile;
                var currentPossibleTiles = grid.NodeFromCoordinates(x, y).Possible_Node_MapTile;

                var Left_list = leftPossibleTiles
                    .Where(left_possible => currentPossibleTiles.Any(right_possible => left_possible.Right_Status == right_possible.Left_Status) &&
                                            checkThe_Down_Tile(x - 1, y, left_possible) &&
                                            checkThe_Up_Tile(x - 1, y, left_possible) &&
                                            checkThe_Left_Tile(x - 1, y, left_possible) &&
                                            checkThe_Right_Tile(x - 1, y, left_possible))
                    .ToList();

                grid.SetMapTilesFromCoordinate(x - 1, y, Left_list);

                if (Left_list.Count == 1)
                {
                    collapse_at(x, y);
                }
            }
        }

        //-----------------------RIGHT-----------------------------
        //checking rightside side
        if (x + 1 <= gridSizeX)
        {
            var rightNode = grid.NodeFromCoordinates(x + 1, y);

            if (rightNode.Walkable)
            {
                var leftPossibleTiles = grid.NodeFromCoordinates(x, y).Possible_Node_MapTile;
                var rightPossibleTiles = rightNode.Possible_Node_MapTile;

                var list = leftPossibleTiles
                    .SelectMany(left_possible => rightPossibleTiles
                        .Where(right_possible => left_possible.Right_Status == right_possible.Left_Status &&
                                                 checkThe_Down_Tile(x + 1, y, right_possible) &&
                                                 checkThe_Up_Tile(x + 1, y, right_possible) &&
                                                 checkThe_Left_Tile(x + 1, y, right_possible) &&
                                                 checkThe_Right_Tile(x + 1, y, right_possible))
                        .Select(right_possible => right_possible))
                    .ToList();

                grid.SetMapTilesFromCoordinate(x + 1, y, list);

                if (list.Count == 1)
                {
                    collapse_at(x + 1, y);
                }
            }
        }

        //-----------------------UP-----------------------------
        //checking Upside side
        if (y + 1 <= gridSizeY)
        {
            var upNode = grid.NodeFromCoordinates(x, y + 1);

            if (upNode.Walkable)
            {
                var downPossibleTiles = grid.NodeFromCoordinates(x, y).Possible_Node_MapTile;
                var upPossibleTiles = upNode.Possible_Node_MapTile;

                var list = downPossibleTiles
                    .SelectMany(down_possible => upPossibleTiles
                        .Where(up_possible => down_possible.Up_Status == up_possible.Down_Status &&
                                              checkThe_Down_Tile(x, y + 1, up_possible) &&
                                              checkThe_Up_Tile(x, y + 1, up_possible) &&
                                              checkThe_Left_Tile(x, y + 1, up_possible) &&
                                              checkThe_Right_Tile(x, y + 1, up_possible))
                        .Select(up_possible => up_possible))
                    .ToList();

                grid.SetMapTilesFromCoordinate(x, y + 1, list);

                if (list.Count == 1)
                {
                    collapse_at(x, y + 1);
                }
            }
        }

        //-----------------------DOWN-----------------------------
        //checking downside side
        if (y - 1 >= 0)
        {
            var downNode = grid.NodeFromCoordinates(x, y - 1);

            if (downNode.Walkable)
            {
                var downPossibleTiles = downNode.Possible_Node_MapTile;
                var upPossibleTiles = grid.NodeFromCoordinates(x, y).Possible_Node_MapTile;

                var leftList = downPossibleTiles
                    .Where(down_possible => upPossibleTiles.Any(up_possible => down_possible.Up_Status == up_possible.Down_Status) &&
                                            checkThe_Down_Tile(x, y - 1, down_possible) &&
                                            checkThe_Up_Tile(x, y - 1, down_possible) &&
                                            checkThe_Left_Tile(x, y - 1, down_possible) &&
                                            checkThe_Right_Tile(x, y - 1, down_possible))
                    .ToList();

                grid.SetMapTilesFromCoordinate(x, y - 1, leftList);

                if (leftList.Count == 1)
                {
                    collapse_at(x, y - 1);
                }
            }
        }

        //if any of neghibours dosent fit to our chosen tilemap will remove and others will stay
    }

    //.....................Right........................
    bool CheckThe_Right_Compatibility(int x, int y, MapTiles maptile)
    {
        if (x + 1 <= gridSizeX)
        {
            if (grid.NodeFromCoordinates(x + 1, y).Walkable)
            {
                foreach (MapTiles right_possible in grid.NodeFromCoordinates(x + 1, y).Possible_Node_MapTile)
                {
                    if (maptile.Right_Status == right_possible.Left_Status)
                    {
                        return true;
                        //Canditate_MapTiles.Add(Right_Possible_maptile);
                    }
                }
                return false;
            }
        }
        return true;
    }

    //.....................Left........................
    bool CheckThe_Left_Compatibility(int x, int y, MapTiles maptile)
    {
        if (x - 1 >= 0)
        {
            if (grid.NodeFromCoordinates(x - 1, y).Walkable)
            {
                foreach (MapTiles left_possible in grid.NodeFromCoordinates(x - 1, y).Possible_Node_MapTile)
                {
                    if (left_possible.Right_Status == maptile.Left_Status)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        return true;
    }

    //.....................Up........................
    bool CheckThe_Up_Compatibility(int x, int y, MapTiles maptile)
    {
        if (y + 1 <= gridSizeY)
        {
            if (grid.NodeFromCoordinates(x, y + 1).Walkable)
            {
                foreach (MapTiles Up_possible in grid.NodeFromCoordinates(x, y + 1).Possible_Node_MapTile)
                {
                    if (maptile.Up_Status == Up_possible.Down_Status)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        return true;
    }
    //.....................Down........................
    bool CheckThe_Down_Compatibility(int x, int y, MapTiles maptile)
    {
        if (y - 1 >= 0)
        {
            if (grid.NodeFromCoordinates(x, y - 1).Walkable)
            {
                foreach (MapTiles Down_possible in grid.NodeFromCoordinates(x, y - 1).Possible_Node_MapTile)
                {
                    if (grid.NodeFromCoordinates(x, y - 1).Walkable)
                    {
                        if (Down_possible.Up_Status == maptile.Down_Status)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        return true;
    }


    #endregion

    public void CombineMeshes()
    {
        borderMeshCombiner.MeshCombiner();
        environmentMeshCombiner.MeshCombiner();
    }

    void SetupFlag(Transform flagPlace)
    {
        GameObject flag = Pool_Manager.instance.GetPoolObject(Pool_Manager.Object.Flag);
        flag.transform.position = flagPlace.position;
        flag.SetActive(true);
    }
    void DustEffect(Vector3 pos)
    {
        GameObject dustEffect = pool_Manager.GetPoolObject(Pool_Manager.Object.PlatformAppearanceDustEffect);
        dustEffect.transform.position = pos;
        dustEffect.SetActive(true);
    }
}


