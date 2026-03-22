using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.ga
{
	public class NetworkInformation
	{
		public List<Guid> Terminals { get; set; } = [];
		public List<Guid> Hubs { get; set; } = [];
		public List<Guid> GenericStops { get; set; } = [];

		public List<Guid> AllStops { get; set; } = [];

		//Minden megállóhoz tartozik egy lista
		// aa lista tartalmazza azokat a megállókat amivel közvetlen össze van kötve, valamint az él súlyával
		public Dictionary<Guid, List<(Guid Destination, double Weight)>> StopConnectivityMatrix { get; set; }
			= [];

		public List<GenomeRoute> StaticRoutes { get; set; } = [];
		public Dictionary<Guid, List<Guid>> Districts { get; set; } = [];



	}
}
