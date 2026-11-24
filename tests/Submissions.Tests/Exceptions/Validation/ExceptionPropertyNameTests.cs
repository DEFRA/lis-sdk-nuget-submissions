// <copyright file="ExceptionPropertyNameTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Exceptions;

using Defra.Livestock.Sdk.Submissions.Exceptions;

/// <summary>
/// Verifies that exceptions preserve names taken from properties on a caller's class.
/// </summary>
public sealed class ExceptionPropertyNameTests
{
    /// <summary>
    /// Uses the caller's AnimalIdentifier property name in <see cref="AnimalIdRequiredException"/>.
    /// </summary>
    [Fact]
    public void AnimalIdRequiredShouldUseSourcePropertyName()
    {
        var request = new TestRequest();

        var exception = Should.Throw<AnimalIdRequiredException>(() =>
            throw new AnimalIdRequiredException(nameof(request.AnimalIdentifier)));

        exception.ParamName.ShouldBe("AnimalIdentifier");
        exception.Message.ShouldContain("AnimalIdentifier");
    }

    /// <summary>
    /// Uses the caller's AnimalData property name in <see cref="AnimalDataRequiredException"/>.
    /// </summary>
    [Fact]
    public void AnimalDataRequiredShouldUseSourcePropertyName()
    {
        var request = new TestRequest();

        var exception = Should.Throw<AnimalDataRequiredException>(() =>
            throw new AnimalDataRequiredException(nameof(request.AnimalData)));

        exception.ParamName.ShouldBe("AnimalData");
        exception.Message.ShouldContain("AnimalData");
    }

    /// <summary>
    /// Uses the caller's DataItemKey property name in <see cref="DataItemKeyRequiredException"/>.
    /// </summary>
    [Fact]
    public void DataItemKeyRequiredShouldUseSourcePropertyName()
    {
        var request = new TestRequest();

        var exception = Should.Throw<DataItemKeyRequiredException>(() =>
            throw new DataItemKeyRequiredException(nameof(request.DataItemKey)));

        exception.ParamName.ShouldBe("DataItemKey");
        exception.Message.ShouldContain("DataItemKey");
    }

    /// <summary>
    /// Uses the caller's AnimalData property name in <see cref="DuplicateDataItemException"/>.
    /// </summary>
    [Fact]
    public void DuplicateDataItemShouldUseSourcePropertyName()
    {
        var request = new TestRequest();

        var exception = Should.Throw<DuplicateDataItemException>(() =>
            throw new DuplicateDataItemException(request.DataItemKey, nameof(request.AnimalData)));

        exception.ParamName.ShouldBe("AnimalData");
        exception.Message.ShouldContain("AnimalData");
    }

    /// <summary>
    /// Uses the caller's HoldingNumber property name in <see cref="InvalidCphException"/>.
    /// </summary>
    [Fact]
    public void InvalidCphShouldUseSourcePropertyName()
    {
        var request = new TestRequest();

        var exception = Should.Throw<InvalidCphException>(() =>
            throw new InvalidCphException("Invalid holding number.", nameof(request.HoldingNumber)));

        exception.ParamName.ShouldBe("HoldingNumber");
        exception.Message.ShouldContain("HoldingNumber");
    }

    /// <summary>
    /// Uses the caller's AnimalData property name in <see cref="NullDataItemException"/>.
    /// </summary>
    [Fact]
    public void NullDataItemShouldUseSourcePropertyName()
    {
        var request = new TestRequest();

        var exception = Should.Throw<NullDataItemException>(() =>
            throw new NullDataItemException(nameof(request.AnimalData)));

        exception.ParamName.ShouldBe("AnimalData");
        exception.Message.ShouldContain("AnimalData");
    }

    /// <summary>
    /// Uses the caller's DataItemType property name in <see cref="UnsupportedDataItemTypeException"/>.
    /// </summary>
    [Fact]
    public void UnsupportedDataItemTypeShouldUseSourcePropertyName()
    {
        var request = new TestRequest();

        var exception = Should.Throw<UnsupportedDataItemTypeException>(() =>
            throw new UnsupportedDataItemTypeException(request.DataItemKey, request.DataItemType, nameof(request.DataItemType)));

        exception.ParamName.ShouldBe("DataItemType");
        exception.Message.ShouldContain("DataItemType");
    }

    /// <summary>
    /// Uses the caller's AnimalStatus property name in <see cref="UnsupportedItemStatusException"/>.
    /// </summary>
    [Fact]
    public void UnsupportedItemStatusShouldUseSourcePropertyName()
    {
        var request = new TestRequest();

        var exception = Should.Throw<UnsupportedItemStatusException>(() =>
            throw new UnsupportedItemStatusException(nameof(request.AnimalStatus)));

        exception.ParamName.ShouldBe("AnimalStatus");
        exception.Message.ShouldContain("AnimalStatus");
    }

    private sealed class TestRequest
    {
        public string AnimalIdentifier { get; init; } = string.Empty;

        public string[] AnimalData { get; init; } = [];

        public string DataItemKey { get; init; } = "weight";

        public string DataItemType { get; init; } = "INVALID";

        public string HoldingNumber { get; init; } = string.Empty;

        public string AnimalStatus { get; init; } = "INVALID";
    }
}
