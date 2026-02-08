public class Company
{
    public required Guid Id { get; init; }
    public required string Name { get; set; }
    public List<Employee> Employees { get; set; } = [];
}
