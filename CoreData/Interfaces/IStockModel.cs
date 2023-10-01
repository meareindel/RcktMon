using System;
using System.Collections.Generic;
using Tinkoff.Trading.OpenApi.Legacy.Models;

namespace CoreData.Interfaces
{
    public interface IStockModel
    { 
         string Figi { get; set; }
         string Isin { get; set; }
         string Name { get; set; }
         string Ticker { get; set; }
         string Currency { get; set; }
         int Lot { get; set; }
         decimal LimitDown { get; set; }
         decimal LimitUp { get; set; }
         decimal MinPriceIncrement { get; set; }
         bool IsDead { get; set; }
         bool CanBeShorted { get; set; }
         string Exchange { get;set; }
         DateTime TodayDate { get; set; }
         decimal TodayOpen { get; set; }
         decimal Price { get; set; }
         decimal BestBidSpb { get; set; }
         decimal BestAskSpb { get; set; }
         decimal DayChange { get; set; }
         decimal DayVolume { get; set; }
         decimal DayVolumeCost { get; }
         decimal AvgPrice { get; }
         string Status { get; set; }
         DateTime LastUpdateOrderbook { get; set; }
         DateTime LastUpdatePrice { get; set; }
         DateTime LastResubscribeAttempt { get; set; }
         DateTime? LastAboveThresholdDate { get; set; }
         DateTime? LastAboveThresholdCandleTime { get; set; }
         DateTime? LastAboveVolThresholdCandleTime { get; set; }

         decimal PriceUSA { get; set; }
         decimal BidUSA { get; set; }
         decimal AskUSA { get; set; }
         decimal BidSizeUSA { get; set; }
         decimal AskSizeUSA { get; set; }
         DateTime? LastTradeUSA { get; set; }
         DateTime? LastUpdateUSA { get; set; }
         decimal DiffPercentUSA { get; set; }
         decimal USBidRUAskDiff { get; set; }
         decimal RUBidUSAskDiff { get; set; }

         decimal MonthOpen { get; set; }
         decimal MonthLow { get; set; }
         decimal MonthHigh { get; set; }
         decimal MonthAvg { get; }
         decimal MonthVolume { get; set; }
         decimal MonthVolumeCost { get; set; }
         decimal AvgDayVolumePerMonth { get; set; }
         decimal AvgDayPricePerMonth { get; set; }
         decimal AvgDayVolumePerMonthCost { get; set; }
         decimal YesterdayVolume { get; set; }
         decimal YesterdayVolumeCost { get; set; }
         decimal YesterdayAvgPrice { get; set; }

         string TodayOpenF { get; }
         string PriceF { get; }
         string AvgPriceF { get; }
         string YesterdayVolumeCostF { get; }
         string YesterdayAvgPriceF { get; }
         string MonthVolumeCostF { get; }
         string DayChangeF { get; }
         string DayVolumeCostF { get; }
         string AvgDayVolumePerMonthCostF { get; }
         string AvgDayPricePerMonthF { get; }
         string AvgMonthPriceF { get; }
         decimal? DayVolChgOfAvg { get; set; }
         decimal MonthChange { get; set; }
         decimal YearChange { get; set; }

         
         decimal PriceAcc { get; set; }
         decimal PriceAccAvg { get; set; }
         decimal HourMinChange { get; set; }
         decimal HourChange { get; set; }
         decimal HourMaxChange { get; set; }
         decimal M5MinChange { get; set; }
         decimal M5Change { get; set; }
         decimal M5MaxChange { get; set; }

         int TicksPerMinute { get; set; }
         decimal LotPrice { get; }
         
         DateTime LastMonthDataUpdate { get; set; }
         bool MonthStatsExpired { get; }

         void LogCandle(CandlePayload candle);

         IEnumerable<ICandleModel> Candles { get; }

         IDictionary<DateTime, CandlePayload> MinuteCandles { get; }
         
         LinkedList<decimal> AccTicks { get; }

         void AddCandle(CandlePayload candle);
    }
}
