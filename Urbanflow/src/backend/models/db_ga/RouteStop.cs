using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Urbanflow.src.backend.models.enums;

namespace Urbanflow.src.backend.models.db_ga
{
	[Table("RouteStops")]
	public class RouteStop
	{
		public Guid Id { get; set; }
		public Guid GenomeRouteId { get; set; }
		public Guid StopId { get; set; }
		public ERouteDirection Direction { get; set; } = ERouteDirection.OnRoute;
		public int StopSequence { get; set; }

		[ForeignKey("GenomeRouteId")]
		private GenomeRoute GenomeRoute;

		public RouteStop()
		{

		}

		public RouteStop(Guid genomeRouteId, Guid stopId, ERouteDirection direction, int sequenceNumber)
		{
			Id = Guid.NewGuid();
			GenomeRouteId = genomeRouteId;
			StopId = stopId;
			Direction = direction;
			StopSequence = sequenceNumber;
		}

	}
}
