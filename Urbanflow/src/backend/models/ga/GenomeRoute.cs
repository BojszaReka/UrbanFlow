using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.ga
{
	public class GenomeRoute
	{
		//main fields -> computed
		public List<Guid> OnRoute { get; set; } = [];
		public List<Guid> BackRoute { get; set; } = [];
		public int OnStartTime { get; } // 0-59 közötti diszkrét érték
		public int BackStartTime { get; } // 0-59 közötti diszkrét érték
		public int Headway { get; } // 5-60 közötti érték
		public bool OneWay { get; }

		public GenomeRoute(List<Guid> onRoute, int onStartTime, List<Guid> backRoute, int backStartTime, int headway, bool oneWay = false)
		{
			OnRoute = onRoute;
			OnStartTime = onStartTime;
			BackRoute = backRoute;
			BackStartTime = backStartTime;
			Headway = headway;
			OneWay = oneWay;
		}

		public GenomeRoute(List<Guid> onRoute, int onStartTime, int headway, bool oneWay = true)
		{
			OnRoute = onRoute;
			OnStartTime = onStartTime;
			BackRoute = [];
			BackStartTime = -1;
			Headway = headway;
			OneWay = oneWay;
		}
	}
}
