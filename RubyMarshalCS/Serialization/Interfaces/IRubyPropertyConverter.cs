namespace RubyMarshalCS.Serialization.Interfaces;

/// <summary>Converts a single property between its Ruby value and its .NET representation.</summary>
public interface IRubyPropertyConverter
{
    /// <summary>Ruby value (after type-level conversion) to the value assigned to the property.</summary>
    object? FromRuby(object? value, object owner);

    /// <summary>Property value to the value that is written to Ruby.</summary>
    object? ToRuby(object? value, object owner);
}
