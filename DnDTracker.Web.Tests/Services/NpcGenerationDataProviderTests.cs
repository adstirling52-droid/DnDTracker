using DnDTracker.Web.Services.NpcGenerator;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DnDTracker.Web.Tests.Services;

public class NpcGenerationDataProviderTests
{
    [Fact]
    public void Load_ReturnsValidatedData_ForSampleNpcGenerationFile()
    {
        var dataFilePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "DnDTracker.Web",
            "Data",
            "NpcGenerator",
            "npc-generation-data.json"));

        var (data, error) = NpcGenerationDataProvider.Load(dataFilePath);

        Assert.Null(error);
        Assert.NotNull(data);
        Assert.Equal(NpcGenerationDataProvider.SupportedSchemaVersion, data.SchemaVersion);
        Assert.Equal(
            "Grounded, system-neutral fantasy with occasional unusual or memorable details.",
            data.Tone);
        Assert.Equal(3, data.Ancestries.Count);
        Assert.Equal(3, data.NamesByAncestry["human"].Count);
        Assert.Equal(5, data.Occupations.Count);
    }

    [Fact]
    public void Provider_LoadsSampleData_WhenConstructedWithContentRoot()
    {
        var contentRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "DnDTracker.Web"));

        var dataFilePath = Path.Combine(contentRoot, "Data", "NpcGenerator", "npc-generation-data.json");
        var provider = new NpcGenerationDataProvider(
            dataFilePath,
            NullLogger<NpcGenerationDataProvider>.Instance);

        Assert.True(provider.IsLoaded);
        Assert.Null(provider.LoadError);
        Assert.Equal(4, provider.Data.AgeCategories.Count);
        Assert.NotEmpty(provider.Data.ImageEnvironments);
    }

    [Fact]
    public void Load_ReturnsError_WhenFileIsMissing()
    {
        var (data, error) = NpcGenerationDataProvider.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));

        Assert.Null(data);
        Assert.NotNull(error);
        Assert.Contains("not found", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_ReturnsError_WhenSchemaVersionIsUnsupported()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        File.WriteAllText(tempFile, """{"schemaVersion":99,"tone":"test","ancestries":[],"namesByAncestry":{}}""");

        try
        {
            var (data, error) = NpcGenerationDataProvider.Load(tempFile);

            Assert.Null(data);
            Assert.Contains("Unsupported NPC generation schema version", error);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
