using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Urbanflow.src.backend.db;
using Urbanflow.src.backend.enums;

namespace Urbanflow.src.backend.models.graph
{
	[Table("Edges")]
	public class Edge
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public Guid FromNodeId { get; set; }
		public Guid ToNodeId { get; set; }
		public double Weight { get; set; } // travel time in seconds
		public EEdgeType Type { get; set; } = EEdgeType.Default;

		public Edge() { }

		public Edge(Guid edgeId)
		{
			using var db = new DatabaseContext();
			var edge = db.Edges?.Find(edgeId);
			if (edge is not null)
			{
				Id = edge.Id;
				FromNodeId = edge.FromNodeId;
				ToNodeId = edge.ToNodeId;
				Weight = edge.Weight;
				Type = edge.Type;
			}
			else
			{
				throw new Exception("Edge not found in database.");
			}
		}

		public Edge(Guid fromNodeId, Guid toNodeId, double weight, EEdgeType type = EEdgeType.Default)
		{
			FromNodeId = fromNodeId;
			ToNodeId = toNodeId;
			Weight = weight;
			Type = type;
			SaveToDatabase();
		}

		public override string ToString()
		{
			return $"Edge {Id}: From {FromNodeId} to {ToNodeId}, Weight: {Weight}, Type: {Type}";
		}

		public void SaveToDatabase()
		{
			using var db = new DatabaseContext();
			var existingEdge = db.Edges?.Find(Id);
			if (existingEdge is not null)
			{
				db.Edges?.Update(this);
				db.SaveChanges();
			}
			else
			{
				db.Edges?.Add(this);
				db.SaveChanges();
			}
		}
	}
}
