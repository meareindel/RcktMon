using System.Text.Json.Serialization;

namespace Tinkoff.Trading.OpenApi.Legacy.Models
{
    public abstract class StreamingRequest
    {
        [JsonPropertyName("event")]
        public abstract string Event { get; }

        [JsonPropertyName("request_id")]
        public string RequestId { get; }

        protected StreamingRequest(string requestId)
        {
            RequestId = requestId;
        }

        public static CandleSubscribeRequest SubscribeCandle(string figi, CandleInterval interval)
        {
            return new CandleSubscribeRequest(figi, interval);
        }

        public static CandleUnsubscribeRequest UnsubscribeCandle(string figi, CandleInterval interval)
        {
            return new CandleUnsubscribeRequest(figi, interval);
        }

        public static OrderbookSubscribeRequest SubscribeOrderbook(string figi, int depth)
        {
            return new OrderbookSubscribeRequest(figi, depth);
        }

        public static OrderbookUnsubscribeRequest UnsubscribeOrderbook(string figi, int depth)
        {
            return new OrderbookUnsubscribeRequest(figi, depth);
        }

        public static InstrumentInfoSubscribeRequest SubscribeInstrumentInfo(string figi)
        {
            return new InstrumentInfoSubscribeRequest(figi);
        }

        public static InstrumentInfoUnsubscribeRequest UnsubscribeInstrumentInfo(string figi)
        {
            return new InstrumentInfoUnsubscribeRequest(figi);
        }

        public abstract class BaseCandleRequest : StreamingRequest
        {
            public override string Event => "candle:subscribe";

            [JsonPropertyName("figi")]
            public string Figi { get; }

            [JsonPropertyName("interval")]
            public CandleInterval Interval { get; }

            public BaseCandleRequest(string figi, CandleInterval interval, string requestId = null)
                : base(requestId)
            {
                Figi = figi;
                Interval = interval;
            }
            
        }
        
        public class CandleSubscribeRequest : BaseCandleRequest
        {
            public CandleSubscribeRequest(string figi, CandleInterval interval, string requestId = null) : base(figi, interval, requestId)
            {
            }
        }

        public class CandleUnsubscribeRequest : BaseCandleRequest
        {
            public CandleUnsubscribeRequest(string figi, CandleInterval interval, string requestId = null) : base(figi, interval, requestId)
            {
            }
        }

        public abstract class BaseOrderbookRequest : StreamingRequest
        {
            public override string Event => "orderbook:subscribe";

            [JsonPropertyName("figi")]
            public string Figi { get; }

            [JsonPropertyName("depth")]
            public int Depth { get; }

            public BaseOrderbookRequest(string figi, int depth, string requestId = null)
                : base(requestId)
            {
                Figi = figi;
                Depth = depth;
            }
        }
        
        public class OrderbookSubscribeRequest : BaseOrderbookRequest
        {
            public OrderbookSubscribeRequest(string figi, int depth, string requestId = null) : base(figi, depth, requestId)
            {
            }
        }

        public class OrderbookUnsubscribeRequest : BaseOrderbookRequest
        {
            public OrderbookUnsubscribeRequest(string figi, int depth, string requestId = null) : base(figi, depth, requestId)
            {
            }
        }

        public abstract class BaseInstrumentInfoRequest : StreamingRequest
        {
            public override string Event => "instrument_info:subscribe";

            [JsonPropertyName("figi")]
            public string Figi { get; }

            public BaseInstrumentInfoRequest(string figi, string requestId = null)
                : base(requestId)
            {
                Figi = figi;
            }
        }
        
        public class InstrumentInfoSubscribeRequest : BaseInstrumentInfoRequest
        {
            public InstrumentInfoSubscribeRequest(string figi, string requestId = null) : base(figi, requestId)
            {
            }
        }

        public class InstrumentInfoUnsubscribeRequest : BaseInstrumentInfoRequest
        {
            public InstrumentInfoUnsubscribeRequest(string figi, string requestId = null) : base(figi, requestId)
            {
            }
        }
    }
}