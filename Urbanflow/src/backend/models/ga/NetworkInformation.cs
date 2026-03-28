using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.ga
{
	public class NetworkInformation
	{
		public IReadOnlyList<Guid> Terminals { get; }
		public IReadOnlyList<Guid> Hubs { get; }
		public IReadOnlyList<Guid> GenericStops { get; }

		public IReadOnlyList<Guid> AllStops { get; } 

		//Minden megállóhoz tartozik egy lista
		// aa lista tartalmazza azokat a megállókat amivel közvetlen össze van kötve, valamint az él súlyával
		public IReadOnlyDictionary<Guid, List<(Guid Destination, double Weight)>> StopConnectivityMatrix { get; }
			

		public IReadOnlyList<GenomeRoute> StaticRoutes { get; }
		public IReadOnlyDictionary<Guid, List<Guid>> Districts { get; }

		public Dictionary<Guid, Dictionary<Guid, List<Guid>>> CachedShortestPaths { get; set; } = [];

		public NetworkInformation(in List<Guid> terminals, in List<Guid> hubs, in List<Guid> genStops, in List<Guid> allStops, in Dictionary<Guid, List<(Guid Destination, double Weight)>> matrix, in List<GenomeRoute> staticRoutes, in Dictionary<Guid, List<Guid>> districts) { 
			Terminals = terminals;
			Hubs = hubs;
			GenericStops = genStops;
			AllStops = allStops;
			StopConnectivityMatrix = matrix;
			StaticRoutes = staticRoutes;
			Districts = districts;
		}
	}
}
