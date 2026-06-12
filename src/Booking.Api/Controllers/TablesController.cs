using Booking.Api.Contracts.Tables;
using Booking.Application.Tables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

[Route("api/admin/restaurant/tables")]
[Authorize(Policy = "RestaurantOwner")]
public sealed class TablesController(
    CreateTableUseCase createTableUseCase,
    GetTablesUseCase getTablesUseCase,
    UpdateTableUseCase updateTableUseCase,
    DeactivateTableUseCase deactivateTableUseCase,
    ReactivateTableUseCase reactivateTableUseCase,
    UpdateTableLayoutUseCase updateTableLayoutUseCase) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TableApiResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TableApiResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            var tables = await getTablesUseCase.ExecuteAsync(restaurantId, cancellationToken);
            return Ok(tables.Select(ToApiResponse).ToList());
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(TableApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TableApiResponse>> Create(
        [FromBody] CreateTableApiRequest? request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateCreateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(ToError(validationError));
        }

        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            var table = await createTableUseCase.ExecuteAsync(
                new CreateTableRequest(restaurantId, request!.Name!, request.Capacity!.Value, request.Section),
                cancellationToken);

            return CreatedAtAction(nameof(GetAll), ToApiResponse(table));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPut("{tableId:guid}")]
    [ProducesResponseType(typeof(TableApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TableApiResponse>> Update(
        Guid tableId,
        [FromBody] UpdateTableApiRequest? request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateUpdateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(ToError(validationError));
        }

        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            var table = await updateTableUseCase.ExecuteAsync(
                new UpdateTableRequest(restaurantId, tableId, request!.Name!, request.Capacity!.Value, request.Section),
                cancellationToken);

            return Ok(ToApiResponse(table));
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpDelete("{tableId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        Guid tableId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            await deactivateTableUseCase.ExecuteAsync(restaurantId, tableId, cancellationToken);
            return NoContent();
        }
        catch (Exception exception) when (exception is KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPost("{tableId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(
        Guid tableId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            await reactivateTableUseCase.ExecuteAsync(restaurantId, tableId, cancellationToken);
            return NoContent();
        }
        catch (Exception exception) when (exception is KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    [HttpPatch("{tableId:guid}/layout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Contracts.Common.ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLayout(
        Guid tableId,
        [FromBody] UpdateTableLayoutApiRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(ToError("Request body is required."));
        }

        if (!TryGetCurrentRestaurantId(out var restaurantId))
        {
            return Forbid();
        }

        try
        {
            await updateTableLayoutUseCase.ExecuteAsync(
                new UpdateTableLayoutRequest(restaurantId, tableId, request.PosX, request.PosY, request.Rotation, request.Shape),
                cancellationToken);

            return NoContent();
        }
        catch (Exception exception) when (exception is KeyNotFoundException)
        {
            return HandleKnownException(exception);
        }
    }

    private static string? ValidateCreateRequest(CreateTableApiRequest? request)
    {
        if (request is null) return "Request body is required.";
        if (string.IsNullOrWhiteSpace(request.Name)) return "Tafelnaam is verplicht.";
        if (request.Capacity is null or <= 0) return "Capaciteit moet minimaal 1 zijn.";
        return null;
    }

    private static string? ValidateUpdateRequest(UpdateTableApiRequest? request)
    {
        if (request is null) return "Request body is required.";
        if (string.IsNullOrWhiteSpace(request.Name)) return "Tafelnaam is verplicht.";
        if (request.Capacity is null or <= 0) return "Capaciteit moet minimaal 1 zijn.";
        return null;
    }

    private static TableApiResponse ToApiResponse(Application.Tables.TableResponse response) =>
        new(response.Id, response.Name, response.Capacity, response.Section, response.IsActive,
            response.PosX, response.PosY, response.Rotation, response.Shape);
}
