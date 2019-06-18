using System;

internal class InstanceInfo
{
    public string Name { get; set; }
    public Version Version { get; set; }
    public string SharedName { get; set; }
    public string Owner { get; set; }
    public bool AutoCreate { get; set; }
    public State State { get; set; }
    public DateTime LastStartTime { get; set; }
    public string InstancePipeName { get; set; }
}