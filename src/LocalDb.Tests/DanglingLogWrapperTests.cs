﻿using System;
using Microsoft.Data.SqlClient;
using Xunit;

public class DanglingLogWrapperTests
{
    [Fact]
    public void Run()
    {
        var name = "DanglingLogWrapperTests";
        LocalDbApi.StopAndDelete(name);
        var instance = new Wrapper(s => new SqlConnection(s), name, DirectoryFinder.Find(name));
        instance.Start(DateTime.Now, TestDbBuilder.CreateTable);
    }
}