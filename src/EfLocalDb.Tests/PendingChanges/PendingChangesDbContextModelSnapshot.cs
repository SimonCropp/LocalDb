[DbContext(typeof(PendingChangesDbContext))]
class PendingChangesDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder model)
    {
#pragma warning disable 612, 618
        model
            .HasAnnotation("ProductVersion", "10.0.3")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        model.UseIdentityColumns();

        model.Entity("PendingChangesEntity", entity =>
        {
            entity.Property<int>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("int");

            entity.Property<int>("Id").UseIdentityColumn();

            entity.Property<string>("Property")
                .HasColumnType("nvarchar(max)");

            entity.HasKey("Id");

            entity.ToTable("PendingChangesEntities");
        });
#pragma warning restore 612, 618
    }
}
