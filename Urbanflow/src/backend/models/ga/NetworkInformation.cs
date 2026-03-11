using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.ga
{
	public class NetworkInformation
	{
		public List<Guid> Terminals { get; set; } = new List<Guid>();
		public List<Guid> Hubs { get; set; } = new List<Guid>();
		public List<Guid> GenericStops { get; set; } = new List<Guid>();

		//Minden megállóhoz tartozik egy lista
		// aa lista tartalmazza azokat a megállókat amivel közvetlen össze van kötve, valamint az él súlyával
		public Dictionary<Guid, List<(Guid Destination, double Weight)>> StopConnectivityMatrix { get; set; }
			= new Dictionary<Guid, List<(Guid, double)>>();

	}
}
