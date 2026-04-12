using System;
using System.Collections.Generic;
using System.Text;
using Urbanflow.src.backend.models.util;

namespace Urbanflow.src.backend.models.DTO
{
	public class EdgeDataDTO
	{
		public Guid FromStopId {  get; set; }
		public Guid ToStopId { get; set; }
		public int TravelTimeMinutes { get; set; }

		public (byte r, byte g, byte b)? Color { get; set; }

	}
}
