using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Vibe.UI.FluentValidation;
using Vibe.UI.Metadata;

namespace Vibe.UI.Tests.FluentValidationAdapter;

public class FluentValidationMetadataProviderTests
{
    // Scope validation is on: resolving a scoped validator from the root provider would throw,
    // which is exactly what a singleton metadata provider must not do.
    private static IPropertyMetadataResolver Build(
        Action<IServiceCollection>? register = null,
        Action<VibeMetadataOptions>? profiles = null
    )
    {
        var services = new ServiceCollection();
        services.AddVibeMetadata(profiles);
        services.AddVibeFluentValidation();
        if (register is null)
            services.AddScoped<IValidator<Order>, OrderValidator>();
        else
            register(services);
        return services
            .BuildServiceProvider(
                new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = false }
            )
            .GetRequiredService<IPropertyMetadataResolver>();
    }

    [Fact]
    public void Reads_required_and_maximum_length()
    {
        PropertyMetadata name = Build().GetMetadata<Order, string>(x => x.Name);

        name.Required.ShouldBe(true);
        name.MaxLength.ShouldBe(100);
        name.MinLength.ShouldBeNull();
        name.Label.ShouldBe("Name");
    }

    [Fact]
    public void Reads_both_ends_of_a_length_rule()
    {
        PropertyMetadata code = Build().GetMetadata<Order, string>(x => x.Code);

        code.MinLength.ShouldBe(3);
        code.MaxLength.ShouldBe(10);
        code.Required.ShouldBeNull();
    }

    [Fact]
    public void Reads_the_email_data_type()
    {
        PropertyMetadata email = Build().GetMetadata<Order, string>(x => x.Email);

        email.DataType.ShouldBe(DataType.EmailAddress);
        email.Required.ShouldBe(true);
    }

    [Fact]
    public void Reads_an_inclusive_range()
    {
        PropertyMetadata quantity = Build().GetMetadata<Order, int>(x => x.Quantity);

        quantity.Minimum.ShouldBe(1);
        quantity.Maximum.ShouldBe(10);
    }

    [Fact]
    public void Reads_inclusive_comparisons_as_bounds_and_ignores_an_exclusive_one()
    {
        IPropertyMetadataResolver resolver = Build();

        PropertyMetadata price = resolver.GetMetadata<Order, decimal>(x => x.Price);
        PropertyMetadata discount = resolver.GetMetadata<Order, decimal>(x => x.Discount);

        price.Minimum.ShouldBe(0m);
        price.Maximum.ShouldBe(999m);
        // GreaterThan(0) has no smallest allowed value, so it must not be shown as a minimum of 0.
        discount.Minimum.ShouldBeNull();
    }

    [Fact]
    public void NotNull_on_a_non_nullable_value_type_is_not_required_but_on_a_nullable_one_it_is()
    {
        IPropertyMetadataResolver resolver = Build();

        resolver.GetMetadata<Order, bool>(x => x.IsActive).Required.ShouldBeNull();
        resolver.GetMetadata<Order, bool?>(x => x.Confirmed).Required.ShouldBe(true);
    }

    [Fact]
    public void NotNull_on_a_reference_type_is_required()
    {
        Build().GetMetadata<Order, string?>(x => x.Reference).Required.ShouldBe(true);
    }

    [Fact]
    public void NotEmpty_on_an_inherited_value_type_property_is_required()
    {
        Build().GetMetadata<Order, Guid>(x => x.Id).Required.ShouldBe(true);
    }

    [Fact]
    public void A_rule_under_When_is_not_read()
    {
        Build().GetMetadata<Order, string>(x => x.Nickname).Required.ShouldBeNull();
    }

    [Fact]
    public void A_condition_on_one_validator_leaves_the_rest_of_the_rule_readable()
    {
        PropertyMetadata phone = Build().GetMetadata<Order, string>(x => x.Phone);

        phone.Required.ShouldBeNull();
        phone.MaxLength.ShouldBe(20);
    }

    [Fact]
    public void Rules_inside_a_When_block_or_under_Unless_are_not_read()
    {
        IPropertyMetadataResolver resolver = Build();

        resolver.GetMetadata<Order, string>(x => x.InBlock).Required.ShouldBeNull();
        resolver.GetMetadata<Order, string>(x => x.UnlessActive).Required.ShouldBeNull();
    }

    [Fact]
    public void An_exclusive_range_is_not_shown_as_a_minimum_and_maximum()
    {
        // ExclusiveBetween(0, 100) allows neither 0 nor 100, so neither is a limit an input can show.
        PropertyMetadata percent = Build().GetMetadata<Order, int>(x => x.Percent);

        percent.Minimum.ShouldBeNull();
        percent.Maximum.ShouldBeNull();
    }

    [Fact]
    public void A_rule_in_a_named_rule_set_is_not_read()
    {
        Build().GetMetadata<Order, string>(x => x.Notes).Required.ShouldBeNull();
    }

    [Fact]
    public void An_explicit_WithName_becomes_the_label()
    {
        Build().GetMetadata<Order, string>(x => x.Title).Label.ShouldBe("Job title");
    }

    [Fact]
    public void The_name_FluentValidation_invents_is_not_used_so_labels_keep_one_style()
    {
        // FluentValidation would call this "Is Active". The convention layer says "Is active".
        Build().GetMetadata<Order, bool>(x => x.IsActive).Label.ShouldBe("Is active");
    }

    [Fact]
    public void Known_limit_a_WithName_that_only_respaces_the_property_name_is_lost()
    {
        // Documented in the provider: "User name" on Username cannot be told apart from an invented name.
        Build().GetMetadata<Order, string>(x => x.Username).Label.ShouldBe("Username");
    }

    [Fact]
    public void A_property_with_no_rules_falls_through_to_lower_layers()
    {
        PropertyMetadata unvalidated = Build().GetMetadata<Order, string>(x => x.Unvalidated);

        unvalidated.Label.ShouldBe("Unvalidated");
        unvalidated.Required.ShouldBeNull();
    }

    [Fact]
    public void Validator_sits_above_DataAnnotations_and_below_a_profile()
    {
        IPropertyMetadataResolver resolver = Build(profiles: m =>
            m.Model<Order>(p => p.Property(x => x.Title).Label("From the profile"))
        );

        PropertyMetadata summary = resolver.GetMetadata<Order, string>(x => x.Summary);
        PropertyMetadata title = resolver.GetMetadata<Order, string>(x => x.Title);

        summary.MaxLength.ShouldBe(200); // validator beats [StringLength(500)]
        summary.Label.ShouldBe("From the attribute"); // validator set no name, the attribute did
        title.Label.ShouldBe("From the profile"); // profile beats WithName("Job title")
        title.Required.ShouldBe(true); // and the validator still supplies required
    }

    [Fact]
    public void No_registered_validator_means_no_metadata_and_no_error()
    {
        IPropertyMetadataResolver resolver = Build(register: _ => { });

        PropertyMetadata name = resolver.GetMetadata<Order, string>(x => x.Name);

        name.Label.ShouldBe("Name");
        name.Required.ShouldBeNull();
    }

    [Fact]
    public void A_validator_registered_for_a_base_type_describes_a_derived_model()
    {
        IPropertyMetadataResolver resolver = Build(register: s =>
            s.AddScoped<IValidator<OrderBase>, OrderBaseValidator>()
        );

        resolver.GetMetadata<Order, Guid>(x => x.Id).Required.ShouldBe(true);
    }

    [Fact]
    public void The_validator_is_constructed_once_per_model_type_not_once_per_property()
    {
        IPropertyMetadataResolver resolver = Build(
            register: s => s.AddScoped<IValidator<Order>, CountingOrderValidator>()
        );
        int before = CountingOrderValidator.Constructed;

        resolver.GetMetadata<Order, string>(x => x.Name);
        resolver.GetMetadata<Order, string>(x => x.Code);
        resolver.GetMetadata<Order, string>(x => x.Email);
        resolver.GetMetadata<Order, int>(x => x.Quantity);

        (CountingOrderValidator.Constructed - before).ShouldBe(1);
    }

    [Fact]
    public void A_validator_the_container_cannot_construct_fails_loudly()
    {
        IPropertyMetadataResolver resolver = Build(register: s =>
            s.AddScoped<IValidator<Order>, UnconstructibleValidator>()
        );

        Should.Throw<InvalidOperationException>(() =>
            resolver.GetMetadata<Order, string>(x => x.Name)
        );
    }

    [Fact]
    public void AddVibeFluentValidation_is_idempotent_and_registers_one_provider()
    {
        var services = new ServiceCollection();
        services.AddVibeUI();
        services.AddVibeFluentValidation();
        services.AddVibeFluentValidation();

        IEnumerable<IPropertyMetadataProvider> providers = services
            .BuildServiceProvider()
            .GetServices<IPropertyMetadataProvider>();

        providers.OfType<FluentValidationMetadataProvider>().Count().ShouldBe(1);
        providers.Count().ShouldBe(4);
        providers
            .OfType<FluentValidationMetadataProvider>()
            .Single()
            .Order.ShouldBe(PropertyMetadataOrder.Validator);
    }
}
