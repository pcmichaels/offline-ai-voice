using AiVoiceTest.Core.Session;
using Xunit;

namespace AiVoiceTest.Core.Tests.Session;

public sealed class TranslationChoiceResolverTests
{
    [Theory]
    [InlineData("Spanish", TranslationLanguages.SpanishCode)]
    [InlineData("Mandarin", TranslationLanguages.MandarinCode)]
    [InlineData("German", TranslationLanguages.GermanCode)]
    public void ResolveLanguageChoice_WhenLanguageSelected_ReturnsCode(string choice, string expectedCode)
    {
        var language = TranslationChoiceResolver.ResolveLanguageChoice(choice);

        Assert.NotNull(language);
        Assert.Equal(expectedCode, language.Value.Code);
    }

    [Theory]
    [InlineData("Skip")]
    [InlineData(null)]
    [InlineData("French")]
    public void ResolvePromptChoice_WhenSkipOrUnknown_ReturnsNull(string? choice)
    {
        Assert.Null(TranslationChoiceResolver.ResolvePromptChoice(choice));
    }

    [Fact]
    public void LanguagePromptChoices_DoNotIncludeSkip()
    {
        Assert.DoesNotContain("Skip", TranslationChoiceResolver.LanguagePromptChoices);
        Assert.Equal(3, TranslationChoiceResolver.LanguagePromptChoices.Count);
    }
}
