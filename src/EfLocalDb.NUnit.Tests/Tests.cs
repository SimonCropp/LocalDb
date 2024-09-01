﻿#pragma warning disable CS0612 // Type or member is obsolete

[TestFixture]
public class Tests :
    LocalDbTestBase<TheDbContext>
{
    [Test]
    public async Task Simple()
    {
        ArrangeData.TestEntities.Add(
            new()
            {
                Property = "value"
            });
        await ArrangeData.SaveChangesAsync();

        var entity = await ActData.TestEntities.SingleAsync();
        entity.Property = "value2";
        await ActData.SaveChangesAsync();

        var result = await AssertData.TestEntities.SingleAsync();
        await Verify(result);
    }
}