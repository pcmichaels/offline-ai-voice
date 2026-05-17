using AiVoiceTest.Core.Session;
using Xunit;

namespace AiVoiceTest.Core.Tests.Session;

public sealed class TranslationLanguagesTests
{
    [Theory]
    [InlineData("es", "Spanish")]
    [InlineData("zh", "Mandarin")]
    [InlineData("de", "German")]
    public void TryGetByCode_WhenSupported_ReturnsLanguage(string code, string displayName)
    {
        var found = TranslationLanguages.TryGetByCode(code, out var language);

        Assert.True(found);
        Assert.NotNull(language);
        Assert.Equal(displayName, language.Value.DisplayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("fr")]
    public void TryGetByCode_WhenUnsupported_ReturnsFalse(string? code)
    {
        Assert.False(TranslationLanguages.TryGetByCode(code, out _));
    }

    [Fact]
    public void All_ContainsThreeLanguages()
    {
        Assert.Equal(3, TranslationLanguages.All.Count);
    }
}
