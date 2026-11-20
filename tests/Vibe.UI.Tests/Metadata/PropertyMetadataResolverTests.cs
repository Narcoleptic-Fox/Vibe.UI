using System.ComponentModel.DataAnnotations;
using Vibe.UI.Metadata;

namespace Vibe.UI.Tests.Metadata;

public class PropertyMetadataResolverTests
{
    private static IPropertyMetadataResolver Build(
        Action<VibeMetadataOptions>? profiles = null,
        params IPropertyMetadataProvider[] extra
    )
    {
        var services = new ServiceCollection();
        services.AddVibeMetadata(profiles);
        foreach (IPropertyMetadataProvider provider in extra)
            services.AddSingleton(provider);
        return services.BuildServiceProvider().GetRequiredService<IPropertyMetadataResolver>();
    }

    [Theory]
    [InlineData("FirstName", "First name")]
    [InlineData("IsActive", "Is active")]
    [InlineData("UserID", "User ID")]
    [InlineData("HTMLParser", "HTML parser")]
    [InlineData("Address2", "Address 2")]
    [InlineData("first_name", "First name")]
    [InlineData("Id", "Id")]
    [InlineData("X", "X")]
    [InlineData("_", "_")]
    [InlineData("_private_value", "Private value")]
    public void Convention_turns_a_property_name_into_a_label(string name, string expected) =>
        ConventionMetadataProvider.ToLabel(name).ShouldBe(expected);

    [Fact]
    public void Convention_answers_when_nothing_else_does()
    {
        PropertyMetadata metadata = Build().GetMetadata<FixtureCommand, string>(x => x.FirstName);

        metadata.Label.ShouldBe("First name");
        metadata.Required.ShouldBeNull();
        metadata.MaxLength.ShouldBeNull();
    }

    [Fact]
    public void DataAnnotations_supply_label_description_prompt_required_lengths_and_data_type()
    {
        PropertyMetadata metadata = Build().GetMetadata<AnnotatedModel, string>(x => x.Email);

        metadata.Label.ShouldBe("Email address");
        metadata.Description.ShouldBe("Where we send receipts");
        metadata.Placeholder.ShouldBe("you@example.com");
        metadata.Required.ShouldBe(true);
        metadata.MinLength.ShouldBe(5);
        metadata.MaxLength.ShouldBe(120);
        metadata.DataType.ShouldBe(DataType.EmailAddress);
    }

    [Fact]
    public void DataAnnotations_supply_display_name_range_and_format()
    {
        PropertyMetadata metadata = Build().GetMetadata<AnnotatedModel, decimal>(x => x.Price);

        metadata.Label.ShouldBe("Unit price");
        metadata.Minimum.ShouldBe(1);
        metadata.Maximum.ShouldBe(500);
        metadata.Format.ShouldBe("C2");
        metadata.Required.ShouldBeNull();
    }

    [Fact]
    public void DataAnnotations_read_MinLength_and_MaxLength_attributes()
    {
        PropertyMetadata metadata = Build().GetMetadata<AnnotatedModel, string>(x => x.Code);

        metadata.MinLength.ShouldBe(2);
        metadata.MaxLength.ShouldBe(40);
        metadata.Label.ShouldBe("Code");
    }

    [Fact]
    public void An_explicit_DataType_attribute_wins_when_a_property_has_more_than_one()
    {
        IPropertyMetadataResolver resolver = Build();

        resolver.GetMetadata<AnnotatedModel, string>(x => x.TwoDataTypes).DataType.ShouldBe(DataType.Password);
        resolver.GetMetadata<AnnotatedModel, string>(x => x.TwoDerivedDataTypes).DataType.ShouldNotBeNull();
    }

    [Fact]
    public void A_property_without_attributes_gets_nothing_from_DataAnnotations()
    {
        var provider = new DataAnnotationsMetadataProvider();

        provider
            .GetMetadata(
                typeof(AnnotatedModel),
                typeof(AnnotatedModel)
                    .GetProperties()
                    .Single(p => p.Name == nameof(AnnotatedModel.Plain))
            )
            .ShouldBeNull();
    }

    [Fact]
    public void Profile_wins_over_DataAnnotations_one_facet_at_a_time()
    {
        // The profile answers only the label. Required and the lengths must still come from the attributes.
        IPropertyMetadataResolver resolver = Build(m =>
            m.Model<AnnotatedModel>(p => p.Property(x => x.Email).Label("Work email"))
        );

        PropertyMetadata metadata = resolver.GetMetadata<AnnotatedModel, string>(x => x.Email);

        metadata.Label.ShouldBe("Work email");
        metadata.Required.ShouldBe(true);
        metadata.MaxLength.ShouldBe(120);
        metadata.Placeholder.ShouldBe("you@example.com");
    }

    [Fact]
    public void Profile_can_set_every_facet()
    {
        IPropertyMetadataResolver resolver = Build(m =>
            m.Model<FixtureCommand>(p =>
            {
                p.Property(x => x.FirstName)
                    .Label("Given name")
                    .Description("As on your passport")
                    .Placeholder("Ada")
                    .Required()
                    .MinLength(2)
                    .MaxLength(50)
                    .DataType(DataType.Text)
                    .Control<Badge>();
                p.Property(x => x.Quantity).Range(1, 10).Format("N0");
            })
        );

        PropertyMetadata name = resolver.GetMetadata<FixtureCommand, string>(x => x.FirstName);
        PropertyMetadata quantity = resolver.GetMetadata<FixtureCommand, int>(x => x.Quantity);

        name.ShouldBe(
            new PropertyMetadata
            {
                Label = "Given name",
                Description = "As on your passport",
                Placeholder = "Ada",
                Required = true,
                MinLength = 2,
                MaxLength = 50,
                DataType = DataType.Text,
                Control = typeof(Badge),
            }
        );
        quantity.Minimum.ShouldBe(1);
        quantity.Maximum.ShouldBe(10);
        quantity.Format.ShouldBe("N0");
        quantity.Label.ShouldBe("Quantity");
    }

    [Fact]
    public void Profile_can_mark_a_required_attribute_as_optional()
    {
        IPropertyMetadataResolver resolver = Build(m =>
            m.Model<AnnotatedModel>(p => p.Property(x => x.Email).Required(false))
        );

        resolver.GetMetadata<AnnotatedModel, string>(x => x.Email).Required.ShouldBe(false);
    }

    [Fact]
    public void Layers_apply_in_the_charter_order_profile_then_validator_then_attributes_then_convention()
    {
        var validator = new FakeValidatorProvider
        {
            Answer = new PropertyMetadata
            {
                Label = "From validator",
                Description = "From validator",
                Required = false,
                MaxLength = 100,
            },
        };
        IPropertyMetadataResolver resolver = Build(
            m => m.Model<AnnotatedModel>(p => p.Property(x => x.Email).Label("From profile")),
            validator
        );

        PropertyMetadata metadata = resolver.GetMetadata<AnnotatedModel, string>(x => x.Email);

        metadata.Label.ShouldBe("From profile"); // profile beats validator
        metadata.Description.ShouldBe("From validator"); // validator beats attributes
        metadata.Required.ShouldBe(false); // validator beats [Required]
        metadata.MaxLength.ShouldBe(100); // validator beats [StringLength(120)]
        metadata.MinLength.ShouldBe(5); // only the attributes answer
        metadata.Placeholder.ShouldBe("you@example.com"); // only the attributes answer
    }

    [Fact]
    public void A_provider_from_another_package_slots_in_by_order_not_by_registration_order()
    {
        // Registered last, as an add-on package would be, yet it must still outrank convention.
        var validator = new FakeValidatorProvider();

        PropertyMetadata metadata = Build(null, validator)
            .GetMetadata<FixtureCommand, string>(x => x.FirstName);

        metadata.Label.ShouldBe("From validator");
        metadata.Required.ShouldBe(true);
        metadata.MaxLength.ShouldBe(100);
    }

    [Fact]
    public void Metadata_is_resolved_once_per_property_then_served_from_the_cache()
    {
        var counting = new FakeValidatorProvider();
        IPropertyMetadataResolver resolver = Build(null, counting);
        var command = new FixtureCommand();

        for (int i = 0; i < 25; i++)
        {
            resolver.GetMetadata<FixtureCommand, string>(x => x.FirstName);
            resolver.Bind(() => command.FirstName);
        }
        counting.Calls.ShouldBe(1);

        resolver.GetMetadata<FixtureCommand, int>(x => x.Quantity);
        counting.Calls.ShouldBe(2);
    }

    [Fact]
    public void A_profile_on_a_base_type_applies_to_an_inherited_property_reached_through_the_derived_type()
    {
        IPropertyMetadataResolver resolver = Build(m =>
            m.Model<FixtureBase>(p => p.Property(x => x.Id).Label("Reference"))
        );
        var command = new FixtureCommand();

        resolver.GetMetadata<FixtureCommand, Guid>(x => x.Id).Label.ShouldBe("Reference");
        resolver.Bind(() => command.Id).Metadata.Label.ShouldBe("Reference");
    }

    [Fact]
    public void A_profile_on_the_derived_type_wins_over_one_on_the_base_type_and_keeps_the_rest()
    {
        IPropertyMetadataResolver resolver = Build(m =>
            m.Model<FixtureBase>(p =>
                    p.Property(x => x.Id).Label("Reference").Description("Assigned by the server")
                )
                .Model<FixtureCommand>(p => p.Property(x => x.Id).Label("Command reference"))
        );

        PropertyMetadata derived = resolver.GetMetadata<FixtureCommand, Guid>(x => x.Id);

        derived.Label.ShouldBe("Command reference");
        derived.Description.ShouldBe("Assigned by the server");
        resolver.GetMetadata<FixtureBase, Guid>(x => x.Id).Label.ShouldBe("Reference");
    }

    [Fact]
    public void Bind_resolves_metadata_for_the_instance_and_a_call_site_layer_can_be_laid_on_top()
    {
        IPropertyMetadataResolver resolver = Build(m =>
            m.Model<FixtureCommand>(p =>
                p.Property(x => x.FirstName).Label("Given name").Required()
            )
        );
        var command = new FixtureCommand();

        PropertyMetadata resolved = resolver.Bind(() => command.FirstName).Metadata;
        PropertyMetadata atCallSite = new PropertyMetadata { Label = "Customer name" }.OverlayOn(
            resolved
        );

        resolved.Label.ShouldBe("Given name");
        atCallSite.Label.ShouldBe("Customer name");
        atCallSite.Required.ShouldBe(true);
    }

    [Fact]
    public void AddVibeMetadata_can_be_called_repeatedly_and_keeps_every_profile()
    {
        var services = new ServiceCollection();
        services.AddVibeUI();
        services.AddVibeMetadata(m =>
            m.Model<FixtureCommand>(p => p.Property(x => x.FirstName).Label("One"))
        );
        services.AddVibeMetadata(m =>
            m.Model<FixtureCommand>(p => p.Property(x => x.Quantity).Label("Two"))
        );
        ServiceProvider provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IPropertyMetadataResolver>();

        resolver.GetMetadata<FixtureCommand, string>(x => x.FirstName).Label.ShouldBe("One");
        resolver.GetMetadata<FixtureCommand, int>(x => x.Quantity).Label.ShouldBe("Two");
        provider.GetServices<IPropertyMetadataProvider>().Count().ShouldBe(3);
    }

    [Fact]
    public void AddVibeUI_registers_the_resolver()
    {
        var services = new ServiceCollection();
        services.AddVibeUI();

        services.BuildServiceProvider().GetService<IPropertyMetadataResolver>().ShouldNotBeNull();
    }
}
