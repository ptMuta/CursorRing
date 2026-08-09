namespace CursorRing.Tests;

public sealed class GcdStateTests
{
    [Theory]
    [InlineData(1f, 0f)]
    [InlineData(1f, -2f)]
    [InlineData(-1f, 2f)]
    [InlineData(2f, 2f)]
    [InlineData(3f, 2f)]
    [InlineData(float.NaN, 2f)]
    [InlineData(1f, float.PositiveInfinity)]
    public void InvalidNativeStateIsInactive(float elapsed, float total)
    {
        Assert.Equal(GcdState.Inactive, GcdState.Create(true, elapsed, total));
    }

    [Theory]
    [InlineData(0f, 2f, 0f)]
    [InlineData(1f, 2f, 0.5f)]
    public void ActiveProgressIsNormalized(float elapsed, float total, float expected)
    {
        var state = GcdState.Create(true, elapsed, total);

        Assert.True(state.IsActive);
        Assert.Equal(expected, state.Progress);
    }

    [Fact]
    public void AdvancingTimerOverridesFalseNativeFlag()
    {
        var state = GcdState.Create(false, 0.5f, 2.5f);

        Assert.True(state.IsActive);
        Assert.Equal(0.2f, state.Progress);
    }

    [Fact]
    public void IdleTimerWithFalseNativeFlagIsInactive()
    {
        Assert.Equal(GcdState.Inactive, GcdState.Create(false, 0f, 2.5f));
    }

    [Fact]
    public void DirectInvalidStateHasSafeProgress()
    {
        Assert.Equal(0f, new GcdState(true, 1f, 0f).Progress);
    }
}
