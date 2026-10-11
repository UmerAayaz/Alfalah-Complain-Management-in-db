using BankingPlatform.Application.DTOs;

namespace BankingPlatform.Application.Abstractions;

public interface IFormService
{
    Task<IReadOnlyList<FormListItemDto>> ListAsync(CancellationToken ct);

    Task<FormDto> GetAsync(Guid id, CancellationToken ct);

    Task<FormDto> CreateAsync(CreateFormRequest request, CancellationToken ct);

    Task<FormDto> UpdateAsync(Guid id, UpdateFormRequest request, CancellationToken ct);

    Task DeleteAsync(Guid id, CancellationToken ct);

    Task<FormDto> PublishAsync(Guid id, CancellationToken ct);

    Task<FormDto> CreateNewVersionAsync(Guid id, CancellationToken ct);
}