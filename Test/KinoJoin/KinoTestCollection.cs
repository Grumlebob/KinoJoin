using Test.Monkey;

namespace Test.KinoJoin;

[CollectionDefinition("KinoJoinCollection")]
public class KinoTestCollection : ICollectionFixture<MonkeyServiceWebAppFactory> { }
