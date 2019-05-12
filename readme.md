<!--
This file was generate by MarkdownSnippets.
Source File: /readme.source.md
To change this file edit the source file and then re-run the generation using either the dotnet global tool (https://github.com/SimonCropp/MarkdownSnippets#markdownsnippetstool) or using the api (https://github.com/SimonCropp/MarkdownSnippets#running-as-a-unit-test).
-->
# EfLocalDb



### Usage


<!-- snippet: ModuleInitializer -->
```cs
static class ModuleInitializer
{
    public static void Initialize()
    {
        LocalDb<TheDataContext>.Register(
            (connection, optionsBuilder) =>
            {
                using (var dataContext = new TheDataContext(optionsBuilder.Options))
                {
                    dataContext.Database.EnsureCreated();
                }
            },
            builder => new TheDataContext(builder.Options));
    }
}
```
<sup>[snippet source](/src/Tests/ModuleInitializerSnippet.cs#L5-L22)</sup>
<!-- endsnippet -->


## Icon

<a href="https://thenounproject.com/term/robot/960055/" target="_blank">Robot</a> designed by Creaticca Creative Agency from The Noun Project
