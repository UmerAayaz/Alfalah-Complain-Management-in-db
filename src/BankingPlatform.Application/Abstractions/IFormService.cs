using BankingPlatform.Application.DTOs;

namespace BankingPlatform.Application.Abstractions;

public interface IFormService
{
    Task<IReadOnlyList<FormListItemDto>> ListAsync(
        bool includeAllVersions = false,
        CancellationToken ct = default);

    Task<FormDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<FormDto> CreateAsync(CreateFormRequest request, CancellationToken ct = default);
    Task<FormDto> UpdateAsync(Guid id, UpdateFormRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<FormDto> PublishAsync(Guid id, CancellationToken ct = default);
    Task<FormDto> CreateNewVersionAsync(Guid id, CancellationToken ct = default);
}