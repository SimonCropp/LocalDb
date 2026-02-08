public class Employee
{
    public required Guid CompanyId { get; set; }
    public Company? Company { get; init; }
    public required Guid Id { get; init; }
    public required string Name { get; set; }
    public List<Vehicle> Vehicles { get; set; } = [];
}
