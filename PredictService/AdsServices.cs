using Microsoft.ML;
using PredictModel;
using Microsoft.ML.Trainers;
using Mysqlx.Expr;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.ML.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.ML.Calibrators;

namespace adsScore
{
    public class AdsServices
    {
        public static readonly string ModelFile = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == true ? @"AdsML.zip" : @"/usr/local/DPX/DPXAdsModel/AdsML.zip";
        private readonly PredictionEngine<ModelInput, ModelOutput> _predictEngine;
        private readonly IMemoryCache _cache;

        public AdsServices(IMemoryCache cache)
        {
            _predictEngine = CreatePredictEngine();
            _cache = cache;
        }

        public async Task<ModelOutput> Predict(string content)
        {
            if (_cache.TryGetValue(content, out ModelOutput result))
            {
                return result;
            }

            result = await Task.Run(() => _predictEngine.Predict(new ModelInput { Content = content }));
            _cache.Set(content, result);

            return result;
        }
        public static IEstimator<ITransformer> BuildPipeline(MLContext mlContext)
        {
            var pipeline = mlContext.Transforms.Text.FeaturizeText(inputColumnName: @"Content", outputColumnName: @"Content")
                .Append(mlContext.Transforms.Concatenate(@"Features", new[] { @"Content" }))
                .Append(mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: @"Label", inputColumnName: @"Label"))
                .Append(mlContext.Transforms.NormalizeMinMax(@"Features", @"Features"))
                .Append(mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(new LbfgsMaximumEntropyMulticlassTrainer.Options() { L1Regularization = 0.03125F, L2Regularization = 0.2431656F, LabelColumnName = @"Label", FeatureColumnName = @"Features" }))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue(outputColumnName: @"PredictedLabel", inputColumnName: @"PredictedLabel"));
            return pipeline;
        }
        public static bool RetrainModel(List<ModelInput> newData)
        {
            if (newData == null)
            {
                return false;
            }

            Console.WriteLine("start");

            var mlContext = new MLContext();

            Console.WriteLine("load data");
            var originalModel = mlContext.Model.Load(ModelFile, out var modelSchema);

            var predictor = (originalModel as TransformerChain<ITransformer>).Reverse().ToArray()[1] as MulticlassPredictionTransformer<MaximumEntropyModelParameters>;
            MaximumEntropyModelParameters originalModelParameters = predictor.Model;

            var dataView = mlContext.Data.LoadFromEnumerable<ModelInput>(newData);

            var featurizedModel = new TransformerChain<ITransformer>((originalModel as TransformerChain<ITransformer>).Take(4).ToArray());
            var featurizedData = featurizedModel.Transform(dataView);
            var trainer = mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(new LbfgsMaximumEntropyMulticlassTrainer.Options() { L1Regularization = 0.03125F, L2Regularization = 0.2431656F, LabelColumnName = @"Label", FeatureColumnName = @"Features" });

            // fit new trainer with weight from original model
            var retrainedModel = trainer.Fit(featurizedData, originalModelParameters);
            featurizedData = retrainedModel.Transform(featurizedData);

            // fit mapKeyToValueEstimator
            var mapKeyToValueTransformer = (originalModel as TransformerChain<ITransformer>).Last();

            // the label will be in featurizedData.GetColumn<string>("PredictedLabel");

            // note
            // 1. the retrained model is not the same as the original model, it only contains the lbfgs trainer
            // 2. to save the retrained model with the original model, you need to append the retrained model to featurizer, and then append mapKeyToValueTransformer to the appended model
            // 3. Warning: I'm not sure the following code work 100% correctly, but it should be close.
            var retrainedModelWithOriginalModel = featurizedModel.Append(retrainedModel).Append(mapKeyToValueTransformer);
            mlContext.Model.Save(retrainedModelWithOriginalModel, dataView.Schema, ModelFile);
            return true;
            
        }
        public static ITransformer RetrainPipeline(MLContext mlContext, IDataView trainData)
        {
            var pipeline = BuildPipeline(mlContext);
            var model = pipeline.Fit(trainData);

            return model;
        }
        public static PredictionEngine<ModelInput, ModelOutput> CreatePredictEngine()
        {
            var mlContext = new MLContext();
            ITransformer mlModel = mlContext.Model.Load(AdsServices.ModelFile, out var _);
            return mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(mlModel);
        }

        public string ClearMemoryCache()
        {
            if (_cache == null)
            {
                return "We dont have any data in memory";
            }
            else if (_cache is MemoryCache memCache)
            {
                memCache.Compact(1.0);
                return "success";
            }
            else
            {
                MethodInfo clearMethod = _cache.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
                if (clearMethod != null)
                {
                    clearMethod.Invoke(_cache, null);
                    return "success";
                }
                else
                {
                    PropertyInfo prop = _cache.GetType().GetProperty("EntriesCollection", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public);
                    if (prop != null)
                    {
                        object innerCache = prop.GetValue(_cache);
                        if (innerCache != null)
                        {
                            clearMethod = innerCache.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public);
                            if (clearMethod != null)
                            {
                                clearMethod.Invoke(innerCache, null);
                                return "success";
                            }
                        }
                    }
                }
            }

            return "Unable to clear memory cache instance of type " + _cache.GetType().FullName;

        }

    }
}