using Microsoft.ML;
using Microsoft.ML.Data;
using RadioPulse.Domain;

namespace RadioPulse.Ml.Services;

public sealed class RecommendationEngine
{
    private readonly MLContext mlContext = new(seed: 42);
    private ITransformer? model;
    private DataViewSchema? trainingSchema;

    public void Train(IEnumerable<ListenEvent> listenEvents)
    {
        var rows = listenEvents.Select(x => new RecommendationInput
        {
            UserFeature = HashToFloat(x.UserId),
            StationFeature = HashToFloat(x.StationId),
            Label = x.Liked ? 1f : 0f
        }).ToList();

        if (rows.Count == 0)
        {
            rows = SampleData();
        }

        var dataView = mlContext.Data.LoadFromEnumerable(rows);

        var pipeline = mlContext.Transforms.Concatenate("Features", nameof(RecommendationInput.UserFeature), nameof(RecommendationInput.StationFeature))
            .Append(mlContext.Regression.Trainers.Sdca(labelColumnName: nameof(RecommendationInput.Label), featureColumnName: "Features"));

        model = pipeline.Fit(dataView);
        trainingSchema = dataView.Schema;
    }

    public void EnsureModel(string modelPath, IEnumerable<ListenEvent> listenEvents)
    {
        if (File.Exists(modelPath))
        {
            Load(modelPath);
            return;
        }

        Train(listenEvents);
        Save(modelPath);
    }

    public IReadOnlyList<RecommendationResult> Recommend(Guid userId, IEnumerable<Station> stations)
    {
        if (model is null)
        {
            return stations.Select(x => new RecommendationResult(x.Id, x.Name, 0f)).ToList();
        }

        var predictionEngine = mlContext.Model.CreatePredictionEngine<RecommendationInput, RecommendationPrediction>(model);

        return stations
            .Select(station =>
            {
                var prediction = predictionEngine.Predict(new RecommendationInput
                {
                    UserFeature = HashToFloat(userId),
                    StationFeature = HashToFloat(station.Id)
                });

                return new RecommendationResult(station.Id, station.Name, prediction.Score);
            })
            .OrderByDescending(x => x.Score)
            .Take(5)
            .ToList();
    }

    private static float HashToFloat(Guid value)
    {
        var hash = Math.Abs(value.GetHashCode()) % 10_000;
        return hash / 10_000f;
    }

    public void Save(string modelPath)
    {
        if (model is null || trainingSchema is null)
        {
            return;
        }

        var directory = Path.GetDirectoryName(modelPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        mlContext.Model.Save(model, trainingSchema, modelPath);
    }

    public void Load(string modelPath)
    {
        model = mlContext.Model.Load(modelPath, out var schema);
        trainingSchema = schema;
    }

    private static List<RecommendationInput> SampleData()
    {
        return
        [
            new RecommendationInput { UserFeature = 0.12f, StationFeature = 0.72f, Label = 1f },
            new RecommendationInput { UserFeature = 0.12f, StationFeature = 0.21f, Label = 0f },
            new RecommendationInput { UserFeature = 0.34f, StationFeature = 0.72f, Label = 1f },
            new RecommendationInput { UserFeature = 0.34f, StationFeature = 0.55f, Label = 1f },
            new RecommendationInput { UserFeature = 0.51f, StationFeature = 0.21f, Label = 1f },
            new RecommendationInput { UserFeature = 0.51f, StationFeature = 0.55f, Label = 0f }
        ];
    }
}

public sealed class RecommendationInput
{
    public float UserFeature { get; set; }
    public float StationFeature { get; set; }
    public float Label { get; set; }
}

public sealed class RecommendationPrediction
{
    public float Score { get; set; }
}

public sealed record RecommendationResult(Guid StationId, string StationName, float Score);
