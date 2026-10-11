using BankingPlatform.Application.DTOs;

namespace BankingPlatform.Application.Abstractions;

public interface ILibraryFieldService
{
    Task<IReadOnlyList<LibraryFieldDto>> ListAsync(CancellationToken ct);

    Task<LibraryFieldDto> GetAsync(Guid id, CancellationToken ct);

    Task<LibraryFieldDto> CreateAsync(
        CreateLibraryFieldRequest request,
        CancellationToken ct);

    Task<LibraryFieldDto> UpdateAsync(
        Guid id,
        UpdateLibraryFieldRequest request,
        CancellationToken ct);

    Task DeleteAsync(Guid id, CancellationToken ct);
}