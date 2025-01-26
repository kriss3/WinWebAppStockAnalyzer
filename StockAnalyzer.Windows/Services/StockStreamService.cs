using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using StockAnalyzer.Core.Domain;

namespace StockAnalyzer.Windows.Services;

public interface IStockStreamService
{
	//IAsyncEnumerable<T> is a new interface in C# 8.0
	//Exposes an enumerator the provides asynchronous iteration over values of a specified type
	IAsyncEnumerable<StockPrice>
        GetAllStockPrices(CancellationToken cancellationToken = default);
}


//The EnumeratorCancellation attribute is optional. Only required when using WithCancellation()
public class MockStockStreamService : IStockStreamService
{
    public async IAsyncEnumerable<StockPrice> 
        GetAllStockPrices([EnumeratorCancellation]
                          CancellationToken cancellationToken = default)
    {
        await Task.Delay(500, cancellationToken);

		//using yield return with the IAsyncEnumerable will signal
		//to the iterator using this enumerator that it has an item to process.
		yield return new StockPrice { Identifier = "MSFT", Change = 0.5m };

        //this delay indicates that the caller will be able to get each item after 500ms each.
        await Task.Delay(500, cancellationToken);

        yield return new StockPrice { Identifier = "MSFT", Change = 0.2m };

        await Task.Delay(500, cancellationToken);

        yield return new StockPrice { Identifier = "GOOG", Change = 0.3m };

        await Task.Delay(500, cancellationToken);

        yield return new StockPrice { Identifier = "GOOG", Change = 0.8m };
    }
}

public class StockDiskStreamService : IStockStreamService
{
    public async IAsyncEnumerable<StockPrice> 
        GetAllStockPrices([EnumeratorCancellation]
                           CancellationToken cancellationToken = default)
    {
        using var stream = new StreamReader(File.OpenRead("StockPrices_Small.csv"));

        await stream.ReadLineAsync(); // Skip header row in the file

        while(await stream.ReadLineAsync() is string line)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            yield return StockPrice.FromCSV(line);
        }
    }
}
