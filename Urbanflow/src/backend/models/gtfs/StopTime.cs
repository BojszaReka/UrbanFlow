using GTFS.Entities;
using GTFS.Entities.Enumerations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.db;

namespace Urbanflow.src.backend.models.gtfs
{
	[Table("StopTimes")]
	public class StopTime
	{
		//Database fields
		[Key]
		public Guid Id { get; internal set; }
		public Guid GtfsFeedId { get; internal set; }

		//GTFS fields
		public string TripId { get; internal set; }
		public string ArrivalTime { get; internal set; }
		public string DepartureTime { get; internal set; }
		public string StopId { get; internal set; }
		public uint StopSequence { get; internal set; }
		public string StopHeadsign { get; internal set; }
		public PickupType? PickupType { get; internal set; }
		public DropOffType? DropOffType { get; internal set; }
		public string? ShapeDistTravelled { get; internal set; }
		[ForeignKey("GtfsFeedId")]
		public GtfsFeed GtfsFeed { get; internal set; }

		// Contructors
		public StopTime() { }

		public StopTime(GTFS.Entities.StopTime st, Guid id)
		{
			using var db = new DatabaseContext();
			Id = Guid.NewGuid();
			GtfsFeedId = id;
			TripId = st.TripId;
			ArrivalTime = st.ArrivalTime.Hours + ":" + st.ArrivalTime.Minutes + ":" + st.ArrivalTime.Seconds;
			DepartureTime = st.DepartureTime.Hours + ":" + st.DepartureTime.Minutes + ":" + st.DepartureTime.Seconds;
			StopId = st.StopId;
			StopSequence = st.StopSequence;
			StopHeadsign = st.StopHeadsign;
			PickupType = st.PickupType;
			DropOffType = st.DropOffType;
			ShapeDistTravelled = st.ShapeDistTravelled;
			db.StopTimes.Add(this);
			db.SaveChanges();
		}

		public StopTime(Guid id)
		{
			Id = id;
			using var db = new DatabaseContext();
			StopTime? stopTime = (db.StopTimes?.Find(id)) ?? throw new InvalidOperationException($"StopTime with id {id} not found.");
			GtfsFeedId = stopTime.GtfsFeedId;
			TripId = stopTime.TripId;
			ArrivalTime = stopTime.ArrivalTime;
			DepartureTime = stopTime.DepartureTime;
			StopId = stopTime.StopId;
			StopSequence = stopTime.StopSequence;
			StopHeadsign = stopTime.StopHeadsign;
			PickupType = stopTime.PickupType;
			DropOffType = stopTime.DropOffType;
			ShapeDistTravelled = stopTime.ShapeDistTravelled;
		}


		// GTFS methods
		public GTFS.Entities.StopTime Export()
		{
			var at = ArrivalTime.Split(':');
			var dt = DepartureTime.Split(':');

			return new GTFS.Entities.StopTime
			{
				TripId = TripId,
				ArrivalTime = new TimeOfDay
				{
					Hours = int.Parse(at[0]),
					Minutes = int.Parse(at[1]),
					Seconds = int.Parse(at[2])
				},
				DepartureTime = new TimeOfDay
				{
					Hours = int.Parse(dt[0]),
					Minutes = int.Parse(dt[1]),
					Seconds = int.Parse(dt[2])
				},
				StopId = StopId,
				StopSequence = StopSequence,
				StopHeadsign = StopHeadsign,
				PickupType = PickupType,
				DropOffType = DropOffType,
				ShapeDistTravelled = ShapeDistTravelled
			};
		}

		// Stolen methods
		public override string ToString()
		{
			return string.Format("[{0}:{1}] {2}", new object[3] { TripId, StopId, StopHeadsign });
		}

		public int CompareTo(object obj)
		{
			StopTime? stopTime = obj as StopTime ?? throw new ArgumentException("Object is not a StopTime", nameof(obj));
			if (!string.Equals(TripId, stopTime.TripId, StringComparison.Ordinal))
			{
				return string.Compare(TripId, stopTime.TripId, StringComparison.Ordinal);
			}

			return StopSequence.CompareTo(stopTime.StopSequence);
		}

		public override int GetHashCode()
		{
			return ((((((((53 * 59 + ArrivalTime.GetHashCode()) * 59 + DepartureTime.GetHashCode()) * 59 + DropOffType.GetHashCode()) * 59 + PickupType.GetHashCode()) * 59 + ShapeDistTravelled.GetHashCode()) * 59 + StopHeadsign.GetHashCode()) * 59 + StopId.GetHashCode()) * 59 + StopSequence.GetHashCode()) * 59 + TripId.GetHashCode();
		}

		public override bool Equals(object? obj)
		{
			if (obj is StopTime stopTime)
			{
				if (ArrivalTime.Equals(stopTime.ArrivalTime) && DepartureTime.Equals(stopTime.DepartureTime) && DropOffType == stopTime.DropOffType)
				{
					PickupType? pickupType = PickupType;
					PickupType? pickupType2 = stopTime.PickupType;
					if (pickupType.GetValueOrDefault() == pickupType2.GetValueOrDefault() && pickupType.HasValue == pickupType2.HasValue && (ShapeDistTravelled ?? string.Empty) == (stopTime.ShapeDistTravelled ?? string.Empty) && (StopHeadsign ?? string.Empty) == (stopTime.StopHeadsign ?? string.Empty) && (StopId ?? string.Empty) == (stopTime.StopId ?? string.Empty) && StopSequence == stopTime.StopSequence)
					{
						return (TripId ?? string.Empty) == (stopTime.TripId ?? string.Empty);
					}
				}

				return false;
			}

			return false;
		}

	}
}
