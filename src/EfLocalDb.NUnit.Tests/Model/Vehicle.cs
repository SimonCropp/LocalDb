public class Vehicle
{
    public required Guid EmployeeId { get; set; }
    public Employee? Employee { get; init; }
    public required Guid Id { get; init; }
    public required string Model { get; set; }
}