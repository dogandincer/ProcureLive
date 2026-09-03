using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
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
        // CurrencyNormalizer treats any currency with IsBaseCurrency=true as already-normalized (returns
        // the raw amount unchanged). A second base currency would silently make cross-currency comparisons
        // wrong with no exception anywhere - so this must stay a hard single-base-currency invariant.
        if (request.IsBaseCurrency)
        {
            bool baseCurrencyExists = await _context.Currencies.AnyAsync(c => c.IsBaseCurrency, cancellationToken);

            if (baseCurrencyExists)
            {
                throw new ConflictException(
                    "Sistemde zaten bir baz para birimi tanımlı. Aynı anda yalnızca bir baz para birimi olabilir.");
            }
        }

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
