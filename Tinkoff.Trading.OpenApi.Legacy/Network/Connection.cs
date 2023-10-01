using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using Tinkoff.Trading.OpenApi.Legacy.Extensions;
using Tinkoff.Trading.OpenApi.Legacy.Models;
using Account = Tinkoff.Trading.OpenApi.Legacy.Models.Account;
using CandleInterval = Tinkoff.Trading.OpenApi.Legacy.Models.CandleInterval;
using Currency = Tinkoff.Trading.OpenApi.Legacy.Models.Currency;
using Enum = System.Enum;
using InstrumentType = Tinkoff.Trading.OpenApi.Legacy.Models.InstrumentType;
using Operation = Tinkoff.Trading.OpenApi.Legacy.Models.Operation;
using Order = Tinkoff.Trading.OpenApi.Legacy.Models.Order;

namespace Tinkoff.Trading.OpenApi.Legacy.Network
{
    public class Connection : IConnection
    {
        private readonly InvestApiClient _investClient;
        private readonly CancellationTokenSource _streamProcessingTaskCts = new CancellationTokenSource();
        private readonly AsyncDuplexStreamingCall<MarketDataRequest, MarketDataResponse> _stream;
        private readonly ConcurrentBag<StreamingRequest> _requests = new ConcurrentBag<StreamingRequest>();
        private readonly ConcurrentDictionary<string, bool> _dayRequests = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// Событие, возникающее при получении сообщения от WebSocket-клиента.
        /// </summary>
        public event EventHandler<StreamingEventReceivedEventArgs> StreamingEventReceived;

        /// <summary>
        /// Событие, возникающее при ошибке WebSocket-клиента (например, при обрыве связи).
        /// </summary>
        public event EventHandler<WebSocketException> WebSocketException;

        /// <summary>
        /// Событие, возникающее при закрытии WebSocket соединения.
        /// </summary>
        public event EventHandler StreamingClosed;

        public Connection(string token) : this(token, false, false)
        {
            
        }
        
        public Connection(string token, bool isStreaming) : this(token, false, isStreaming)
        {
            
        }

        private static int eventsCounter = 0;
        
        protected Connection(string token, bool sandbox, bool isStreaming)
        {
            _investClient = InvestApiClientFactory.Create(token, sandbox);
            Task.Factory.StartNew(ProcessDayRequests(), TaskCreationOptions.LongRunning);
            if (isStreaming)
            {
                _stream = _investClient.MarketDataStream.MarketDataStream();
                Task.Factory.StartNew(ProcessStream(), TaskCreationOptions.LongRunning);
                Task.Factory.StartNew(ProcessRequests(), TaskCreationOptions.LongRunning);
            }
        }

        private Func<Task> ProcessDayRequests()
        {
            return async () =>
            {
                while (!_streamProcessingTaskCts.IsCancellationRequested)
                {
                    foreach (var entry in _dayRequests)
                    {
                        try
                        {
                            var dayCandles = await MarketCandlesAsync(entry.Key, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1), CandleInterval.Day);
                            if (dayCandles.Candles.Count > 0)
                            {
                                var dayCandle = dayCandles.Candles[0];
                                var dayCandleResponse = new CandleResponse(dayCandle, dayCandle.Time);
                                StreamingEventReceived?.Invoke(this, new StreamingEventReceivedEventArgs(dayCandleResponse));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(DateTime.Now + "- Ошибка при получении дневных свечей: " + ex.Message);
                        }

                        await Task.Delay(250);
                    }
                    await Task.Delay(250);
                }
            };
        }

        private Func<Task> ProcessRequests()
        {
            return async () =>
            {
                while (!_streamProcessingTaskCts.IsCancellationRequested)
                {
                    var requestsToSend = new List<StreamingRequest>();
                    while (_requests.TryTake(out var request))
                        requestsToSend.Add(request);

                    if (requestsToSend.Count > 0)
                    {
                        var marketDataRequests = requestsToSend.GroupBy(r => r.GetType().Name).Select(ConvertToMarketDataRequest);
                        foreach (var marketDataRequest in marketDataRequests)
                        {
                            try
                            {
                                await _stream.RequestStream.WriteAsync(marketDataRequest);
                            }
                            catch (RpcException ex)
                            {
                                if (ex.StatusCode == StatusCode.Cancelled)
                                {
                                    Console.WriteLine(DateTime.Now + " - Ошибка при записи в поток cancelled: " + ex.Message);
                                }
                                Console.WriteLine(DateTime.Now + " - Ошибка при записи в поток grpc: " + ex.Message);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(DateTime.Now + " - Ошибка при записи в поток: " + ex.Message);
                            }
                        }
                    }

                    await Task.Delay(1000);
                }
            };
        }

        private MarketDataRequest ConvertToMarketDataRequest(IGrouping<string, StreamingRequest> requests)
        {
            var result = new MarketDataRequest();
            switch (requests.Key)
            {
                case nameof(StreamingRequest.CandleSubscribeRequest):
                    result.SubscribeCandlesRequest = ToSubscribeCandlesRequest(requests.OfType<StreamingRequest.BaseCandleRequest>(), true);
                    break;
                case nameof(StreamingRequest.CandleUnsubscribeRequest):
                    result.SubscribeCandlesRequest = ToSubscribeCandlesRequest(requests.OfType<StreamingRequest.BaseCandleRequest>(), false);
                    break;
                case nameof(StreamingRequest.OrderbookSubscribeRequest):
                    result.SubscribeOrderBookRequest = ToSubscribeOrderbookRequest(requests.OfType<StreamingRequest.BaseOrderbookRequest>(), true);
                    break;
                case nameof(StreamingRequest.OrderbookUnsubscribeRequest):
                    result.SubscribeOrderBookRequest = ToSubscribeOrderbookRequest(requests.OfType<StreamingRequest.BaseOrderbookRequest>(), false);
                    break;
            }

            return result;
        }

        private Func<Task> ProcessStream()
        {
            return async () =>
            {
                var responseStream = _stream.ResponseStream;
                while (!_streamProcessingTaskCts.IsCancellationRequested)
                {
                    try
                    {
                        while (await responseStream.MoveNext())
                        {
                            var streamingResponse = ConvertToStreamingResponse(responseStream.Current);
                            if (streamingResponse != null)
                                StreamingEventReceived?.Invoke(this, new StreamingEventReceivedEventArgs(streamingResponse));

                            _streamProcessingTaskCts.Token.ThrowIfCancellationRequested();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(DateTime.Now + "- Ошибка при обработке сообщений из потока: " + ex.Message);                        
                    }
                }
            };
        }

        /// <summary>
        /// Получение брокерских счетов клиента.
        /// </summary>
        /// <returns>Список брокерских счетов.</returns>
        public async Task<IReadOnlyCollection<Account>> AccountsAsync()
        {
            var response = await _investClient.Users.GetAccountsAsync();
            return response.Accounts.Select(ConvertAccount).ToArray();
        }

        private Account ConvertAccount(InvestApi.V1.Account account)
        {
            var type = account.Type == AccountType.TinkoffIis ? BrokerAccountType.TinkoffIis : BrokerAccountType.Tinkoff;
            return new Account(type, account.Id);
        }

        /// <summary>
        /// Получение списка активных заявок.
        /// </summary>
        /// <param name="brokerAccountId">Номер счета (по умолчанию - Тинькофф).</param>
        /// <returns>Список заявок.</returns>
        public Task<List<Order>> OrdersAsync(string brokerAccountId = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Размещение лимитной заявки.
        /// </summary>
        /// <param name="limitOrder">Параметры отправляемой заявки.</param>
        /// <returns>Параметры размещённой заявки.</returns>
        public Task<PlacedLimitOrder> PlaceLimitOrderAsync(LimitOrder limitOrder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Создание рыночной заявки.
        /// </summary>
        /// <param name="marketOrder">Параметры отправляемой заявки.</param>
        /// <returns>Параметры размещённой заявки.</returns>
        public Task<PlacedMarketOrder> PlaceMarketOrderAsync(MarketOrder marketOrder)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Отзыв лимитной заявки.
        /// </summary>
        /// <param name="id">Идентификатор заявки.</param>
        /// <param name="brokerAccountId">Номер счета (по умолчанию - Тинькофф).</param>
        public Task CancelOrderAsync(string id, string brokerAccountId = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Получение информации по портфелю инструментов.
        /// </summary>
        /// <param name="brokerAccountId">Номер счета (по умолчанию - Тинькофф).</param>
        /// <returns>Портфель инструментов.</returns>
        public Task<Portfolio> PortfolioAsync(string brokerAccountId = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Получение информации по валютным активам.
        /// </summary>
        /// <param name="brokerAccountId">Номер счета (по умолчанию - Тинькофф).</param>
        /// <returns>Валютные активы.</returns>
        public Task<PortfolioCurrencies> PortfolioCurrenciesAsync(string brokerAccountId = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Получение списка акций, доступных для торговли.
        /// </summary>
        /// <returns>Список акций.</returns>
        public async Task<MarketInstrumentList> MarketStocksAsync()
        {
            var shares = await _investClient.Instruments.SharesAsync();
            var instruments = shares.Instruments.Select(ConvertShare).ToList();
            return new MarketInstrumentList(instruments.Count, instruments);
        }

        private MarketInstrument ConvertShare(Share share)
        {
            return new MarketInstrument(share.Figi, share.Ticker, share.Isin, share.MinPriceIncrement.ToDecimal(), share.Lot,
                Enum.Parse<Currency>(share.Currency, true), share.Name, InstrumentType.Stock);
        }

        /// <summary>
        /// Получение списка бондов, доступных для торговли.
        /// </summary>
        /// <returns>Список бондов.</returns>
        public Task<MarketInstrumentList> MarketBondsAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Получение списка фондов, доступных для торговли.
        /// </summary>
        /// <returns>Список фондов.</returns>
        public Task<MarketInstrumentList> MarketEtfsAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Получение списка валют, доступных для торговли.
        /// </summary>
        /// <returns>Список валют.</returns>
        public Task<MarketInstrumentList> MarketCurrenciesAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Поиск инструмента по FIGI.
        /// </summary>
        /// <param name="figi">FIGI.</param>
        /// <returns></returns>
        public Task<MarketInstrument> MarketSearchByFigiAsync(string figi)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Поиск инструмента по тикеру.
        /// </summary>
        /// <param name="ticker">Тикер.</param>
        /// <returns></returns>
        public Task<MarketInstrumentList> MarketSearchByTickerAsync(string ticker)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Получение исторических значений свечей по FIGI.
        /// </summary>
        /// <param name="figi">FIGI.</param>
        /// <param name="from">Начало временного промежутка.</param>
        /// <param name="to">Конец временного промежутка.</param>
        /// <param name="interval">Интервал свечи.</param>
        /// <returns>Значения свечей.</returns>
        public async Task<CandleList> MarketCandlesAsync(string figi, DateTime from, DateTime to, CandleInterval interval)
        {
            var request = new GetCandlesRequest
            {
                Interval = interval.ToInterval(),
                From = from.ToTimestamp(),
                To = to.ToTimestamp(),
                InstrumentId = figi
            };
            var response = await _investClient.MarketData.GetCandlesAsync(request);
            var candles = response.Candles.Select(candle => ConvertCandle(candle, interval, figi)).ToList();
            return new CandleList(figi, interval, candles);
        }

        private CandlePayload ConvertCandle(HistoricCandle candle, CandleInterval interval, string figi)
        {
            return new CandlePayload(candle.Open.ToDecimal(), candle.Close.ToDecimal(), candle.High.ToDecimal(), candle.Low.ToDecimal(), candle.Volume,
                candle.Time.ToDateTime(), interval, figi);
        }

        /// <summary>
        /// Получение стакана (книги заявок) по FIGI.
        /// </summary>
        /// <param name="figi">FIGI.</param>
        /// <param name="depth">Глубина стакана.</param>
        /// <returns>Книга заявок.</returns>
        public Task<Orderbook> MarketOrderbookAsync(string figi, int depth)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Получение списка операций.
        /// </summary>
        /// <param name="from">Начало временного промежутка.</param>
        /// <param name="to">Конец временного промежутка.</param>
        /// <param name="figi">FIGI инструмента для фильтрации.</param>
        /// <param name="brokerAccountId">Номер счета (по умолчанию - Тинькофф).</param>
        /// <returns>Список операций.</returns>
        public Task<List<Operation>> OperationsAsync(DateTime from, DateTime to, string figi, string brokerAccountId = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Получение списка операций.
        /// </summary>
        /// <param name="from">Начало временного промежутка.</param>
        /// <param name="interval">Длительность временного промежутка.</param>
        /// <param name="figi">FIGI инструмента для фильтрации.</param>
        /// <param name="brokerAccountId">Номер счета (по умолчанию - Тинькофф).</param>
        /// <returns>Список операций.</returns>
        public Task<List<Operation>> OperationsAsync(DateTime from, Interval interval, string figi, string brokerAccountId = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Посылает запрос по streaming-протоколу.
        /// </summary>
        /// <param name="request">Запрос.</param>
        public async Task SendStreamingRequestAsync(StreamingRequest request) 
        {
            if (request is StreamingRequest.BaseCandleRequest { Interval: CandleInterval.Day } candleRequest)
            {
                if (request is StreamingRequest.CandleSubscribeRequest)
                    _dayRequests[candleRequest.Figi] = true;
                else
                    _dayRequests.Remove(candleRequest.Figi, out _);

                return;
            }

            _requests.Add(request);
        }

        private MarketDataRequest ConvertToMarketDataRequest(StreamingRequest request)
        {
            var result = new MarketDataRequest();
            switch (request)
            {
                case StreamingRequest.BaseCandleRequest candleRequest:
                    result.SubscribeCandlesRequest = ToSubscribeCandlesRequest(candleRequest);
                    break;
                case StreamingRequest.BaseInstrumentInfoRequest instrumentInfoRequest:
                    result.SubscribeInfoRequest = ToSubscribeInfoRequest(instrumentInfoRequest);
                    break;
                case StreamingRequest.BaseOrderbookRequest orderbookRequest:
                    result.SubscribeOrderBookRequest = ToSubscribeOrderbookRequest(orderbookRequest);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request));
            }

            return result;
        }

        private SubscribeOrderBookRequest ToSubscribeOrderbookRequest(StreamingRequest.BaseOrderbookRequest orderbookRequest)
        {
            return ToSubscribeOrderbookRequest(new[] { orderbookRequest }, orderbookRequest is StreamingRequest.OrderbookSubscribeRequest);
        }

        private SubscribeOrderBookRequest ToSubscribeOrderbookRequest(IEnumerable<StreamingRequest.BaseOrderbookRequest> requests, bool subscribe)
        {
            return new SubscribeOrderBookRequest
            {
                Instruments =
                {
                    requests.Select(ToOrderBookInstrument).ToArray()
                },
                SubscriptionAction = subscribe ? SubscriptionAction.Subscribe : SubscriptionAction.Unsubscribe
            };
            
        }

        private static OrderBookInstrument ToOrderBookInstrument(StreamingRequest.BaseOrderbookRequest orderbookRequest)
        {
            var depth = orderbookRequest.Depth switch
            {
                < 1 => 1,
                > 1 and < 10 => 10,
                > 10 and < 20 => 20,
                > 20 and < 30 => 30,
                > 30 and < 40 => 40,
                > 40 => 50,
                _ => orderbookRequest.Depth
            };
            return new OrderBookInstrument
            {
                InstrumentId = orderbookRequest.Figi,
                Depth = depth
            };
        }

        private static SubscribeInfoRequest ToSubscribeInfoRequest(StreamingRequest.BaseInstrumentInfoRequest instrumentInfoRequest)
        {
            return new SubscribeInfoRequest
            {
                Instruments =
                {
                    new InfoInstrument { InstrumentId = instrumentInfoRequest.Figi }
                },
                SubscriptionAction = instrumentInfoRequest is StreamingRequest.InstrumentInfoSubscribeRequest
                    ? SubscriptionAction.Subscribe
                    : SubscriptionAction.Unsubscribe
            };
        }

        private static SubscribeCandlesRequest ToSubscribeCandlesRequest(StreamingRequest.BaseCandleRequest candleRequest)
        {
            return ToSubscribeCandlesRequest(new[] { candleRequest }, candleRequest is StreamingRequest.CandleSubscribeRequest);
        }
        
        private static SubscribeCandlesRequest ToSubscribeCandlesRequest(IEnumerable<StreamingRequest.BaseCandleRequest> candleRequests, bool subscribe)
        {
            return new SubscribeCandlesRequest
            {
                Instruments =
                {
                    candleRequests.Select(ToCandleInstrument).ToArray()
                },
                SubscriptionAction = subscribe ? SubscriptionAction.Subscribe : SubscriptionAction.Unsubscribe,
                WaitingClose = false,
            };
        }

        private static CandleInstrument ToCandleInstrument(StreamingRequest.BaseCandleRequest candleRequest)
        {
            return new CandleInstrument
            {
                Interval = candleRequest.Interval.ToSubscriptionInterval(),
                InstrumentId = candleRequest.Figi
            };
        }

        private StreamingResponse ConvertToStreamingResponse(MarketDataResponse response)
        {
            return response.PayloadCase switch
            {
                MarketDataResponse.PayloadOneofCase.Candle => ToCandleResponse(response),
                MarketDataResponse.PayloadOneofCase.Orderbook => ToOrderbookResponse(response),
                // TODO: instrument subscription
                _ => null
            };
        }

        private OrderbookResponse ToOrderbookResponse(MarketDataResponse response)
        {
            var orderbook = response.Orderbook;
            return new OrderbookResponse(new OrderbookPayload(orderbook.Depth, orderbook.Bids.Select(ToDecimals).ToList(),
                orderbook.Asks.Select(ToDecimals).ToList(), orderbook.Figi), orderbook.Time.ToDateTime());
        }

        private CandleResponse ToCandleResponse(MarketDataResponse response)
        {
            var candle = response.Candle;
            return new CandleResponse(
                new CandlePayload(candle.Open.ToDecimal(), candle.Close.ToDecimal(), candle.High.ToDecimal(), candle.Low.ToDecimal(), candle.Volume,
                    candle.Time.ToDateTime(), candle.Interval.ToLegacyInterval(), candle.Figi), candle.Time.ToDateTime());
        }

        private decimal[] ToDecimals(InvestApi.V1.Order order)
        {
            var result = new decimal[2];
            result[0] = order.Price.ToDecimal();
            result[1] = order.Quantity;
            return result;
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            _streamProcessingTaskCts.Cancel();
        }

        /// <summary>
        /// Регистрация в песочнице.
        /// </summary>
        /// <param name="brokerAccountType">
        /// Тип счета.
        /// </param>
        public Task<SandboxAccount> RegisterAsync(BrokerAccountType? brokerAccountType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Установка значения валютного актива.
        /// </summary>
        /// <param name="currency">Валюта.</param>
        /// <param name="balance">Желаемое значение.</param>
        /// <param name="brokerAccountId">Номер счета (по умолчанию - Тинькофф).</param>
        public Task SetCurrencyBalanceAsync(Currency currency, decimal balance, string brokerAccountId = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Установка позиции по инструменту.
        /// </summary>
        /// <param name="figi">FIGI.</param>
        /// <param name="balance">Желаемое значение.</param>
        /// <param name="brokerAccountId">Номер счета (по умолчанию - Тинькофф).</param>
        public Task SetPositionBalanceAsync(string figi, decimal balance, string brokerAccountId = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Удаление счета клиента.
        /// </summary>
        /// <param name="brokerAccountId">Номер счета (по умолчанию - Тинькофф).</param>
        public Task RemoveAsync(string brokerAccountId = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Сброс всех установленных значений по активам.
        /// </summary>
        /// <param name="brokerAccountId">Номер счета (по умолчанию - Тинькофф).</param>
        public Task ClearAsync(string brokerAccountId = null)
        {
            throw new NotImplementedException();
        }
    }
}
