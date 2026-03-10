using System.Text.RegularExpressions;
using UrlShortener.Api.Services;

namespace EncurtadorUrl.Api.Tests;

public class Base62ServiceTests
{
    [Fact(DisplayName = "Gerar custom alias aleatoriamente validando padrao")]
    public void GerarAleatoriamenteCustomAlias()
    {
        // Act
        var alias = Base62Service.GerarAleatoriamenteCustomAlias(4);

        // Assert 
        Assert.Matches(new Regex(@"^[0-9A-Za-z]{4}-[0-9A-Za-z]{4}$"), alias);
    }

    [Fact(DisplayName ="Gerar ID aleatoriamente validando padrao")]
    public void GerarAleatoriamenteId()
    {
        var id = Base62Service.GenerateRandom(6);
        Assert.Equal(6, id.Length);
        Assert.False(string.IsNullOrWhiteSpace(id));

        Assert.Matches(new Regex(@"^[0-9A-Za-z]{5,64}$"), id);

    }
}