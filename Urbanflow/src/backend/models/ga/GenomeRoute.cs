using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.ga
{
	public class GenomeRoute
	{
		//main fields -> computed
		public List<Guid> OnRoute { get; set; } = new List<Guid>();
		public List<Guid> BackRoute { get; set; } = new List<Guid>();
		public int OnStartTime { get; } // 0-59 közötti diszkrét érték
		public int BackStartTime { get; } // 0-59 közötti diszkrét érték
		public int Headway { get; } // 5-60 közötti érték

		public GenomeRoute(List<Guid> onRoute, int onStartTime, List<Guid> backRoute, int backStartTime, int headway)
		{
			OnRoute = onRoute;
			OnStartTime = onStartTime;
			BackRoute = backRoute;
			BackStartTime = backStartTime;
			Headway = headway;
		}
	}
}
