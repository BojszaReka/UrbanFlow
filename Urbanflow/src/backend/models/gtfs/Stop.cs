using GTFS.Entities.Enumerations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.db;
using Urbanflow.src.backend.enums;

namespace Urbanflow.src.backend.models.gtfs
{
	[Table("Stops")]
	public class Stop
	{
		//Database fields
		[Key]
		public Guid Id { get; internal set; }
		public Guid GtfsFeedId { get; internal set; }
		public ENodeType NodeType { get; internal set; } = ENodeType.Default;

		//GTFS fields
		public string StopId { get; internal set; }
		public string Code { get; internal set; }
		public string Name { get; internal set; }
		public string Description { get; internal set; }
		public double Latitude { get; internal set; }
		public double Longitude { get; internal set; }
		public string Zone { get; internal set; }
		public string Url { get; internal set; }
		public LocationType? LocationType { get; internal set; }
		public string ParentStation { get; internal set; }
		public string Timezone { get; internal set; }
		public string WheelchairBoarding { get; internal set; }
		[ForeignKey("GtfsFeedId")]
		public GtfsFeed GtfsFeed { get; internal set; }

		//Constructors
		public Stop() { }

		public Stop(GTFS.Entities.Stop stop, Guid id)
		{
			using var db = new DatabaseContext();
			Id = Guid.NewGuid();
			GtfsFeedId = id;
			NodeType = ENodeType.Default;

			StopId = stop.Id;
			Code = stop.Code;
			Name = stop.Name;
			Description = stop.Description;
			Latitude = stop.Latitude;
			Longitude = stop.Longitude;
			Zone = stop.Zone;
			Url = stop.Url;
			LocationType = stop.LocationType;
			ParentStation = stop.ParentStation;
			Timezone = stop.Timezone;
			WheelchairBoarding = stop.WheelchairBoarding ?? "Unknown";

			db.Stops.Add(this);
			db.SaveChanges();
		}

		public Stop(Guid id)
		{
			Id = id;
			using var context = new DatabaseContext();
			var stop = context.Stops?.Find(id);
			if (stop is not null)
			{
				GtfsFeedId = stop.GtfsFeedId;
				StopId = stop.StopId;
				NodeType = stop.NodeType;
				Code = stop.Code;
				Name = stop.Name;
				Description = stop.Description;
				Latitude = stop.Latitude;
				Longitude = stop.Longitude;
				Zone = stop.Zone;
				Url = stop.Url;
				LocationType = stop.LocationType;
				ParentStation = stop.ParentStation;
				Timezone = stop.Timezone;
				WheelchairBoarding = stop.WheelchairBoarding ?? "Unknown";
			}
			else
			{
				throw new InvalidOperationException($"Stop with id {id} not found.");
			}
		}

		// GTFS methods
		public GTFS.Entities.Stop Export()
		{
			return new GTFS.Entities.Stop
			{
				Id = StopId,
				Code = Code,
				Name = Name,
				Description = Description,
				Latitude = Latitude,
				Longitude = Longitude,
				Zone = Zone,
				Url = Url,
				LocationType = LocationType,
				ParentStation = ParentStation,
				Timezone = Timezone,
				WheelchairBoarding = WheelchairBoarding == "Unknown" ? null : WheelchairBoarding
			};
		}

		//Stolen methods
		public override string ToString()
		{
			return string.Format("[{0}] {1} - {2}", new object[3] { Id, Name, Description });
		}

		public override int GetHashCode()
		{
			return (((((((((((41 * 43 + Code.GetHashCode()) * 43 + Description.GetHashCode()) * 43 + StopId.GetHashCode()) * 43 + Latitude.GetHashCode()) * 43 + LocationType.GetHashCode()) * 43 + Longitude.GetHashCode()) * 43 + Name.GetHashCode()) * 43 + ParentStation.GetHashCode()) * 43 + Timezone.GetHashCode()) * 43 + Url.GetHashCode()) * 43 + WheelchairBoarding.GetHashCode()) * 43 + Zone.GetHashCode();
		}

		public override bool Equals(object? obj)
		{
			if (obj is Stop stop)
			{
				const double Tolerance = 1e-6;
				if ((Code ?? string.Empty) == (stop.Code ?? string.Empty) &&
					(Description ?? string.Empty) == (stop.Description ?? string.Empty) &&
					(StopId ?? string.Empty) == (stop.StopId ?? string.Empty) &&
					Math.Abs(Latitude - stop.Latitude) < Tolerance)
				{
					LocationType? locationType = LocationType;
					LocationType? locationType2 = stop.LocationType;
					if (locationType.GetValueOrDefault() == locationType2.GetValueOrDefault() &&
						locationType.HasValue == locationType2.HasValue &&
						Math.Abs(Longitude - stop.Longitude) < Tolerance &&
						(Name ?? string.Empty) == (stop.Name ?? string.Empty) &&
						(ParentStation ?? string.Empty) == (stop.ParentStation ?? string.Empty) &&
						(Timezone ?? string.Empty) == (stop.Timezone ?? string.Empty) &&
						(Url ?? string.Empty) == (stop.Url ?? string.Empty) &&
						(WheelchairBoarding ?? string.Empty) == (stop.WheelchairBoarding ?? string.Empty))
					{
						return (Zone ?? string.Empty) == (stop.Zone ?? string.Empty);
					}
				}

				return false;
			}

			return false;
		}
	}
}
