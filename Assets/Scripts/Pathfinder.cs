using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    // Required nodes for our search
    [SerializeField] Vector2Int startCoordinates;
    [SerializeField] Vector2Int destinationCoordinates;
    Node startNode;
    Node destinationNode;
    Node currentSearchNode;

    // Has a node already been explored / reached?
    Dictionary<Vector2Int, Node> explored = new Dictionary<Vector2Int, Node>();

    // Grid and directions
    GridManager gridManager;
    Vector2Int[] directions = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down, Vector2Int.one, -Vector2Int.one, new Vector2Int(-1, 1), new Vector2Int(1, -1)};
    Dictionary<Vector2Int, Node> grid = new Dictionary<Vector2Int, Node>();

    /* Temp */
    List<Node> frontier_astar = new List<Node>();
    // Our tiles are 10 x 10, the cost of moving diagonally would be 14 sqrt(x^2 + z^2), pythagorean
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    // Call on awake
    void Awake() {
        gridManager = FindObjectOfType<GridManager>();
        if(gridManager) {
            grid = gridManager.Grid;
        }
    }

    // Call on start
    void Start() {
        // Initialize our start and end coordinates
        startNode = gridManager.Grid[startCoordinates];
        destinationNode = gridManager.Grid[destinationCoordinates];

        AStar();
        BuildPath();
    }

    /****** A-STAR IMPLEMENTATION ******/
    // Initialize our g cost, f cost and connected nodes for all nodes in the grid.
    void InitializeCosts()
    {
        for(int x = 0; x < gridManager.GridSize.x; x++) {
            for(int y = 0; y < gridManager.GridSize.y; y++) {
                Vector2Int coordinates = new Vector2Int(x, y);
                Node node = gridManager.GetNode(coordinates);
                node.gCost = int.MaxValue;
                node.CalculateFCost();
                node.connectedTo = null;
            }
        }
    }

     void AStar()
    {
        bool isRunning = true;
        // Add the start node to the frontier and explored list.
        frontier_astar.Add(startNode);
        explored.Add(startNode.coordinates, startNode);
        // Call our initializeCosts() function
        InitializeCosts();
        // Initialize the fCost, gCost and hCost of the starting node
        startNode.gCost = 0;
        startNode.hCost = CalculateDistance(startNode, destinationNode);
        startNode.CalculateFCost();
        // While the frontier is not empty
        while(frontier_astar.Count > 0 && isRunning) {
            // Find the node with the lowest f-cost in the frontier, called Q
            currentSearchNode = GetLowestFCostNode(frontier_astar);
            currentSearchNode.isExplored = true;
            // If we reached our goal
            if(currentSearchNode == destinationNode) {
                break;
            }
            // Remove current node from the frontier
            frontier_astar.Remove(currentSearchNode);
            // Explore the neighbors of the current search node.
            ExploreNeighbors_AStar();
        }
    }

    void ExploreNeighbors_AStar()
    {
        // Create an empty list of neighbors
        List<Node> neighbors = new List<Node>();
        // Find all neighboring nodes
        foreach(Vector2Int direction in directions) {
            Vector2Int neighborCoordinates = currentSearchNode.coordinates + direction;
            // Check if the neighbor's coordinates exist in the grid
            if(grid.ContainsKey(neighborCoordinates)) {
                neighbors.Add(grid[neighborCoordinates]);
            }
        }

        // Keep unexplored neighbor nodes that are also traversable
        foreach(Node neighbor in neighbors) {
            // If we have not explored the neighbor already and we are also able to traverse the node
            if(!explored.ContainsKey(neighbor.coordinates) && neighbor.isTraversable) {
                // Calculate a tentative g-cost
                int tempGCost = currentSearchNode.gCost + CalculateDistance(currentSearchNode, neighbor);
                // If there is a node with the same position as the neighbor in the frontier that also has a lower f-value, skip this
                // If there is a node with the same position as the neighbor on the explored list that has a lower f-value than the current neighbor, skip this
                if(tempGCost < neighbor.gCost) {
                    // Create a connection on our map
                    neighbor.connectedTo = currentSearchNode;
                    // Calculate our G, H and F costs.
                    neighbor.gCost = tempGCost;
                    neighbor.hCost = CalculateDistance(neighbor, destinationNode);
                    neighbor.CalculateFCost();
                    if(!frontier_astar.Contains(neighbor)) {
                        frontier_astar.Add(neighbor);

                    }
                }
            }
        }
    }

    // Calculate the distance while ignoring the blockable areas.
    int CalculateDistance(Node a, Node b)
    {
        int xDistance = Mathf.Abs(a.coordinates.x - b.coordinates.x);
        int yDistance = Mathf.Abs(a.coordinates.y - b.coordinates.y);
        int remainder = Mathf.Abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * Mathf.Min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remainder;
    }

    // Return the lowest FCost node in our list of nodes.
    Node GetLowestFCostNode(List<Node> nodeList) {
        // Initialize lowest f-cost as the first node in the list
        Node lowestFCostNode = nodeList[0];
        // For loop that returns the node with the lowest f-cost in the entire list
        for(int i = 1; i < nodeList.Count; i++) {
            if(nodeList[i].fCost < lowestFCostNode.fCost) {
                lowestFCostNode = nodeList[i];
            }
        }
        return lowestFCostNode;
    }

    // Search from destination back to start
    List<Node> BuildPath()
    {
        List<Node> path = new List<Node>();
        Node currentSearchNode = destinationNode;

        path.Add(currentSearchNode);
        currentSearchNode.isPath = true;

        // Iteratively loop through all of the connected nodes.
        while(currentSearchNode.connectedTo != null) {
            currentSearchNode = currentSearchNode.connectedTo;
            path.Add(currentSearchNode);
            Debug.Log("Path node: " + currentSearchNode.coordinates);
            currentSearchNode.isPath = true;
        }
        // Reverse list and return the path
        path.Reverse();
        return path;
    }
}
