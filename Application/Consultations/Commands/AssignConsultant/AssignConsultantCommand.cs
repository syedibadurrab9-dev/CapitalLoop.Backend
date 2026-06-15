using System;
using MediatR;

namespace Application.Consultations.Commands.AssignConsultant;

/// <summary>
/// CQRS Command used within the Angular back-office support console to map a lead context to a specialist [CL-502].
/// </summary>
/// <param name="ConsultationId">The unique structural aggregate identifier tracking the calendar booking node.</param>
/// <param name="AgentEmail">The authenticated specialist email identity coordinates claiming this consultation loop.</param>
public record AssignConsultantCommand(Guid ConsultationId, string AgentEmail) : IRequest<bool>;