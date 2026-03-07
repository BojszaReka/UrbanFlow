using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.ga
{
	public class Genome
	{
		public int Id { get;  }
		public int Generation { get; }

		//main fields -> computed
		public List<(int stopSequence, Guid stopId)> Route;
		public int StartTime { get;  }
		public int Headway { get;  }

		//helper values
		public List<int> Parents { get; }
		public Guid StartTerminal { get;  }
		public Guid EndTerminal { get;  }
		public List<Guid> MetHubs { get; }
		public int FitnessValue { get; }

		//initialization
		public Genome(int id, int generation, in OptimizationParameters parameters, in NetworkInformation network)
		{
			Id = id;
			Generation = generation;

			int hubCount = parameters.Genome_HubNumberInRoute;

			var terminals = network.Terminals;
			var hubs = network.Hubs;
			var otherStops = network.GenericStops;
			var networkConnectivity = network.StopConnectivityMatrix;

			// choose 2 on random from Terminals
			// choose hubCount number of stops from Hub on random
			// based on network connectivity connect the terminals and hubs with the (shortest) possible paths 
			CalculateFitnessValue(parameters);
		}

		
		//Crossing, step = {"route", "time"}
		public Genome(int id, int generation, Genome parent1, Genome parent2, in OptimizationParameters parameters, string step)
		{
			Id = id;
			Generation = generation;
			Parents.Add(parent1.Id);
			Parents.Add(parent2.Id);
			switch (step)
			{
				case "route": break;
				case "time": break;
				default: break;
			}
			CalculateFitnessValue(parameters);
		}


		//Mutation
		public Genome(int id, int generation, Genome parent, in OptimizationParameters parameters, string step)
		{
			Id = id;
			Generation = generation;
			Parents.Add(parent.Id);
			switch (step)
			{
				case "route": break;
				case "time": break;
				default: break;
			}
			CalculateFitnessValue(parameters);
		}


		//Fitness function
		public void CalculateFitnessValue(in OptimizationParameters parameters) { 
		
			throw new NotImplementedException();
		}



	}
}
