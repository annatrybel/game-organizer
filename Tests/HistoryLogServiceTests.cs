using GameOrganizer.Api.Models;
using GameOrganizer.Api.Models.DatabaseModels;
using GameOrganizer.Api.Models.Dto;
using GameOrganizer.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests;

[TestFixture]
public class HistoryLogServiceTests
{
    private GameOrganizerDbContext _dbContext = null!;
    private HistoryLogService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<GameOrganizerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GameOrganizerDbContext(options);
        var logger = new Mock<ILogger<HistoryLogService>>();
        _service = new HistoryLogService(_dbContext, logger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task GetHistoryLogs_ReturnsPagedAndSortedData()
    {
        _dbContext.HistoryLogs.AddRange(
            new HistoryLog { CreationDate = DateTime.UtcNow.AddDays(-1), EventType = "Update", ObjectType = "Game", ObjectId = "2" },
            new HistoryLog { CreationDate = DateTime.UtcNow, EventType = "Create", ObjectType = "Game", ObjectId = "1" },
            new HistoryLog { CreationDate = DateTime.UtcNow.AddDays(-2), EventType = "Delete", ObjectType = "Collection", ObjectId = "3" }
        );
        await _dbContext.SaveChangesAsync();

        var request = new DataTableRequest
        {
            Draw = 1,
            Start = 0,
            Length = 2,
            OrderColumn = 0,
            OrderDir = "desc",
            SearchValue = string.Empty
        };

        var result = await _service.GetHistoryLogs(request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.RecordsTotal, Is.EqualTo(3));
        Assert.That(result.Data.Data.Count, Is.EqualTo(2));
        Assert.That(result.Data.Data[0].CreationDate >= result.Data.Data[1].CreationDate, Is.True);
    }

    [Test]
    public async Task GetHistoryLogs_SearchFiltersResults()
    {
        _dbContext.HistoryLogs.AddRange(
            new HistoryLog { CreationDate = DateTime.UtcNow, EventType = "Create", ObjectType = "Game", ObjectId = "1", UserEmail = "john@example.com" },
            new HistoryLog { CreationDate = DateTime.UtcNow, EventType = "Delete", ObjectType = "Game", ObjectId = "2", UserEmail = "ann@example.com" }
        );
        await _dbContext.SaveChangesAsync();

        var request = new DataTableRequest
        {
            Draw = 1,
            Start = 0,
            Length = 10,
            OrderColumn = 1,
            OrderDir = "asc",
            SearchValue = "john"
        };

        var result = await _service.GetHistoryLogs(request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.RecordsFiltered, Is.EqualTo(1));
        Assert.That(result.Data.Data.Single().UserEmail, Is.EqualTo("john@example.com"));
    }

    [Test]
    public async Task GetHistoryLogs_FormatsJsonFieldsToHtml()
    {
        _dbContext.HistoryLogs.Add(new HistoryLog
        {
            CreationDate = DateTime.UtcNow,
            EventType = "Update",
            ObjectType = "Game",
            ObjectId = "7",
            Before = "{\"Title\":\"Old\"}",
            After = "{\"Title\":\"New\"}"
        });
        await _dbContext.SaveChangesAsync();

        var request = new DataTableRequest
        {
            Draw = 1,
            Start = 0,
            Length = 10,
            OrderColumn = 0,
            OrderDir = "desc",
            SearchValue = string.Empty
        };

        var result = await _service.GetHistoryLogs(request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.Data[0].Before, Does.Contain("<dl>"));
        Assert.That(result.Data.Data[0].Before, Does.Contain("Title"));
    }

    [Test]
    public async Task GetHistoryLogs_WhenJsonInvalid_ReturnsRawValue()
    {
        _dbContext.HistoryLogs.Add(new HistoryLog
        {
            CreationDate = DateTime.UtcNow,
            EventType = "Update",
            ObjectType = "Game",
            ObjectId = "8",
            Before = "not-json",
            After = "still-not-json"
        });
        await _dbContext.SaveChangesAsync();

        var request = new DataTableRequest
        {
            Draw = 1,
            Start = 0,
            Length = 10,
            OrderColumn = 0,
            OrderDir = "desc",
            SearchValue = string.Empty
        };

        var result = await _service.GetHistoryLogs(request);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Data.Data[0].Before, Is.EqualTo("not-json"));
    }
}
