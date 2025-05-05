using BambooCard.Domain.Abstractions;
using BambooCard.Domain.Abstractions.Base;
using BambooCard.Domain.DbContext;
using BambooCard.Domain.Entities.Main;

namespace BambooCard.Domain.Repositories;

public class RateRepository(BambooCardDbContext context) : Repository<Rate>(context), IRateRepository
{
}