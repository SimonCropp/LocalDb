﻿using System.Diagnostics;
using System.Linq;
using ObjectApproval;
using Xunit;
using Xunit.Abstractions;

public class SqlLocalDbTests :
    XunitLoggingBase
{
    [Fact]
    public void Instances()
    {
        var collection = SqlLocalDb.Instances().ToList();
        foreach (var instance in collection)
        {
            Trace.WriteLine(instance);
        }
        Assert.NotEmpty(collection);
    }

    [Fact]
    public void NonInstanceInfo()
    {
        var info = new ManagedLocalDbApi().GetInstance("Missing");
        Assert.False(info.Exists);
    }

    [Fact]
    public void Info()
    {
        SqlLocalDb.Start("InfoTest");
        var info = new ManagedLocalDbApi().GetInstance("InfoTest");

        ObjectApprover.VerifyWithJson(info);
        SqlLocalDb.DeleteInstance("InfoTest");
    }

    //[Fact]
    //public void DeleteAll()
    //{
    //    foreach (var instance in SqlLocalDb.Instances())
    //    {
    //        SqlLocalDb.DeleteInstance(instance);
    //    }
    //}

    public SqlLocalDbTests(ITestOutputHelper output) :
        base(output)
    {
    }
}