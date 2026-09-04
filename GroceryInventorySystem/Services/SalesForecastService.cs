using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using GroceryInventorySystem.Data;

namespace GroceryInventorySystem.Services
{
    public class SalesForecastService
    {
        private readonly ApplicationDbContext _context;

        public SalesForecastService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ForecastResult>> ForecastSales(
            int productId,
            int forecastDays = 7)
        {
            var salesData = await _context.Sales
                .Where(s => s.ProductId == productId)
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new DailySales
                {
                    Date = g.Key,
                    Quantity = g.Sum(s => s.Quantity)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            if (salesData.Count < 10)
            {
                throw new Exception(
                    "Not enough sales history for forecasting. " +
                    "At least 10 days of sales data are required.");
            }

            var mlContext = new MLContext();

            var trainingData = salesData.Select(x => new SalesData
            {
                Quantity = x.Quantity
            });

            IDataView dataView =
                mlContext.Data.LoadFromEnumerable(trainingData);

            var pipeline = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(SalesForecast.Forecast),
                inputColumnName: nameof(SalesData.Quantity),
                windowSize: Math.Min(7, salesData.Count / 2),
                seriesLength: Math.Min(14, salesData.Count),
                trainSize: salesData.Count,
                horizon: forecastDays,
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn:
                    nameof(SalesForecast.LowerBound),
                confidenceUpperBoundColumn:
                    nameof(SalesForecast.UpperBound)
            );

            var model = pipeline.Fit(dataView);

            var engine = model.CreateTimeSeriesEngine<
                SalesData,
                SalesForecast>(mlContext);

            var prediction = engine.Predict();

            var results = new List<ForecastResult>();

            for (int i = 0; i < forecastDays; i++)
            {
                results.Add(new ForecastResult
                {
                    Date = salesData.Last().Date.AddDays(i + 1),
                    PredictedQuantity =
                        Math.Max(0, prediction.Forecast[i]),
                    LowerBound =
                        Math.Max(0, prediction.LowerBound[i]),
                    UpperBound =
                        Math.Max(0, prediction.UpperBound[i])
                });
            }

            return results;
        }
    }

    public class DailySales
    {
        public DateTime Date { get; set; }
        public float Quantity { get; set; }
    }

    public class SalesData
    {
        public float Quantity { get; set; }
    }

    public class SalesForecast
    {
        public float[] Forecast { get; set; } =
            Array.Empty<float>();

        public float[] LowerBound { get; set; } =
            Array.Empty<float>();

        public float[] UpperBound { get; set; } =
            Array.Empty<float>();
    }

    public class ForecastResult
    {
        public DateTime Date { get; set; }
        public float PredictedQuantity { get; set; }
        public float LowerBound { get; set; }
        public float UpperBound { get; set; }
    }
}