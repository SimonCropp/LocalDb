// Verifies that, when AiCliDetector reports an AI session, every name/directory that flows
// into a SqlInstance carries the chatbot_ prefix — regardless of whether it was derived
// from the DbContext, produced by Storage.FromSuffix, or supplied directly by the caller
// via `new Storage(name, directory)`. The prefixing is centralised in the Storage
// constructor + AiCliDetector helpers, so all entry points pick it up. See commit 2cafc032.

[TestFixture]
public class AiCliDetectorPrefixTests
{
    class FakeContext;

    [Test]
    public void FromSuffix_PrefixesNameAndDirectory_WhenAiDetected()
    {
        var original = AiCliDetector.Detected;
        try
        {
            AiCliDetector.Detected = true;

            var storage = Storage.FromSuffix<FakeContext>("Worker1");

            That(storage.Name, Is.EqualTo("chatbot_FakeContext_Worker1"));
            That(Path.GetFileName(storage.Directory), Is.EqualTo("chatbot_FakeContext_Worker1"));
        }
        finally
        {
            AiCliDetector.Detected = original;
        }
    }

    [Test]
    public void FromSuffix_LeavesNameUnprefixed_WhenNotDetected()
    {
        var original = AiCliDetector.Detected;
        try
        {
            AiCliDetector.Detected = false;

            var storage = Storage.FromSuffix<FakeContext>("Worker1");

            That(storage.Name, Is.EqualTo("FakeContext_Worker1"));
            That(Path.GetFileName(storage.Directory), Is.EqualTo("FakeContext_Worker1"));
        }
        finally
        {
            AiCliDetector.Detected = original;
        }
    }

    [Test]
    public void CustomStorage_PrefixesNameAndDirectoryLeaf_WhenAiDetected()
    {
        var original = AiCliDetector.Detected;
        try
        {
            AiCliDetector.Detected = true;

            var storage = new Storage("MyCustomInstance", @"C:\TestDatabases\MyApp");

            That(storage.Name, Is.EqualTo("chatbot_MyCustomInstance"));
            That(storage.Directory, Is.EqualTo(@"C:\TestDatabases\chatbot_MyApp"));
        }
        finally
        {
            AiCliDetector.Detected = original;
        }
    }

    [Test]
    public void CustomStorage_LeavesUserInputAlone_WhenNotDetected()
    {
        var original = AiCliDetector.Detected;
        try
        {
            AiCliDetector.Detected = false;

            var storage = new Storage("MyCustomInstance", @"C:\TestDatabases\MyApp");

            That(storage.Name, Is.EqualTo("MyCustomInstance"));
            That(storage.Directory, Is.EqualTo(@"C:\TestDatabases\MyApp"));
        }
        finally
        {
            AiCliDetector.Detected = original;
        }
    }

    [Test]
    public void PrefixIfDetected_IsIdempotent()
    {
        var original = AiCliDetector.Detected;
        try
        {
            AiCliDetector.Detected = true;

            That(AiCliDetector.PrefixIfDetected("chatbot_Foo"), Is.EqualTo("chatbot_Foo"));
            That(AiCliDetector.PrefixIfDetected("Foo"), Is.EqualTo("chatbot_Foo"));
        }
        finally
        {
            AiCliDetector.Detected = original;
        }
    }

    [Test]
    public void PrefixDirectoryIfDetected_IsIdempotent()
    {
        var original = AiCliDetector.Detected;
        try
        {
            AiCliDetector.Detected = true;

            That(
                AiCliDetector.PrefixDirectoryIfDetected(@"C:\Data\chatbot_Foo"),
                Is.EqualTo(@"C:\Data\chatbot_Foo"));
            That(
                AiCliDetector.PrefixDirectoryIfDetected(@"C:\Data\Foo"),
                Is.EqualTo(@"C:\Data\chatbot_Foo"));
        }
        finally
        {
            AiCliDetector.Detected = original;
        }
    }

    [Test]
    public void PrefixIfDetected_ReturnsInputUnchanged_WhenNotDetected()
    {
        var original = AiCliDetector.Detected;
        try
        {
            AiCliDetector.Detected = false;
            That(AiCliDetector.PrefixIfDetected("Foo"), Is.EqualTo("Foo"));
            That(AiCliDetector.PrefixDirectoryIfDetected(@"C:\Data\Foo"), Is.EqualTo(@"C:\Data\Foo"));
        }
        finally
        {
            AiCliDetector.Detected = original;
        }
    }
}
