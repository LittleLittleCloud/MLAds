using System.Diagnostics;
using adsScore;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.Extensions.Caching.Memory;
using PredictAPI.Data;
using PredictModel;
using System.Text.RegularExpressions;

namespace PredictAPI
{
    public class Respository
    {
        private readonly PredictAPIContext _dbContext;
        private readonly AdsServices AdsServices;
        public Respository(PredictAPIContext dbContext, AdsServices adsServices)
        {
            _dbContext = dbContext;
            AdsServices = adsServices;
        }
        public  void GetData()
        {
            // Retrieve data from the database
            var time = Stopwatch.StartNew();
            time.Start();
            var data = _dbContext.retrain.ToList();
            if (data.Count >= 1)
            {

                var InputModel = new List<ModelInput>();
                foreach (var itemRetrain in data)
                {
                    if (itemRetrain.Label == "ads")
                    {
                        InputModel.Add(new ModelInput { Content = Regex.Replace(itemRetrain.Content, @"\p{Cs}", " "), Label = 0 });
                       // _dbContext.Remove(itemRetrain);
                    }
                    else if (itemRetrain.Label == "normal")
                    {
                        InputModel.Add(new ModelInput { Content = Regex.Replace(itemRetrain.Content, @"\p{Cs}", " "), Label = 1 });
                       // _dbContext.Remove(itemRetrain);
                    }
                }
                Console.WriteLine("Start training {0} data", data.Count);
                var result = AdsServices.RetrainModel(InputModel);
                if (result)
                {
                  // await _dbContext.SaveChangesAsync();
                    Console.WriteLine("The training was successfully completed");
                }
                else
                {
                    Console.WriteLine("Training failed");
                }
            }
            else
            {
                Console.WriteLine("There is no data for training");
            }
            time.Stop();
            Console.WriteLine("Time: {0}ms",time.ElapsedMilliseconds);
        }
    }
}
