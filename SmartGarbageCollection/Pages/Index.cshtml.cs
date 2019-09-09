using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Xml;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BingMapsRESTToolkit;
using BingMapsRESTToolkit.Extensions;
using Microsoft.AspNetCore.SignalR;
using System.Configuration;

namespace SmartGarbageCollection.Pages
{
    public class IndexModel : PageModel
    {

        //public List<string> containers { get; private set; } = new List<string>();
        public List<DataEntity> containers { get; private set; } = new List<DataEntity>();
        public List<string> optimizedRoute { get; private set; } = new List<string>();
        public List<string> locations = new List<string>();


        string storageKey = ConfigurationManager.AppSettings["AzureStorageAccountKey"];
        string bingKey = ConfigurationManager.AppSettings["BingMapsApiKey"];

        public async Task OnGetAsync()
        {
            CloudStorageAccount storageAccount = new CloudStorageAccount(
       new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(
           "gcollectionstorage", storageKey), true);

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Get a reference to a table named "data"
            CloudTable table = tableClient.GetTableReference("data3");

            TableQuery<DataEntity> query = new TableQuery<DataEntity>();
            TableContinuationToken token = null;
            query.TakeCount = 50;
            int segmentNumber = 0;
            do
            {
                // Execute the query, passing in the continuation token.
                // The first time this method is called, the continuation token is null. If there are more results, the call
                // populates the continuation token for use in the next call.
                TableQuerySegment<DataEntity> segment = await table.ExecuteQuerySegmentedAsync(query, token);

                // Indicate which segment is being displayed
                if (segment.Results.Count > 0)
                {
                    segmentNumber++;
                    Console.WriteLine();
                    Console.WriteLine("Segment {0}", segmentNumber);
                }

                // Save the continuation token for the next call to ExecuteQuerySegmentedAsync
                token = segment.ContinuationToken;
                // Write out the properties for each entity returned.
                foreach (DataEntity entity in segment)
                {
                    containers.Add(entity);
                    locations.Add(entity.Location);
                }

                Console.WriteLine();
            }
            while (token != null);

            await NearestRoute();


        }

        public async Task<IActionResult> OnLoadPageAsync()
        {
            await OnGetAsync();
            return Page();
        }

        public async Task NearestRoute()
        {
            List<SimpleWaypoint> simpleWaypoints = new List<SimpleWaypoint>();

            /*
             * A to B - 2.2km
             * A to C - 2.6km
             *
             * B to A - 2.2km
             * B to C - 3.7km
             *
             * C to A - 2.6km
             * C to B - 3.7km
             */

            simpleWaypoints = Enumerable.Range(0, locations.Count).Select(x => new SimpleWaypoint { Address = locations[x] }).ToList();

            var tspResult = await TravellingSalesmen.Solve(simpleWaypoints, TravelModeType.Driving, TspOptimizationType.TravelTime, DateTime.Now, bingKey);
            foreach (var item in tspResult.OptimizedWaypoints)
            {
                var contenedor = containers.Find(x => x.Location == item.Address);
                optimizedRoute.Add(contenedor.RowKey);
            }

        }


    }



    public class DataEntity : TableEntity
    {
        public DataEntity(string region, int containerId)
        {
            this.PartitionKey = region;
            this.RowKey = containerId.ToString();
        }

        public DataEntity() { }

        public string Region { get; set; }
        public int contenedorId { get; set; }
        public double Percent { get; set; }
        public string Location { get; set; }

    }
}

