using EconomIA.Infrastructure.Scheduling;

namespace EconomIA.Infrastructure.Tests.Scheduling;

public class BriefingSchedulerOptionsTests
{
    [Fact]
    public void DefaultOptions_ShouldHaveCorrectDefaults()
    {
        var options = new BriefingSchedulerOptions();

        Assert.True(options.Enabled);
        Assert.Equal(7, options.ScheduleHourUtc);
        Assert.Equal(0, options.ScheduleMinuteUtc);
        Assert.True(options.WorkdaysOnly);
    }

    [Fact]
    public void Options_CanBeCustomized()
    {
        var options = new BriefingSchedulerOptions
        {
            Enabled = false,
            ScheduleHourUtc = 9,
            ScheduleMinuteUtc = 30,
            WorkdaysOnly = false,
        };

        Assert.False(options.Enabled);
        Assert.Equal(9, options.ScheduleHourUtc);
        Assert.Equal(30, options.ScheduleMinuteUtc);
        Assert.False(options.WorkdaysOnly);
    }
}
