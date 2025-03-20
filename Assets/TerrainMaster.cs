using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing; //for point struct
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using Color = UnityEngine.Color;

public class TerrainMaster : MonoBehaviour
{
    public Terrain baseTerrain;
    private Vector2 perlinOffset;
    public Transform playerTrans;
    public float squareWidth;
    public float height;
    public int renderRadius;
    public AnimationCurve broadBias;
    public AnimationCurve erosionBias;
    public Light sun;
    public float dayMinutes;
    public int dayDivisions;
    private float sunUpdateDurationS;
    Dictionary<Point, Chunk> chunks; //Stores terrain positions in units of terrain blocks (0, 2) is at (0, 2 * width)
    public GameObject thermalPrefab;

	class Chunk
    {
        public Terrain terrain;
        public float heat = 0f;
        public Vector3 averageNormal = Vector3.zero;
        public ThermalSampler? thermal = null;
        public float thermalDur = 0f;

        public Chunk() { }
        public Chunk(Terrain terrain) 
        {
            this.terrain = terrain;
            for (int c = 0; c < 25; c++)
                averageNormal += terrain.terrainData.GetInterpolatedNormal(Random.Range(0f, 1f), Random.Range(0f, 1f));
            averageNormal = averageNormal.normalized;
        }
    }

	void Start()
    {
        Random.InitState(System.DateTime.Now.Millisecond);
		perlinOffset = Vector2.zero;
		perlinOffset = new Vector2(Random.Range(-20000000f, 20000000f), Random.Range(-20000000f, 20000000f));
        chunks = new();
		StartCoroutine(UpdateTerrain());
        sunUpdateDurationS = (dayMinutes * 60) / dayDivisions;
        StartCoroutine(UpdateSun());
        StartCoroutine(UpdateWeather());
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

	IEnumerator UpdateWeather()
	{
        float period = dayMinutes / 2f;
        float cutoff = 0.4f;
        float releaseChance = 0.2f;
        float minSpeed = 5f;
        float maxSpeed = 25f;
        float minDurationS = dayMinutes * 0.1f * 60f;
        float maxDurationS = dayMinutes * 0.3f * 60f;
        float succeedChance = 0.05f;
		while (true)
		{
			foreach (var pair in chunks)
            {
                Chunk chunk = pair.Value;
                Point point = pair.Key;
                float angleFactor = 
                chunk.heat += Mathf.Clamp(Vector3.Dot(-sun.transform.forward, chunk.averageNormal), 0f, 1f) * 0.1f;
                chunk.heat = Mathf.Clamp(chunk.heat, 0f, 1f);

                if (chunk.thermal)
                {
                    //update wind
                    //TODO: add thermal release addition

					chunk.thermalDur -= period;
					if (chunk.thermalDur < 0)
					{
                        Destroy(chunk.thermal.gameObject);
                        chunk.thermal = null;
                        chunk.thermalDur = 0f;
					}
				}
                else
                {
					if (releaseChance >= Random.Range(0f, 1f)) //may need more complicated chance
					{
                        if (chunk.heat <= cutoff)
                        {
                            chunk.heat = 0f;
                            continue;
                        }

                        if (chunk.heat > cutoff && succeedChance > Random.Range(0f, 1f))
                        {
                            float xNormed = Random.Range(0f, 1f);
                            float zNormed = Random.Range(0f, 1f);
                            Vector3 thermLocalPos = new Vector3(point.X * squareWidth + xNormed * squareWidth,
                                chunk.terrain.terrainData.GetInterpolatedHeight(xNormed, zNormed),
                                point.Y * squareWidth + zNormed * squareWidth);

                            ThermalSampler newTherm = Instantiate(thermalPrefab, this.transform).GetComponent<ThermalSampler>();
                            newTherm.transform.position = thermLocalPos;
                            newTherm.cloudBaseHeight = height;
                            newTherm.prevailingWind = Vector3.zero; //TODO: fill from simulation

                            //TODO: set thermal properties properly
                            float strength = (chunk.heat - cutoff) / (1 - cutoff); //minimum heat above cutoff -> 0, max -> 1
                            newTherm.speed = minSpeed + strength * (maxSpeed - minSpeed);
                            chunk.thermal = newTherm;
                            chunk.thermalDur = Random.Range(minDurationS, maxDurationS); 
                        }

                        chunk.heat = 0f;
					}
				}
            }
			yield return new WaitForSeconds(period);
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
        Vector3 erosionScaledPos = perlinPosition / 9000f;
		float erosion = erosionBias.Evaluate(Mathf.PerlinNoise(erosionScaledPos.x, erosionScaledPos.z));
		return (broadFactor + localFactor) * erosion;
    }
}
