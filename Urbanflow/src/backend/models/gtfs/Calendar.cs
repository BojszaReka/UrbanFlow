using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Urbanflow.src.backend.db;

namespace Urbanflow.src.backend.models.gtfs
{
	[Table("Calendars")]
	public class Calendar
	{
		// Database fields
		public Guid GtfsFeedId { get; internal set; }
		[Key]
		public Guid Id { get; internal set; }

		// GTFS fields
		public string? ServiceId { get; set; }
		public byte Mask { get; set; }
		public bool Monday
		{
			get
			{
				return this[DayOfWeek.Monday];
			}
			set
			{
				this[DayOfWeek.Monday] = value;
			}
		}
		public bool Tuesday
		{
			get
			{
				return this[DayOfWeek.Tuesday];
			}
			set
			{
				this[DayOfWeek.Tuesday] = value;
			}
		}
		public bool Wednesday
		{
			get
			{
				return this[DayOfWeek.Wednesday];
			}
			set
			{
				this[DayOfWeek.Wednesday] = value;
			}
		}
		public bool Thursday
		{
			get
			{
				return this[DayOfWeek.Thursday];
			}
			set
			{
				this[DayOfWeek.Thursday] = value;
			}
		}
		public bool Friday
		{
			get
			{
				return this[DayOfWeek.Friday];
			}
			set
			{
				this[DayOfWeek.Friday] = value;
			}
		}
		public bool Saturday
		{
			get
			{
				return this[DayOfWeek.Saturday];
			}
			set
			{
				this[DayOfWeek.Saturday] = value;
			}
		}
		public bool Sunday
		{
			get
			{
				return this[DayOfWeek.Sunday];
			}
			set
			{
				this[DayOfWeek.Sunday] = value;
			}
		}
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }

		// Constructors
		public Calendar(Guid id)
		{
			Id = id;
			DatabaseContext db = new();
			Calendar? c = db.Calendars?.Find(id);
			if (c is not null)
			{
				GtfsFeedId = c.GtfsFeedId;
				ServiceId = c.ServiceId;
				Mask = c.Mask;
				StartDate = c.StartDate;
				EndDate = c.EndDate;
			}
			else
			{
				throw new InvalidOperationException($"Calendar with id {id} not found.");
			}
		}

		public Calendar(GTFS.Entities.Calendar c, Guid id)
		{
			GtfsFeedId = id;
			Id = Guid.NewGuid();
			ServiceId = c.ServiceId;
			Mask = c.Mask;
			StartDate = c.StartDate;
			EndDate = c.EndDate;
		}

		public GTFS.Entities.Calendar Export()
		{
			return new GTFS.Entities.Calendar
			{
				ServiceId = ServiceId,
				Mask = Mask,
				StartDate = StartDate,
				EndDate = EndDate
			};
		}


		// Stolen Methods
		public bool this[DayOfWeek dayOfWeek]
		{
			get
			{
				return dayOfWeek switch
				{
					DayOfWeek.Monday => (Mask & 1) > 0,
					DayOfWeek.Tuesday => (Mask & 2) > 0,
					DayOfWeek.Wednesday => (Mask & 4) > 0,
					DayOfWeek.Thursday => (Mask & 8) > 0,
					DayOfWeek.Friday => (Mask & 0x10) > 0,
					DayOfWeek.Saturday => (Mask & 0x20) > 0,
					DayOfWeek.Sunday => (Mask & 0x40) > 0,
					_ => throw new ArgumentOutOfRangeException("Not a valid day of the week."),
				};
			}

			set
			{
				if (value)
				{
					switch (dayOfWeek)
					{
						case DayOfWeek.Monday:
							Mask |= 1;
							break;
						case DayOfWeek.Tuesday:
							Mask |= 2;
							break;
						case DayOfWeek.Wednesday:
							Mask |= 4;
							break;
						case DayOfWeek.Thursday:
							Mask |= 8;
							break;
						case DayOfWeek.Friday:
							Mask |= 16;
							break;
						case DayOfWeek.Saturday:
							Mask |= 32;
							break;
						case DayOfWeek.Sunday:
							Mask |= 64;
							break;
					}
				}
				else
				{
					switch (dayOfWeek)
					{
						case DayOfWeek.Monday:
							Mask &= 126;
							break;
						case DayOfWeek.Tuesday:
							Mask &= 125;
							break;
						case DayOfWeek.Wednesday:
							Mask &= 123;
							break;
						case DayOfWeek.Thursday:
							Mask &= 119;
							break;
						case DayOfWeek.Friday:
							Mask &= 111;
							break;
						case DayOfWeek.Saturday:
							Mask &= 95;
							break;
						case DayOfWeek.Sunday:
							Mask &= 63;
							break;
					}
				}
			}
		}

		public override string ToString()
		{
			return string.Format("[{0}] mon-sun {1}:{2}:{3}:{4}:{5}:{6}:{7}", ServiceId, Monday ? "1" : "0", Tuesday ? "1" : "0", Wednesday ? "1" : "0", Thursday ? "1" : "0", Friday ? "1" : "0", Saturday ? "1" : "0", Sunday ? "1" : "0");
		}

		public override int GetHashCode()
		{
			return (((17 * 23 + EndDate.GetHashCode()) * 23 + Mask.GetHashCode()) * 23 + (ServiceId ?? string.Empty).GetHashCode()) * 23 + StartDate.GetHashCode();
		}

		public override bool Equals(object? obj)
		{
			if (obj is Calendar calendar)
			{
				if (EndDate == calendar.EndDate && StartDate == calendar.StartDate && Mask == calendar.Mask)
				{
					return (ServiceId ?? string.Empty) == (calendar.ServiceId ?? string.Empty);
				}

				return false;
			}

			return false;
		}
	}
}
