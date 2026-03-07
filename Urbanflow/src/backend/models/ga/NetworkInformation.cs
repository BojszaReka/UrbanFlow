using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.ga
{
	public class NetworkInformation
	{
		public List<Guid> Terminals { get; set; }
		public List<Guid> Hubs { get; set; }
		public List<Guid> GenericStops { get; set; }

		//Összeköttetési mátrix, ha x=y akk -1
		//ha x-ből y-ba nem megy út 0
		//ha x-ből y-ba megy út akkor n, ami a súlya az élnek
		public HashSet<(Guid StopAxisX, Guid StopAxisY)> StopConnectivityMatrix { get; set; }

	}
}
