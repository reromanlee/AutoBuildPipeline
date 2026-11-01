using NUnit.Framework;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    public sealed class FileNamesTests
    {
        [TestCase("My Game", "My Game")]
        [TestCase("My Game: Deluxe", "My Game Deluxe")]
        [TestCase("  Spaced  ", "Spaced")]
        [TestCase("Who? Me*", "Who Me")]
        [TestCase("Path/To\\Game", "PathToGame")]
        [TestCase("Ends With Dots...", "Ends With Dots")]
        [TestCase("Tab\tName", "TabName")]
        [TestCase("com.company.app", "com.company.app")]
        [TestCase("Игра", "Игра")]
        public void RemoveInvalidCharacters(string name, string expected)
        {
            Assert.That(FileNames.Sanitize(name), Is.EqualTo(expected));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("???")]
        [TestCase("...")]
        public void FallBackWhenNothingIsLeft(string name)
        {
            Assert.That(FileNames.Sanitize(name), Is.EqualTo(FileNames.FallbackName));
        }

        [TestCase("CON", "CON_")]
        [TestCase("nul", "nul_")]
        [TestCase("com1.game", "com1.game_")]
        [TestCase("Console", "Console")]
        public void AvoidReservedWindowsNames(string name, string expected)
        {
            Assert.That(FileNames.Sanitize(name), Is.EqualTo(expected));
        }
    }
}
