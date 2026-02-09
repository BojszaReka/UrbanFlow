using GTFS.Entities.Enumerations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.db;

namespace Urbanflow.src.backend.models.gtfs
{
	[Table("Routes")]
	public class Route
	{
		// Database fields
		[Key]
		public Guid Id { get; internal set; }
		public Guid GtfsFeedId { get; internal set; }

		//GTFS fields
		public string RouteId { get; internal set; }
		public string AgencyId { get; internal set; }
		public string ShortName { get; internal set; }
		public string LongName { get; internal set; }
		public string Description { get; internal set; }
		public RouteTypeExtended Type { get; internal set; } = RouteTypeExtended.BusService;
		public string Url { get; internal set; }
		public int? Color { get; internal set; }
		public int? TextColor { get; internal set; }

		// Constructors
		public Route(GTFS.Entities.Route r, Guid id)
		{
			Id = Guid.NewGuid();
			GtfsFeedId = id;
			RouteId = r.Id;
			AgencyId = r.AgencyId;
			ShortName = r.ShortName;
			LongName = r.LongName;
			Description = r.Description;
			Url = r.Url;
			Color = r.Color;
			TextColor = r.TextColor;
		}

		public Route(Guid id)
		{
			DatabaseContext context = new();
			var route = context.Routes?.Find(id);
			if (route != null)
			{
				Id = route.Id;
				GtfsFeedId = route.GtfsFeedId;
				RouteId = route.RouteId;
				AgencyId = route.AgencyId;
				ShortName = route.ShortName;
				LongName = route.LongName;
				Description = route.Description;
				Type = route.Type;
				Url = route.Url;
				Color = route.Color;
				TextColor = route.TextColor;
			}
			else
			{
				throw new ArgumentException("Route with the specified ID does not exist.");
			}
		}

		// GTFS methods
		public GTFS.Entities.Route Export()
		{
			return new GTFS.Entities.Route
			{
				Id = RouteId,
				AgencyId = AgencyId,
				ShortName = ShortName,
				LongName = LongName,
				Description = Description,
				Type = Type,
				Url = Url,
				Color = Color,
				TextColor = TextColor
			};
		}

		//Stolen methods
		public override int GetHashCode()
		{
			return ((((((((41 * 43 + (AgencyId ?? string.Empty).GetHashCode()) * 43 + Color.GetHashCode()) * 43 + (Description ?? string.Empty).GetHashCode()) * 43 + (RouteId ?? string.Empty).GetHashCode()) * 43 + (LongName ?? string.Empty).GetHashCode()) * 43 + (ShortName ?? string.Empty).GetHashCode()) * 43 + TextColor.GetHashCode()) * 43 + Type.GetHashCode()) * 43 + Url.GetHashCode();
		}

		public override bool Equals(object? obj)
		{
			if (obj is Route route)
			{
				if ((AgencyId ?? string.Empty) == (route.AgencyId ?? string.Empty))
				{
					int? color = Color;
					int? color2 = route.Color;
					if (color.GetValueOrDefault() == color2.GetValueOrDefault() && color.HasValue == color2.HasValue && (Description ?? string.Empty) == (route.Description ?? string.Empty) && (RouteId ?? string.Empty) == (route.RouteId ?? string.Empty) && (LongName ?? string.Empty) == (route.LongName ?? string.Empty) && (ShortName ?? string.Empty) == (route.ShortName ?? string.Empty))
					{
						color2 = TextColor;
						color = route.TextColor;
						if (color2.GetValueOrDefault() == color.GetValueOrDefault() && color2.HasValue == color.HasValue && Type == route.Type)
						{
							return Url == route.Url;
						}
					}
				}

				return false;
			}

			return false;
		}
	}
}
