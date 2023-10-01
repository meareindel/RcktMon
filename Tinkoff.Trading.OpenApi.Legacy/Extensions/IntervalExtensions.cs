using System;
using Tinkoff.InvestApi.V1;
using CandleInterval = Tinkoff.Trading.OpenApi.Legacy.Models.CandleInterval;

namespace Tinkoff.Trading.OpenApi.Legacy.Extensions;

public static class IntervalExtensions
{
    public static CandleInterval ToLegacyInterval(this Tinkoff.InvestApi.V1.CandleInterval interval)
    {
        return interval switch
        {
            InvestApi.V1.CandleInterval._1Min => CandleInterval.Minute,
            InvestApi.V1.CandleInterval._5Min => CandleInterval.FiveMinutes,
            InvestApi.V1.CandleInterval._15Min => CandleInterval.QuarterHour,
            InvestApi.V1.CandleInterval.Hour => CandleInterval.Hour,
            InvestApi.V1.CandleInterval.Day => CandleInterval.Day,
            InvestApi.V1.CandleInterval._2Min => CandleInterval.TwoMinutes,
            InvestApi.V1.CandleInterval._3Min => CandleInterval.ThreeMinutes,
            InvestApi.V1.CandleInterval._10Min => CandleInterval.TenMinutes,
            InvestApi.V1.CandleInterval._30Min => CandleInterval.HalfHour,
            InvestApi.V1.CandleInterval.Week => CandleInterval.Week,
            InvestApi.V1.CandleInterval.Month => CandleInterval.Month,
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
        };
    }
    
    public static Tinkoff.InvestApi.V1.CandleInterval ToInterval(this CandleInterval interval)
    {
        return interval switch
        {
            CandleInterval.Minute => InvestApi.V1.CandleInterval._1Min,
            CandleInterval.FiveMinutes => InvestApi.V1.CandleInterval._5Min ,
            CandleInterval.QuarterHour => InvestApi.V1.CandleInterval._15Min,
            CandleInterval.Hour => InvestApi.V1.CandleInterval.Hour,
            CandleInterval.Day => InvestApi.V1.CandleInterval.Day,
            CandleInterval.TwoMinutes => InvestApi.V1.CandleInterval._2Min,
            CandleInterval.ThreeMinutes => InvestApi.V1.CandleInterval._3Min,
            CandleInterval.TenMinutes => InvestApi.V1.CandleInterval._10Min,
            CandleInterval.HalfHour => InvestApi.V1.CandleInterval._30Min,
            CandleInterval.Week => InvestApi.V1.CandleInterval.Week,
            CandleInterval.Month => InvestApi.V1.CandleInterval.Month,
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
        };
    }

    public static SubscriptionInterval ToSubscriptionInterval(this CandleInterval interval)
    {
        return interval switch
        {
            CandleInterval.Minute => SubscriptionInterval.OneMinute,
            CandleInterval.FiveMinutes => SubscriptionInterval.FiveMinutes,
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
        };
    }
    
    public static CandleInterval ToLegacyInterval(this SubscriptionInterval interval)
    {
        return interval switch
        {
            SubscriptionInterval.OneMinute => CandleInterval.Minute,
            SubscriptionInterval.FiveMinutes => CandleInterval.FiveMinutes,
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
        };
    }
}