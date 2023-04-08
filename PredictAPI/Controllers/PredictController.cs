using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using adsScore;
using Microsoft.ML.Data;

namespace PredictApi.Controllers
{
    [ApiController]
    public class PredictController : ControllerBase
    {
        private readonly AdsServices AdsServices;
        private readonly IHostApplicationLifetime _appLifetime;
        public PredictController(AdsServices adsServices, IHostApplicationLifetime appLifetime)
        {
            AdsServices = adsServices;
            _appLifetime = appLifetime;
        }

        [HttpPost]
        [Route("predict")]
        public async Task<ActionResult<object>> Predict(PredictionInputModel Input)
        {
           var time = Stopwatch.StartNew();
           time.Start();
            var predictionResult = await AdsServices.Predict(Input.Content);
            
            if (predictionResult.PredictedLabel == 1)
            {
                time.Stop();
                return new { status = "success", ads = false , time = time.ElapsedMilliseconds};
            }
            time.Stop();
            var result = new
            {
                status = "success",
                ads = new
                {
                    status = true,
                    certainty = Math.Round(predictionResult.Score[0] * 100, 2)
                },
                time = time.ElapsedMilliseconds
            };

            return await Task.FromResult(result);
        }

        [HttpGet]
        [Route("Shutdown")]
        public async Task Shutdown()
        {
            _appLifetime.StopApplication();
        }

        [HttpGet]
        [Route("ClearCache")]
        public async Task<ActionResult<object>> ClearCache()
        {
           var Result = AdsServices.ClearMemoryCache();
           return new { status = Result };
        }
    }
}