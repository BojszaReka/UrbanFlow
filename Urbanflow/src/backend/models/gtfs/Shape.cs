using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.db;

namespace Urbanflow.src.backend.models.gtfs
{
	[Table("Shapes")]
	public class Shape
	{
		// Database Fields
		[Key]
		public Guid Id { get; internal set; }
		public Guid GtfsFeedId { get; internal set; }

		// GTFS Fields
		public string? ShapeId { get; internal set; }
		public double Latitude { get; internal set; }
		public double Longitude { get; internal set; }
		public uint Sequence { get; internal set; }
		public double? DistanceTravelled { get; internal set; }

		// Constructors
		public Shape(GTFS.Entities.Shape s, Guid id)
		{
			Id = Guid.NewGuid();
			GtfsFeedId = id;
			ShapeId = s.Id;
			Latitude = s.Latitude;
			Longitude = s.Longitude;
			Sequence = s.Sequence;
			DistanceTravelled = s.DistanceTravelled;
		}

		public Shape(Guid id)
		{
			Id = id;
			DatabaseContext db = new();
			var shape = db.Shapes?.Find(id);
			if (shape is not null)
			{
				GtfsFeedId = shape.GtfsFeedId;
				ShapeId = shape.ShapeId;
				Latitude = shape.Latitude;
				Longitude = shape.Longitude;
				Sequence = shape.Sequence;
				DistanceTravelled = shape.DistanceTravelled;
			}
			else
			{
				throw new InvalidOperationException($"Shape with id {id} not found.");
			}
		}


		//GTFS methods
		public GTFS.Entities.Shape Export()
		{
			return new GTFS.Entities.Shape
			{
				Id = ShapeId,
				Latitude = Latitude,
				Longitude = Longitude,
				Sequence = Sequence,
				DistanceTravelled = DistanceTravelled
			};
		}

		//Stolen methods
		public override int GetHashCode()
		{
			return ((((43 * 47 + DistanceTravelled.GetHashCode()) * 47 + (ShapeId ?? string.Empty).GetHashCode()) * 47 + Latitude.GetHashCode()) * 47 + Longitude.GetHashCode()) * 47 + Sequence.GetHashCode();
		}

		public override bool Equals(object? obj)
		{
			const double Tolerance = 1e-9;
			if (obj is Shape shape)
			{
				double? distanceTravelled = DistanceTravelled;
				double? distanceTravelled2 = shape.DistanceTravelled;
				bool distanceEqual = (!distanceTravelled.HasValue && !distanceTravelled2.HasValue) ||
					(distanceTravelled.HasValue && distanceTravelled2.HasValue &&
						Math.Abs(distanceTravelled.GetValueOrDefault() - distanceTravelled2.GetValueOrDefault()) < Tolerance);

				bool latitudeEqual = Math.Abs(Latitude - shape.Latitude) < Tolerance;
				bool longitudeEqual = Math.Abs(Longitude - shape.Longitude) < Tolerance;

				if (distanceEqual &&
					(ShapeId ?? string.Empty) == (shape.ShapeId ?? string.Empty) &&
					latitudeEqual &&
					longitudeEqual)
				{
					return Sequence == shape.Sequence;
				}

				return false;
			}

			return false;
		}
	}
}
