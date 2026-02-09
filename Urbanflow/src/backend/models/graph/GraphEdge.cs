using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Urbanflow.src.backend.models.graph
{
	[Table("GraphEdges")]
	public class GraphEdge
	{
		public Guid GraphId { get; set; } = Guid.Empty;
		public Guid EdgeId { get; set; } = Guid.Empty;

		public GraphEdge() { }

		public GraphEdge(Guid graphid, Guid edgeid)
		{
			GraphId = graphid;
			EdgeId = edgeid;
		}

		
	}
}
