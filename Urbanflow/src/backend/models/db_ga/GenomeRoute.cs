

using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.models.ga;

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


	}
}
