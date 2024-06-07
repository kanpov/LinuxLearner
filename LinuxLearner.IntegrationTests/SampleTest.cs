namespace LinuxLearner.IntegrationTests;

public class SampleTest(IntegrationTestFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task SomeTest()
    {
        await Task.Delay(3000);
    }
}