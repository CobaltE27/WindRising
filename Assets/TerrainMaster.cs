using System;
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
    public float squareWidth;
    public float height;
    public int renderRadius;
    public AnimationCurve broadBias;
    public Light sun;
    public float dayMinutes;
    public int dayDivisions;
    private float sunUpdateDurationS;
    Dictionary<Point, Chunk> chunks; //Stores terrain positions in units of terrain blocks (0, 2) is at (0, 2 * width)

	class Chunk
    {
        public Terrain terrain;
        public float heat = 0f;

        public Chunk() { }
        public Chunk(Terrain terrain) 
        {
            this.terrain = terrain;
        }
    }

	void Start()
    {
        UnityEngine.Random.InitState(System.DateTime.Now.Millisecond);
		perlinOffset = Vector2.zero;
		perlinOffset = new Vector2(UnityEngine.Random.Range(-20000000f, 20000000f), UnityEngine.Random.Range(-20000000f, 20000000f));
        chunks = new();
		StartCoroutine(UpdateTerrain());
        sunUpdateDurationS = (dayMinutes * 60) / dayDivisions;
        StartCoroutine(UpdateSun());
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

	IEnumerator UpdateSun()
	{
        float rotationStep = 180f / dayDivisions;
        sun.transform.localEulerAngles = new Vector3(70, 0, 0); //set latitude
        sun.transform.Rotate(0, 0, -90, Space.World);
		while (sun.transform.forward.y < 0.15f) //rotate past horizon then stop
		{
			yield return new WaitForSeconds(sunUpdateDurationS);
			sun.transform.Rotate(0, 0, rotationStep, Space.World);
		}
        yield break;
	}

	void GenerateInRadius()
    {
        Point playerCoords = ContainedIn(playerTrans);
        bool terrChanged = false;
        for (int keyX = playerCoords.X - renderRadius; keyX <= playerCoords.X + renderRadius; keyX++)
			for (int keyZ = playerCoords.Y - renderRadius; keyZ <= playerCoords.Y + renderRadius; keyZ++)
            {
                Point potentialPoint = new Point(keyX, keyZ);
                if (!chunks.TryGetValue(potentialPoint, out Chunk preExistingChunk)) //if no terrain there currently
                {
                    Chunk newChunk = new(CreateAtKey(potentialPoint));
                    chunks.Add(potentialPoint, newChunk);
                    terrChanged = true;
                }
            }
        if (terrChanged)
            Terrain.SetConnectivityDirty();
        //Unload outside chunks (aquired via .activeTerrains)
	}

    Terrain CreateAtKey(Point posKey)
    {
        Terrain newT = Instantiate(baseTerrain, this.transform);
        newT.gameObject.name = "Terrain(x:" + posKey.X + ", z:" + posKey.Y + ")";
        newT.gameObject.SetActive(true);
        newT.transform.position = PositionedAt(posKey);

        //Set new terrain's values
        SetDataFor(posKey, newT);
        newT.GetComponent<TerrainCollider>().terrainData = newT.terrainData;

        Terrain[] neighbors = new Terrain[4]; 
        if (chunks.TryGetValue(new Point(posKey.X + 1, posKey.Y), out Chunk leftNeigh)) //+X
        {
            Terrain leftN = leftNeigh.terrain;
			leftN.SetNeighbors(leftN.leftNeighbor, leftN.topNeighbor, newT, leftN.bottomNeighbor);
            neighbors[0] = leftN;
		}
		if (chunks.TryGetValue(new Point(posKey.X - 1, posKey.Y), out Chunk rightNeigh)) //-X
		{
            Terrain rightN = rightNeigh.terrain;
			rightN.SetNeighbors(newT, rightN.topNeighbor, rightN.rightNeighbor, rightN.bottomNeighbor);
			neighbors[2] = rightN;
		}
		if (chunks.TryGetValue(new Point(posKey.X, posKey.Y + 1), out Chunk topNeigh)) //+Z
		{
			Terrain topN = topNeigh.terrain;
			topN.SetNeighbors(topN.leftNeighbor, topN.topNeighbor, topN.rightNeighbor, newT);
			neighbors[1] = topN;
		}
		if (chunks.TryGetValue(new Point(posKey.X, posKey.Y - 1), out Chunk bottomNeigh)) //-Z
		{
            Terrain bottomN = bottomNeigh.terrain;
			bottomN.SetNeighbors(bottomN.leftNeighbor, newT, bottomN.rightNeighbor, bottomN.bottomNeighbor);
			neighbors[3] = bottomN;
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
        float localFactor = (150f / height) * Mathf.PerlinNoise(localScaledPos.x, localScaledPos.z);
		return broadFactor + localFactor;
    }
}
