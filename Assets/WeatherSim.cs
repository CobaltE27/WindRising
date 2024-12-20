using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WeatherSim : MonoBehaviour
{
    public float height;
    public float squareWidth;
    public float cellHeight;
    public float cellSquareWidth;
    public float diffusedPressureFraction;
    public float tempDiffBonus;
    public float windStrength;
    Vector3 cellDims;
	public Vector3 sunDir;
    List<List<List<CellData>>> cells;
    List<List<List<CellData>>> nextCells;
	int cellsWide;
    int cellsTall;
    Vector3 globalWind;
    private Dictionary<string, Vector3> directions;
    private Dictionary<string, float> pressureWeights;
    private Dictionary<string, float> tempWeights;
	public int simPeriodS;
    int simCounter = 0;
    List<List<List<TMP_Text>>> debTexts;
    public TMP_Text textTemplate;
    public float gravityBonus;

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
		pressureWeights = new Dictionary<string, float>();
		pressureWeights.Add("C", 5);
		pressureWeights.Add("U", 3);
		pressureWeights.Add("D", 3);
		pressureWeights.Add("R", 1);
		pressureWeights.Add("L", 1);
		pressureWeights.Add("F", 1);
		pressureWeights.Add("B", 1);
		tempWeights = new Dictionary<string, float>();
		tempWeights.Add("C", 1);
		tempWeights.Add("U", 10);
		tempWeights.Add("D", 30);
		tempWeights.Add("R", 1);
		tempWeights.Add("L", 1);
		tempWeights.Add("F", 1);
		tempWeights.Add("B", 1);
		debTexts = new List<List<List<TMP_Text>>>();

		cells = new List<List<List<CellData>>>();
        cellsWide = (int)(squareWidth / cellSquareWidth);
        cellsTall = (int)(height / cellHeight);
		cellDims = new Vector3(cellSquareWidth, cellHeight, cellSquareWidth);
		for (int x = 0; x < cellsWide; x++)
        {
            cells.Add(new List<List<CellData>>());
            debTexts.Add(new List<List<TMP_Text>>());

			for (int y = 0; y < cellsTall; y++)
            {
                cells[x].Add(new List<CellData>());
                debTexts[x].Add(new List<TMP_Text>());

				for (int z = 0; z < cellsWide; z++)
                {
                    cells[x][y].Add(new CellData(Vector3.zero, 1, 0.5f));
                    debTexts[x][y].Add(Instantiate(textTemplate));
                    debTexts[x][y][z].transform.position = CenterPosFromIndecies(x, y, z);
				}
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (simCounter <= 0)
        {
            Debug.Log("Sim step!");
			if (Input.GetKey(KeyCode.LeftShift))
			{
				cells[cellsWide / 2][0][cellsWide / 2].temp = 20;
			}

			nextCells = new List<List<List<CellData>>>();
            for (int x = 0; x < cellsWide; x++)
            {
                nextCells.Add(new List<List<CellData>>());

                for (int y = 0; y < cellsTall; y++)
                {
                    nextCells[x].Add(new List<CellData>());

                    for (int z = 0; z < cellsWide; z++)
                    {
                        nextCells[x][y].Add(new CellData(Vector3.zero, 0, 0));
                    }
                }
            }

            simCounter = simPeriodS * 50;

			for (int x = 0; x < cellsWide; x++)
				for (int z = 0; z < cellsWide; z++)
					for (int y = 0; y < cellsTall; y++)
						Iterate(x, y, z, cells, nextCells);

			cells = nextCells;

            for (int x = 0; x < cellsWide; x++)
                for (int z = 0; z < cellsWide; z++)
                    for (int y = 0; y < cellsTall; y++)
                    {
                        debTexts[x][y][z].text = cells[x][y][z].pressure.ToString("n1");
						debTexts[x][y][z].color = new Color (cells[x][y][z].temp / 5, 0, 0);
					}
	}
        simCounter--;

		for (int x = 0; x < cellsWide; x++)
            for (int z = 0; z < cellsWide; z++)
                for (int y = 0; y < cellsTall; y++)
                {
                    Vector3 centerPos = CenterPosFromIndecies(x, y, z);
                    CellData cell = cells[x][y][z];
                    Debug.DrawRay(centerPos, cell.wind);
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

        /*float pressureTotal = 0f;
        float tempTotal = 0f;
        float pressureWeightTotal = 0f;
        float tempWeightTotal = 0f;
		foreach (string key in currentCells.Keys)
        {
			float gravityDiff = 0f;
            float tempMult = 1f;
            if (key == "U")
                gravityDiff = gravityBonus;
            else if (key == "D")
                gravityDiff = -gravityBonus;
            else if (key == "C")
                tempMult = 0.2f;
            pressureTotal += (currentCells[key].pressure + gravityDiff) * pressureWeights[key];
            tempTotal += (currentCells[key].temp) * tempMult * tempWeights[key];
            pressureWeightTotal += pressureWeights[key];
            tempWeightTotal += tempWeights[key];
        }
        nextCells["C"].pressure = pressureTotal / pressureWeightTotal;
        nextCells["C"].temp = tempTotal / tempWeightTotal;*/

		//      float pressureForceSum = 0f;
		//      float currCenPressure = currentCells["C"].pressure;
		//      float currCenTemp = currentCells["C"].temp;
		//Dictionary<string, float> pressureForce = new Dictionary<string, float>();
		//foreach (string key in currentCells.Keys)
		//{
		//	if (key == "C")
		//		continue;
		//	float tempDiffFraction = Mathf.Max(0, (currCenTemp - currentCells[key].temp) / currCenTemp);
		//	float gravityBonus = 0f;
		//	if (key == "U")
		//		gravityBonus = -gravityDiff;
		//	else if (key == "D")
		//		gravityBonus = gravityDiff;

		//          float pressDiff = currCenPressure - currentCells[key].pressure;
		//          float tempForce = Mathf.Max(0, (tempDiffFraction + (pressDiff / currCenPressure)) * tempDiffBonus);
		//          Debug.Log(key + " t" + tempForce);
		//	float dirPressForce = pressDiff + (currCenPressure * gravityBonus) + tempForce; //how much lower the pressure is in the cell to the keyed direction, bias by gravity and temperature
		//          if (dirPressForce > 0)
		//          {
		//		//Debug.Log(key + " f" + dirPressForce);
		//		pressureForce.Add(key, dirPressForce);
		//              pressureForceSum += dirPressForce;
		//          }
		//}
		//      //Debug.Log("T:" + pressureForceSum);

		//float totalOutflow = 0;
		//      float totalTempLoss = 0;
		//      float availablePressureFraction = (-1 / (diffusedPressureFraction * pressureForceSum + 1)) + 1; //ensures available pressure fraction approaches 1
		////Debug.Log("A:" + availablePressureFraction);
		//foreach (string key in pressureForce.Keys) //at this point, only runs for directions where pressure force is outward
		//      {
		//          //Debug.Log(x + " " + y + " " + z + ": " + fraction + " " + key);
		//          float pressureOut = (pressureForce[key] / pressureForceSum) * (availablePressureFraction * currCenPressure);
		//          nextCells[key].pressure += pressureOut;
		//          totalOutflow += pressureOut;
		//	nextCells["C"].wind += directions[key] * pressureOut * windStrength;
		//          float tempOut = currCenTemp * (pressureOut / currCenPressure);
		//	nextCells[key].temp += tempOut;
		//          totalTempLoss += tempOut;
		//}

		//      //fill in C based on remaining pressure/temp
		//nextCells["C"].pressure += currentCells["C"].pressure - totalOutflow;

		////foreach (string key in currentCells.Keys) //at this point only runs for directions where the cell has less pressure than this one
		////{
		////	if (key == "C")
		////		continue;
		////	float portion = (1f / (float)currentCells.Keys.Count) * (leftoverHeat * diffusedTempFraction);
		////	nextCells[key].temp += portion;
		////}
		////      leftoverHeat -= leftoverHeat * diffusedTempFraction;
		//      nextCells["C"].temp += currCenTemp - totalTempLoss;
	}
}
