using System.Collections;
using System.Collections.Generic;
using System.Drawing; //for point struct
using Unity.VisualScripting;
using UnityEngine;

public class TerrainMaster : MonoBehaviour
{
    public Terrain baseTerrain;
    private Vector2 perlinOffset;
    public Transform playerTrans;
    Dictionary<Point, Terrain> terrains; //Stores terrain positions in units of terrain blocks (0, 2) is at (0, 2 * width)
    public float squareWidth;
    public float height;
    public int renderRadius;


	void Start()
    {
        System.Random rng = new();
        perlinOffset = new Vector2((float)rng.NextDouble(), (float)rng.NextDouble());
        terrains = new();
    }

    void FixedUpdate()
    {
        GenerateInRadius();
    }

    void GenerateInRadius()
    {
        Point playerCoords = ContainedIn(playerTrans);
        for (int keyX = playerCoords.X - renderRadius; keyX <= playerCoords.X + renderRadius; keyX++)
			for (int keyZ = playerCoords.Y - renderRadius; keyZ <= playerCoords.Y + renderRadius; keyZ++)
            {
                Point potentialPoint = new Point(keyX, keyZ);
                if (!terrains.TryGetValue(potentialPoint, out Terrain preExistingTerrain)) //if no terrain there currently
                    terrains.Add(potentialPoint, CreateAtKey(potentialPoint));
            }

        //Unload outside terrains (aquired via .activeTerrains)
	}

    Terrain CreateAtKey(Point posKey)
    {
        Debug.Log("trying at: " + posKey);
        Terrain newT = Instantiate(baseTerrain, this.transform);
        newT.gameObject.name = "Terrain(x:" + posKey.X + ", z:" + posKey.Y + ")";
        newT.gameObject.SetActive(true);
        newT.transform.position = PositionedAt(posKey);

        //Set new terrain's values
            //generate and set terrainData
        Terrain[] neighbors = new Terrain[4]; 
        if (terrains.TryGetValue(new Point(posKey.X + 1, posKey.Y), out Terrain leftNeigh)) //+X
        {
            leftNeigh.SetNeighbors(leftNeigh.leftNeighbor, leftNeigh.topNeighbor, newT, leftNeigh.bottomNeighbor);
            neighbors[0] = leftNeigh;
        }
		if (terrains.TryGetValue(new Point(posKey.X - 1, posKey.Y), out Terrain rightNeigh)) //-X
		{
			rightNeigh.SetNeighbors(newT, rightNeigh.topNeighbor, rightNeigh.rightNeighbor, rightNeigh.bottomNeighbor);
			neighbors[2] = rightNeigh;
		}
		if (terrains.TryGetValue(new Point(posKey.X, posKey.Y + 1), out Terrain topNeigh)) //+Z
		{
			topNeigh.SetNeighbors(topNeigh.leftNeighbor, topNeigh.topNeighbor, topNeigh.rightNeighbor, newT);
			neighbors[1] = topNeigh;
		}
		if (terrains.TryGetValue(new Point(posKey.X, posKey.Y - 1), out Terrain bottomNeigh)) //-Z
		{
			bottomNeigh.SetNeighbors(bottomNeigh.leftNeighbor, newT, bottomNeigh.rightNeighbor, bottomNeigh.bottomNeighbor);
			neighbors[3] = bottomNeigh;
		}
		newT.SetNeighbors(neighbors[0], neighbors[1], neighbors[2], neighbors[3]);

		return newT;
    }

	Point ContainedIn(Transform trackedObject)
    {
        Vector3 trackedtPos = trackedObject.position;
        trackedtPos.y = 0;
        trackedtPos /= squareWidth;
        return new Point(Mathf.FloorToInt(trackedtPos.x), Mathf.FloorToInt(trackedtPos.z));
    }

    Vector3 PositionedAt(Point positionKey)
    {
        return new Vector3(positionKey.X * squareWidth, 0, positionKey.Y * squareWidth);
    }
}
