using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Models;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;
using ŠišAppApi.Services;
using ŠišAppApi.Filters;
using ŠišAppApi.Services.Interfaces;

using ŠišAppApi.Models.DTOs;

namespace ŠišAppApi.Controllers
{
    [Authorize]
    public class AppointmentsController : BaseCRUDController<AppointmentDto, AppointmentSearchObject, AppointmentInsertRequest, AppointmentUpdateRequest>
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService service, ICurrentUserService currentUser) : base(service, currentUser)
        {
            _appointmentService = service;
        }

        [HttpGet]
        public override async Task<ActionResult<IEnumerable<AppointmentDto>>> Get([FromQuery] AppointmentSearchObject search)
        {

            search.CurrentUserId = GetUserId();
            search.CurrentUserRole = GetUserRole();

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
                return BadRequest(new
                {
                    code = "BUSINESS_RULE_VIOLATION",
                    userError = ex.Message
                });
            }
        }

        [HttpGet("available-slots")]
        public async Task<ActionResult<IEnumerable<string>>> GetAvailableSlots(int barberId, DateOnly date, int? serviceId = null)
        {
            var slots = await _appointmentService.GetAvailableSlots(barberId, date, serviceId);
            return Ok(slots);
        }

        [HttpPut("{id}/cancel")]
    public async Task<ActionResult<AppointmentDto>> Cancel(int id, [FromBody] AppointmentCancelRequest? request = null)
        {
            var result = await _appointmentService.Cancel(id, GetUserId(), GetUserRole(), request?.Reason);
            return Ok(result);
        }
    }
}
