# EF.LocalDb

Produces text which, on first glance, looks like real, ponderous, prose; replete with clichés.

This output can be used in similar way to [Lorem ipsum](https://en.wikipedia.org/wiki/Lorem_ipsum) content, in that it is useful for producing text for build software and producing design mockups.

Based on the awesome work by [Andrew Clarke](https://www.red-gate.com/simple-talk/author/andrew-clarke/) outlined in [The Waffle Generator](https://www.red-gate.com/simple-talk/dotnet/net-tools/the-waffle-generator/).

Code based on [SDGGenerators - Red Gate SQL Data Generator Community Generators](https://archive.codeplex.com/?p=sdggenerators).


## Blazor App

The [Blazing Waffles](http://wafflegen.azurewebsites.net/) app allows the generation of waffle text online.

 * Azure Website: http://wafflegen.azurewebsites.net/
 * Source: https://github.com/gbiellem/BlazingWaffles


## Main Package - WaffleGenerator [![NuGet Status](http://img.shields.io/nuget/v/WaffleGenerator.svg?style=flat)](https://www.nuget.org/packages/WaffleGenerator/)

https://nuget.org/packages/WaffleGenerator/

    PM> Install-Package WaffleGenerator


### Usage

The `WaffleEngine` can be used to produce Html or text:

snippet: htmlUsage

snippet: textUsage


## WaffleGenerator.Bogus [![NuGet Status](http://img.shields.io/nuget/v/WaffleGenerator.Bogus.svg?style=flat)](https://www.nuget.org/packages/WaffleGenerator.Bogus/)

Extends [Bogus](https://github.com/bchavez/Bogus) to use WaffleGenerator.

https://nuget.org/packages/WaffleGenerator.Bogus/

    PM> Install-Package WaffleGenerator.Bogus


### Usage

The entry extension method is `WaffleHtml()` or `WaffleText()`:

snippet: BogusUsage


## Icon

<a href="https://thenounproject.com/term/robot/960055/" target="_blank">Robot</a> designed by Creaticca Creative Agency from The Noun Project
