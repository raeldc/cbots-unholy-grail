using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using System.Collections.Generic;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.SingaporeStandardTime, AccessRights = AccessRights.None)]
    public class UnholyGrail : Robot
    {
        [Parameter("How Many Waves?", DefaultValue = 4)]
        public int Waves { get; set; }

        [Parameter("Wave Pip Interval", DefaultValue = 20)]
        public int WaveInterval { get; set; }

        [Parameter("Protective SL Add Pips", DefaultValue = 2)]
        public int PSLAddPips { get; set; }

        [Parameter("Volume", DefaultValue = 1000)]
        public int Volume { get; set; }

        protected override void OnStart()
        {
            CancelPendingOrders();

            ExecuteMarketOrder(TradeType.Buy, Symbol, Volume, "", null, null, null);
            ExecuteMarketOrder(TradeType.Sell, Symbol, Volume, "", null, null, null);

            CreateWaves(TradeType.Buy);
            CreateWaves(TradeType.Sell);
        }

        protected override void OnTick()
        {
            foreach (Position position in Positions.FindAll("", Symbol))
            {
                double ProtectiveStopPrice = GetProtectiveStopPrice(position);

                if (ProtectiveStopPrice > 0)
                {
                    Print("Moved ProtectiveStopLoss to: " + ProtectiveStopPrice);
                    ModifyPosition(position, ProtectiveStopPrice, position.TakeProfit);
                }
            }
        }

        private void CreateWaves(TradeType type)
        {
            double TargetPrice = type == TradeType.Buy ? Symbol.Ask : Symbol.Bid;

            for (int i = 0; i < Waves; i++)
            {
                TargetPrice = type == TradeType.Buy ? TargetPrice + WaveInterval * Symbol.PipSize : TargetPrice - WaveInterval * Symbol.PipSize;
                PlaceStopOrder(type, Symbol, Volume, TargetPrice, "", null, null, null);
            }
        }

        private void CancelPendingOrders()
        {
            foreach (var order in PendingOrders)
            {
                if (order.SymbolCode == Symbol.Code)
                {
                    CancelPendingOrder(order);
                }
            }
        }

        private double GetProtectiveStopPrice(Position position)
        {
            for (int i = 0; i < Waves * 2; i++)
            {
                if (position.Pips >= WaveInterval * (i + 1))
                {
                    double ProtectiveStopSize = ((i * WaveInterval) + PSLAddPips) * Symbol.PipSize;

                    if (position.TradeType == TradeType.Buy)
                    {
                        double ProtectiveStopPrice = position.EntryPrice + ProtectiveStopSize;

                        if(position.StopLoss == null || position.StopLoss < ProtectiveStopPrice)
                        {
                            return ProtectiveStopPrice;
                        }
                    }
                    else if(position.TradeType == TradeType.Sell)
                    {
                        double ProtectiveStopPrice = position.EntryPrice - ProtectiveStopSize;

                        if(position.StopLoss == null || position.StopLoss > ProtectiveStopPrice)
                        {
                            return ProtectiveStopPrice;
                        }
                    }
                    
                }
            }

            return 0;
        }
    }
}
