using Catrashop.Modules.Catalog.Application.Abstractions;
using Catrashop.Modules.Catalog.Application.Configuration;
using Catrashop.Modules.Catalog.Application.Services;
using Catrashop.Modules.Catalog.Domain;
using Catrashop.Modules.Catalog.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace Catrashop.Modules.Catalog.Tests;

public class ProductImageRulesTests
{
    private static Product NewProduct() =>
        Product.Create("Blue shirt", "A shirt", Guid.NewGuid(), null, DateTimeOffset.UtcNow);

    [Fact]
    public void The_first_attached_image_becomes_primary_and_later_ones_do_not()
    {
        var product = NewProduct();

        var first = product.AttachImage("/a.jpg", null).Value;
        var second = product.AttachImage("/b.jpg", "Back").Value;

        Assert.True(first.IsPrimary);
        Assert.False(second.IsPrimary);
        Assert.True(second.SortOrder > first.SortOrder);
    }

    [Fact]
    public void A_product_cannot_hold_more_than_the_maximum_number_of_images()
    {
        var product = NewProduct();
        for (var i = 0; i < Product.MaxImages; i++)
            Assert.True(product.AttachImage($"/{i}.jpg", null).IsSuccess);

        var result = product.AttachImage("/extra.jpg", null);

        Assert.True(result.IsFailure);
        Assert.Equal(CatalogErrors.TooManyImages, result.Error);
    }

    [Fact]
    public void Removing_the_primary_image_promotes_the_next_one()
    {
        var product = NewProduct();
        var first = product.AttachImage("/a.jpg", null).Value;
        var second = product.AttachImage("/b.jpg", null).Value;

        Assert.True(product.RemoveImage(first.Id).IsSuccess);

        Assert.Single(product.Images);
        Assert.True(second.IsPrimary);
    }

    [Fact]
    public void Removing_an_unknown_image_fails()
    {
        var result = NewProduct().RemoveImage(Guid.NewGuid());

        Assert.Equal(CatalogErrors.ImageNotFound, result.Error);
    }

    [Fact]
    public void Setting_the_primary_image_demotes_the_others()
    {
        var product = NewProduct();
        var first = product.AttachImage("/a.jpg", null).Value;
        var second = product.AttachImage("/b.jpg", null).Value;

        Assert.True(product.SetPrimaryImage(second.Id).IsSuccess);

        Assert.False(first.IsPrimary);
        Assert.True(second.IsPrimary);
        Assert.Equal(CatalogErrors.ImageNotFound, product.SetPrimaryImage(Guid.NewGuid()).Error);
    }
}

public class ProductVariantRulesTests
{
    private static (Product Product, ProductVariant Variant) ActiveProduct()
    {
        var product = Product.Create("Mug", "A mug", Guid.NewGuid(), null, DateTimeOffset.UtcNow);
        var variant = product.AddVariant("MUG-1", "White", 100m, "HNL").Value;
        Assert.True(product.Publish().IsSuccess);
        return (product, variant);
    }

    [Fact]
    public void A_variant_can_be_renamed_and_repriced_without_touching_sku_or_currency()
    {
        var (product, variant) = ActiveProduct();

        var result = product.UpdateVariant(variant.Id, "  Black ", 120m, isActive: true);

        Assert.True(result.IsSuccess);
        Assert.Equal("Black", variant.Name);
        Assert.Equal(120m, variant.Price);
        Assert.Equal("MUG-1", variant.Sku);
        Assert.Equal("HNL", variant.Currency);
    }

    [Fact]
    public void The_last_active_variant_of_a_published_product_cannot_be_deactivated()
    {
        var (product, variant) = ActiveProduct();

        var result = product.UpdateVariant(variant.Id, variant.Name, variant.Price, isActive: false);

        Assert.Equal(CatalogErrors.CannotDeactivateLastVariant, result.Error);
        Assert.True(variant.IsActive);
    }

    [Fact]
    public void A_variant_can_be_deactivated_when_another_one_stays_active()
    {
        var (product, variant) = ActiveProduct();
        product.AddVariant("MUG-2", "Black", 100m, "HNL");

        Assert.True(product.UpdateVariant(variant.Id, variant.Name, variant.Price, isActive: false).IsSuccess);
        Assert.False(variant.IsActive);
    }

    [Fact]
    public void Updating_an_unknown_variant_fails()
    {
        var (product, _) = ActiveProduct();

        Assert.Equal(CatalogErrors.VariantNotFound, product.UpdateVariant(Guid.NewGuid(), "x", 1m, true).Error);
    }

    [Fact]
    public void An_archived_product_can_be_published_again()
    {
        var (product, _) = ActiveProduct();
        product.Archive();

        Assert.True(product.Publish().IsSuccess);
        Assert.Equal(ProductStatus.Active, product.Status);
    }
}

public class ImageFileTests
{
    [Fact]
    public void Jpeg_png_and_webp_are_recognised_from_their_first_bytes()
    {
        Assert.Equal(".jpg", ImageFile.Sniff([0xFF, 0xD8, 0xFF, 0xE0])!.Extension);
        Assert.Equal(".png", ImageFile.Sniff([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0])!.Extension);
        Assert.Equal("image/webp", ImageFile.Sniff("RIFF\0\0\0\0WEBPVP8 "u8)!.ContentType);
    }

    [Theory]
    [InlineData("%PDF-1.7")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg'/>")]
    [InlineData("GIF89a")]
    public void Anything_else_is_refused(string text)
    {
        Assert.Null(ImageFile.Sniff(System.Text.Encoding.ASCII.GetBytes(text)));
    }

    [Fact]
    public void An_empty_file_is_refused()
    {
        Assert.Null(ImageFile.Sniff([]));
    }
}

public class FileSystemProductImageStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "catrashop-img-" + Guid.NewGuid().ToString("N"));

    private FileSystemProductImageStore Store() =>
        new(Options.Create(new CatalogOptions { ImagesPath = _root }));

    [Fact]
    public async Task A_saved_image_can_be_opened_and_deleted()
    {
        var store = Store();

        var key = await store.SaveAsync([0xFF, 0xD8, 0xFF, 1, 2, 3], ".jpg");
        var opened = await store.OpenAsync(key);
        Assert.NotNull(opened);
        Assert.Equal("image/jpeg", opened!.ContentType);
        await opened.Content.DisposeAsync(); // release the file before deleting it

        await store.DeleteAsync(key);
        Assert.Null(await store.OpenAsync(key));
    }

    [Theory]
    [InlineData("../secret.jpg")]
    [InlineData("..\\secret.jpg")]
    [InlineData("sub/dir.jpg")]
    [InlineData("")]
    public async Task Keys_that_try_to_leave_the_folder_are_refused(string key)
    {
        Assert.Null(await Store().OpenAsync(key));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
