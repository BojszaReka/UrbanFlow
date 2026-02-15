using GTFS.Entities.Enumerations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.db;

namespace Urbanflow.src.backend.models.gtfs
{
	[Table("CalendarDates")]
	public class CalendarDate
	{
		// Database fields
		[Key]
		public Guid Id { get; internal set; }
		public Guid GtfsFeedId { get; internal set; }

		// GTFS fields
		public string ServiceId { get; internal set; }
		public DateTime Date { get; internal set; }
		public ExceptionType ExceptionType { get; internal set; }
		[ForeignKey("GtfsFeedId")]
		public GtfsFeed GtfsFeed { get; internal set; }

		// Constructors
		public CalendarDate() { }
		public CalendarDate(GTFS.Entities.CalendarDate cd, Guid id)
		{
			using var db = new DatabaseContext();
			Id = Guid.NewGuid();
			GtfsFeedId = id;
			ServiceId = cd.ServiceId;
			Date = cd.Date;
			ExceptionType = cd.ExceptionType;
			db.CalendarDates.Add(this);
			db.SaveChanges();
		}

		public CalendarDate(Guid id)
		{
			using var db = new DatabaseContext();
			CalendarDate? cd = db.CalendarDates?.Find(id);
			if (cd != null)
			{
				Id = cd.Id;
				GtfsFeedId = cd.GtfsFeedId;
				ServiceId = cd.ServiceId;
				Date = cd.Date;
				ExceptionType = cd.ExceptionType;
			}
			else
				throw new ArgumentException("No CalendarDate with the given ID exists in the database.");
		}

		// GTFS methods
		public GTFS.Entities.CalendarDate Export()
		{
			return new GTFS.Entities.CalendarDate
			{
				ServiceId = this.ServiceId,
				Date = this.Date,
				ExceptionType = this.ExceptionType
			};
		}

		// Stolen methods
		public override string ToString()
		{
			return string.Format("[{0}] {1} {2}", new object[3]
			{
			ServiceId,
			Date,
			ExceptionType.ToString()
			});
		}

		public override int GetHashCode()
		{
			return ((29 * 31 + Date.GetHashCode()) * 31 + ExceptionType.GetHashCode()) * 31 + (ServiceId ?? string.Empty).GetHashCode();
		}

		public override bool Equals(object? obj)
		{
			if (obj is CalendarDate calendarDate)
			{
				if (Date == calendarDate.Date && ExceptionType == calendarDate.ExceptionType)
				{
					return (ServiceId ?? string.Empty) == (calendarDate.ServiceId ?? string.Empty);
				}

				return false;
			}

			return false;
		}
	}
}
