using Newtonsoft.Json;
using StockAnalyzer.Core;
using StockAnalyzer.Core.Domain;
using StockAnalyzer.Core.Services;
using StockAnalyzer.Windows.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace StockAnalyzer.Windows;

public partial class MainWindow : Window
{
    private static readonly string API_URL = "https://ps-async.fekberg.com/api/stocks";
    private readonly Stopwatch stopwatch = new();
    private readonly Random random = new();

    public MainWindow()
    {
        InitializeComponent();
    }


    private async void Search_Click(object sender, RoutedEventArgs e) 
    {
        //Progress bar and custom implementation IProgress<T>
        try
        {
            BeforeLoadingStockData();
            var progress = new Progress<IEnumerable<StockPrice>>();
            progress.ProgressChanged += (_, stocks) =>
            {
                StockProgress.Value++;
                Notes.Text = $"Loaded {stocks.Count()} for {stocks.First().Identifier}{Environment.NewLine}";
            };

            // now I can pass progress to the executing method:
            await SearchForStocks(progress);
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
        finally 
        {
            AfterLoadingStockData();
        }
	}

	private async void Search_Click_Ch5(object sender, RoutedEventArgs e) 
    {
        try
        {
            //ch5 working with async Stream.
            BeforeLoadingStockData();
            var identifier = StockIdentifier.Text.Split(' ', ',');
            var data = new ObservableCollection<StockPrice>();
            Stocks.ItemsSource = data;

            var svc = new MockStockStreamService();
            var enumerator = svc.GetAllStockPrices();

            await foreach (var price in enumerator.WithCancellation(CancellationToken.None))
			{
				if (identifier.Contains(price.Identifier))
				{
					data.Add(price);
				}
			}
		}
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
		finally
		{
			AfterLoadingStockData();
		}   
	}

    private async void Search_Click_Ch4(object sender, RoutedEventArgs e)
    {
        BeforeLoadingStockData();

        var stocks = new Dictionary<string, IEnumerable<StockPrice>>
            {
                { "MSFT", Generate("MSFT") },
                { "GOOGL", Generate("GOOGL") },
                { "AAPL", Generate("AAPL") },
                { "CAT", Generate("CAT") },
                { "ABC", Generate("ABC") },
                { "DEF", Generate("DEF") }
            };

        var bag = new ConcurrentBag<StockCalculation>();

        try
        {
            await Task.Run(() =>
            {
                try
                {
                    Parallel.For(0, 10, (i, state) => {
                        // i == current index
                    });

                    var parallelLoopResult = Parallel.ForEach(stocks,
                        new ParallelOptions { MaxDegreeOfParallelism = 1 },
                        (element, state) => {
                            if (element.Key == "MSFT" || state.ShouldExitCurrentIteration)
                            {
                                state.Break();

                                return;
                            }
                            else
                            {
                                var result = Calculate(element.Value);
                                bag.Add(result);
                            }
                        });
                }
                catch (Exception ex)
                {
                    Notes.Text = ex.Message;
                }
            });
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
        Stocks.ItemsSource = bag;

        AfterLoadingStockData();
    }

    private async void Search_Sync_Click(object sender, RoutedEventArgs e) 
    {
        BeforeLoadingStockData();

        try
        {
            using HttpClient client = new();

            Task<HttpResponseMessage> responseTask = client.GetAsync($"{API_URL}/{StockIdentifier.Text}");

            var response = await responseTask;
            var content = await response.Content.ReadAsStringAsync();

            IEnumerable<StockPrice>? data = JsonConvert.DeserializeObject<IEnumerable<StockPrice>>(content);

            Stocks.ItemsSource = data;
        }
        catch (Exception ex)
        {

            Notes.Text = ex.Message;
        }

        AfterLoadingStockData();
    }

    private async void Search_Local_Async_Click(object sender, RoutedEventArgs e) 
    {
        BeforeLoadingStockData();

        var getStocksTask = GetStocks();

        await getStocksTask;

        AfterLoadingStockData();
    }

    private async void Search_Local_File_Click(object sender, RoutedEventArgs e) 
    {
        try
        {
            BeforeLoadingStockData();

            var lines = File.ReadAllLines("StockPrices_Small.csv");

            List<StockPrice> data = [];
            data.AddRange(
                from string line in lines.Skip(1)
                let price = StockPrice.FromCSV(line)
                select price);
            
            Stocks.ItemsSource = data.Where(sp => sp.Identifier == StockIdentifier.Text);

            AfterLoadingStockData();
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
    }

    // Introducing async method only. It still needs to be called from an async method
    private async Task GetStocks() 
    {
        try
        {
            DataStore store = new();
            var responseTask = store.GetStockPrices(StockIdentifier.Text);
            Stocks.ItemsSource = await responseTask;
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
    }

    private static StockCalculation Calculate(IEnumerable<StockPrice> prices)
    {
        #region Start stopwatch
        var calculation = new StockCalculation();
        var watch = new Stopwatch();
        watch.Start();
        #endregion

        var end = DateTime.UtcNow.AddSeconds(4);

        // Spin a loop for a few seconds to simulate load
        while (DateTime.UtcNow < end)
        { }

        #region Return a result
        calculation.Identifier = prices.First().Identifier;
        calculation.Result = prices.Average(s => s.Open);

        watch.Stop();

        calculation.TotalSeconds = watch.Elapsed.Seconds;

        return calculation;
        #endregion
    }

    private IEnumerable<StockPrice> Generate(string stockIdentifier)
    {
        return Enumerable.Range(1, random.Next(10, 250))
            .Select(x => new StockPrice
            {
                Identifier = stockIdentifier,
                Open = random.Next(10, 1024)
            });
    }



    private void BeforeLoadingStockData()
    {
        stopwatch.Restart();
        StockProgress.Visibility = Visibility.Visible;
        StockProgress.IsIndeterminate = false;
		StockProgress.Value = 0;
        StockProgress.Maximum = StockIdentifier.Text.Split(',', ' ').Length;
	}

    private void AfterLoadingStockData()
    {
        StocksStatus.Text = $"Loaded stocks for {StockIdentifier.Text} in {stopwatch.ElapsedMilliseconds}ms";
        StockProgress.Visibility = Visibility.Hidden;
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });

        e.Handled = true;
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private async Task SearchForStocks() 
    {
        var svc = new StockService();
        var loadingTasks = new List<Task<IEnumerable<StockPrice>>>();

		foreach (var identifier in StockIdentifier.Text.Split(',', ' '))
		{
			var loadTask = svc.GetStockPricesFor(identifier, CancellationToken.None);
			loadingTasks.Add(loadTask);
		}
        var data = await Task.WhenAll(loadingTasks);

        Stocks.ItemsSource = data.SelectMany(stocks => stocks);
	}

	private async Task SearchForStocks(IProgress<IEnumerable<StockPrice>> progress)
	{
		var svc = new StockService();
		var loadingTasks = new List<Task<IEnumerable<StockPrice>>>();

		foreach (var identifier in StockIdentifier.Text.Split(',', ' '))
		{
			var loadTask = svc.GetStockPricesFor(identifier, CancellationToken.None);
			loadingTasks.Add(loadTask);
		}
		var data = await Task.WhenAll(loadingTasks);

		Stocks.ItemsSource = data.SelectMany(stocks => stocks);
	}
}