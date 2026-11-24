// <copyright file="SubmissionTransferRequestTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Livestock.Sdk.Submissions.Tests.Models;

using Defra.Livestock.Sdk.Submissions.Models;
using Defra.Livestock.Sdk.Submissions.Services;

/// <summary>Verifies the public transfer contract and its separation from persistence.</summary>
public sealed class SubmissionTransferRequestTests
{
    /// <summary>Copies submission details and all animal data into storage-independent POCOs.</summary>
    [Fact]
    public void ShouldMapFullSubmissionWithoutSharingDocumentState()
    {
        var document = new SubmissionDocument
        {
            Id = "internal-only",
            HashId = "ABC-DEF-GHJ",
            Cph = "12/345/6789",
            CreatedDate = new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Unspecified),
            Status = SubmissionStatus.Draft,
            Version = 7,
            Animals =
            [
                new SubmissionAnimalDocument
                {
                    AnimalId = "UK123",
                    Status = ItemStatus.Complete,
                    Data = [new SubmissionDataItemDocument { Key = "weight", Value = "42", Type = PropertyTypes.Int }],
                },
                new SubmissionAnimalDocument
                {
                    AnimalId = "UK456",
                    Status = ItemStatus.Draft,
                    Data = [new SubmissionDataItemDocument { Key = "note", Value = null, Type = PropertyTypes.String }],
                },
            ],
        };

        var request = document.ToSubmissionTransferRequest();

        request.SubmissionId.ShouldBe(document.HashId);
        request.Cph.ShouldBe(document.Cph);
        request.CreatedDate.ShouldBe(new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero));
        request.Animals.Count.ShouldBe(2);
        var animal = request.Animals.First();
        animal.AnimalId.ShouldBe("UK123");
        animal.Status.ShouldBe(ItemStatus.Complete);
        var item = animal.Data.ShouldHaveSingleItem();
        item.Key.ShouldBe("weight");
        item.Value.ShouldBe("42");
        item.Type.ShouldBe(PropertyTypes.Int);
        request.Animals.Last().Data.ShouldHaveSingleItem().Value.ShouldBeNull();

        animal.Data[0] = new DataItem { Key = "replacement", Value = "changed", Type = PropertyTypes.String };

        document.Animals[0].Data[0].Key.ShouldBe("weight");
        document.Animals[0].Data[0].Value.ShouldBe("42");
    }

    /// <summary>Exports no Mongo document or repository types to SDK consumers.</summary>
    [Fact]
    public void ShouldKeepPersistenceTypesOutOfPublicApi()
    {
        var exported = typeof(SubmissionTransferRequest).Assembly.GetExportedTypes();

        exported.ShouldNotContain(type => type.Name.EndsWith("Document", StringComparison.Ordinal));
        exported.ShouldNotContain(type => type.Namespace == "Defra.Livestock.Sdk.Submissions.Storage");
        typeof(ISubmissionTransfer).GetMethod(nameof(ISubmissionTransfer.TransferAsync))!
            .GetParameters()[0].ParameterType.ShouldBe(typeof(SubmissionTransferRequest));
        typeof(SubmissionTransferRequest).GetProperties().Select(property => property.Name)
            .ShouldBe(["SubmissionId", "Cph", "CreatedDate", "Animals"], ignoreOrder: true);
        foreach (var type in exported.Where(type => type.Namespace == typeof(SubmissionTransferRequest).Namespace))
        {
            type.GetCustomAttributesData().ShouldNotContain(attribute =>
                attribute.AttributeType.Namespace != null && attribute.AttributeType.Namespace.StartsWith("MongoDB", StringComparison.Ordinal));
        }
    }
}
