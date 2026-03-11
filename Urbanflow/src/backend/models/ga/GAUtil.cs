using System;
using System.Collections.Generic;
using System.Text;
using Urbanflow.src.backend.models.util;

namespace Urbanflow.src.backend.models.ga
{
	public class GAUtil
	{
		public static Result<List<Guid>> GetShortestPath(Guid start, Guid end, in NetworkInformation network)
		{
			var distances = new Dictionary<Guid, double>();
			var previousNodes = new Dictionary<Guid, Guid>();
			var priorityQueue = new PriorityQueue<Guid, double>();

			// Távolságok inicializálása
			foreach (var node in network.StopConnectivityMatrix.Keys)
			{
				distances[node] = double.MaxValue;
			}

			distances[start] = 0;
			priorityQueue.Enqueue(start, 0);

			while (priorityQueue.Count > 0)
			{
				var current = priorityQueue.Dequeue();

				if (current == end) break;
				if (!network.StopConnectivityMatrix.ContainsKey(current)) continue;

				foreach (var (Destination, Weight) in network.StopConnectivityMatrix[current])
				{
					double alt = distances[current] + Weight;

					// Relaxáció
					if (!distances.TryGetValue(Destination, out double value) || alt < value)
					{
						value = alt;
						distances[Destination] = value;
						previousNodes[Destination] = current;
						priorityQueue.Enqueue(Destination, alt);
					}
				}
			}

			// Útvonal visszafejtése
			var path = new List<Guid>();
			var curr = end;
			if (!previousNodes.ContainsKey(curr) && curr != start)
				return Result<List<Guid>>.Failure("No path found");

			while (curr != start)
			{
				path.Add(curr);
				curr = previousNodes[curr];
			}
			path.Add(start);
			path.Reverse();

			return Result<List<Guid>>.Success(path);
		}

		public static Genome TournamentSelect(in List<Genome> pop, int k, Random rnd)
		{
			Genome best = null;
			for (int i = 0; i < k; i++)
			{
				var candidate = pop[rnd.Next(pop.Count)];
				if (best == null || candidate.FitnessValue < best.FitnessValue)
				{
					best = candidate;
				}
			}
			return best;
		}

		public static double CalculateHubGiniIndex(in List<int> hubDegrees)
		{
			if (hubDegrees == null || hubDegrees.Count == 0) return 0;

			int C = hubDegrees.Count;
			int[] f = hubDegrees.ToArray();

			// Szigorúan növekvő sorrendbe rendezés a formula alkalmazásához
			Array.Sort(f);

			double sumOfDegrees = 0;
			double weightedSum = 0;

			for (int i = 0; i < C; i++)
			{
				sumOfDegrees += f[i];
				weightedSum += (i + 1) * f[i]; // (i+1) a formula szerinti aktuális rang/index
			}

			if (sumOfDegrees == 0) return 0; // Nullával való osztás (pl. üres hálózat) elkerülése

			// A formális dokumentációban szereplő képlet pontos implementációja
			double gini = (2.0 * weightedSum) / (C * sumOfDegrees) - ((double)(C + 1) / C);

			return gini;
		}
	}
}
