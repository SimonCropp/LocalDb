using TUnit.Core.Interfaces;

[assembly: ParallelLimiter<ParallelLimit2>]

public class ParallelLimit2 : IParallelLimit
{
    public int Limit => 2;
}
