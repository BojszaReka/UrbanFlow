

using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.models.DTO;
using Urbanflow.src.backend.models.ga;
using Urbanflow.src.backend.models.util;

namespace Urbanflow.src.backend.models.db_ga
{
	[Table("GenomeRoutes")]
	public class GenomeRoute
	{
		public Guid Id { get; set; }
		public Guid GenomeId { get; set; }

		//main fields -> computed
		
		public int OnStartTime { get;  set; } // 0-59 közötti diszkrét érték
		public int BackStartTime { get;  set; } // 0-59 közötti diszkrét érték
		public int Headway { get;  set; } // 5-60 közötti érték
		public bool OneWay { get; }

		[ForeignKey("GenomeId")]
		private Genome Genome;

		public List<RouteStop> OnRouteAndBackRouteStops { get; set; } = [];

		[NotMapped]
		public List<RouteStop> OnRoute { get; set; } = [];
		[NotMapped]
		public List<RouteStop> BackRoute { get; set; } = [];

		public GenomeRoute() { }
		public GenomeRoute(in ga.GenomeRoute genomeRoute, Guid genomeId) { 
			Id = Guid.NewGuid();
			GenomeId = genomeId;

			OnStartTime = genomeRoute.OnStartTime;
			BackStartTime = genomeRoute.BackStartTime;
			Headway = genomeRoute.Headway;
			OneWay = genomeRoute.OneWay;
		
		}

		internal Result<List<EdgeDataDTO>> GatherEdgeDataForAllRoutes(IReadOnlyDictionary<Guid, List<(Guid Destination, double Weight)>> stopConnectivityMatrix)
		{
			if((OnRoute == null || OnRoute.Count == 0)  || ((!OneWay && BackStartTime != -1) && (BackRoute == null || BackRoute.Count == 0)))
			{
				return Result<List<EdgeDataDTO>>.Failure("Needed route stops are not loaded");
			}

			HashSet<(Guid, Guid, double)> edges = [];

			OnRoute.OrderBy(sr => sr.StopSequence);
			for (var i = 0; i< OnRoute.Count-1; i++) {
				var from = OnRoute[i].StopId;
				var to = OnRoute[i+1].StopId;
				if(!stopConnectivityMatrix.TryGetValue(from, out var neighbours))
				{
					edges.Add((from, to, 5.0));
				}
				if (neighbours == null || neighbours.Count == 0) {
					edges.Add((from, to, 5.0));
				}
				else
				{
					bool found = false;
					foreach (var (dest, weight) in neighbours)
					{
						if (!found && dest.Equals(to))
						{
							edges.Add((from, to, weight));
							found = true;
						}
					}
				}
			}

			if(!OneWay && BackStartTime != -1)
			{
				BackRoute.OrderBy(sr => sr.StopSequence);
				for (var i = 0; i < BackRoute.Count - 1; i++)
				{
					var from = BackRoute[i].StopId;
					var to = BackRoute[i + 1].StopId;
					if (!stopConnectivityMatrix.TryGetValue(from, out var neighbours))
					{
						edges.Add((from, to, 5.0));
					}
					if (neighbours == null || neighbours.Count == 0)
					{
						edges.Add((from, to, 5.0));
					}
					else
					{
						foreach (var (dest, weight) in neighbours)
						{
							if (dest.Equals(to))
							{
								edges.Add((from, to, weight));
							}
						}
					}
				}
			}

			List<EdgeDataDTO> edgeDataDTOs = new List<EdgeDataDTO>();
			foreach (var (from, to, weight) in edges) {
				edgeDataDTOs.Add(new EdgeDataDTO()
				{
					FromStopId = from,
					ToStopId = to,
					TravelTimeMinutes = (int)weight
				});
			}

			return Result<List<EdgeDataDTO>>.Success(edgeDataDTOs);
		}

		internal HashSet<Guid> CollectIds()
		{
			HashSet<Guid> result = new HashSet<Guid>();
			foreach(var stop in OnRoute)
			{
				result.Add(stop.StopId);
			}
			if(BackRoute!= null && !OneWay && BackStartTime != -1)
			{
				foreach (var stop in BackRoute)
				{
					result.Add(stop.StopId);
				}
			}
			return result;
		}
	}
}
