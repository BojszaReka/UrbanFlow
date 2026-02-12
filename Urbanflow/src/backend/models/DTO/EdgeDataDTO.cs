using System;
using System.Collections.Generic;
using System.Text;

namespace Urbanflow.src.backend.models.DTO
{
	public class EdgeDataDTO
	{
		public Guid FromStopId {  get; set; }
		public Guid ToStopId { get; set; }
		public int TravelTimeMinutes { get; set; }

	}
}
