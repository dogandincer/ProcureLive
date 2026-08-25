using MediatR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Finance;

namespace ProcureLive.Application.Finance.Commands.CreateExchangeRate;

public sealed class CreateExchangeRateCommandHandler : IRequestHandler<CreateExchangeRateCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateExchangeRateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateExchangeRateCommand request, CancellationToken cancellationToken)
    {
        var exchangeRate = new ExchangeRate
        {
            CurrencyId = request.CurrencyId,
            RateDate = request.RateDate,
            RateToBase = request.RateToBase,
            Source = request.Source
        };

        _context.ExchangeRates.Add(exchangeRate);
        await _context.SaveChangesAsync(cancellationToken);

        return exchangeRate.Id;
    }
}
