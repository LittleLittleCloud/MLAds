using Microsoft.ML.Data;

namespace PredictModel
{
    public class ModelInput
    {
        [ColumnName(@"Label")]
        public float Label { get; set; }

        [ColumnName(@"Content")]
        public string Content { get; set; }

    }
}