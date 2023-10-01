using System;
using Tinkoff.InvestApi.V1;

namespace Tinkoff.Trading.OpenApi.Legacy.Extensions;

public static class QuotationExtensions
{
    public static decimal ToDecimal(this Quotation quotation)
    {
        var integral = quotation.Units;
        var fraction = quotation.Nano;
        var result = Convert.ToDecimal(integral);
        result += 1m * fraction / 1000000000;
        return result;
    }
}