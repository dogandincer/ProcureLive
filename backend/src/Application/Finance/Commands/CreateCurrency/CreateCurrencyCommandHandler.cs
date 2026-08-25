using MediatR;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Finance;

namespace ProcureLive.Application.Finance.Commands.CreateCurrency;

public sealed class CreateCurrencyCommandHandler : IRequestHandler<CreateCurrencyCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateCurrencyCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateCurrencyCommand request, CancellationToken cancellationToken)
    {
        var currency = new Currency
        {
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            Symbol = request.Symbol,
            IsBaseCurrency = request.IsBaseCurrency
        };

        _context.Currencies.Add(currency);
        await _context.SaveChangesAsync(cancellationToken);

        return currency.Id;
    }
}
