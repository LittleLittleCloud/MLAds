using adsScore;
using PredictModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictSrevice
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var input = new ModelInput
            {
                Content = "abc",
                Label = 1,
            };

            AdsServices.RetrainModel(new List<ModelInput>(){
                input,
            });
        }
    }
}
