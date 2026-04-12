
using GTFS.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.models.ga;
using Urbanflow.src.backend.models.gtfs;
using Urbanflow.src.backend.models.util;

namespace Urbanflow.src.backend.models.db_ga
{
	[Table("Genomes")]
	public class Genome
	{
		public Guid Id { get; set; }
		public Guid WorkflowId { get; set; }

		public int GenomeID { get;  }
		public int GenerationID { get; set; }
		
		public double FitnessValue { get; private set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		[ForeignKey("WrokflowId")]
		private Workflow Workflow;
		public List<GenomeRoute> MutableRoutes { get; } = [];

		public Genome()
		{

		}

		public Genome(in ga.Genome genome, Guid workflowId)
		{
			WorkflowId = workflowId;
			Id = Guid.NewGuid();
			GenomeID =  genome.GenomeID;
			GenerationID = genome.GenerationID;
			FitnessValue = genome.FitnessValue;
		}

		

		public override string ToString()
		{
			return $"GenomeID={GenomeID}, Gen={GenerationID}, Fitness={FitnessValue:F4}, Created At={CreatedAt}";
		}

		internal IEnumerable<Guid> GetStopIdList()
		{
			HashSet<Guid> ids = new HashSet<Guid>();
			foreach (var route in MutableRoutes) {
				HashSet<Guid> routeIds = route.CollectIds();
				foreach (var routeId in routeIds) { 
					ids.Add(routeId);
				}
			}
			return ids;
		}
	}
}
