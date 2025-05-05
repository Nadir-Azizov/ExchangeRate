using BambooCard.Infrastructure.Enums;
using BambooCard.Infrastructure.Exceptions;
using BambooCard.WebService.Abstractions;
using BambooCard.WebService.Implementions;
using NSubstitute;

namespace BambooCard.Tests.WebService.Implementions;

public class CurrencyProviderFactoryTests
{
    [Fact]
    public void GetProvider_ShouldReturnCorrectProvider()
    {
        // Arrange
        var mockProvider = Substitute.For<ICurrencyProvider>();
        mockProvider.Provider.Returns(EProvider.Frankfurter);

        var factory = new CurrencyProviderFactory([mockProvider]);

        // Act
        var result = factory.GetProvider(EProvider.Frankfurter);

        // Assert
        Assert.Equal(mockProvider, result);
    }

    [Fact]
    public void GetProvider_ShouldThrow_WhenProviderNotFound()
    {
        var factory = new CurrencyProviderFactory([]);

        var ex = Assert.Throws<NotFoundException>(() =>
            factory.GetProvider(EProvider.Frankfurter));

        Assert.Contains("No currency‐provider", ex.Message);
    }
}
