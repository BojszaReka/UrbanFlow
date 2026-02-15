using GTFS.Entities.Enumerations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.db;

namespace Urbanflow.src.backend.models.gtfs
{
	[Table("Trips")]
	public class Trip
	{
		//Database fields
		[Key]
		public Guid Id { get; set; }
		public Guid GtfsFeedId { get; internal set; }

		//GTFS fields
		public string TripId { get; set; }
		public string RouteId { get; set; }
		public string ServiceId { get; set; }
		public string Headsign { get; set; }
		public string ShortName { get; set; }
		public DirectionType? Direction { get; set; }
		public string BlockId { get; set; }
		public string ShapeId { get; set; }
		public WheelchairAccessibilityType? AccessibilityType { get; set; }
		[ForeignKey("GtfsFeedId")]
		public GtfsFeed GtfsFeed { get; internal set; }

		//Constructors
		public Trip() { }

		public Trip(GTFS.Entities.Trip trip, Guid id)
		{
			using var db = new DatabaseContext();
			Id = Guid.NewGuid();
			GtfsFeedId = id;
			TripId = trip.Id;
			RouteId = trip.RouteId;
			ServiceId = trip.ServiceId;
			Headsign = trip.Headsign;
			ShortName = trip.ShortName;
			Direction = trip.Direction;
			BlockId = trip.BlockId;
			ShapeId = trip.ShapeId;
			AccessibilityType = trip.AccessibilityType;

			db.Trips.Add(this);
			db.SaveChanges();
		}

		public Trip(Guid id)
		{
			Id = id;
			using var context = new DatabaseContext();
			Trip? t = context.Trips?.Find(id) ?? throw new InvalidOperationException($"Trip with id {id} not found.");
			GtfsFeedId = t.GtfsFeedId;
			TripId = t.TripId;
			RouteId = t.RouteId;
			ServiceId = t.ServiceId;
			Headsign = t.Headsign;
			ShortName = t.ShortName;
			Direction = t.Direction;
			BlockId = t.BlockId;
			ShapeId = t.ShapeId;
			AccessibilityType = t.AccessibilityType;
		}


		// GTFS methods
		public GTFS.Entities.Trip Export()
		{
			return new GTFS.Entities.Trip
			{
				Id = TripId,
				RouteId = RouteId,
				ServiceId = ServiceId,
				Headsign = Headsign,
				ShortName = ShortName,
				Direction = Direction,
				BlockId = BlockId,
				ShapeId = ShapeId,
				AccessibilityType = AccessibilityType
			};
		}

		// Stolen methods
		public override string ToString()
		{
			return string.Format("[{0}] {1}", new object[2] { ShortName, Headsign });
		}

		public override int GetHashCode()
		{
			return ((((((((83 * 89 + AccessibilityType.GetHashCode()) * 89 + BlockId.GetHashCode()) * 89 + Direction.GetHashCode()) * 89 + Headsign.GetHashCode()) * 89 + TripId.GetHashCode()) * 89 + RouteId.GetHashCode()) * 89 + ServiceId.GetHashCode()) * 89 + ShapeId.GetHashCode()) * 89 + ShortName.GetHashCode();
		}

		public override bool Equals(object? obj)
		{
			if (obj is Trip trip)
			{
				WheelchairAccessibilityType? accessibilityType = AccessibilityType;
				WheelchairAccessibilityType? accessibilityType2 = trip.AccessibilityType;
				if (accessibilityType.GetValueOrDefault() == accessibilityType2.GetValueOrDefault() && accessibilityType.HasValue == accessibilityType2.HasValue && (BlockId ?? string.Empty) == (trip.BlockId ?? string.Empty))
				{
					DirectionType? direction = Direction;
					DirectionType? direction2 = trip.Direction;
					if (direction.GetValueOrDefault() == direction2.GetValueOrDefault() && direction.HasValue == direction2.HasValue && (Headsign ?? string.Empty) == (trip.Headsign ?? string.Empty) && (TripId ?? string.Empty) == (trip.TripId ?? string.Empty) && (RouteId ?? string.Empty) == (trip.RouteId ?? string.Empty) && (ServiceId ?? string.Empty) == (trip.ServiceId ?? string.Empty) && (ShapeId ?? string.Empty) == (trip.ShapeId ?? string.Empty))
					{
						return (ShortName ?? string.Empty) == (trip.ShortName ?? string.Empty);
					}
				}

				return false;
			}

			return false;
		}
	}
}
