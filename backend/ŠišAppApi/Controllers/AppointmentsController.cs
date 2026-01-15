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

             // Basic Validation: Check if time slot is taken for this barber
             var isTaken = await _context.Appointments.AnyAsync(a => 
                a.BarberId == appointment.BarberId && 
                a.AppointmentDateTime == appointment.AppointmentDateTime && // Simplified check
                a.Status != "Cancelled");

             if (isTaken)
             {
                 return BadRequest("Termin je već zauzet.");
             }

             _context.Appointments.Add(appointment);
             await _context.SaveChangesAsync();

             return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
        }

        // Add Cancel, Complete, etc.
    }
}
