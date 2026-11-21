using System.Linq.Expressions;
using System.Reflection;
using Vibe.UI.Metadata;

namespace Vibe.UI.Tests.Metadata;

public class PropertyAccessorTests
{
    private static PropertyMetadataResolver Resolver() => new([new ConventionMetadataProvider()]);

    [Fact]
    public void Fixture_setter_really_is_init_only()
    {
        // Guards every "writes an init-only property" test below: without the modreq they would
        // pass against an ordinary setter and prove nothing about immutable records.
        MethodInfo setter = typeof(FixtureCommand)
            .GetProperties()
            .Single(p => p.Name == nameof(FixtureCommand.FirstName))
            .SetMethod!;

        setter
            .ReturnParameter.GetRequiredCustomModifiers()
            .ShouldContain(typeof(System.Runtime.CompilerServices.IsExternalInit));
    }

    [Fact]
    public void Bind_writes_an_init_only_property_in_place()
    {
        var command = new FixtureCommand { FirstName = "before" };

        PropertyBinding<string> binding = Resolver().Bind(() => command.FirstName);
        binding.Value = "after";

        command.FirstName.ShouldBe("after");
        binding.Value.ShouldBe("after");
        binding.Model.ShouldBeSameAs(command);
        binding.Accessor.IsInitOnly.ShouldBeTrue();
        binding.Accessor.CanWrite.ShouldBeTrue();
    }

    [Fact]
    public void Bind_reads_and_writes_value_enum_and_nullable_properties()
    {
        var command = new FixtureCommand();
        PropertyMetadataResolver resolver = Resolver();

        resolver.Bind(() => command.Quantity).Value = 7;
        resolver.Bind(() => command.Role).Value = FixtureRole.Editor;
        PropertyBinding<int?> optional = resolver.Bind(() => command.OptionalCount);
        optional.Value = 3;

        command.Quantity.ShouldBe(7);
        command.Role.ShouldBe(FixtureRole.Editor);
        command.OptionalCount.ShouldBe(3);
        optional.Value = null;
        command.OptionalCount.ShouldBeNull();
    }

    [Fact]
    public void Bind_writes_an_inherited_init_only_property()
    {
        var command = new FixtureCommand();
        var id = Guid.NewGuid();

        PropertyBinding<Guid> binding = Resolver().Bind(() => command.Id);
        binding.Value = id;

        command.Id.ShouldBe(id);
        binding.Field.FieldName.ShouldBe(nameof(FixtureBase.Id));
        binding.Field.Model.ShouldBeSameAs(command);
    }

    [Fact]
    public void Bind_field_identifier_matches_the_one_the_framework_creates()
    {
        var command = new FixtureCommand();

        PropertyBinding<string> binding = Resolver().Bind(() => command.FirstName);

        binding.Field.ShouldBe(
            Microsoft.AspNetCore.Components.Forms.FieldIdentifier.Create(() => command.FirstName)
        );
    }

    [Fact]
    public void Accessor_is_built_once_per_property_and_shared_across_instances_and_expressions()
    {
        PropertyMetadataResolver resolver = Resolver();
        var first = new FixtureCommand();
        var second = new FixtureCommand();

        PropertyBinding<string> a = resolver.Bind(() => first.FirstName);
        PropertyBinding<string> b = resolver.Bind(() => second.FirstName);

        b.Accessor.ShouldBeSameAs(a.Accessor);
        a.Value = "one";
        b.Value = "two";
        first.FirstName.ShouldBe("one");
        second.FirstName.ShouldBe("two");
    }

    [Fact]
    public void Accessor_can_read_a_property_as_object()
    {
        PropertyMetadataResolver resolver = Resolver();
        PropertyInfo quantity =
            (
                (MemberExpression)((Expression<Func<FixtureCommand, int>>)(x => x.Quantity)).Body
            ).Member as PropertyInfo
            ?? throw new InvalidOperationException();
        var command = new FixtureCommand { Quantity = 4 };

        PropertyAccessor<object> accessor = resolver.GetAccessor<object>(quantity);
        accessor.SetValue(command, 9);

        accessor.GetValue(command).ShouldBe(9);
        command.Quantity.ShouldBe(9);
    }

    [Fact]
    public void Bind_looks_through_the_conversion_the_compiler_adds_for_object_and_nullable()
    {
        // () => command.Quantity typed as Func<object> or Func<int?> wraps the member access in a
        // Convert node. The binding must still find the property and write the underlying int.
        var command = new FixtureCommand { Quantity = 1 };
        PropertyMetadataResolver resolver = Resolver();

        PropertyBinding<object> boxed = resolver.Bind<object>(() => command.Quantity);
        PropertyBinding<int?> widened = resolver.Bind<int?>(() => command.Quantity);

        boxed.Field.FieldName.ShouldBe(nameof(FixtureCommand.Quantity));
        boxed.Value.ShouldBe(1);
        boxed.Value = 5;
        command.Quantity.ShouldBe(5);
        widened.Value = 8;
        command.Quantity.ShouldBe(8);
        widened.Value.ShouldBe(8);
    }

    [Fact]
    public void Read_only_property_reads_and_refuses_to_write()
    {
        var command = new FixtureCommand { FirstName = "ada" };

        PropertyBinding<string> binding = Resolver().Bind(() => command.Computed);

        binding.Value.ShouldBe("ADA");
        binding.Accessor.CanWrite.ShouldBeFalse();
        Should
            .Throw<InvalidOperationException>(() => binding.Value = "x")
            .Message.ShouldContain("Computed");
    }

    [Fact]
    public void Accessor_writes_into_a_boxed_struct()
    {
        PropertyMetadataResolver resolver = Resolver();
        PropertyInfo count =
            ((MemberExpression)((Expression<Func<FixtureStruct, int>>)(x => x.Count)).Body).Member
                as PropertyInfo
            ?? throw new InvalidOperationException();
        object boxed = new FixtureStruct { Count = 1 };

        resolver.GetAccessor<int>(count).SetValue(boxed, 5);

        ((FixtureStruct)boxed).Count.ShouldBe(5);
    }

    [Fact]
    public void Expressions_that_are_not_a_property_access_are_rejected()
    {
        PropertyMetadataResolver resolver = Resolver();
        var command = new FixtureCommand();

        Should.Throw<ArgumentException>(() => resolver.Bind(() => command.FirstName.Length + 1));
        Should.Throw<ArgumentException>(() =>
            resolver.GetMetadata<FixtureCommand, int>(x => x.FirstName.Length)
        );
    }

    [Fact]
    public void Resolver_is_safe_to_use_from_many_threads()
    {
        PropertyMetadataResolver resolver = Resolver();
        var accessors = new System.Collections.Concurrent.ConcurrentBag<object>();

        Parallel.For(
            0,
            200,
            i =>
            {
                var command = new FixtureCommand();
                PropertyBinding<int> binding = resolver.Bind(() => command.Quantity);
                binding.Value = i;
                command.Quantity.ShouldBe(i);
                accessors.Add(binding.Accessor);
                resolver
                    .GetMetadata<FixtureCommand, string>(x => x.FirstName)
                    .Label.ShouldBe("First name");
            }
        );

        accessors.Distinct().Count().ShouldBe(1);
    }
}
