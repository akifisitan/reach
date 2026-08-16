using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Reach.Options;

public sealed class FluentValidationOptions<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T
>(IValidator<T> validator) : IValidateOptions<T>
    where T : class
{
    public ValidateOptionsResult Validate(string? name, T options)
    {
        var result = validator.Validate(options);

        return result.IsValid
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                result.Errors.Select(x => $"{x.PropertyName}: {x.ErrorMessage}")
            );
    }
}

public interface IOptionsValidationTarget
{
    OptionsValidationFailure? Validate();
}

public sealed record OptionsValidationFailure(
    Type OptionsType,
    string? OptionsName,
    IReadOnlyList<string> Failures
);

public sealed class AggregateOptionsValidationException(
    IReadOnlyList<OptionsValidationFailure> failures
) : Exception(CreateMessage(failures))
{
    public IReadOnlyList<OptionsValidationFailure> Failures { get; } = failures;

    private static string CreateMessage(IReadOnlyList<OptionsValidationFailure> failures)
    {
        var messages = failures.SelectMany(failure =>
        {
            var name = string.IsNullOrEmpty(failure.OptionsName)
                ? failure.OptionsType.Name
                : $"{failure.OptionsType.Name} ({failure.OptionsName})";

            return failure.Failures.Select(message => $"{name}: {message}");
        });

        return $"Configuration validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, messages)}";
    }
}

public sealed class OptionsValidationTarget<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T
>(IOptions<T> options) : IOptionsValidationTarget
    where T : class
{
    public OptionsValidationFailure? Validate()
    {
        try
        {
            _ = options.Value;

            return null;
        }
        catch (OptionsValidationException ex)
        {
            return new OptionsValidationFailure(
                ex.OptionsType,
                ex.OptionsName,
                ex.Failures.ToList()
            );
        }
    }
}

public static class OptionsValidationServiceCollectionExtensions
{
    public static OptionsBuilder<T> WithValidator<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            T,
        TValidator
    >(this OptionsBuilder<T> builder)
        where T : class
        where TValidator : class, IValidator<T>, new()
    {
        var services = builder.Services;
        services.AddSingleton<IValidator<T>>(new TValidator());
        services.AddSingleton<IValidateOptions<T>, FluentValidationOptions<T>>();
        services.AddSingleton<IOptionsValidationTarget, OptionsValidationTarget<T>>();

        return builder;
    }
}
