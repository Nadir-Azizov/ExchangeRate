using BambooCard.Infrastructure.Results;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace BambooCard.Business.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Paginates and projects the result set into TDto using Mapster.
    /// </summary>
    public static async Task<PaginationResult<TDto>> ToPagedResultAsync<TEntity, TDto>(
        this IQueryable<TEntity> query,
        int pageNumber,
        int pageSize)
    {
        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = data.Select(e => e.Adapt<TDto>());

        var totalRecords = await query.CountAsync();

        return new PaginationResult<TDto>(
            dtos,
            totalRecords,
            pageNumber,
            pageSize
        );
    }
}
