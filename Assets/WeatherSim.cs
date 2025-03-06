using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using static WeatherSim;

public class WeatherSim : MonoBehaviour
{
    public float height;
    public float squareWidth;
    public float cellHeight;
    public float cellSquareWidth;
    public float diffusionCoeff;
    public float viscosity;
    public float windStrength;
    Vector3 cellDims;
	public Vector3 sunDir;
    List<List<List<CellData>>> cells;
    List<List<List<CellData>>> nextCells;
	int cellsWide;
    int cellsTall;
    private Dictionary<string, Vector3> directions;
	public int simPeriodS;
    int simCounter = 0;
    List<List<List<TMP_Text>>> debTexts;
    public TMP_Text textTemplate;
	public List<WeatherSim?> neighborsSims;
	public Mesh debBox;

	public class CellData
    {
        public Vector3 wind;
        public float pressure;
        public float temp;

        public CellData(Vector3 wind, float pressure, float temp)
        {
            this.wind = wind;
            this.pressure = pressure;
            this.temp = temp;
        }

		public override string ToString()
		{
			return wind + ", p:" + pressure + ", t:" + temp; 
		}
    }
    // Start is called before the first frame update
    void Start()
    {
        directions = new Dictionary<string, Vector3>();
		directions.Add("U", Vector3.up);
        directions.Add("D", -Vector3.up);
        directions.Add("R", Vector3.right);
        directions.Add("L", -Vector3.right);
        directions.Add("F", Vector3.forward);
        directions.Add("B", -Vector3.forward);

		cells = new List<List<List<CellData>>>();
        cellsWide = (int)(squareWidth / cellSquareWidth);
        cellsTall = (int)(height / cellHeight);
		cellDims = new Vector3(cellSquareWidth, cellHeight, cellSquareWidth);
		for (int x = 0; x < cellsWide; x++)
        {
            cells.Add(new List<List<CellData>>());

			for (int y = 0; y < cellsTall; y++)
            {
                cells[x].Add(new List<CellData>());

				for (int z = 0; z < cellsWide; z++)
                {
                    cells[x][y].Add(new CellData(Vector3.zero, 1, 0.5f));
				}
            }
        }

		neighborsSims = new List<WeatherSim?>();
		for (int i = 0; i < 4; i++)
			neighborsSims.Add(null);

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (simCounter <= 0)
        {
            Debug.Log("Sim step!");
			if (Input.GetKey(KeyCode.LeftShift))
			{
				cells[cellsWide / 2][1][cellsWide / 2].pressure = 20;
				cells[cellsWide / 2][1][cellsWide / 2].wind = new Vector3(5, 0, 0);
			}

            nextCells = InitializeCells();

            VelocityStep();
            DensityStep();

			cells = nextCells;

			simCounter = simPeriodS * 50;
		}
        simCounter--;

        Debug.DrawRay(new Vector3(-1, -1, -1), SampleWind(new Vector3(-1, -1, -1)));
        Debug.DrawRay(new Vector3(-10, -10, -10), SampleWind(new Vector3(-10, -10, -10)));
        Debug.DrawRay(new Vector3(20, 20, 20), SampleWind(new Vector3(20, 20, 20)));
	}

	void OnDrawGizmos()
	{
		if (cells == null)
			return;

		Gizmos.color = Color.white;
		for (int x = 0; x < cellsWide; x++)
			for (int z = 0; z < cellsWide; z++)
				for (int y = 0; y < cellsTall; y++)
				{
					Vector3 centerPos = CenterPosFromIndecies(x, y, z);
					CellData? cell = cells[x][y][z];
					if (cell == null)
						continue;
					Debug.DrawRay(centerPos, cell.wind * 5);
					Gizmos.color = new Color(0, 0, 0, cell.pressure / 20);
					Gizmos.DrawMesh(debBox, centerPos, Quaternion.identity, cellDims);
				}
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
			avgWind += cells[indecies[0]][indecies[1]][indecies[2]].wind * weight;
        }
        return avgWind * windStrength;
    }

    /// <summary>
    /// Works similarly to SampleWind, but takes in approximate array indecies instead of actual position since simulation iterates across indecies
    /// </summary>
    /// <param name="approxIndecies"></param>
    /// <returns></returns>
    private float BacktracedPressure(Vector3 approxIndecies)
    {
		int[] baseIndecies = {(int)approxIndecies.x, (int)approxIndecies.y, (int)approxIndecies.z};
		float avgPressure = 0f;
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

			Vector3 displacement = new Vector3(indecies[0], indecies[1], indecies[2]) - approxIndecies;
			float weight = (1 - Mathf.Abs(displacement.x)) *
				(1 - Mathf.Abs(displacement.y)) *
				(1 - Mathf.Abs(displacement.z));
			avgPressure += cells[indecies[0]][indecies[1]][indecies[2]].pressure * weight;
		}
		return avgPressure;
	}

	private Vector3 BacktracedWind(Vector3 approxIndecies)
	{
		int[] baseIndecies = { (int)approxIndecies.x, (int)approxIndecies.y, (int)approxIndecies.z };
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

			Vector3 displacement = new Vector3(indecies[0], indecies[1], indecies[2]) - approxIndecies;
			float weight = (1 - Mathf.Abs(displacement.x)) *
				(1 - Mathf.Abs(displacement.y)) *
				(1 - Mathf.Abs(displacement.z));
			avgWind += cells[indecies[0]][indecies[1]][indecies[2]].wind * weight;
		}
		return avgWind;
	}

	private void Iterate(int x, int y, int z, List<List<List<CellData>>> current, List<List<List<CellData>>> next)
    {
		Debug.Log(x + " " + y + " " + z + ":");
        Dictionary<string, CellData> currentCells = new Dictionary<string, CellData>();
        Dictionary<string, CellData> nextCells = new Dictionary<string, CellData>();
        currentCells.Add("C", current[x][y][z]);
        nextCells.Add("C", next[x][y][z]);
        if (x < cellsWide - 1)
        {
			currentCells.Add("R", current[x + 1][y][z]);
			nextCells.Add("R", next[x + 1][y][z]);
		}
		if (x > 0)
		{
			currentCells.Add("L", current[x - 1][y][z]);
			nextCells.Add("L", next[x - 1][y][z]);
		}
		if (y < cellsTall - 1)
		{
			currentCells.Add("U", current[x][y + 1][z]);
			nextCells.Add("U", next[x][y + 1][z]);
		}
		if (y > 0)
		{
			currentCells.Add("D", current[x][y - 1][z]);
			nextCells.Add("D", next[x][y - 1][z]);
		}
		if (z < cellsWide - 1)
		{
			currentCells.Add("F", current[x][y][z + 1]);
			nextCells.Add("F", next[x][y][z + 1]);
		}
		if (z > 0)
		{
			currentCells.Add("B", current[x][y][z - 1]);
			nextCells.Add("B", next[x][y][z - 1]);
		}


	}

    private List<List<List<CellData>>> InitializeCells()
    {
		List<List<List<CellData>>> cellList = new List<List<List<CellData>>>();
		for (int x = 0; x < cellsWide; x++)
		{
			cellList.Add(new List<List<CellData>>());

			for (int y = 0; y < cellsTall; y++)
			{
				cellList[x].Add(new List<CellData>());

				for (int z = 0; z < cellsWide; z++)
				{
					cellList[x][y].Add(new CellData(Vector3.zero, 0, 0));
				}
			}
		}
        return cellList;
	}

	private List<List<List<CellData>>> InitializeCells(CellData baseCell)
	{
		List<List<List<CellData>>> cellList = new List<List<List<CellData>>>();
		for (int x = 0; x < cellsWide; x++)
		{
			cellList.Add(new List<List<CellData>>());

			for (int y = 0; y < cellsTall; y++)
			{
				cellList[x].Add(new List<CellData>());

				for (int z = 0; z < cellsWide; z++)
				{
					cellList[x][y].Add(new CellData(baseCell.wind, baseCell.pressure, baseCell.temp));
				}
			}
		}
		return cellList;
	}

	private void DensityStep()
    {
        //add source pressure here if desired
        List<List<List<CellData>>> postDiffuse = InitializeCells();
		DiffusePressure(nextCells, postDiffuse, simPeriodS);
        nextCells = postDiffuse;
        List<List<List<CellData>>> postAdvect = InitializeCells();
        AdvectPressure(nextCells, postAdvect, simPeriodS);
		nextCells = postAdvect;
    }

    private void DiffusePressure(List<List<List<CellData>>> current, List<List<List<CellData>>> next, float dt)
    {
		float diffused = diffusionCoeff * dt * (cellsWide - 1) * (cellsWide - 1) * (cellsTall - 1);
        for (int k = 0; k < 20; k++)
        {
            for (int x = 1; x < cellsWide - 1; x++)
                for (int y = 1; y < cellsTall - 1; y++)
                    for (int z = 1; z < cellsWide - 1; z++)
                    {
                        next[x][y][z].pressure = (current[x][y][z].pressure + diffused *
                            (current[x + 1][y][z].pressure + current[x - 1][y][z].pressure + current[x][y + 1][z].pressure + 
                            current[x][y - 1][z].pressure + current[x][y][z + 1].pressure + current[x][y][z - 1].pressure))
                            / (1 + 6 * diffused);
                    }
			SetBoundaries(current, next);
		}
		for (int x = 1; x < cellsWide - 1; x++)
			for (int y = 1; y < cellsTall - 1; y++)
				for (int z = 1; z < cellsWide - 1; z++)
				{
                    next[x][y][z].wind = current[x][y][z].wind;
				}
	}

    private void AdvectPressure(List<List<List<CellData>>> current, List<List<List<CellData>>> next, float dt)
    {
        Vector3 dt0 = dt * new Vector3(cellsWide - 1, cellsTall - 1, cellsWide - 1);
        dt0.x /= cellSquareWidth; dt0.y /= cellHeight; dt0.z /= cellSquareWidth;
		for (int x = 1; x < cellsWide - 1; x++)
			for (int y = 1; y < cellsTall - 1; y++)
				for (int z = 1; z < cellsWide - 1; z++)
                {
                    Vector3 from = new Vector3(x, y, z) - Vector3.Scale(dt0, current[x][y][z].wind);
                    if (from.x < 0.5f) from.x = 0.5f;
                    else if (from.x > cellsWide - 0.5f) from.x = cellsWide - 0.5f;
					if (from.y < 0.5f) from.y = 0.5f;
					else if (from.y > cellsTall - 0.5f) from.y = cellsTall - 0.5f;
					if (from.x < 0.5f) from.x = 0.5f;
					else if (from.z > cellsWide - 0.5f) from.z = cellsWide - 0.5f;
					//sample from 8 surrounding cells
					next[x][y][z].pressure = BacktracedPressure(from);
                    //Debug.Log(x + " " + y + " " + z + ": " + "f:" + from + " f0:" + from0 + " f1:" + from1 + " s1:" + stu1 + " s0:" + stu0 + " dt0:" + dt0 + " w:" + current[x][y][z].wind);
				}
		for (int x = 1; x < cellsWide - 1; x++)
			for (int y = 1; y < cellsTall - 1; y++)
				for (int z = 1; z < cellsWide - 1; z++)
				{
					next[x][y][z].wind = current[x][y][z].wind;
				}
		SetBoundaries(current, next);
	}

	private void VelocityStep()
    {
		//add source wind velocities here if desired
		Debug.Log(cells[5][1][5].ToString());

		List<List<List<CellData>>> postVelDiffuse = InitializeCells();
		DiffuseVelocity(cells, postVelDiffuse, simPeriodS);
		nextCells = postVelDiffuse;
		Debug.Log(cells[5][1][5].ToString());

		List<List<List<CellData>>> postProject = InitializeCells();
		Project(nextCells, postProject);
		nextCells = postProject;
		Debug.Log(cells[5][1][5].ToString());

		List<List<List<CellData>>> postAdvect = InitializeCells();
		AdvectVelocity(nextCells, postAdvect, simPeriodS);
		nextCells = postAdvect;
		Debug.Log(cells[5][1][5].ToString());

		List<List<List<CellData>>> postFinalProject = InitializeCells();
		Project(nextCells, postFinalProject);
		nextCells = postFinalProject;
		Debug.Log(cells[5][1][5].ToString());
	}

	private void DiffuseVelocity(List<List<List<CellData>>> current, List<List<List<CellData>>> next, float dt)
	{
		float diffused = viscosity * dt * (cellsWide - 1) * (cellsWide - 1) * (cellsTall - 1);
		for (int k = 0; k < 20; k++)
		{
			for (int x = 1; x < cellsWide - 1; x++)
				for (int y = 1; y < cellsTall - 1; y++)
					for (int z = 1; z < cellsWide - 1; z++)
					{
						next[x][y][z].wind = (current[x][y][z].wind + diffused *
							(current[x + 1][y][z].wind + current[x - 1][y][z].wind + current[x][y + 1][z].wind +
							current[x][y - 1][z].wind + current[x][y][z + 1].wind + current[x][y][z - 1].wind))
							/ (1 + 6 * diffused);
					}
			SetBoundaries(current, next);
		}
		for (int x = 1; x < cellsWide - 1; x++)
			for (int y = 1; y < cellsTall - 1; y++)
				for (int z = 1; z < cellsWide - 1; z++)
				{
					next[x][y][z].pressure = current[x][y][z].pressure;
				}
	}

	private void AdvectVelocity(List<List<List<CellData>>> current, List<List<List<CellData>>> next, float dt)
	{
		Vector3 dt0 = dt * new Vector3(cellsWide - 1, cellsTall - 1, cellsWide - 1);
		dt0.x /= cellSquareWidth; dt0.y /= cellHeight; dt0.z /= cellSquareWidth;
		for (int x = 1; x < cellsWide - 1; x++)
			for (int y = 1; y < cellsTall - 1; y++)
				for (int z = 1; z < cellsWide - 1; z++)
				{
					Vector3 from = new Vector3(x, y, z) - Vector3.Scale(dt0, current[x][y][z].wind);
					if (from.x < 0.5f) from.x = 0.5f;
					else if (from.x > cellsWide - 0.5f) from.x = cellsWide - 0.5f;
					if (from.y < 0.5f) from.y = 0.5f;
					else if (from.y > cellsTall - 0.5f) from.y = cellsTall - 0.5f;
					if (from.x < 0.5f) from.x = 0.5f;
					else if (from.z > cellsWide - 0.5f) from.z = cellsWide - 0.5f;
					//sample from 8 surrounding cells
					next[x][y][z].wind = BacktracedWind(from);
				}
		for (int x = 1; x < cellsWide - 1; x++)
			for (int y = 1; y < cellsTall - 1; y++)
				for (int z = 1; z < cellsWide - 1; z++)
				{
					next[x][y][z].pressure = current[x][y][z].pressure;
				}
		SetBoundaries(current, next);
	}

	private void Project(List<List<List<CellData>>> current, List<List<List<CellData>>> next)
	{
		float h = ((1 / ((float)cellsWide - 1)) + (1 / ((float)cellsTall - 1)) + (1 / ((float)cellsWide - 1))) / 3f;
		Debug.Log("h:" + h);
		List<List<List<float>>> div = new List<List<List<float>>>();
		for (int x = 0; x < cellsWide; x++)
		{
			div.Add(new List<List<float>>());

			for (int y = 0; y < cellsTall; y++)
			{
				div[x].Add(new List<float>());

				for (int z = 0; z < cellsWide; z++)
				{
					div[x][y].Add(0f);
				}
			}
		}
		List<List<List<float>>> p = new List<List<List<float>>>();
		for (int x = 0; x < cellsWide; x++)
		{
			p.Add(new List<List<float>>());

			for (int y = 0; y < cellsTall; y++)
			{
				p[x].Add(new List<float>());

				for (int z = 0; z < cellsWide; z++)
				{
					p[x][y].Add(0f);
				}
			}
		}

		for (int x = 1; x < cellsWide - 1; x++)
			for (int y = 1; y < cellsTall - 1; y++)
				for (int z = 1; z < cellsWide - 1; z++)
				{
					div[x][y][z] = -0.5f * h * (current[x + 1][y][z].wind.x - current[x - 1][y][z].wind.x
						+ current[x][y + 1][z].wind.y - current[x][y - 1][z].wind.y
						+ current[x][y][z + 1].wind.z - current[x][y][z - 1].wind.z);
					//p[x][y][z] = 0;
				}
		NeighborBoundaries(div);
		NeighborBoundaries(p);

		for (int k = 0; k < 20; k++)
		{
			for (int x = 1; x < cellsWide - 1; x++)
				for (int y = 1; y < cellsTall - 1; y++)
					for (int z = 1; z < cellsWide - 1; z++)
					{
						p[x][y][z] = (div[x][y][z] + p[x + 1][y][z] + p[x - 1][y][z] + p[x][y + 1][z] + p[x][y - 1][z] + p[x][y][z + 1] + p[x][y][z - 1]) / 6;
					}
			NeighborBoundaries(p);
		}

		for (int x = 1; x < cellsWide - 1; x++)
			for (int y = 1; y < cellsTall - 1; y++)
				for (int z = 1; z < cellsWide - 1; z++)
					next[x][y][z].wind -= 0.5f * new Vector3((p[x + 1][y][z] - p[x - 1][y][z]) / h, (p[x][y + 1][z] - p[x][y - 1][z]) / h, (p[x][y][z + 1] - p[x][y][z + 1]) / h); //TODO:use directional h instead of average

		for (int x = 1; x < cellsWide - 1; x++)
			for (int y = 1; y < cellsTall - 1; y++)
				for (int z = 1; z < cellsWide - 1; z++)
				{
					next[x][y][z].pressure = current[x][y][z].pressure;
				}

		SetBoundaries(current, next); //may require different boundary conditions?
	}

	private void SetBoundaries(List<List<List<CellData>>> current, List<List<List<CellData>>> next)
    {
		for (int x = 0; x < cellsWide; x++)
			for (int y = 0; y < cellsTall; y++)
				for (int z = 0; z < cellsWide; z++)
                    if (x == 0 || y == 0 || z == 0 || x == cellsWide - 1 || y == cellsTall - 1 || z == cellsWide - 1)
                    {
                        next[x][y][z] = current[x][y][z];
                    }
	}

	private void SetBoundaries<T>(List<List<List<T>>> list, T value)
	{
		for (int x = 0; x < cellsWide; x++)
			for (int y = 0; y < cellsTall; y++)
				for (int z = 0; z < cellsWide; z++)
					if (x == 0 || y == 0 || z == 0 || x == cellsWide - 1 || y == cellsTall - 1 || z == cellsWide - 1)
					{
						list[x][y][z] = value;
					}
	}

	private void NeighborBoundaries(List<List<List<float>>> cellList)
	{
		int greaterDim = Math.Max(cellsTall, cellsWide);
		for (int i = 1; i < greaterDim - 1; i++)
			for (int j = 1; j < greaterDim - 1; j++)
			{

				if (i < cellsTall - 1 && j < cellsWide - 1)
					cellList[0][i][j] = cellList[1][i][j];
				if (i < cellsWide - 1 && j < cellsWide - 1)
					cellList[i][0][j] = cellList[i][1][j];
				if (i < cellsWide - 1 && j < cellsTall - 1)
					cellList[i][j][0] = cellList[i][j][1];
				if (i < cellsTall - 1 && j < cellsWide - 1)
					cellList[cellsWide - 1][i][j] = cellList[cellsWide - 2][i][j];
				if (i < cellsWide - 1 && j < cellsWide - 1)
					cellList[i][cellsTall - 1][j] = cellList[i][cellsTall - 2][j];
				if (i < cellsWide - 1 && j < cellsTall - 1)
					cellList[i][j][cellsWide - 1] = cellList[i][j][cellsWide - 2];
			}

		int maxWIndex = cellsWide - 1;
		int maxHIndex = cellsTall - 1;
		for (int i = 1; i < greaterDim - 1; i++) //"edge" cases
		{
			if (i < cellsWide - 1)
				cellList[0][0][i] = (cellList[1][0][i] + cellList[0][1][i]) / 2f;
			if (i < cellsTall - 1)
				cellList[0][i][0] = (cellList[1][i][0] + cellList[0][i][1]) / 2f;
			if (i < cellsWide - 1)
				cellList[i][0][0] = (cellList[i][0][1] + cellList[i][1][0]) / 2f;

			if (i < cellsWide - 1)
				cellList[maxWIndex][0][i] = (cellList[maxWIndex - 1][0][i] + cellList[maxWIndex][1][i]) / 2f;
			if (i < cellsWide - 1)
				cellList[0][maxHIndex][i] = (cellList[1][maxHIndex][i] + cellList[0][maxHIndex - 1][i]) / 2f;

			if (i < cellsTall - 1)
				cellList[maxHIndex][i][0] = (cellList[maxHIndex - 1][i][0] + cellList[maxHIndex][i][1]) / 2f;
			if (i < cellsTall - 1)
				cellList[0][i][maxHIndex] = (cellList[1][i][maxHIndex] + cellList[0][i][maxHIndex - 1]) / 2f;

			if (i < cellsWide - 1)
				cellList[i][maxHIndex][0] = (cellList[i][maxHIndex][1] + cellList[i][maxHIndex - 1][0]) / 2f;
			if (i < cellsWide - 1)
				cellList[i][0][maxWIndex] = (cellList[i][0][maxWIndex - 1] + cellList[i][1][maxWIndex]) / 2f;

			if (i < cellsWide - 1)
				cellList[maxWIndex][maxHIndex][i] = (cellList[maxWIndex - 1][maxHIndex][i] + cellList[maxWIndex][maxHIndex - 1][i]) / 2f;
			if (i < cellsTall - 1)
				cellList[maxWIndex][i][maxWIndex] = (cellList[maxWIndex - 1][i][maxWIndex] + cellList[maxWIndex][i][maxWIndex - 1]) / 2f;
			if (i < cellsWide - 1)
				cellList[i][maxHIndex][maxWIndex] = (cellList[i][maxHIndex][maxWIndex - 1] + cellList[i][maxHIndex - 1][maxWIndex]) / 2f;
		}

		//"corner" cases
		cellList[0][0][0] = (cellList[1][0][0] + cellList[0][1][0] + cellList[0][0][1]) / 3f;
		cellList[maxWIndex][0][0] = (cellList[maxWIndex - 1][0][0] + cellList[maxWIndex][1][0] + cellList[maxWIndex][0][1]) / 3f;
		cellList[0][maxHIndex][0] = (cellList[1][maxHIndex][0] + cellList[0][maxHIndex - 1][0] + cellList[0][maxHIndex][1]) / 3f;
		cellList[0][0][maxWIndex] = (cellList[1][0][maxWIndex] + cellList[0][1][maxWIndex] + cellList[0][0][maxWIndex - 1]) / 3f;
		cellList[maxWIndex][0][maxWIndex] = (cellList[maxWIndex - 1][0][maxWIndex] + cellList[maxWIndex][1][maxWIndex] + cellList[maxWIndex][0][maxWIndex - 1]) / 3f;
		cellList[maxWIndex][maxHIndex][0] = (cellList[maxWIndex - 1][maxHIndex][0] + cellList[maxWIndex][maxHIndex - 1][0] + cellList[maxWIndex][maxHIndex][1]) / 3f;
		cellList[0][maxHIndex][maxWIndex] = (cellList[1][maxHIndex][maxWIndex] + cellList[0][maxHIndex - 1][maxWIndex] + cellList[0][maxHIndex][maxWIndex - 1]) / 3f;
		cellList[maxWIndex][maxHIndex][maxWIndex] = (cellList[maxWIndex - 1][maxHIndex][maxWIndex] + cellList[maxWIndex][maxHIndex - 1][maxWIndex] + cellList[maxWIndex][maxHIndex][maxWIndex]) / 3f;
	}
}
