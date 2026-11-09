using FluentAssertions;
using SchoolOS.BuildingBlocks.SharedKernel;
using Xunit;

namespace SchoolOS.UnitTests;

public class EntityTests
{
    private class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }
    }

    private class TestDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.CreateVersion7();
        public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
    }

    [Fact]
    public void Entity_Equality_SameId_ShouldBeEqual()
    {
        var id = Guid.CreateVersion7();
        var entity1 = new TestEntity(id);
        var entity2 = new TestEntity(id);

        entity1.Should().Be(entity2);
        (entity1 == entity2).Should().BeTrue();
    }

    [Fact]
    public void Entity_Equality_DifferentId_ShouldNotBeEqual()
    {
        var entity1 = new TestEntity(Guid.CreateVersion7());
        var entity2 = new TestEntity(Guid.CreateVersion7());

        entity1.Should().NotBe(entity2);
        (entity1 != entity2).Should().BeTrue();
    }

    [Fact]
    public void Entity_DomainEvents_AddAndClear_ShouldWork()
    {
        var entity = new TestEntity(Guid.CreateVersion7());
        var domainEvent = new TestDomainEvent();

        entity.AddDomainEvent(domainEvent);
        entity.DomainEvents.Should().ContainSingle().Which.Should().Be(domainEvent);

        entity.ClearDomainEvents();
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Entity_AuditMetadata_Setters_ShouldUpdateValues()
    {
        var entity = new TestEntity(Guid.CreateVersion7());
        var createdAt = DateTime.UtcNow.AddHours(-1);
        var updatedAt = DateTime.UtcNow;

        entity.SetCreated(createdAt, "admin-1");
        entity.SetUpdated(updatedAt, "admin-2");

        entity.CreatedAtUtc.Should().Be(createdAt);
        entity.CreatedBy.Should().Be("admin-1");
        entity.UpdatedAtUtc.Should().Be(updatedAt);
        entity.UpdatedBy.Should().Be("admin-2");
    }
}
