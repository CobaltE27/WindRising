using System.Collections;
using System.Collections.Generic;
using System.Drawing; //for point struct
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
    public AnimationCurve broadBias;

	void Start()
    {
        Random.InitState(System.DateTime.Now.Millisecond);
		perlinOffset = Vector2.zero;
		perlinOffset = new Vector2(Random.Range(-20000000f, 20000000f), Random.Range(-20000000f, 20000000f));
        terrains = new();
		StartCoroutine(UpdateTerrain());
	}

	void FixedUpdate()
    {
    }

    IEnumerator UpdateTerrain()
    {
        while (true)
        {
			GenerateInRadius();
			yield return new WaitForSeconds(1f);
		}
    }

    void GenerateInRadius()
    {
        Point playerCoords = ContainedIn(playerTrans);
        bool terrChanged = false;
        for (int keyX = playerCoords.X - renderRadius; keyX <= playerCoords.X + renderRadius; keyX++)
			for (int keyZ = playerCoords.Y - renderRadius; keyZ <= playerCoords.Y + renderRadius; keyZ++)
            {
                Point potentialPoint = new Point(keyX, keyZ);
                if (!terrains.TryGetValue(potentialPoint, out Terrain preExistingTerrain)) //if no terrain there currently
                {
                    terrains.Add(potentialPoint, CreateAtKey(potentialPoint));
                    terrChanged = true;
                }
            }
        if (terrChanged)
            Terrain.SetConnectivityDirty();
        //Unload outside terrains (aquired via .activeTerrains)
	}

    Terrain CreateAtKey(Point posKey)
    {
        Terrain newT = Instantiate(baseTerrain, this.transform);
        newT.gameObject.name = "Terrain(x:" + posKey.X + ", z:" + posKey.Y + ")";
        newT.gameObject.SetActive(true);
        newT.transform.position = PositionedAt(posKey);

        //Set new terrain's values
        //generate and set terrainData
        SetDataFor(posKey, newT);
        newT.GetComponent<TerrainCollider>().terrainData = newT.terrainData;
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

    void SetDataFor(Point posKey, Terrain target)
    {
        TerrainData newData = new();
		newData.heightmapResolution = (int)squareWidth / 4;
        int resolution = newData.heightmapResolution;
        newData.size = new Vector3(squareWidth, height, squareWidth);
        float[,] heights = new float[resolution, resolution];
		for (int x = 0; x < resolution; x++)
            for (int z = 0; z < resolution; z++)
                heights[z, x] = HeightAt(posKey, x, z, resolution);
        newData.SetHeights(0, 0, heights);
        target.terrainData = newData;
    }

    float HeightAt(Point posKey, int x, int z, int resolution)
    {
        Vector3 worldPosition = new Vector3(posKey.X * squareWidth, 0, posKey.Y * squareWidth);
		worldPosition.x += (x * squareWidth) / (resolution - 1);
        worldPosition.z += (z * squareWidth) / (resolution - 1);
        Vector3 perlinPosition = worldPosition + new Vector3(perlinOffset.x, 0, perlinOffset.y);
        Vector3 broadScaledPos = perlinPosition / 3000f;
        float broadFactor = broadBias.Evaluate(Mathf.PerlinNoise(broadScaledPos.x, broadScaledPos.z));
        Vector3 localScaledPos = perlinPosition / 800f;
        float localFactor = (300f / height) * Mathf.PerlinNoise(localScaledPos.x, localScaledPos.z);
		return broadFactor + localFactor;
    }
}
