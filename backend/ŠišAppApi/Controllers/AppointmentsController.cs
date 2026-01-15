using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ŠišAppApi.Data;
using ŠišAppApi.Models;

namespace ŠišAppApi.Controllers
{
    [Authorize]
    public class AppointmentsController : BaseController<Appointment>
    {
        public AppointmentsController(ApplicationDbContext context) : base(context)
        {
        }

        [HttpGet]
        public override async Task<ActionResult<IEnumerable<Appointment>>> GetAll()
        {
            var userId = GetUserId();
            var role = GetUserRole();

            IQueryable<Appointment> query = _context.Appointments
                .Include(a => a.Salon)
                .Include(a => a.Service)
                .Include(a => a.Barber)
                .Include(a => a.User);

            if (role == "User")
            {
                query = query.Where(a => a.UserId == userId);
            }
            else if (role == "Barber")
            {
                 // Assuming Barber is linked to a specific User or we have BarberId
                 var barber = await _context.Barbers.FirstOrDefaultAsync(b => b.UserId == userId);
                 if (barber != null)
                 {
                     query = query.Where(a => a.BarberId == barber.Id);
                 }
            }
            // Admin sees all

            return await query.OrderByDescending(a => a.AppointmentDateTime).ToListAsync();
        }

        [HttpGet("{id}")]
        public override async Task<ActionResult<Appointment>> GetById(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Salon)
                .Include(a => a.Service)
                .Include(a => a.Barber)
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            var userId = GetUserId();
            var role = GetUserRole();

            if (role == "User" && appointment.UserId != userId)
            {
                return Forbid();
            }

            return appointment;
        }

        [HttpPost]
        public override async Task<ActionResult<Appointment>> Create(Appointment appointment)
        {
             var userId = GetUserId();
             appointment.UserId = userId; // Force current user
             appointment.CreatedAt = DateTime.UtcNow;
             appointment.Status = "Pending";
             appointment.PaymentStatus = "Pending";

             // Basic Validation: Check for overlap
             var newAppStart = appointment.AppointmentDateTime;
             var newAppEnd = newAppStart.AddMinutes(30); // Todo: Fetch service duration

             var isTaken = await _context.Appointments.AnyAsync(a => 
                a.BarberId == appointment.BarberId && 
                a.Status != "Cancelled" &&
                a.AppointmentDateTime < newAppEnd && 
                a.AppointmentDateTime.AddMinutes(30) > newAppStart // Assuming 30 min duration for existing
             );

             if (isTaken)
             {
                 return BadRequest("Termin je već zauzet.");
             }

             _context.Appointments.Add(appointment);
             await _context.SaveChangesAsync();

             return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
        }

        // Add Cancel, Complete, etc.
        // GET: api/Appointments/available-slots
        [HttpGet("available-slots")]
        [AllowAnonymous] // Allow anyone to check availability? Or just auth users. Let's keep Authorize on class level but maybe allow this one?
        // Actually, frontend will be auth'd mostly. Mobile app will be auth'd.
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableSlots(int barberId, DateOnly date, int? serviceId = null)
        {
            // 1. Get Service Duration
            int durationMinutes = 30; // Default
            if (serviceId.HasValue)
            {
               // TODO: Fetch service duration from DB. For now hardcode or assume 30.
               // var service = await _context.Services.FindAsync(serviceId);
               // if (service != null) durationMinutes = service.Duration;
            }

            // 2. Get Working Hours for that day
            var dayOfWeek = (int)date.DayOfWeek;
            var workingHours = await _context.WorkingHours
                .FirstOrDefaultAsync(w => w.BarberId == barberId && w.DayOfWeek == dayOfWeek && w.IsWorking);

            if (workingHours == null)
            {
                return Ok(new List<string>()); // Not working today
            }

            // 3. Get existing appointments
            var startDateTime = date.ToDateTime(TimeOnly.MinValue);
            var endDateTime = date.ToDateTime(TimeOnly.MaxValue);
            
            var existingAppointments = await _context.Appointments
                .Where(a => a.BarberId == barberId && 
                            a.AppointmentDateTime >= startDateTime && 
                            a.AppointmentDateTime <= endDateTime &&
                            a.Status != "Cancelled")
                .ToListAsync();

            // 4. Generate slots
            var availableSlots = new List<string>();
            var currentTime = workingHours.StartTime;
            var endTime = workingHours.EndTime;

            // Prevent booking in the past if date is today
            var now = DateTime.Now;
            var isToday = date == DateOnly.FromDateTime(now);
            var currentTimeNow = TimeOnly.FromDateTime(now);

            while (currentTime.Add(TimeSpan.FromMinutes(durationMinutes)) <= endTime)
            {
                // Slot time as DateTime for comparison
                var slotStart = date.ToDateTime(TimeOnly.FromTimeSpan(currentTime));
                var slotEnd = slotStart.AddMinutes(durationMinutes);

                // Check if slot is in the past
                if (isToday && TimeOnly.FromTimeSpan(currentTime) <= currentTimeNow) 
                {
                     currentTime = currentTime.Add(TimeSpan.FromMinutes(30)); // Step 30 mins
                     continue;
                }

                // Check overlap
                bool isOverlap = existingAppointments.Any(a => 
                {
                    var appStart = a.AppointmentDateTime;
                    var appEnd = appStart.AddMinutes(30); // Assuming existing apps are also 30 mins for now
                    // TODO: Store Duration in Appointment or fetch from Service
                    
                    return (slotStart < appEnd && slotEnd > appStart);
                });

                if (!isOverlap)
                {
                    availableSlots.Add(currentTime.ToString(@"hh\:mm"));
                }

                currentTime = currentTime.Add(TimeSpan.FromMinutes(30)); // Always step by 30 mins for slot offering
            }

            return Ok(availableSlots);
        }
    }
}
