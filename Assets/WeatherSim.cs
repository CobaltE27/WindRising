using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeatherSim : MonoBehaviour
{
    public float height;
    public float squareWidth;
    public float cellHeight;
    public float cellSquareWidth;
    Vector3 cellDims;
	public Vector3 sunDir;
    public List<List<List<CellData>>> cells;
    int cellsWide;
    int cellsTall;
    Vector3 globalWind;


    public struct CellData
    {
        public Vector3 Wind { get; set; }
        public float Pressure { get; set; }
        public float Temp { get; set; }

        public CellData(Vector3 wind, float pressure, float temp)
        {
            Wind = wind;
            Pressure = pressure;
            Temp = temp;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        cells = new List<List<List<CellData>>>();
        cellsWide = (int)(squareWidth / cellSquareWidth);
        cellsTall = (int)(height / cellHeight);
		cellDims = new Vector3(cellSquareWidth, cellHeight, cellSquareWidth);
		for (int i = 0; i < cellsWide; i++)
        {
            cells.Add(new List<List<CellData>>());

            for (int j = 0; j < cellsTall; j++)
            {
                cells[i].Add(new List<CellData>());

                for (int h = 0; h < cellsWide; h++)
                {
                    cells[i][j].Add(new CellData(Vector3.up, 1, 0));
                }
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        for (int x = 0; x < cellsWide; x++)
            for (int z = 0; z < cellsWide; z++)
                for (int y = 0; y < cellsTall; y++)
                {
                    Vector3 centerPos = CenterPosFromIndecies(x, y, z);
                    CellData cell = cells[x][y][z];
                    Vector3 colorComp = Vector3.Lerp(new Vector3(0, 0, 1), new Vector3(1, 0, 0), cell.Temp);
                    Debug.DrawRay(centerPos, cell.Wind, new Color(colorComp.x, colorComp.y, colorComp.z));
                }

        Debug.DrawRay(new Vector3(-1, -1, -1), SampleWind(new Vector3(-1, -1, -1)));
        Debug.DrawRay(new Vector3(-10, -10, -10), SampleWind(new Vector3(-10, -10, -10)));
        Debug.DrawRay(new Vector3(20, 20, 20), SampleWind(new Vector3(20, 20, 20)));
	}

    private Vector3 CenterPosFromIndecies(int x, int y, int z)
    {
        return transform.position + new Vector3(x * cellSquareWidth + 0.5f * cellSquareWidth, y * cellHeight + 0.5f * cellHeight, z * cellSquareWidth + 0.5f * cellSquareWidth);
    }

    /// <summary>
    /// Tries to get {x, y, z} indecies in this weather grid for the given world-space position.
    /// Since these are converted to integers, the values are rounded down, thus the position is in the space between {x, y, z} and {x + 1, y + 1, z + 1};
    /// </summary>
    /// <param name="centerPos"></param>
    /// <returns>Indecies for given poition in weather grid, CAN RETURN INDECIES OUTSIDE WEATHER GRID</returns>
    private int[] IndeciesFromCenterPos(Vector3 centerPos)
    {
        int[] indecies = {0, 0, 0};
		Vector3 offsetInGrid = centerPos - transform.position;
        Vector3 approxIndecies = (offsetInGrid - 0.5f * cellDims);
        approxIndecies.x /= cellSquareWidth;
        approxIndecies.y /= cellHeight;
        approxIndecies.z /= cellSquareWidth;

        for (int i = 0; i < 3; i++)
            indecies[i] = (int)approxIndecies[i];

        return indecies;
    }

    /// <summary>
    /// Gives interpolated wind velocity from surrounding cells, will give 0 vector if outside this grid
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public Vector3 SampleWind(Vector3 position)
    {
        int[] baseIndecies = IndeciesFromCenterPos(position);
        Vector3 avgWind = Vector3.zero;
		for (int i = 0; i < 8; i++)
        {
            int[] offset = { 0, 0, 0 };
            if ((i / 4) % 2 == 1) //goes through all possible combination of offsets by counting in binary
                offset[0] = 1;
            if ((i / 2) % 2 == 1)
                offset[1] = 1;
            if ((i) % 2 == 1)
                offset[2] = 1;

            int[] indecies = { 0, 0, 0 };
            bool valid = true;
            for (int j = 0; j < baseIndecies.Length; j++)
            {
                indecies[j] = baseIndecies[j] + offset[j];

                if (indecies[j] < 0) //indecies are too low to be in grid
                {
                    valid = false; break;
                }
            }
            if (indecies[0] > cellsWide - 1 || indecies[1] > cellsTall - 1 || indecies[2] > cellsWide - 1) //indecies are to high to be in grid
                valid = false;
            if (!valid)
                continue;

            Vector3 displacement = CenterPosFromIndecies(indecies[0], indecies[1], indecies[2]) - position;
			float weight = ((cellSquareWidth - Mathf.Abs(displacement.x)) / cellSquareWidth) * 
                ((cellHeight - Mathf.Abs(displacement.y)) / cellHeight) * 
                ((cellSquareWidth - Mathf.Abs(displacement.z)) / cellSquareWidth);
			avgWind += cells[indecies[0]][indecies[1]][indecies[2]].Wind * weight;
        }
        return avgWind;
    }
}
