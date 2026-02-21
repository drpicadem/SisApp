using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Models;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;
using ŠišAppApi.Services;
using ŠišAppApi.Filters;

using ŠišAppApi.Models.DTOs;

namespace ŠišAppApi.Controllers
{
    [Authorize]
    public class AppointmentsController : BaseCRUDController<AppointmentDto, AppointmentSearchObject, AppointmentInsertRequest, AppointmentUpdateRequest>
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService service) : base(service)
        {
            _appointmentService = service;
        }

        [HttpGet]
        public override async Task<ActionResult<IEnumerable<AppointmentDto>>> Get([FromQuery] AppointmentSearchObject search)
        {
            // Inject Security Context
            search.CurrentUserId = GetUserId();
            search.CurrentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            Console.WriteLine($"[RBAC DEBUG] GetAppointments - UserID: {search.CurrentUserId}, Role: {search.CurrentUserRole}");
            Console.WriteLine($"[SEARCH DEBUG] Filters - From: {search.FromDate}, To: {search.ToDate}, Status: {search.Status}, UserId: {search.UserId}");

            return await base.Get(search);
        }

        [HttpPost]
        public override async Task<ActionResult<AppointmentDto>> Insert([FromBody] AppointmentInsertRequest request)
        {
            var userId = GetUserId();
            try
            {
                var result = await _appointmentService.Insert(request, userId);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (UserException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("available-slots")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableSlots(int barberId, DateOnly date, int? serviceId = null)
        {
            var slots = await _appointmentService.GetAvailableSlots(barberId, date, serviceId);
            return Ok(slots);
        }

        [HttpPut("{id}/cancel")]
        public async Task<ActionResult<AppointmentDto>> Cancel(int id)
        {
            try
            {
                var result = await _appointmentService.Cancel(id, GetUserId());
                return Ok(result);
            }
            catch (UserException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
