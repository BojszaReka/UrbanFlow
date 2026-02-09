using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.db;

namespace Urbanflow.src.backend.models.gtfs
{
	[Table("Agencies")]
	public class Agency
	{
		// Database Fields
		public Guid Id { get; internal set; }
		public Guid GtfsFeedId { get; internal set; }

		// GTFS Fields
		public string Agency_Id { get; internal set; }
		public string Name { get; internal set; }
		public string URL { get; internal set; }
		public string Timezone { get; internal set; }
		public string LanguageCode { get; internal set; }
		public string Phone { get; internal set; }
		public string FareURL { get; internal set; }

		// Contructors
		public Agency(Guid id)
		{
			DatabaseContext db = new();
			var agency = db.Agencies?.Find(id);
			if (agency != null)
			{
				Id = agency.Id;
				GtfsFeedId = agency.GtfsFeedId;
				Agency_Id = agency.Agency_Id;
				Name = agency.Name;
				URL = agency.URL;
				Timezone = agency.Timezone;
				LanguageCode = agency.LanguageCode;
				Phone = agency.Phone;
				FareURL = agency.FareURL ?? "Unknown";
			}
			else
			{
				throw new InvalidOperationException($"Agency with id {id} not found.");
			}
		}

		public Agency(GTFS.Entities.Agency a, Guid id)
		{
			Id = Guid.NewGuid();
			GtfsFeedId = id;
			Agency_Id = a.Id;
			Name = a.Name;
			URL = a.URL;
			Timezone = a.Timezone;
			LanguageCode = a.LanguageCode;
			Phone = a.Phone;
			FareURL = a.FareURL ?? "Unknown";
		}

		// GTFS methods
		public GTFS.Entities.Agency Export()
		{
			return new GTFS.Entities.Agency
			{
				Id = Agency_Id,
				Name = Name,
				URL = URL,
				Timezone = Timezone,
				LanguageCode = LanguageCode,
				Phone = Phone,
				FareURL = FareURL == "Unknown" ? null : FareURL
			};
		}

		// Stolen methods
		public override string ToString()
		{
			return string.Format("[{0}] {1}", new object[2] { Id, Name });
		}

		public override int GetHashCode()
		{
			return ((((((83 * 89 + FareURL.GetHashCode()) * 89 + Id.GetHashCode()) * 89 + LanguageCode.GetHashCode()) * 89 + Name.GetHashCode()) * 89 + Phone.GetHashCode()) * 89 + Timezone.GetHashCode()) * 89 + URL.GetHashCode();
		}

		public override bool Equals(object? obj)
		{
			if (obj is Agency agency)
			{
				if ((FareURL ?? string.Empty) == (agency.FareURL ?? string.Empty) && (Agency_Id ?? string.Empty) == (agency.Agency_Id ?? string.Empty) && (LanguageCode ?? string.Empty) == (agency.LanguageCode ?? string.Empty) && (Name ?? string.Empty) == (agency.Name ?? string.Empty) && (Phone ?? string.Empty) == (agency.Phone ?? string.Empty) && (Timezone ?? string.Empty) == (agency.Timezone ?? string.Empty))
				{
					return (URL ?? string.Empty) == (agency.URL ?? string.Empty);
				}

				return false;
			}

			return false;
		}
	}
}
